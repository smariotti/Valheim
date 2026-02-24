using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Splatform;

namespace ArrowCamMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ArrowCamPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.arrowcam";
        public const string PluginName = "ArrowCam";
        public const string PluginVersion = "0.1.2";

        public static ArrowCamPlugin Instance { get; private set; }

        public static ConfigEntry<int> PipWidth;
        public static ConfigEntry<int> PipHeight;
        public static ConfigEntry<int> PipMarginRight;
        public static ConfigEntry<int> PipMarginTop;
        public static ConfigEntry<float> CameraFOV;
        public static ConfigEntry<float> LingerDuration;
        public static ConfigEntry<float> FadeDuration;

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        void Awake()
        {
            Instance = this;

            PipWidth = Config.Bind("UI", "PipWidth", 320, "Width of the kill-cam inset in pixels.");
            PipHeight = Config.Bind("UI", "PipHeight", 180, "Height of the kill-cam inset in pixels.");
            PipMarginRight = Config.Bind("UI", "PipMarginRight", 10, "Gap from the right edge of the screen.");
            PipMarginTop = Config.Bind("UI", "PipMarginTop", 240, "Gap from the top of the screen (positions it below minimap).");
            CameraFOV = Config.Bind("Camera", "FOV", 60f, "Field of view for the projectile camera.");
            LingerDuration = Config.Bind("Camera", "LingerSeconds", 1.5f, "Seconds the PiP stays frozen at impact.");
            FadeDuration = Config.Bind("Camera", "FadeSeconds", 0.5f, "Seconds the PiP takes to fade out after the linger.");

            _harmony.PatchAll();
            Logger.LogInfo($"{PluginName} loaded.");
        }

        void OnDestroy() => _harmony.UnpatchSelf();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ArrowCamSession
    //
    //  Lives entirely on its OWN persistent GameObject — completely independent
    //  of the projectile's lifetime. Tracks the projectile transform by reference
    //  while in flight, then freezes in place on impact, lingers, fades, and
    //  self-destructs. No re-parenting, no OnDestroy side-effects from the
    //  projectile GO.
    //
    //  Key design decisions that fix the reported bugs:
    //
    //  1. ARROW FADE BUG — Root cause: DestroySelf() was called on a component
    //     whose gameObject was the anchor, triggering OnDestroy again (re-entrant
    //     cleanup). Fix: _destroyed guard + Cleanup() is the single exit point.
    //     Also: we disable the Canvas (SetActive false) before Destroy so it
    //     immediately vanishes regardless of alpha interpolation timing.
    //
    //  2. SPEAR WINDOW-STAYS + NEXT CAMERA BROKEN — Root cause: spear GOs are
    //     pooled / deactivated rather than destroyed, so the component survived
    //     re-activation on the *next* throw. The pip-camera (depth -2) kept
    //     rendering into its RT, shadowing the new session's camera. Fix: the
    //     session lives on its own GO and has no relationship to the projectile
    //     GO at all after Init(). On impact we immediately *disable* the pip-camera
    //     (RT retains the last frame — the frozen image stays visible) so it
    //     cannot conflict with any subsequent session's camera.
    // ════════════════════════════════════════════════════════════════════════
    public class ArrowCamSession : MonoBehaviour
    {
        // ── State machine ────────────────────────────────────────────────────
        private enum Phase { Riding, Lingering, FadingOut, Dead }
        private Phase _phase = Phase.Riding;
        private float _timer;
        private bool _cleaned; // re-entrancy guard

        // ── Projectile tracking ──────────────────────────────────────────────
        // Stored as Transform only — no Projectile component ref — so it goes
        // null naturally when the projectile GO is destroyed (or stays valid if
        // the GO is merely deactivated, like a retrieved spear).
        private Transform _projectile;

        // ── Camera ───────────────────────────────────────────────────────────
        private Camera _cam;
        private RenderTexture _rt;

        // Position / orientation offsets in projectile local space.
        // Camera sits slightly behind the tip and above the shaft centreline,
        // angled slightly downward so the shaft appears at the bottom of frame.
        private static readonly Vector3 LocalOffset = new Vector3(0f, 0.06f, -0.35f);
        private static readonly Vector3 LocalLookDelta = new Vector3(0f, -0.08f, 1f);

        // ── UI ───────────────────────────────────────────────────────────────
        private GameObject _uiRoot;   // Canvas root — SetActive(false) hides instantly
        private Image _border;
        private RawImage _rawImage;

        // ═══════════════════════════════════════════════════════════════════
        //  Public API
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Called once immediately after AddComponent.</summary>
        public void Init(Transform projectileTf)
        {
            _projectile = projectileTf;
            CreateRT();
            CreateCamera();
            CreateUI();
        }

        /// <summary>Called by the OnHit Harmony patch when the projectile lands.</summary>
        public void FreezeAndLinger()
        {
            if (_phase != Phase.Riding) return;

            // Leave the camera ENABLED so the scene continues to render and
            // animate during the linger period — the camera just stops moving
            // because FollowProjectile() is only called in the Riding phase.
            // The camera is disabled when FadingOut begins (see LateUpdate)
            // to prevent depth-order conflicts with any subsequent session.
            _projectile = null;
            _timer = 0f;
            _phase = Phase.Lingering;
        }

        /// <summary>Used by the OnHit patch to find the session tracking a given transform.</summary>
        public bool IsTracking(Transform t) => _phase == Phase.Riding && _projectile == t;

        // ═══════════════════════════════════════════════════════════════════
        //  Unity messages
        // ═══════════════════════════════════════════════════════════════════

        void LateUpdate()
        {
            switch (_phase)
            {
                // ── RIDING ───────────────────────────────────────────────────
                case Phase.Riding:
                    if (_projectile == null)
                    {
                        // Projectile was destroyed without an OnHit (fell into void,
                        // timeout despawn, etc.) — treat like an impact.
                        FreezeAndLinger();
                        return;
                    }
                    FollowProjectile();
                    break;

                // ── LINGERING ────────────────────────────────────────────────
                case Phase.Lingering:
                    _timer += Time.deltaTime;
                    if (_timer >= ArrowCamPlugin.LingerDuration.Value)
                    {
                        // Now disable the camera — the RT holds the last rendered
                        // frame for the fade-out, and no further rendering prevents
                        // depth conflicts with any new session that fires meanwhile.
                        if (_cam != null)
                            _cam.enabled = false;

                        _timer = 0f;
                        _phase = Phase.FadingOut;
                    }
                    break;

                // ── FADING OUT ───────────────────────────────────────────────
                case Phase.FadingOut:
                    _timer += Time.deltaTime;
                    float t = Mathf.Clamp01(_timer / Mathf.Max(0.001f, ArrowCamPlugin.FadeDuration.Value));
                    ApplyAlpha(1f - t);
                    if (t >= 1f)
                        Cleanup();
                    break;

                case Phase.Dead:
                    break;
            }
        }

        void OnDestroy()
        {
            // Safety net — covers scene unloads and any other external destruction.
            Cleanup();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Private helpers
        // ═══════════════════════════════════════════════════════════════════

        void FollowProjectile()
        {
            if (_cam == null) return;
            _cam.transform.position = _projectile.TransformPoint(LocalOffset);
            _cam.transform.rotation = Quaternion.LookRotation(
                _projectile.TransformDirection(LocalLookDelta.normalized),
                _projectile.up);
        }

        void ApplyAlpha(float a)
        {
            if (_border != null)
            {
                var c = _border.color;
                c.a = 0.75f * a;
                _border.color = c;
            }
            if (_rawImage != null)
            {
                var c = _rawImage.color;
                c.a = a;
                _rawImage.color = c;
            }
        }

        /// <summary>
        /// Single cleanup exit point. Guards against re-entry (OnDestroy being
        /// called after we already called Destroy(gameObject) from here).
        /// ── FIX #1 ──
        /// </summary>
        void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            _phase = Phase.Dead;

            // 1. Hide the UI canvas immediately — this makes the PiP vanish on
            //    screen regardless of where the alpha interpolation is right now.
            if (_uiRoot != null)
            {
                _uiRoot.SetActive(false);
                Destroy(_uiRoot);
                _uiRoot = null;
            }

            // 2. Detach the RT from the camera before releasing it — prevents
            //    Unity from attempting one final render into a released texture.
            if (_cam != null)
            {
                _cam.targetTexture = null;
                Destroy(_cam.gameObject);
                _cam = null;
            }

            // 3. Release the render texture.
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }

            // 4. Destroy our own host GO.  Because _cleaned is already true,
            //    the OnDestroy call that follows will be a no-op.
            Destroy(gameObject);
        }

        void CreateRT()
        {
            _rt = new RenderTexture(
                ArrowCamPlugin.PipWidth.Value,
                ArrowCamPlugin.PipHeight.Value,
                16, RenderTextureFormat.ARGB32)
            {
                name = "ArrowCam_RT"
            };
            _rt.Create();
        }

        void CreateCamera()
        {
            var camGO = new GameObject("ArrowCam_Camera");
            DontDestroyOnLoad(camGO);

            _cam = camGO.AddComponent<Camera>();
            _cam.fieldOfView = ArrowCamPlugin.CameraFOV.Value;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 500f;
            _cam.targetTexture = _rt;
            _cam.depth = -2;   // renders before the main camera
            _cam.cullingMask = ~0;
            _cam.clearFlags = CameraClearFlags.Skybox;
            _cam.enabled = true;
        }

        void CreateUI()
        {
            _uiRoot = new GameObject("ArrowCam_UI");
            DontDestroyOnLoad(_uiRoot);

            var canvas = _uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _uiRoot.AddComponent<GraphicRaycaster>();

            // Outer border / background panel
            var panel = new GameObject("ArrowCam_Panel");
            panel.transform.SetParent(canvas.transform, false);

            var pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(1f, 1f);
            pr.pivot = new Vector2(1f, 1f);
            pr.sizeDelta = new Vector2(
                ArrowCamPlugin.PipWidth.Value + 4,
                ArrowCamPlugin.PipHeight.Value + 4);
            pr.anchoredPosition = new Vector2(
                -ArrowCamPlugin.PipMarginRight.Value,
                -ArrowCamPlugin.PipMarginTop.Value);

            _border = panel.AddComponent<Image>();
            _border.color = new Color(0f, 0f, 0f, 0.75f);

            // Inner RawImage
            var imgGO = new GameObject("ArrowCam_Image");
            imgGO.transform.SetParent(panel.transform, false);

            var ir = imgGO.AddComponent<RectTransform>();
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(2, 2);
            ir.offsetMax = new Vector2(-2, -2);

            _rawImage = imgGO.AddComponent<RawImage>();
            _rawImage.texture = _rt;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Harmony patches
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fired when any projectile is set up. We create a self-contained
    /// ArrowCamSession on its own GO for the local player's projectiles.
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    static class Patch_Projectile_Setup
    {
        static void Postfix(Projectile __instance,
                            Character owner,
                            Vector3 velocity,
                            float hitNoise,
                            HitData hitData,
                            ItemDrop.ItemData item,
                            ItemDrop.ItemData ammo)
        {
            // Local player only
            if (!(owner is Player p) || p != Player.m_localPlayer) return;

            // Need at least one item ref to exclude creature projectiles
            if (item == null && ammo == null) return;

            var sessionGO = new GameObject("ArrowCam_Session");
            Object.DontDestroyOnLoad(sessionGO);
            sessionGO.AddComponent<ArrowCamSession>().Init(__instance.transform);
        }
    }

    /// <summary>
    /// Fired when a projectile hits something. Finds the session tracking
    /// this specific transform and tells it to freeze.
    /// </summary>
    [HarmonyPatch(typeof(Projectile), "OnHit")]
    static class Patch_Projectile_OnHit
    {
        static void Prefix(Projectile __instance)
        {
            // Sessions live on their own GOs so we can't GetComponent on the
            // projectile. FindObjectsOfType is acceptable here because it fires
            // at most once per projectile-land event (not every frame).
            foreach (var session in Object.FindObjectsOfType<ArrowCamSession>())
            {
                if (session.IsTracking(__instance.transform))
                {
                    session.FreezeAndLinger();
                    break;
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Chat commands
    //
    //  Type any of these into the in-game chat box (Enter key) and they will
    //  be intercepted before being sent as chat messages. The response is
    //  printed back into the chat window via Chat.instance.AddString so it
    //  is visible immediately.
    //
    //  Usage:
    //    /arrowcam                — print all current values
    //    /arrowcam width 400
    //    /arrowcam height 225
    //    /arrowcam marginright 15
    //    /arrowcam margintop 260
    //    /arrowcam fov 75
    //    /arrowcam linger 2.5
    //    /arrowcam fade 0.3
    // ════════════════════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(Chat), nameof(Chat.InputText))]
    static class Patch_Chat_InputText
    {
        // Return false to swallow the input (prevent it from being sent as chat).
        static bool Prefix(Chat __instance)
        {
            // Grab whatever is in the chat input field right now
            string raw = __instance.m_terminalInstance.m_input.text.Trim();

            if (!raw.StartsWith("/arrowcam", System.StringComparison.OrdinalIgnoreCase))
                return true; // not our command — let Valheim handle it normally

            // Tokenise: "/arrowcam width 400"  →  ["width", "400"]
            string[] parts = raw.Split(new[] { ' ', '\t' },
                System.StringSplitOptions.RemoveEmptyEntries);
            // parts[0] == "/arrowcam"

            string sub = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
            string arg = parts.Length > 2 ? parts[2] : "";

            string reply = HandleCommand(sub, arg);
            PrintToChat(__instance, reply);

            // Clear the input field so the text doesn't linger
            __instance.m_terminalInstance.m_input.text = "";
            return false; // swallow — don't send as chat
        }

        // ── Command dispatcher ───────────────────────────────────────────────
        static string HandleCommand(string sub, string arg)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var floatStyle = System.Globalization.NumberStyles.Float;

            switch (sub)
            {
                case "":
                case "help":
                    return "ArrowCam settings:\n" +
                           $"  width        = {ArrowCamPlugin.PipWidth.Value}\n" +
                           $"  height       = {ArrowCamPlugin.PipHeight.Value}\n" +
                           $"  marginright  = {ArrowCamPlugin.PipMarginRight.Value}\n" +
                           $"  margintop    = {ArrowCamPlugin.PipMarginTop.Value}\n" +
                           $"  fov          = {ArrowCamPlugin.CameraFOV.Value}\n" +
                           $"  linger       = {ArrowCamPlugin.LingerDuration.Value}\n" +
                           $"  fade         = {ArrowCamPlugin.FadeDuration.Value}\n" +
                           "Usage: /arrowcam <setting> <value>";

                case "width":
                    if (!int.TryParse(arg, out int w))
                        return "Usage: /arrowcam width <int>";
                    ArrowCamPlugin.PipWidth.Value = w;
                    return $"ArrowCam: width = {w}";

                case "height":
                    if (!int.TryParse(arg, out int h))
                        return "Usage: /arrowcam height <int>";
                    ArrowCamPlugin.PipHeight.Value = h;
                    return $"ArrowCam: height = {h}";

                case "marginright":
                    if (!int.TryParse(arg, out int mr))
                        return "Usage: /arrowcam marginright <int>";
                    ArrowCamPlugin.PipMarginRight.Value = mr;
                    return $"ArrowCam: marginright = {mr}";

                case "margintop":
                    if (!int.TryParse(arg, out int mt))
                        return "Usage: /arrowcam margintop <int>";
                    ArrowCamPlugin.PipMarginTop.Value = mt;
                    return $"ArrowCam: margintop = {mt}";

                case "fov":
                    if (!float.TryParse(arg, floatStyle, inv, out float fov))
                        return "Usage: /arrowcam fov <float>";
                    ArrowCamPlugin.CameraFOV.Value = fov;
                    return $"ArrowCam: fov = {fov}";

                case "linger":
                    if (!float.TryParse(arg, floatStyle, inv, out float li))
                        return "Usage: /arrowcam linger <float>";
                    ArrowCamPlugin.LingerDuration.Value = li;
                    return $"ArrowCam: linger = {li}";

                case "fade":
                    if (!float.TryParse(arg, floatStyle, inv, out float fa))
                        return "Usage: /arrowcam fade <float>";
                    ArrowCamPlugin.FadeDuration.Value = fa;
                    return $"ArrowCam: fade = {fa}";

                default:
                    return $"ArrowCam: unknown setting '{sub}'. Type /arrowcam for help.";
            }
        }

        // ── Print a line into the local chat window (never sent to server) ───
        static void PrintToChat(Chat chat, string message)
        {
            // Talker.Type.Normal puts it in white; we use a local-only overload
            // so it never goes to the network.
            Chat.instance.AddString(message);
        }
    }
}