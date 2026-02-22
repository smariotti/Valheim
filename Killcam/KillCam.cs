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
        public const string PluginVersion = "1.0.0";

        public static KillCamPlugin Instance { get; private set; }

        // ── Config ──────────────────────────────────────────────────────────
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
            LingerDuration = Config.Bind("Camera", "LingerSeconds", 1.5f, "Seconds the PiP stays frozen at the impact location after the projectile lands.");
            FadeDuration = Config.Bind("Camera", "FadeSeconds", 0.5f, "Seconds the PiP takes to fade out after the linger period.");

            _harmony.PatchAll();
            Logger.LogInfo($"{PluginName} loaded.");
        }

        void OnDestroy() => _harmony.UnpatchSelf();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Component that lives on the projectile GameObject while it is in flight,
    //  then detaches from the projectile on impact and manages its own linger
    //  + fade-out lifecycle independently.
    // ════════════════════════════════════════════════════════════════════════
    public class ProjectileKillCam : MonoBehaviour
    {
        // ── State machine ────────────────────────────────────────────────────
        private enum State { Riding, Lingering, FadingOut }
        private State _state = State.Riding;
        private float _timer;   // counts up while Lingering / FadingOut

        // ── Camera / render texture ──────────────────────────────────────────
        private Camera _pipCamera;
        private RenderTexture _rt;

        // When we enter Lingering state we detach the camera from the projectile
        // and freeze it at the impact position/rotation.
        private bool _cameraStopped;

        // ── UI references ────────────────────────────────────────────────────
        private GameObject _uiRoot;
        private Image _borderPanel;   // we fade this too
        private RawImage _rawImage;

        // Camera offset so the projectile model appears at the bottom of frame.
        private static readonly Vector3 CamLocalOffset = new Vector3(0f, 0.06f, -0.35f);
        private static readonly Vector3 CamLocalLookDelta = new Vector3(0f, -0.08f, 1f);

        // ── Lifecycle ────────────────────────────────────────────────────────
        void Awake()
        {
            CreateRenderTexture();
            CreatePipCamera();
            CreateUI();
        }

        void LateUpdate()
        {
            switch (_state)
            {
                case State.Riding:
                    TrackProjectile();
                    break;

                case State.Lingering:
                    // Camera is frozen; just count down the linger period.
                    _timer += Time.deltaTime;
                    if (_timer >= KillCamPlugin.LingerDuration.Value)
                    {
                        _timer = 0f;
                        _state = State.FadingOut;
                    }
                    break;

                case State.FadingOut:
                    _timer += Time.deltaTime;
                    float t = Mathf.Clamp01(_timer / Mathf.Max(0.001f, KillCamPlugin.FadeDuration.Value));
                    SetUIAlpha(1f - t);
                    if (t >= 1f)
                        DestroySelf();
                    break;
            }
        }

        // Called by the OnHit patch — freeze the camera and start the linger.
        public void OnProjectileHit()
        {
            if (_state != State.Riding) return;   // already handled

            // Detach the pip-camera from the (about-to-be-destroyed) projectile
            // by recording world-space pose and never updating it again.
            _cameraStopped = true;
            _timer = 0f;
            _state = State.Lingering;

            // Detach this component from the projectile so we survive its destruction.
            // We re-parent to a persistent stand-alone GO.
            var anchor = new GameObject("KillCam_Anchor");
            DontDestroyOnLoad(anchor);
            transform.SetParent(anchor.transform, true);

            // Stop the camera from updating — it already holds the last world pose.
        }

        void OnDestroy()
        {
            // If the projectile is destroyed without OnHit (e.g. despawn timeout)
            // and we haven't started lingering yet, kick off the linger sequence
            // but we have no parent GO to reparent to — just freeze in place.
            if (_state == State.Riding)
            {
                _state = State.Lingering;
                _timer = 0f;
                // Keep running via a persistent runner
                var runner = new GameObject("KillCam_Runner");
                DontDestroyOnLoad(runner);
                runner.AddComponent<KillCamRunner>().Init(this);
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────
        void TrackProjectile()
        {
            if (_pipCamera == null || _cameraStopped) return;
            _pipCamera.transform.position = transform.TransformPoint(CamLocalOffset);
            _pipCamera.transform.rotation = Quaternion.LookRotation(
                transform.TransformDirection(CamLocalLookDelta.normalized),
                transform.up);
        }

        void SetUIAlpha(float alpha)
        {
            if (_borderPanel != null)
            {
                var c = _borderPanel.color;
                c.a = 0.75f * alpha;
                _borderPanel.color = c;
            }
            if (_rawImage != null)
            {
                var c = _rawImage.color;
                c.a = alpha;
                _rawImage.color = c;
            }
        }

        void DestroySelf()
        {
            if (_uiRoot != null) Destroy(_uiRoot);
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_pipCamera != null) Destroy(_pipCamera.gameObject);
            // Destroy our own anchor GO if we reparented to one
            if (transform.parent != null &&
                transform.parent.name == "KillCam_Anchor")
                Destroy(transform.parent.gameObject);
            Destroy(gameObject);
        }

        void CreateRenderTexture()
        {
            _rt = new RenderTexture(
                KillCamPlugin.PipWidth.Value,
                KillCamPlugin.PipHeight.Value,
                16, RenderTextureFormat.ARGB32);
            _rt.name = "KillCam_RT";
            _rt.Create();
        }

        void CreatePipCamera()
        {
            var camGO = new GameObject("KillCam_Camera");
            camGO.transform.SetParent(null);
            _pipCamera = camGO.AddComponent<Camera>();
            _pipCamera.fieldOfView = KillCamPlugin.CameraFOV.Value;
            _pipCamera.nearClipPlane = 0.05f;
            _pipCamera.farClipPlane = 500f;
            _pipCamera.targetTexture = _rt;
            _pipCamera.depth = -2;
            _pipCamera.cullingMask = ~0;
            _pipCamera.clearFlags = CameraClearFlags.Skybox;
            _pipCamera.enabled = true;
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

            // Border panel
            var panel = new GameObject("KillCam_Panel");
            panel.transform.SetParent(canvas.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            int w = KillCamPlugin.PipWidth.Value;
            int h = KillCamPlugin.PipHeight.Value;
            int mr = KillCamPlugin.PipMarginRight.Value;
            int mt = KillCamPlugin.PipMarginTop.Value;

            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(w + 4, h + 4);
            panelRect.anchoredPosition = new Vector2(-mr, -mt);

            _borderPanel = panel.AddComponent<Image>();
            _borderPanel.color = new Color(0f, 0f, 0f, 0.75f);

            // RawImage
            var imgGO = new GameObject("KillCam_Image");
            imgGO.transform.SetParent(panel.transform, false);

            var imgRect = imgGO.AddComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = new Vector2(2, 2);
            imgRect.offsetMax = new Vector2(-2, -2);

            _rawImage = imgGO.AddComponent<RawImage>();
            _rawImage.texture = _rt;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Thin runner component — only used when the projectile GO is destroyed
    //  before OnHit fires (e.g. timeout despawn). It keeps calling LateUpdate
    //  on the orphaned ProjectileKillCam which has been reparented onto this GO.
    // ════════════════════════════════════════════════════════════════════════
    public class KillCamRunner : MonoBehaviour
    {
        private ProjectileKillCam _killCam;

        public void Init(ProjectileKillCam kc)
        {
            _killCam = kc;
            kc.transform.SetParent(transform, true);
        }

        // ProjectileKillCam.LateUpdate() will continue running because it is
        // still an active MonoBehaviour on this persistent GO.
        // When it calls DestroySelf() it destroys its own GO; we self-destruct
        // here in the next frame once there are no more children.
        void Update()
        {
            if (transform.childCount == 0)
                Destroy(gameObject);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Harmony patches — attach / detach kill-cam when projectile is fired
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Patch Projectile.Setup (called when a projectile is spawned/fired).
    /// Valheim's Projectile class is in the base assembly.
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
            // Only activate for the LOCAL player's projectiles
            if (owner == null) return;
            if (!(owner is Player p) || p != Player.m_localPlayer) return;

            // Only arrows & spears (check item type)
            if (item == null && ammo == null) return;

            var itemToCheck = ammo ?? item;
            if (itemToCheck?.m_shared == null) return;

            var cat = itemToCheck.m_shared.m_itemType;
            bool isProjectileItem =
                cat == ItemDrop.ItemData.ItemType.Ammo ||      // arrows / bolts
                cat == ItemDrop.ItemData.ItemType.Torch ||     // thrown torches (edge case)
                                                               // Spears are OneHandedWeapon but the attack spawns projectile
                item?.m_shared?.m_skillType == Skills.SkillType.Spears;

            // Fallback: always attach if the projectile was spawned for the local player
            // (keeps it broad so bows, crossbows, and spears all work)
            __instance.gameObject.AddComponent<ProjectileKillCam>();
        }
    }

    /// <summary>
    /// Patch Projectile.OnHit — remove the kill-cam component (and its UI/camera)
    /// when the projectile lands.
    /// </summary>
    [HarmonyPatch(typeof(Projectile), "OnHit")]
    static class Patch_Projectile_OnHit
    {
        static void Prefix(Projectile __instance)
        {
            var kc = __instance.GetComponent<ProjectileKillCam>();
            if (kc != null)
                kc.OnProjectileHit();   // freeze camera; begin linger + fade sequence
        }
    }

    /// Safety net: if the projectile GameObject is destroyed without an explicit
    /// OnHit call (e.g. it flew off a cliff and despawned), ProjectileKillCam.OnDestroy
    /// kicks off the linger sequence via KillCamRunner so resources are never leaked.
    /// No additional patch is needed for this path.
}