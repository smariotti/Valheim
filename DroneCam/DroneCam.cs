using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using static Terminal;
using static ZNet;

namespace DroneCam
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class DroneCamPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.xdc";
        public const string PluginName = "Xpert's Drone Cam";
        public const string PluginVersion = "0.1.28";

        internal static ManualLogSource Log;

        public static ConfigEntry<float> FlySpeed;
        public static ConfigEntry<float> FlySpeedFast;
        public static ConfigEntry<float> RotationSpeed;
        public static ConfigEntry<float> SmoothTime;
        public static ConfigEntry<float> TeleportDetectionDistance;
        public static ConfigEntry<float> ScrollSensitivity;

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        private void Awake()
        {
            Log = Logger;

            FlySpeed = Config.Bind("Camera", "FlySpeed", 10f, "Normal fly speed (units/sec).");
            FlySpeedFast = Config.Bind("Camera", "FlySpeedFast", 40f, "Fast fly speed (units/sec).");
            RotationSpeed = Config.Bind("Camera", "RotationSpeed", 90f, "Keyboard rotation speed (degrees/sec).");
            SmoothTime = Config.Bind("Camera", "SmoothTime", 0.25f, "Movement smoothing time for free-fly, orbit and security.");
            TeleportDetectionDistance = Config.Bind("Camera", "TeleportDetectionDistance", 50f, "Distance delta in one frame that triggers a drone snap on target teleport.");
            ScrollSensitivity = Config.Bind("Camera", "ScrollSensitivity", 0.5f, "Mouse wheel sensitivity for distance/radius adjustment.");

            RegisterConsoleCommands();
            _harmony.PatchAll();
            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Console: 'dc help'  Chat: '/dc help'");
        }

        private static void RegisterConsoleCommands()
        {
            new ConsoleCommand("dc", "Xpert's Drone Cam control. Type 'dc help' for usage.",
                args => DroneCamCommands.Handle(
                    DroneCamCommands.CollapseQuotedArgs(
                        args.Args.Skip(1).Where(s => !string.IsNullOrEmpty(s)).ToArray())));
        }

        private void OnDestroy() => _harmony.UnpatchSelf();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stream protocol
    // ─────────────────────────────────────────────────────────────────────────
    public enum StreamProtocol { Spout, NDI }

    // ─────────────────────────────────────────────────────────────────────────
    // Target system
    // ─────────────────────────────────────────────────────────────────────────
    public enum TargetType { None, Player, Enemy, Position }

    public class DroneCamTarget
    {
        public TargetType Type;
        public string Name;
        public Transform Transform;
        public Vector3 WorldPosition;

        public bool IsValid =>
            Type == TargetType.Position ||
            (Transform != null &&
             Transform.gameObject.activeInHierarchy &&
             !(Transform.GetComponent<Character>()?.IsDead() ?? false));

        public Vector3 GetPosition()
        {
            if (Type == TargetType.Position) return WorldPosition;
            if (Transform != null) return Transform.position;
            return Vector3.zero;
        }

        public static DroneCamTarget ForEnemy(Character c)
            => new DroneCamTarget { Type = TargetType.Enemy, Name = c.GetHoverName(), Transform = c.transform };

        public static DroneCamTarget ForPosition(Vector3 pos)
            => new DroneCamTarget { Type = TargetType.Position, WorldPosition = pos };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ZInput button name constants
    // ─────────────────────────────────────────────────────────────────────────
    internal static class Btn
    {
        public const string Toggle = "DroneCam_Toggle";
        public const string Fast = "DroneCam_Fast";
        public const string Forward = "DroneCam_Forward";
        public const string Back = "DroneCam_Back";
        public const string StrafeLeft = "DroneCam_StrafeLeft";
        public const string StrafeRight = "DroneCam_StrafeRight";
        public const string Up = "DroneCam_Up";
        public const string Down = "DroneCam_Down";
        public const string YawLeft = "DroneCam_YawLeft";
        public const string YawRight = "DroneCam_YawRight";
        public const string PitchUp = "DroneCam_PitchUp";
        public const string PitchDown = "DroneCam_PitchDown";
        public const string MouseLook = "DroneCam_MouseLook";
        public const string WheelUp = "DroneCam_WheelUp";
        public const string WheelDown = "DroneCam_WheelDown";
        public const string ModHeight = "DroneCam_ModHeight";
        public const string ModSpeed = "DroneCam_ModSpeed";
        public const string NextPlayer = "DroneCam_NextPlayer";
        public const string PrevPlayer = "DroneCam_PrevPlayer";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Register buttons every time ZInput rebuilds its table
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(ZInput), "Reset")]
    public static class ZInput_Reset_Patch
    {
        static void Postfix(ZInput __instance)
        {
            __instance.AddButton(Btn.Toggle, "<Keyboard>/f8");
            __instance.AddButton(Btn.Fast, "<Keyboard>/leftShift");
            __instance.AddButton(Btn.Forward, "<Keyboard>/w");
            __instance.AddButton(Btn.Back, "<Keyboard>/s");
            __instance.AddButton(Btn.StrafeLeft, "<Keyboard>/a");
            __instance.AddButton(Btn.StrafeRight, "<Keyboard>/d");
            __instance.AddButton(Btn.Up, "<Keyboard>/e");
            __instance.AddButton(Btn.Down, "<Keyboard>/q");
            __instance.AddButton(Btn.YawLeft, "<Keyboard>/leftArrow");
            __instance.AddButton(Btn.YawRight, "<Keyboard>/rightArrow");
            __instance.AddButton(Btn.PitchUp, "<Keyboard>/upArrow");
            __instance.AddButton(Btn.PitchDown, "<Keyboard>/downArrow");
            __instance.AddButton(Btn.MouseLook, "<Mouse>/rightButton");
            __instance.AddButton(Btn.WheelUp, "<Mouse>/scroll/up");
            __instance.AddButton(Btn.WheelDown, "<Mouse>/scroll/down");
            __instance.AddButton(Btn.ModHeight, "<Keyboard>/leftAlt");
            __instance.AddButton(Btn.ModSpeed, "<Keyboard>/leftCtrl");
            __instance.AddButton(Btn.NextPlayer, "<Keyboard>/period");
            __instance.AddButton(Btn.PrevPlayer, "<Keyboard>/comma");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Drone cam modes
    // ─────────────────────────────────────────────────────────────────────────
    public enum DroneCamMode
    {
        Disabled,
        FreeSetup,
        Follow,
        Orbit,
        Security,
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core controller
    // ─────────────────────────────────────────────────────────────────────────
    public class DroneCamController : MonoBehaviour
    {
        public static DroneCamController Instance { get; private set; }

        public DroneCamMode Mode { get; private set; } = DroneCamMode.Disabled;

        // ── unified target state ──────────────────────────────────────────────
        private DroneCamTarget _anchor;
        private DroneCamTarget _lookTarget;

        private Vector3 _anchorLastPos = Vector3.zero;
        private Vector3 _anchorLastKnownPos = Vector3.zero;
        private Vector3 _anchorLastRelOffset = Vector3.zero;
        private bool _anchorWaiting = false;
        private Vector3 _anchorLastInfoPos = Vector3.zero;

        // focal offset - ctrl+wheel slides look target up/down
        private float _focalOffsetY = 0f;

        // last resolved look position - used by orbit pos, security pos
        private Vector3 _lastLookPosition = Vector3.zero;

        // last safe ground position for F8 emergency exit
        private Vector3 _lastSafeGroundPosition = Vector3.zero;

        // teleport state - waiting for TeleportTo to complete
        private bool _waitingForTeleport = false;

        // ── follow params ─────────────────────────────────────────────────────
        private float _followDistance = 5f;
        private float _followSmoothTime = 0.1f;
        private Vector3 _followHeightOffset = new Vector3(0, 2f, 0);

        // ── orbit params ──────────────────────────────────────────────────────
        private float _orbitRadius = 10f;
        private float _orbitSpeed = 30f;
        private float _orbitHeight = 4f;
        private float _orbitAngle = 0f;

        // ── security ──────────────────────────────────────────────────────────
        private Vector3 _securityPos;

        // ── free-fly state ────────────────────────────────────────────────────
        private Vector3 _dronePos;
        private Quaternion _droneRot;
        private Vector3 _smoothVelocity = Vector3.zero;
        private Vector3 _smoothVelRef = Vector3.zero;

        // ── sleep state ───────────────────────────────────────────────────────
        private DroneCamMode _modeBeforeSleep = DroneCamMode.Disabled;
        private GameObject _fakeAttachPoint;
        private bool _broadcastRealPosition = false;
        public bool BroadcastRealPosition => _broadcastRealPosition;

        private bool _targetWasSleeping = false;

        // ── hud state ─────────────────────────────────────────────────────────
        private bool _hudHidden = false;
        public bool HudHidden => _hudHidden;

        // ── streaming ─────────────────────────────────────────────────────────
        private Camera _streamCamera;
        private RenderTexture _streamTexture;
        private Component _spoutSender;
        private Component _ndiSender;
        private bool _streamActive = false;
        private int _streamWidth = 1920;
        private int _streamHeight = 1080;
        private StreamProtocol _streamProtocol = StreamProtocol.Spout;

        // ── refs ──────────────────────────────────────────────────────────────
        private GameCamera _gameCamera;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            Instance = this;
            _gameCamera = GetComponent<GameCamera>();
        }

        private void Update()
        {
            if (ZInput.instance == null) return;

            if (ZInput.GetButtonDown(Btn.Toggle))
            {
                if (Mode == DroneCamMode.Disabled) EnterFreeSetup();
                else ExitDroneCam();
                return;
            }

            if (_waitingForTeleport)
            {
//                DroneCamPlugin.Log.LogInfo($"[XDC-DBG] Update: waiting for teleport, IsTeleporting={Player.m_localPlayer?.IsTeleporting()}");
                PollTeleportComplete();
                return;
            }

            switch (Mode)
            {
                case DroneCamMode.FreeSetup: UpdateFreeSetup(); break;
                case DroneCamMode.Follow: UpdateFollow(); break;
                case DroneCamMode.Orbit: UpdateOrbit(); break;
                case DroneCamMode.Security: UpdateSecurity(); break;
            }

            if (Mode != DroneCamMode.Disabled)
            {
                HandleScrollWheel();
                HandlePlayerCycle();
                PollTargetSleepState();
            }

            if (Mode != DroneCamMode.Disabled &&
                Player.m_localPlayer != null &&
                Player.m_localPlayer.m_nview != null &&
                Player.m_localPlayer.m_nview.IsValid())
            {
                Player.m_localPlayer.m_body.position = transform.position;
                Player.m_localPlayer.SetVisible(false);

                if (ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(transform.position))
                {
                    Vector3 ground = FindGroundPosition(transform.position);
                    if (ground != transform.position)
                        _lastSafeGroundPosition = ground;
                }
            }
        }

        private void LateUpdate()
        {
            if (!_streamActive || _streamCamera == null) return;
            if (Mode == DroneCamMode.Disabled) return;

            _streamCamera.transform.position = transform.position;
            _streamCamera.transform.rotation = transform.rotation;

            Camera gameCam = _gameCamera?.GetComponent<Camera>();
            if (gameCam != null)
                _streamCamera.fieldOfView = gameCam.fieldOfView;

            _streamCamera.Render();
        }

        // ── mode entry / exit ─────────────────────────────────────────────────
        public void EnterFreeSetup()
        {
            _dronePos = transform.position;
            _droneRot = transform.rotation;
            _smoothVelocity = Vector3.zero;
            Mode = DroneCamMode.FreeSetup;
            SetGameCameraEnabled(false);
            EnterDroneMode();
            Notify("Free-fly active. WASD/QE move, Shift=fast, RMB/arrows rotate. F8 to exit.");
        }

        public void ExitDroneCam()
        {
            if (_streamActive) TeardownStreamCamera();

            _waitingForTeleport = false;
            Mode = DroneCamMode.Disabled;
            SetGameCameraEnabled(true);

            if (_hudHidden)
            {
                _hudHidden = false;
                Hud.instance?.gameObject.SetActive(true);
            }

            if (Player.m_localPlayer != null)
            {
                Vector3 safePos = _lastSafeGroundPosition != Vector3.zero
                    ? _lastSafeGroundPosition
                    : Player.m_localPlayer.transform.position;

                Player.m_localPlayer.m_body.position = safePos;
                Player.m_localPlayer.m_body.linearVelocity = Vector3.zero;
                Player.m_localPlayer.SetVisible(true);
                ZNet.instance.SetReferencePosition(safePos);
            }

            Notify("Disabled - normal camera restored.");
        }

        public void ToggleHud()
        {
            _hudHidden = !_hudHidden;
            Hud.instance?.gameObject.SetActive(!_hudHidden);
            Notify(_hudHidden ? "HUD hidden." : "HUD visible.");
        }

        // ── finders ───────────────────────────────────────────────────────────────
        private static PlayerInfo FindPlayerInfo(string name)
        {
            foreach (PlayerInfo pi in ZNet.instance.m_players)
                if (string.Equals(pi.m_name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return pi;
            return default;
        }

        private static bool IsValidPlayerInfo(PlayerInfo pi)
            => !string.IsNullOrEmpty(pi.m_name) && !pi.m_characterID.IsNone();

        private static ZDO FindPlayerZdo(PlayerInfo pi)
        {
            if (pi.m_characterID.IsNone()) return null;
            return ZDOMan.instance.GetZDO(pi.m_characterID);
        }

        private Vector3 GetPlayerRealtimePosition(PlayerInfo pi)
        {
            ZDO localZdo = Player.m_localPlayer?.m_nview?.GetZDO();
            ZDO zdo = FindPlayerZdo(pi);

            // Try Transform first - most accurate, updates every frame
            Player p = FindPlayerByZdo(zdo);
            if (p != null && p != Player.m_localPlayer)
                return p.transform.position;

            // Try ZDO - only if not local player's ZDO
            if (zdo != null && zdo != localZdo)
                return zdo.GetPosition();

            // Fall back to periodic PlayerInfo position
            if (pi.m_publicPosition && pi.m_position != Vector3.zero)
                return pi.m_position;

            // Last resort - last position drone was tracking
            if (_anchorLastKnownPos != Vector3.zero)
                return _anchorLastKnownPos;

            return Vector3.zero;
        }

        private Vector3 GetPlayerInfoPosition(PlayerInfo pi)
        {
            ZDO localZdo = Player.m_localPlayer?.m_nview?.GetZDO();

            // Periodic server-broadcast position - reliable at any distance
            if (pi.m_publicPosition && pi.m_position != Vector3.zero)
                return pi.m_position;

            // ZDO fallback - only if not local player's ZDO
            ZDO zdo = FindPlayerZdo(pi);
            if (zdo != null && zdo != localZdo)
                return zdo.GetPosition();

            // Last resort - last position drone was tracking
            if (_anchorLastKnownPos != Vector3.zero)
                return _anchorLastKnownPos;

            return Vector3.zero;
        }

        // ── teleport handling ─────────────────────────────────────────────────
        private void TravelToPlayer(string playerName, Vector3 targetPos)
        {
            if (Player.m_localPlayer == null) return;
            DroneCamPlugin.Log.LogInfo($"[DroneCam] TravelToPlayer: '{playerName}' at {targetPos}");
            bool started = Player.m_localPlayer.TeleportTo(
                targetPos, Player.m_localPlayer.transform.rotation, true);
            if (started)
            {
                _waitingForTeleport = true;
                Notify($"Teleporting to {playerName}...");
            }
            else
            {
                DroneCamPlugin.Log.LogInfo("[DroneCam] TeleportTo on cooldown - snapping directly.");
                Vector3 dest = targetPos + _anchorLastRelOffset;
                SnapDroneTo(dest);
                _anchorLastKnownPos = targetPos;
                _anchorLastPos = targetPos;
                _anchorLastRelOffset = dest - targetPos;
                _anchorWaiting = false;
            }
        }
        private void TravelToPosition(Vector3 targetPos)
        {
            if (Player.m_localPlayer == null) return;
            DroneCamPlugin.Log.LogInfo($"[DroneCam] TravelToPosition: {targetPos}");
            bool started = Player.m_localPlayer.TeleportTo(
                targetPos, Player.m_localPlayer.transform.rotation, true);
            if (started)
            {
                _waitingForTeleport = true;
                Notify("Teleporting...");
            }
            else
            {
                DroneCamPlugin.Log.LogInfo("[DroneCam] TeleportTo on cooldown - snapping directly.");
                SnapDroneTo(targetPos);
                _anchorLastRelOffset = targetPos - _anchorLastKnownPos;
                _anchorWaiting = false;
            }
        }
        private void PollTeleportComplete()
        {
            if (Player.m_localPlayer == null) return;
            if (Player.m_localPlayer.IsTeleporting()) return;

            // Immediately hide before any rendering
            Player.m_localPlayer.SetVisible(false);
            Player.m_localPlayer.m_body.position = transform.position;

            _waitingForTeleport = false;
            DroneCamPlugin.Log.LogInfo("[DroneCam] Teleport complete - resuming tracking.");

            if (_anchor == null) return;

            // Drone landed at the pre-calculated offset position
            // No lerp needed - we're already in the right place
            Vector3 landedPos = Player.m_localPlayer.transform.position;
            transform.position = landedPos;
            _dronePos = landedPos;
            _smoothVelocity = Vector3.zero;
            _smoothVelRef = Vector3.zero;
            Player.m_localPlayer.m_body.position = landedPos;

            PlayerInfo pi = FindPlayerInfo(_anchor.Name);
            Vector3 infoPos = GetPlayerInfoPosition(pi);
            Vector3 realtimePos = GetPlayerRealtimePosition(pi);
            Vector3 pos = realtimePos != Vector3.zero ? realtimePos : infoPos;

            if (pos != Vector3.zero)
            {
                _anchorLastKnownPos = pos;
                _anchorLastPos = pos;
                _anchorLastInfoPos = infoPos != Vector3.zero ? infoPos : pos;
                _anchorLastRelOffset = landedPos - pos;
                _anchorWaiting = false;

                ZDO zdo = FindPlayerZdo(pi);
                Player p = FindPlayerByZdo(zdo);
                if (p != null) _anchor.Transform = p.transform;

                Notify($"Now tracking {_anchor.Name}.");
            }
            else
            {
                _anchorWaiting = false;
                _anchorLastInfoPos = Vector3.zero;
                Notify($"Arrived near {_anchor.Name} - acquiring target...");
            }
        }

        // ── sleep ─────────────────────────────────────────────────────────────
        public void OnPlayerSleep()
        {
            if (Mode == DroneCamMode.Disabled) return;
            if (_modeBeforeSleep != DroneCamMode.Disabled) return;
            _modeBeforeSleep = Mode;
            SetGameCameraEnabled(true);

            if (Player.m_localPlayer != null)
            {
                _broadcastRealPosition = true;

                _fakeAttachPoint = new GameObject("DroneCam_FakeBed");
                _fakeAttachPoint.transform.position = Player.m_localPlayer.transform.position;

                Player.m_localPlayer.AttachStart(
                    _fakeAttachPoint.transform,
                    null, false, true, false,
                    "attach_bed", Vector3.zero, null);

                Player.m_localPlayer.m_sleeping = true;

                ZDO zdo = Player.m_localPlayer.m_nview.GetZDO();
                zdo.Set(ZDOVars.s_inBed, true);
                ZDOMan.instance.FlushClientObjects();
            }

            Notify("Sleeping - drone suspended.");
        }

        public void OnPlayerWake()
        {
            if (_modeBeforeSleep == DroneCamMode.Disabled) return;
            _broadcastRealPosition = false;

            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.AttachStop();
                Player.m_localPlayer.m_sleeping = false;

                if (_fakeAttachPoint != null)
                {
                    Destroy(_fakeAttachPoint);
                    _fakeAttachPoint = null;
                }
            }

            SetGameCameraEnabled(false);
            Mode = _modeBeforeSleep;
            _modeBeforeSleep = DroneCamMode.Disabled;
            Notify("Awake - drone resumed.");
        }

        public void PollTargetSleepState()
        {
            if (_anchor == null || _anchor.Type != TargetType.Player) return;
            PlayerInfo pi = FindPlayerInfo(_anchor.Name);
            ZDO zdo = FindPlayerZdo(pi);
            if (zdo == null) return;

            bool isSleeping = zdo.GetBool(ZDOVars.s_inBed);
            if (isSleeping && !_targetWasSleeping)
            {
                _targetWasSleeping = true;
                OnPlayerSleep();
            }
            else if (!isSleeping && _targetWasSleeping)
            {
                _targetWasSleeping = false;
                OnPlayerWake();
            }
        }

        private void EnterDroneMode()
        {
            if (Player.m_localPlayer == null) return;
            Player.m_localPlayer.m_godMode = true;
            Player.m_localPlayer.m_ghostMode = true;
            Player.m_localPlayer.m_seman?.RemoveStatusEffect(SEMan.s_statusEffectWet, true);
            Player.m_localPlayer.m_seman?.RemoveStatusEffect(SEMan.s_statusEffectTared, true);
        }

        private void SetGameCameraEnabled(bool state)
        {
            if (_gameCamera != null) _gameCamera.enabled = state;
        }

        // ── ground finding ────────────────────────────────────────────────────
        private static Vector3 FindGroundPosition(Vector3 from)
        {
            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 2000f, ZoneSystem.instance.m_solidRayMask))
                return hit.point + Vector3.up * 0.5f;
            if (Physics.Raycast(from + Vector3.up * 500f, Vector3.down, out hit, 2000f, ZoneSystem.instance.m_solidRayMask))
                return hit.point + Vector3.up * 0.5f;
            return from;
        }

        // ── scroll wheel ──────────────────────────────────────────────────────
        private void HandleScrollWheel()
        {
            if (Chat.instance != null && Chat.instance.HasFocus()) return;

            float delta = 0f;
            if (ZInput.GetButton(Btn.WheelUp)) delta -= DroneCamPlugin.ScrollSensitivity.Value;
            if (ZInput.GetButton(Btn.WheelDown)) delta += DroneCamPlugin.ScrollSensitivity.Value;
            if (Mathf.Approximately(delta, 0f)) return;

            bool modHeight = ZInput.GetButton(Btn.ModHeight);
            bool modSpeed = ZInput.GetButton(Btn.ModSpeed);

            // alt+ctrl+wheel - orbit speed
            if (modHeight && modSpeed)
            {
                if (Mode == DroneCamMode.Orbit)
                    _orbitSpeed = Mathf.Max(1f, _orbitSpeed + delta * 10f);
                return;
            }

            // ctrl+wheel - slide focal point up/down
            if (modSpeed)
            {
                _focalOffsetY += delta;
                return;
            }

            switch (Mode)
            {
                case DroneCamMode.Follow:
                    if (modHeight) _followHeightOffset.y = Mathf.Max(0f, _followHeightOffset.y + delta);
                    else _followDistance = Mathf.Max(1f, _followDistance + delta);
                    break;
                case DroneCamMode.FreeSetup:
                    _followDistance = Mathf.Max(1f, _followDistance + delta);
                    break;
                case DroneCamMode.Orbit:
                    if (modHeight) _orbitHeight = Mathf.Max(0f, _orbitHeight + delta);
                    else _orbitRadius = Mathf.Max(1f, _orbitRadius + delta);
                    break;
                case DroneCamMode.Security:
                    if (modHeight) _securityPos.y = Mathf.Max(0f, _securityPos.y + delta);
                    break;
            }
        }

        // ── player cycling ────────────────────────────────────────────────────
        private void HandlePlayerCycle()
        {
            if (Chat.instance != null && Chat.instance.HasFocus()) return;
            if (_anchor == null || _anchor.Type != TargetType.Player) return;

            int dir = 0;
            if (ZInput.GetButtonDown(Btn.NextPlayer)) dir = 1;
            if (ZInput.GetButtonDown(Btn.PrevPlayer)) dir = -1;
            if (dir == 0) return;

            List<string> names = GetPlayerNames().ToList();
            if (names.Count < 1) return;

            int current = names.FindIndex(n =>
                string.Equals(n, _anchor.Name, StringComparison.OrdinalIgnoreCase));

            int next = current < 0 ? 0 : (current + dir + names.Count) % names.Count;
            string nextName = names[next];

            switch (Mode)
            {
                case DroneCamMode.Follow: SetFollow(nextName, _followDistance, _followHeightOffset.y, _followSmoothTime); break;
                case DroneCamMode.Orbit: SetOrbitPlayer(nextName, _orbitRadius, _orbitSpeed, _orbitHeight); break;
                case DroneCamMode.Security: SetSecurityPlayer(nextName); break;
            }
        }

        // ── free-fly ──────────────────────────────────────────────────────────
        private void UpdateFreeSetup()
        {
            if (Chat.instance != null && Chat.instance.HasFocus()) return;

            float speed = ZInput.GetButton(Btn.Fast) ? DroneCamPlugin.FlySpeedFast.Value : DroneCamPlugin.FlySpeed.Value;
            float rotSpd = DroneCamPlugin.RotationSpeed.Value;
            float smooth = DroneCamPlugin.SmoothTime.Value;

            Vector3 wish = Vector3.zero;
            if (ZInput.GetButton(Btn.Forward)) wish += _droneRot * Vector3.forward;
            if (ZInput.GetButton(Btn.Back)) wish -= _droneRot * Vector3.forward;
            if (ZInput.GetButton(Btn.StrafeLeft)) wish -= _droneRot * Vector3.right;
            if (ZInput.GetButton(Btn.StrafeRight)) wish += _droneRot * Vector3.right;
            if (ZInput.GetButton(Btn.Up)) wish += Vector3.up;
            if (ZInput.GetButton(Btn.Down)) wish -= Vector3.up;

            _smoothVelocity = Vector3.SmoothDamp(
                _smoothVelocity, wish.normalized * speed, ref _smoothVelRef, smooth);
            _dronePos += _smoothVelocity * Time.deltaTime;

            if (ZInput.GetButton(Btn.MouseLook))
            {
                Vector2 delta = ZInput.GetMouseDelta();
                Vector3 e = _droneRot.eulerAngles;
                _droneRot = Quaternion.Euler(e.x + -delta.y * rotSpd * Time.deltaTime,
                                                 e.y + delta.x * rotSpd * Time.deltaTime, 0f);
            }
            if (ZInput.GetButton(Btn.YawLeft))
            {
                Vector3 e = _droneRot.eulerAngles;
                _droneRot = Quaternion.Euler(e.x, e.y - rotSpd * Time.deltaTime, 0f);
            }
            if (ZInput.GetButton(Btn.YawRight))
            {
                Vector3 e = _droneRot.eulerAngles;
                _droneRot = Quaternion.Euler(e.x, e.y + rotSpd * Time.deltaTime, 0f);
            }
            if (ZInput.GetButton(Btn.PitchUp))
            {
                Vector3 e = _droneRot.eulerAngles;
                _droneRot = Quaternion.Euler(e.x - rotSpd * Time.deltaTime, e.y, 0f);
            }
            if (ZInput.GetButton(Btn.PitchDown))
            {
                Vector3 e = _droneRot.eulerAngles;
                _droneRot = Quaternion.Euler(e.x + rotSpd * Time.deltaTime, e.y, 0f);
            }

            transform.position = _dronePos;
            transform.rotation = _droneRot;
        }

        // ── follow ────────────────────────────────────────────────────────────
        private void UpdateFollow()
        {
            RefreshTarget();
            if (_anchor == null || _anchorLastKnownPos == Vector3.zero) return;

            Vector3 targetPos = GetTargetCenter();

            Vector3 forward;
            if (_anchor.Transform != null)
            {
                forward = _anchor.Transform.forward;
            }
            else
            {
                // No Transform yet - derive forward from drone-to-target direction
                // so desiredPos is always sensibly behind the target from the camera's perspective
                Vector3 toTarget = targetPos - transform.position;
                forward = toTarget.sqrMagnitude > 0.01f
                    ? toTarget.normalized
                    : Vector3.forward;
            }

            Vector3 desiredPos = targetPos + (-forward * _followDistance) + _followHeightOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref _smoothVelRef, _followSmoothTime);

            LookSmoothAt(GetLookTarget());
        }
        
        // ── orbit ─────────────────────────────────────────────────────────────
        private void UpdateOrbit()
        {
            if (_anchor != null && _anchor.Type != TargetType.Position)
            {
                RefreshTarget();
                if (_anchor == null || _anchorLastKnownPos == Vector3.zero) return;
            }

            Vector3 center = GetTargetCenter();
            _orbitAngle += _orbitSpeed * Time.deltaTime;

            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(rad) * _orbitRadius,
                _orbitHeight,
                Mathf.Cos(rad) * _orbitRadius);

            transform.position = Vector3.SmoothDamp(
                transform.position, center + offset, ref _smoothVelRef, DroneCamPlugin.SmoothTime.Value);

            LookSmoothAt(GetLookTarget());
        }

        // ── security ──────────────────────────────────────────────────────────
        private void UpdateSecurity()
        {
            if (_anchor != null && _anchor.Type != TargetType.Position)
                RefreshTarget();

            transform.position = _securityPos;
            LookSmoothAt(GetLookTarget(), lerpSpeed: 3f);
        }
        private void RefreshTarget()
        {
            if (_anchor == null || _anchor.Type == TargetType.Position) return;

            if (_anchor.Type == TargetType.Player)
            {
                PlayerInfo pi = FindPlayerInfo(_anchor.Name);
                if (!IsValidPlayerInfo(pi))
                {
                    DroneCamPlugin.Log.LogInfo($"[DroneCam] RefreshTarget: invalid PlayerInfo for '{_anchor.Name}'");
                    return;
                }

                Vector3 infoPos = GetPlayerInfoPosition(pi);
                Vector3 realtimePos = GetPlayerRealtimePosition(pi);
                Vector3 actualPos = realtimePos != Vector3.zero ? realtimePos : infoPos;

//                DroneCamPlugin.Log.LogInfo($"[DroneCam] RefreshTarget: infoPos={infoPos} realtimePos={realtimePos} pos={actualPos} lastKnown={_anchorLastKnownPos} lastInfo={_anchorLastInfoPos}");

                if (actualPos == Vector3.zero)
                {
                    DroneCamPlugin.Log.LogInfo("[DroneCam] RefreshTarget: pos is zero - skipping update");
                    return;
                }

                // Use infoPos for the distance check - it reliably updates after portals
                if (infoPos != Vector3.zero)
                {
                    float dist = _anchorLastInfoPos != Vector3.zero
                        ? Vector3.Distance(infoPos, _anchorLastInfoPos)
                        : 0f;

                    //                    DroneCamPlugin.Log.LogInfo($"[DroneCam] RefreshTarget: infoPos={infoPos} realtimePos={realtimePos} dist={dist}");

                    if (dist > DroneCamPlugin.TeleportDetectionDistance.Value)
                    {
                        DroneCamPlugin.Log.LogInfo($"[DroneCam] Portal detected - dist={dist}");

                        Vector3 droneDest = infoPos + _anchorLastRelOffset;

                        if (ZNetScene.instance.IsAreaReady(droneDest))
                        {
                            SnapDroneTo(droneDest);
                            _anchorLastRelOffset = droneDest - infoPos;
                        }
                        else
                        {
                            TravelToPosition(droneDest);
                        }
                        _anchorLastInfoPos = infoPos;
                        _anchorLastKnownPos = infoPos;
                        _anchorLastPos = infoPos;
                        return;
                    }
                    _anchorLastInfoPos = infoPos;
                }

                // Use realtimePos for smooth tracking
                Vector3 pos = realtimePos != Vector3.zero ? realtimePos : infoPos;
                if (pos == Vector3.zero) return;

                // Update Transform for forward direction
                ZDO zdo = FindPlayerZdo(pi);
                Player p = FindPlayerByZdo(zdo);
                if (p != null) _anchor.Transform = p.transform;

                _anchorLastKnownPos = actualPos;
                _anchorLastPos = actualPos;
                _anchorWaiting = false;
                _anchorLastRelOffset = transform.position - actualPos;
                return;
            }

            // Enemy anchor
            if (_anchor.IsValid)
            {
                Vector3 pos = _anchor.GetPosition();
                if (_anchorLastPos != Vector3.zero &&
                    Vector3.Distance(pos, _anchorLastPos) > DroneCamPlugin.TeleportDetectionDistance.Value)
                    SnapRelativeToTarget(pos);
                _anchorLastPos = pos;
                _anchorLastKnownPos = pos;
                _anchorLastRelOffset = transform.position - pos;
                _anchorWaiting = false;
                return;
            }

            if (!_anchorWaiting && _anchorLastKnownPos != Vector3.zero)
            {
                _anchorLastRelOffset = transform.position - _anchorLastKnownPos;
                _anchorWaiting = true;
                _anchorLastPos = Vector3.zero;
                DroneCamPlugin.Log.LogInfo("[DroneCam] Enemy anchor lost - waiting.");
            }

            Character c = FindNearestCharacter(_anchor.Name, transform.position);
            if (c == null) return;

            _anchor.Transform = c.transform;
            _anchorWaiting = false;
            _anchorLastPos = Vector3.zero;
            SnapRelativeToTarget(c.transform.position);
            DroneCamPlugin.Log.LogInfo("[DroneCam] Enemy anchor reacquired.");
        }
        private Vector3 _anchorRefPosCache = Vector3.zero;

        private Vector3 GetTargetCenter()
        {
            if (_anchor == null) return transform.position;

            Vector3 base_;
            if (_anchor.Type == TargetType.Player)
            {
                if (_anchorWaiting && _anchorRefPosCache != Vector3.zero)
                    base_ = _anchorRefPosCache;
                else
                    base_ = _anchorLastKnownPos != Vector3.zero
                        ? _anchorLastKnownPos
                        : transform.position;
            }
            else if (_anchor.IsValid)
                base_ = _anchor.GetPosition(); // ← no longer writes _anchorLastKnownPos
            else
                base_ = _anchorLastKnownPos;

            return base_ + Vector3.up * _focalOffsetY;
        }

        private Vector3 GetLookTarget()
        {
            if (_lookTarget != null)
            {
                if (_lookTarget.Type == TargetType.Enemy && !_lookTarget.IsValid)
                {
                    Character c = FindNearestCharacter(_lookTarget.Name, transform.position);
                    if (c != null && !c.IsDead())
                        _lookTarget.Transform = c.transform;
                    else
                    {
                        Notify("Enemy target died - returning to anchor target.");
                        _lookTarget = null;
                    }
                }

                if (_lookTarget != null)
                {
                    _lastLookPosition = _lookTarget.GetPosition() + Vector3.up * 1.5f;
                    return _lastLookPosition;
                }
            }

            _lastLookPosition = GetTargetCenter() + Vector3.up * 1.5f;
            return _lastLookPosition;
        }

        private void SnapDroneTo(Vector3 pos)
        {
            transform.position = pos;
            _dronePos = pos;
            _smoothVelocity = Vector3.zero;
            _smoothVelRef = Vector3.zero;
            if (Player.m_localPlayer != null)
                Player.m_localPlayer.m_body.position = pos;
            DroneCamPlugin.Log.LogInfo($"[DroneCam] Drone snapped to {pos}");
        }

        private void SnapRelativeToTarget(Vector3 targetPos)
        {
            _anchorLastKnownPos = targetPos;
            _anchorLastPos = targetPos;

            switch (Mode)
            {
                case DroneCamMode.Follow:
                    SnapDroneTo(targetPos + _anchorLastRelOffset);
                    break;
                case DroneCamMode.Orbit:
                    _orbitAngle = 0f;
                    SnapDroneTo(targetPos + _anchorLastRelOffset.normalized * _orbitRadius);
                    break;
            }
        }

        private void LookSmoothAt(Vector3 worldPoint, float lerpSpeed = 5f)
        {
            Vector3 dir = worldPoint - transform.position;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * lerpSpeed);
        }

        private void ResetTargetState()
        {
            _anchor = null;
            _lookTarget = null;
            _anchorLastPos = Vector3.zero;
            _anchorLastKnownPos = Vector3.zero;
            _anchorLastRelOffset = Vector3.zero;
            _anchorLastInfoPos = Vector3.zero;
            _anchorWaiting = false;
            _lastLookPosition = Vector3.zero;
            _targetWasSleeping = false;
            _focalOffsetY = 0f;
            _waitingForTeleport = false;
        }

        // ── finders ───────────────────────────────────────────────────────────
        private static ZNetPeer FindPeerByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string trimmed = name.Trim();
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                if (string.Equals(peer.m_playerName?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                    return peer;
            return null;
        }

        private static ZDO FindPlayerZdoByName(string name)
        {
            ZNetPeer peer = FindPeerByName(name);
            if (peer == null) return null;

            // Primary path - use characterID if valid
            if (!peer.m_characterID.IsNone())
            {
                ZDO zdo = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (zdo != null) return zdo;
            }

            // Fallback - scan all ZDOs for matching player name
            // Used when m_characterID is temporarily None (portal transit, loading)
            foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                string zdoName = zdo.GetString(ZDOVars.s_playerName);
                if (string.IsNullOrEmpty(zdoName)) continue;
                if (string.Equals(zdoName.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return zdo;
                }
            }

            return null;
        }

        private static Player FindPlayerByZdo(ZDO zdo)
        {
            if (zdo == null) return null;
            foreach (Player p in Player.GetAllPlayers())
            {
                if (p == Player.m_localPlayer) continue;
                if (p.m_nview != null && p.m_nview.GetZDO() == zdo)
                    return p;
            }
            return null;
        }

        private static Character FindNearestCharacter(string name, Vector3 near)
        {
            Character nearest = null;
            float bestDist = float.MaxValue;

            foreach (Character c in Character.GetAllCharacters())
            {
                if (c is Player) continue;
                if (c.IsDead()) continue;

                bool nameMatch = string.IsNullOrEmpty(name) ||
                                 string.Equals(c.GetHoverName(), name, StringComparison.OrdinalIgnoreCase);
                if (!nameMatch) continue;

                float dist = Vector3.Distance(c.transform.position, near);
                if (dist < bestDist) { bestDist = dist; nearest = c; }
            }
            return nearest;
        }

        private static string PlayerNotFoundReason(string name)
        {
            foreach (PlayerInfo pi in ZNet.instance.m_players)
                if (string.Equals(pi.m_name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return pi.m_characterID.IsNone()
                        ? $"Player '{name}' character not ready yet."
                        : $"Player '{name}' found but position unavailable.";
            return $"Player '{name}' not connected.";
        }

        public Vector3 GetLookAtPosition()
            => _lastLookPosition != Vector3.zero
                ? _lastLookPosition
                : transform.position + transform.forward * _followDistance;

        private static void Notify(string msg)
            => Chat.instance?.AddString($"[XDC] {msg}");

        // ── public API ────────────────────────────────────────────────────────

        public void SetFollow(string playerName, float distance, float heightOffset, float smoothTime)
        {
            PlayerInfo pi = FindPlayerInfo(playerName);
            if (!IsValidPlayerInfo(pi)) { Notify(PlayerNotFoundReason(playerName)); return; }

            Vector3 infoPos = GetPlayerInfoPosition(pi);
            Vector3 realtimePos = GetPlayerRealtimePosition(pi);
            Vector3 travelPos = infoPos != Vector3.zero ? infoPos : realtimePos;
            Vector3 pos = realtimePos != Vector3.zero ? realtimePos : infoPos;

            if (travelPos == Vector3.zero) { Notify($"Player '{playerName}' position unavailable."); return; }

            ResetTargetState();
            ZDO zdo = FindPlayerZdo(pi);
            Player p = FindPlayerByZdo(zdo);
            _anchor = new DroneCamTarget { Type = TargetType.Player, Name = playerName, Transform = p?.transform };
            _anchorLastKnownPos = pos != Vector3.zero ? pos : travelPos;
            _anchorLastInfoPos = infoPos != Vector3.zero ? infoPos : pos;

            _followDistance = distance;
            _followHeightOffset = new Vector3(0, heightOffset, 0);
            _followSmoothTime = smoothTime;
            Mode = DroneCamMode.Follow;
            EnterDroneMode();
            SetGameCameraEnabled(false);

            if (Vector3.Distance(transform.position, travelPos) > DroneCamPlugin.TeleportDetectionDistance.Value)
                TravelToPlayer(playerName, travelPos);
            else
                Notify($"Following {playerName}");
        }

        public void SetFollowEnemy(string enemyName, float distance, float heightOffset, float smoothTime)
        {
            Character c = FindNearestCharacter(enemyName, transform.position);
            if (c == null) { Notify($"No enemy '{enemyName}' found nearby."); return; }
            ResetTargetState();
            _anchor = DroneCamTarget.ForEnemy(c);
            _followDistance = distance;
            _followHeightOffset = new Vector3(0, heightOffset, 0);
            _followSmoothTime = smoothTime;
            Mode = DroneCamMode.Follow;
            EnterDroneMode();
            SetGameCameraEnabled(false);
            Notify($"Following enemy {c.GetHoverName()}");
        }

        public void SetOrbitPlayer(string playerName, float radius, float speed, float height)
        {
            PlayerInfo pi = FindPlayerInfo(playerName);
            if (!IsValidPlayerInfo(pi)) { Notify(PlayerNotFoundReason(playerName)); return; }

            // Use infoPos for the travel distance check - it's the authoritative
            // server-broadcast position and won't be stale like a cached ZDO
            Vector3 infoPos = GetPlayerInfoPosition(pi);
            Vector3 realtimePos = GetPlayerRealtimePosition(pi);

            // For travel decision, always use infoPos as it reflects true current location
            // For initial camera placement, prefer realtimePos for accuracy
            Vector3 travelPos = infoPos != Vector3.zero ? infoPos : realtimePos;
            Vector3 pos = realtimePos != Vector3.zero ? realtimePos : infoPos;

            if (travelPos == Vector3.zero) { Notify($"Player '{playerName}' position unavailable."); return; }

            ResetTargetState();
            ZDO zdo = FindPlayerZdo(pi);
            Player p = FindPlayerByZdo(zdo);
            _anchor = new DroneCamTarget { Type = TargetType.Player, Name = playerName, Transform = p?.transform };
            _anchorLastKnownPos = pos != Vector3.zero ? pos : travelPos;
            _anchorLastInfoPos = infoPos != Vector3.zero ? infoPos : pos;

            _orbitRadius = radius;
            _orbitSpeed = speed;
            _orbitHeight = height;
            _orbitAngle = 0f;
            Mode = DroneCamMode.Orbit;
            EnterDroneMode();
            SetGameCameraEnabled(false);

            // Use infoPos for the distance check so we never use a stale ZDO position
            // to incorrectly conclude the player is nearby
            if (Vector3.Distance(transform.position, travelPos) > DroneCamPlugin.TeleportDetectionDistance.Value)
                TravelToPlayer(playerName, travelPos);
            else
                Notify($"Orbiting {playerName}");
        }


        public void SetOrbitEnemy(string enemyName, float radius, float speed, float height)
        {
            Character c = FindNearestCharacter(enemyName, transform.position);
            if (c == null) { Notify($"No enemy '{enemyName}' found nearby."); return; }
            ResetTargetState();
            _anchor = DroneCamTarget.ForEnemy(c);
            _orbitRadius = radius;
            _orbitSpeed = speed;
            _orbitHeight = height;
            _orbitAngle = 0f;
            Mode = DroneCamMode.Orbit;
            EnterDroneMode();
            SetGameCameraEnabled(false);
            Notify($"Orbiting enemy {c.GetHoverName()}");
        }

        public void SetOrbitPosition(Vector3 pos, float radius, float speed, float height)
        {
            ResetTargetState();
            _anchor = DroneCamTarget.ForPosition(pos);
            _orbitRadius = radius;
            _orbitSpeed = speed;
            _orbitHeight = height;
            _orbitAngle = 0f;
            Mode = DroneCamMode.Orbit;
            EnterDroneMode();
            SetGameCameraEnabled(false);
            Notify($"Orbiting position - radius={radius} speed={speed} height={height}");
        }

        public void SetSecurityPlayer(string playerName)
        {
            PlayerInfo pi = FindPlayerInfo(playerName);
            if (!IsValidPlayerInfo(pi)) { Notify(PlayerNotFoundReason(playerName)); return; }

            Vector3 infoPos = GetPlayerInfoPosition(pi);
            Vector3 realtimePos = GetPlayerRealtimePosition(pi);
            Vector3 travelPos = infoPos != Vector3.zero ? infoPos : realtimePos;
            Vector3 pos = realtimePos != Vector3.zero ? realtimePos : infoPos;

            if (travelPos == Vector3.zero) { Notify($"Player '{playerName}' position unavailable."); return; }

            ResetTargetState();
            ZDO zdo = FindPlayerZdo(pi);
            Player p = FindPlayerByZdo(zdo);
            _anchor = new DroneCamTarget { Type = TargetType.Player, Name = playerName, Transform = p?.transform };
            _anchorLastKnownPos = pos != Vector3.zero ? pos : travelPos;
            _anchorLastInfoPos = infoPos != Vector3.zero ? infoPos : pos;
            _securityPos = transform.position;

            Mode = DroneCamMode.Security;
            EnterDroneMode();
            SetGameCameraEnabled(false);

            if (Vector3.Distance(transform.position, travelPos) > DroneCamPlugin.TeleportDetectionDistance.Value)
                TravelToPlayer(playerName, travelPos);
            else
                Notify($"Security cam tracking {playerName}");
        }
         
        public void SetSecurityPosition(Vector3 pos)
        {
            ResetTargetState();
            _anchor = DroneCamTarget.ForPosition(pos);
            _securityPos = transform.position;
            Mode = DroneCamMode.Security;
            EnterDroneMode();
            SetGameCameraEnabled(false);
            Notify("Security cam tracking look-at position");
        }

        public void SetOrbitSpeed(float speed)
        {
            _orbitSpeed = speed;
            Notify($"Orbit speed set to {speed} deg/sec");
        }

        public void SetEnemyTargetNearest()
        {
            Character c = FindNearestCharacter(null, transform.position);
            if (c == null) { Notify("No enemies nearby."); return; }
            _lookTarget = DroneCamTarget.ForEnemy(c);
            Notify($"Enemy target: {c.GetHoverName()}");
        }

        public void SetEnemyTargetNamed(string name)
        {
            Character c = FindNearestCharacter(name, transform.position);
            if (c == null) { Notify($"No enemy '{name}' found nearby."); return; }
            _lookTarget = DroneCamTarget.ForEnemy(c);
            Notify($"Enemy target: {c.GetHoverName()}");
        }

        public void ClearEnemyTarget()
        {
            _lookTarget = null;
            Notify("Enemy target cleared.");
        }

        public void SnapToPlayer()
        {
            if (_anchor == null || _anchor.Type != TargetType.Player) { Notify("No player target set."); return; }

            PlayerInfo pi = FindPlayerInfo(_anchor.Name);
            if (!IsValidPlayerInfo(pi)) { Notify(PlayerNotFoundReason(_anchor.Name)); return; }

            Vector3 realtimePos = GetPlayerRealtimePosition(pi);
            Vector3 infoPos = GetPlayerInfoPosition(pi);
            Vector3 pos = realtimePos != Vector3.zero ? realtimePos : infoPos;
            if (pos == Vector3.zero) { Notify($"Player '{_anchor.Name}' position unavailable."); return; }

            if (Vector3.Distance(transform.position, pos) > DroneCamPlugin.TeleportDetectionDistance.Value)
                TravelToPlayer(_anchor.Name, pos);
            else
            {
                SnapDroneTo(pos + _anchorLastRelOffset);
                _anchorLastKnownPos = pos;
                _anchorLastPos = pos;
                _anchorLastInfoPos = infoPos != Vector3.zero ? infoPos : pos;
                Notify($"Snapped to {_anchor.Name}.");
            }
        }

        public bool IsTargetPlayer(Player p)
        {
            if (_anchor == null || _anchor.Type != TargetType.Player) return false;
            if (p?.m_nview == null) return false;
            ZDO playerZdo = p.m_nview.GetZDO();
            if (playerZdo == null) return false;
            PlayerInfo pi = FindPlayerInfo(_anchor.Name);
            if (!IsValidPlayerInfo(pi)) return false;
            return playerZdo.m_uid == pi.m_characterID;
        }

        public static IEnumerable<string> GetPlayerNames()
        {
            foreach (PlayerInfo pi in ZNet.instance.m_players)
            {
                if (string.IsNullOrEmpty(pi.m_name)) continue;
                // Skip local player
//                if (pi.m_name.ToLower() == Player.m_localPlayer.m_name.ToLower()) continue;
                if (pi.m_characterID == Player.m_localPlayer.GetZDOID()) continue;
                yield return pi.m_name.Trim();
            }
        }

        // ── streaming ─────────────────────────────────────────────────────────
        public void StartStream(int width, int height, StreamProtocol protocol)
        {
            if (Mode == DroneCamMode.Disabled) { Notify("Enable drone cam before starting stream."); return; }

            _streamWidth = width;
            _streamHeight = height;

            if (_streamCamera != null) TeardownStreamCamera();

            SetupStreamCamera(protocol);

            bool senderAttached = protocol == StreamProtocol.Spout
                ? _spoutSender != null
                : _ndiSender != null;

            if (!senderAttached)
            {
                Notify($"Stream setup failed - Klak{protocol} not found. See log.");
                return;
            }

            _streamActive = true;
            Notify($"Streaming via {protocol} 'DroneCam' - {width}x{height}");
        }

        public void StopStream()
        {
            TeardownStreamCamera();
            Notify("Stream stopped.");
        }

        public void SetStreamResolution(int width, int height)
        {
            if (!_streamActive) { Notify("No active stream."); return; }
            StartStream(width, height, _streamProtocol);
        }

        private void SetupStreamCamera(StreamProtocol protocol)
        {
            if (_streamCamera != null) return;

            _streamProtocol = protocol;

            GameObject go = new GameObject("DroneCam_StreamCamera");
            DontDestroyOnLoad(go);

            _streamCamera = go.AddComponent<Camera>();
            _streamCamera.enabled = false;

            Camera gameCam = _gameCamera?.GetComponent<Camera>();
            if (gameCam != null)
            {
                _streamCamera.cullingMask = gameCam.cullingMask;
                _streamCamera.nearClipPlane = gameCam.nearClipPlane;
                _streamCamera.farClipPlane = gameCam.farClipPlane;
                _streamCamera.fieldOfView = gameCam.fieldOfView;
                _streamCamera.allowHDR = gameCam.allowHDR;
                _streamCamera.allowMSAA = gameCam.allowMSAA;
                _streamCamera.renderingPath = gameCam.renderingPath;
            }

            _streamTexture = new RenderTexture(_streamWidth, _streamHeight, 24, RenderTextureFormat.ARGB32);
            _streamTexture.Create();
            _streamCamera.targetTexture = _streamTexture;

            if (protocol == StreamProtocol.Spout)
                TryAttachSpoutSender(go);
            else
                TryAttachNdiSender(go);
        }

        private void TryAttachSpoutSender(GameObject go)
        {
            try
            {
                Type senderType = Type.GetType("Klak.Spout.SpoutSender, Klak.Spout.Runtime");
                if (senderType == null)
                {
                    DroneCamPlugin.Log.LogWarning(
                        "[DroneCam] KlakSpout not found - Spout streaming unavailable. " +
                        "Place Klak.Spout.Runtime.dll and KlakSpout.dll in BepInEx/plugins/DroneCam/");
                    return;
                }

                _spoutSender = (Component)go.AddComponent(senderType);

                foreach (var prop in senderType.GetProperties())
                    DroneCamPlugin.Log.LogInfo($"[DroneCam] SpoutSender.{prop.Name} ({prop.PropertyType.Name})");

                senderType.GetProperty("spoutName")?.SetValue(_spoutSender, "DroneCam");

                var captureTypeProp = senderType.GetProperty("captureType");
                if (captureTypeProp != null)
                    captureTypeProp.SetValue(_spoutSender,
                        Enum.Parse(captureTypeProp.PropertyType, "Texture"));

                senderType.GetProperty("sourceTexture")?.SetValue(_spoutSender, _streamTexture);

                DroneCamPlugin.Log.LogInfo("[DroneCam] Spout sender attached.");
            }
            catch (Exception e)
            {
                DroneCamPlugin.Log.LogError($"[DroneCam] Spout sender setup failed: {e}");
            }
        }

        private void TryAttachNdiSender(GameObject go)
        {
            try
            {
                Type senderType = Type.GetType("Klak.Ndi.NdiSender, Klak.Ndi.Runtime");
                if (senderType == null)
                {
                    DroneCamPlugin.Log.LogWarning(
                        "[DroneCam] KlakNDI not found - NDI streaming unavailable. " +
                        "Place Klak.Ndi.Runtime.dll and KlakNDI.dll in BepInEx/plugins/DroneCam/ " +
                        "and install NDI Tools from ndi.video");
                    return;
                }

                _ndiSender = (Component)go.AddComponent(senderType);

                foreach (var prop in senderType.GetProperties())
                    DroneCamPlugin.Log.LogInfo($"[DroneCam] NdiSender.{prop.Name} ({prop.PropertyType.Name})");

                senderType.GetProperty("ndiName")?.SetValue(_ndiSender, "DroneCam");

                var captureTypeProp = senderType.GetProperty("captureType");
                if (captureTypeProp != null)
                    captureTypeProp.SetValue(_ndiSender,
                        Enum.Parse(captureTypeProp.PropertyType, "Texture"));

                senderType.GetProperty("sourceTexture")?.SetValue(_ndiSender, _streamTexture);

                DroneCamPlugin.Log.LogInfo("[DroneCam] NDI sender attached.");
            }
            catch (Exception e)
            {
                DroneCamPlugin.Log.LogError($"[DroneCam] NDI sender setup failed: {e}");
            }
        }

        private void TeardownStreamCamera()
        {
            if (_streamCamera != null) { Destroy(_streamCamera.gameObject); _streamCamera = null; }
            _spoutSender = null;
            _ndiSender = null;
            if (_streamTexture != null) { _streamTexture.Release(); Destroy(_streamTexture); _streamTexture = null; }
            _streamActive = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – attach DroneCamController to GameCamera
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(GameCamera), "Awake")]
    public static class GameCamera_Awake_Patch
    {
        static void Postfix(GameCamera __instance)
        {
            if (__instance.GetComponent<DroneCamController>() == null)
                __instance.gameObject.AddComponent<DroneCamController>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – suppress GameCamera.LateUpdate while drone cam is active
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    public static class GameCamera_LateUpdate_Patch
    {
        static bool Prefix()
            => DroneCamController.Instance == null ||
               DroneCamController.Instance.Mode == DroneCamMode.Disabled;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – block local player input while drone cam is active
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Player), "TakeInput")]
    public static class Player_TakeInput_Patch
    {
        static bool Prefix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return true;
            if (DroneCamController.Instance == null) return true;
            return DroneCamController.Instance.Mode == DroneCamMode.Disabled;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – suppress damage to local player while drone cam is active
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Character), "Damage")]
    public static class Character_Damage_Patch
    {
        static bool Prefix(Character __instance)
        {
            if (__instance != Player.m_localPlayer) return true;
            if (DroneCamController.Instance == null) return true;
            return DroneCamController.Instance.Mode == DroneCamMode.Disabled;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – block wet/tar status effects while drone cam is active
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(SEMan), "AddStatusEffect", typeof(int), typeof(bool), typeof(int), typeof(float))]
    public static class SEMan_AddStatusEffect_Patch
    {
        static bool Prefix(SEMan __instance, int nameHash)
        {
            if (DroneCamController.Instance == null) return true;
            if (DroneCamController.Instance.Mode == DroneCamMode.Disabled) return true;
            if (Player.m_localPlayer == null) return true;
            if (__instance != Player.m_localPlayer.m_seman) return true;
            if (nameHash == SEMan.s_statusEffectWet || nameHash == SEMan.s_statusEffectTared)
                return false;
            return true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – broadcast offset position to other clients while drone is
    // active. During sleep, broadcast real position so server ZDO stays loaded.
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(ZSyncTransform), "GetPosition")]
    public static class ZSyncTransform_GetPosition_Patch
    {
        static bool Prefix(ZSyncTransform __instance, ref Vector3 __result)
        {
            if (DroneCamController.Instance == null) return true;
            if (DroneCamController.Instance.Mode == DroneCamMode.Disabled) return true;
            if (Player.m_localPlayer == null) return true;
            if (__instance.gameObject != Player.m_localPlayer.gameObject) return true;
            if (DroneCamController.Instance.BroadcastRealPosition) return true;

            // Offset XZ by 101 units - outside the 100m difficulty scaling radius.
            // Y offset intentionally omitted - a large Y offset previously caused
            // the server to stop streaming ZDOs to this client. Since we now use
            // ZNet.m_players for position tracking instead of ZDOs, a small XZ
            // offset is sufficient and safe.
            Vector3 realPos = Player.m_localPlayer.m_body.position;
            __result = new Vector3(realPos.x + 101f, realPos.y, realPos.z + 101f);
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – hide crosshair while drone cam is active
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    public static class Hud_UpdateCrosshair_Patch
    {
        static void Postfix(Hud __instance)
        {
            if (DroneCamController.Instance == null) return;
            if (DroneCamController.Instance.Mode == DroneCamMode.Disabled) return;
            if (DroneCamController.Instance.HudHidden) return;
            __instance.m_crosshair.gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – suspend/resume drone when local player sleeps/wakes.
    // Remote target player sleep is handled via ZDO polling in Update.
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Player), "SetSleeping")]
    public static class Player_SetSleeping_Patch
    {
        static void Postfix(Player __instance, bool sleep)
        {
            if (DroneCamController.Instance == null) return;
            if (__instance != Player.m_localPlayer) return;

            if (sleep) DroneCamController.Instance.OnPlayerSleep();
            else DroneCamController.Instance.OnPlayerWake();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – intercept /dc and /dronecam chat commands
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Chat), "InputText")]
    public static class Chat_InputText_Patch
    {
        static bool Prefix(Chat __instance)
        {
            string text = __instance.m_terminalInstance.m_input.text?.Trim();
            if (string.IsNullOrEmpty(text)) return true;

            bool isDroneCamCmd =
                text.StartsWith("/dronecam", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/dc ", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "/dc", StringComparison.OrdinalIgnoreCase);

            if (!isDroneCamCmd) return true;

            DroneCamCommands.HandleChat(text);
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Command parser + dispatcher
    // ─────────────────────────────────────────────────────────────────────────
    public static class DroneCamCommands
    {
        private static DroneCamController Ctrl => DroneCamController.Instance;

        private const string Help =
            "[XDC] Xpert's Drone Cam - console: 'dc (sub)' / chat: '/dc (sub)'\n" +
            "  help - show this help\n" +
            "  on / ff / freefly - enter free-fly\n" +
            "  off - exit drone cam\n" +
            "  hud - toggle HUD\n" +
            "  snap - snap drone to player target\n" +
            "  players - list players\n" +
            "  follow p (name) [dist] [h] [smooth] - follow player\n" +
            "  follow e (name) [dist] [h] [smooth] - follow enemy\n" +
            "  orbit p (name) [r] [spd] [h] - orbit player\n" +
            "  orbit e (name) [r] [spd] [h] - orbit enemy\n" +
            "  orbit pos [r] [spd] [h] - orbit look-at position\n" +
            "  orbit s (deg/sec) - set orbit speed\n" +
            "  security p (name) - security cam on player\n" +
            "  security pos - security cam on look-at position\n" +
            "  te - target nearest enemy for look-at\n" +
            "  te (name) - target named enemy for look-at\n" +
            "  te c - clear enemy look-at target\n" +
            "  stream on [spout|ndi] [w] [h] - start stream (default spout 1920x1080)\n" +
            "  stream off - stop stream\n" +
            "  stream res (w) (h) - change stream resolution\n" +
            "Wheel - dist/radius / Alt+wheel - height / Ctrl+wheel - focal offset / Alt+Ctrl+wheel - orbit speed\n" +
            ". / , - cycle players / F8 - toggle\n" +
            "Names with spaces: use quotes e.g. follow p \"Big Viking\"";

        private delegate void CmdHandler(string[] args);

        private static readonly Dictionary<string, CmdHandler> Commands =
            new Dictionary<string, CmdHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { "help",        args => Msg(Help) },
            { "on",          args => Ctrl.EnterFreeSetup() },
            { "off",         args => Ctrl.ExitDroneCam() },
            { "ff",          args => Ctrl.EnterFreeSetup() },
            { "freefly",     args => Ctrl.EnterFreeSetup() },
            { "hud",         args => Ctrl.ToggleHud() },
            { "snap",        args => Ctrl.SnapToPlayer() },
            { "players",     args => Msg($"[XDC] Players: {string.Join(", ", DroneCamController.GetPlayerNames())}") },
            { "follow",      HandleFollow },      { "f",  HandleFollow },
            { "orbit",       HandleOrbit },       { "o",  HandleOrbit },
            { "security",    HandleSecurity },    { "s",  HandleSecurity },
            { "targetenemy", HandleTargetEnemy }, { "te", HandleTargetEnemy },
            { "stream",      HandleStream },      { "st", HandleStream },
        };

        // Entry point from console
        public static void Handle(string[] args)
        {
            if (Ctrl == null) { Msg("[XDC] Controller not ready."); return; }
            if (args == null || args.Length == 0) { Msg(Help); return; }

            string sub = args[0];
            if (Commands.TryGetValue(sub, out CmdHandler handler))
                handler(args);
            else
                Msg($"[XDC] Unknown command '{sub}'. Type 'dc help'.");
        }

        // Entry point from chat - strips /dc or /dronecam prefix
        public static void HandleChat(string raw)
        {
            if (Ctrl == null) { Msg("[XDC] Controller not ready."); return; }
            string[] t = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Handle(CollapseQuotedArgs(t.Skip(1).ToArray()));
        }

        // Collapse tokens that were split out of a quoted string.
        // e.g. ["follow", "p", "\"The", "Viking\""] -> ["follow", "p", "The Viking"]
        public static string[] CollapseQuotedArgs(string[] args)
        {
            var result = new List<string>();
            int i = 0;
            while (i < args.Length)
            {
                string arg = args[i];
                if (arg.StartsWith("\""))
                {
                    var sb = new StringBuilder();
                    sb.Append(arg.Substring(1)); // strip leading quote
                    bool closed = arg.Length > 1 && arg.EndsWith("\"");
                    while (!closed && i + 1 < args.Length)
                    {
                        i++;
                        arg = args[i];
                        sb.Append(' ');
                        sb.Append(arg);
                        closed = arg.EndsWith("\"");
                    }
                    string val = sb.ToString();
                    if (val.EndsWith("\""))
                        val = val.Substring(0, val.Length - 1);
                    result.Add(val);
                }
                else
                {
                    result.Add(arg);
                }
                i++;
            }
            return result.ToArray();
        }

        // ── sub-command handlers ──────────────────────────────────────────────

        private static void HandleFollow(string[] args)
        {
            if (args.Length < 2) { Msg("Usage: follow p (player) / e (enemy) [dist] [h] [smooth]"); return; }
            switch (args[1].ToLower())
            {
                case "p":
                case "player":
                    if (args.Length < 3) { Msg("Usage: follow p (player) [dist] [h] [smooth]"); return; }
                    Ctrl.SetFollow(args[2], F(args, 3, 5f), F(args, 4, 2f), F(args, 5, 0.1f));
                    break;
                case "e":
                case "enemy":
                    if (args.Length < 3) { Msg("Usage: follow e (enemy) [dist] [h] [smooth]"); return; }
                    Ctrl.SetFollowEnemy(args[2], F(args, 3, 5f), F(args, 4, 2f), F(args, 5, 0.1f));
                    break;
                default:
                    Ctrl.SetFollow(args[1], F(args, 2, 5f), F(args, 3, 2f), F(args, 4, 0.1f));
                    break;
            }
        }

        private static void HandleOrbit(string[] args)
        {
            if (args.Length < 2) { Msg("Usage: orbit p (n) / e (n) / pos / s (val)"); return; }
            switch (args[1].ToLower())
            {
                case "p":
                case "player":
                    if (args.Length < 3) { Msg("Usage: orbit p (n) [r] [spd] [h]"); return; }
                    Ctrl.SetOrbitPlayer(args[2], F(args, 3, 10f), F(args, 4, 30f), F(args, 5, 4f));
                    break;
                case "e":
                case "enemy":
                    if (args.Length < 3) { Msg("Usage: orbit e (n) [r] [spd] [h]"); return; }
                    Ctrl.SetOrbitEnemy(args[2], F(args, 3, 10f), F(args, 4, 30f), F(args, 5, 4f));
                    break;
                case "pos":
                    Ctrl.SetOrbitPosition(Ctrl.GetLookAtPosition(), F(args, 2, 10f), F(args, 3, 30f), F(args, 4, 4f));
                    break;
                case "s":
                case "speed":
                    if (args.Length < 3) { Msg("Usage: orbit s (deg/sec)"); return; }
                    Ctrl.SetOrbitSpeed(F(args, 2, 30f));
                    break;
                default:
                    Msg($"Unknown orbit sub-command '{args[1]}'.");
                    break;
            }
        }

        private static void HandleSecurity(string[] args)
        {
            if (args.Length < 2) { Msg("Usage: security p (n) / pos"); return; }
            switch (args[1].ToLower())
            {
                case "p":
                case "player":
                    if (args.Length < 3) { Msg("Usage: security p (n)"); return; }
                    Ctrl.SetSecurityPlayer(args[2]);
                    break;
                case "pos":
                    Ctrl.SetSecurityPosition(Ctrl.GetLookAtPosition());
                    break;
                default:
                    Msg($"Unknown security sub-command '{args[1]}'.");
                    break;
            }
        }

        private static void HandleTargetEnemy(string[] args)
        {
            if (args.Length < 2) { Ctrl.SetEnemyTargetNearest(); return; }
            switch (args[1].ToLower())
            {
                case "c": case "clear": Ctrl.ClearEnemyTarget(); break;
                case "n": case "nearest": Ctrl.SetEnemyTargetNearest(); break;
                default: Ctrl.SetEnemyTargetNamed(args[1]); break;
            }
        }

        private static void HandleStream(string[] args)
        {
            if (args.Length < 2) { Msg("Usage: stream on [spout|ndi] [w] [h] / off / res (w) (h)"); return; }
            switch (args[1].ToLower())
            {
                case "on":
                    {
                        StreamProtocol protocol = StreamProtocol.Spout;
                        int nextArg = 2;
                        if (args.Length > 2)
                        {
                            if (string.Equals(args[2], "ndi", StringComparison.OrdinalIgnoreCase))
                            { protocol = StreamProtocol.NDI; nextArg = 3; }
                            else if (string.Equals(args[2], "spout", StringComparison.OrdinalIgnoreCase))
                            { protocol = StreamProtocol.Spout; nextArg = 3; }
                        }
                        Ctrl.StartStream((int)F(args, nextArg, 1920f), (int)F(args, nextArg + 1, 1080f), protocol);
                        break;
                    }
                case "off":
                    Ctrl.StopStream();
                    break;
                case "res":
                case "resolution":
                    if (args.Length < 4) { Msg("Usage: stream res (width) (height)"); return; }
                    Ctrl.SetStreamResolution((int)F(args, 2, 1920f), (int)F(args, 3, 1080f));
                    break;
                default:
                    Msg($"Unknown stream sub-command '{args[1]}'.");
                    break;
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static float F(string[] args, int i, float fallback)
            => args.Length > i && float.TryParse(args[i], NumberStyles.Float,
               CultureInfo.InvariantCulture, out float v) ? v : fallback;

        private static void Msg(string s)
        {
            Console.instance?.Print(s);
            Chat.instance?.AddString(s);
            DroneCamPlugin.Log.LogInfo(s);
        }
    }
}
