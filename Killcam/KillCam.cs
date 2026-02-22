using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KillCamMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class KillCamPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.killcam";
        public const string PluginName = "KillCam";
        public const string PluginVersion = "0.1.0";

        public static KillCamPlugin Instance { get; private set; }

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
    //  KillCamSession
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
    public class KillCamSession : MonoBehaviour
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

            // ── FIX #2: disable the pip-camera immediately. ─────────────────
            // The RT keeps its last rendered frame so the UI continues to show
            // the frozen impact view, but no further render passes occur —
            // eliminating depth-order conflicts with any subsequent session.
            if (_cam != null)
                _cam.enabled = false;

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
                    if (_timer >= KillCamPlugin.LingerDuration.Value)
                    {
                        _timer = 0f;
                        _phase = Phase.FadingOut;
                    }
                    break;

                // ── FADING OUT ───────────────────────────────────────────────
                case Phase.FadingOut:
                    _timer += Time.deltaTime;
                    float t = Mathf.Clamp01(_timer / Mathf.Max(0.001f, KillCamPlugin.FadeDuration.Value));
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
                KillCamPlugin.PipWidth.Value,
                KillCamPlugin.PipHeight.Value,
                16, RenderTextureFormat.ARGB32)
            {
                name = "KillCam_RT"
            };
            _rt.Create();
        }

        void CreateCamera()
        {
            var camGO = new GameObject("KillCam_Camera");
            DontDestroyOnLoad(camGO);

            _cam = camGO.AddComponent<Camera>();
            _cam.fieldOfView = KillCamPlugin.CameraFOV.Value;
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
            _uiRoot = new GameObject("KillCam_UI");
            DontDestroyOnLoad(_uiRoot);

            var canvas = _uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _uiRoot.AddComponent<GraphicRaycaster>();

            // Outer border / background panel
            var panel = new GameObject("KillCam_Panel");
            panel.transform.SetParent(canvas.transform, false);

            var pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(1f, 1f);
            pr.pivot = new Vector2(1f, 1f);
            pr.sizeDelta = new Vector2(
                KillCamPlugin.PipWidth.Value + 4,
                KillCamPlugin.PipHeight.Value + 4);
            pr.anchoredPosition = new Vector2(
                -KillCamPlugin.PipMarginRight.Value,
                -KillCamPlugin.PipMarginTop.Value);

            _border = panel.AddComponent<Image>();
            _border.color = new Color(0f, 0f, 0f, 0.75f);

            // Inner RawImage
            var imgGO = new GameObject("KillCam_Image");
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
    /// KillCamSession on its own GO for the local player's projectiles.
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

            var sessionGO = new GameObject("KillCam_Session");
            Object.DontDestroyOnLoad(sessionGO);
            sessionGO.AddComponent<KillCamSession>().Init(__instance.transform);
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
            foreach (var session in Object.FindObjectsOfType<KillCamSession>())
            {
                if (session.IsTracking(__instance.transform))
                {
                    session.FreezeAndLinger();
                    break;
                }
            }
        }
    }
}