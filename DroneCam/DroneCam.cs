using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
 
namespace DroneCam
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class DroneCamPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.dronecam";
        public const string PluginName = "DroneCam";
        public const string PluginVersion = "0.1.18";

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

            _harmony.PatchAll();
            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Type /dc help in chat.");
        }

        private void OnDestroy() => _harmony.UnpatchSelf();
    }

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

        public static DroneCamTarget ForPlayer(Player p)
            => new DroneCamTarget { Type = TargetType.Player, Name = p.GetPlayerName(), Transform = p.transform };

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
    public partial class DroneCamController : MonoBehaviour
    {
        public static DroneCamController Instance { get; private set; }

        public DroneCamMode Mode { get; private set; } = DroneCamMode.Disabled;

        // ── unified target state ──────────────────────────────────────────────
        private DroneCamTarget _anchor;       // what the camera moves relative to
        private DroneCamTarget _lookTarget;   // what the camera points at (null = use anchor)

        // anchor tracking
        private Vector3 _anchorLastPos = Vector3.zero;
        private Vector3 _anchorLastKnownPos = Vector3.zero;
        private Vector3 _anchorLastRelOffset = Vector3.zero;
        private bool _anchorWaiting = false;
        private Vector3 _lastLookPosition = Vector3.zero;

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

        // ── refs ──────────────────────────────────────────────────────────────
        private GameCamera _gameCamera;

        // ─────────────────────────────────────────────────────────────────────

        private bool _hudHidden = false;
        public bool HudHidden => _hudHidden;

        public void ToggleHud()
        {
            _hudHidden = !_hudHidden;
            Hud.instance?.gameObject.SetActive(!_hudHidden);
            Notify(_hudHidden ? "HUD hidden." : "HUD visible.");
        }

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
                if (Mode == DroneCamMode.Disabled)
                    EnterFreeSetup();
                else
                    ExitDroneCam();
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
            }

            if (Mode != DroneCamMode.Disabled &&
                Player.m_localPlayer != null &&
                Player.m_localPlayer.m_nview != null &&
                Player.m_localPlayer.m_nview.IsValid())
            {
                Player.m_localPlayer.m_body.position = transform.position;
                Player.m_localPlayer.SetVisible(false);
            }

            if (Mode != DroneCamMode.Disabled)
                PollTargetSleepState();

        }

        private bool _targetWasSleeping = false;

        private void PollTargetSleepState()
        {
            if (_anchor == null || _anchor.Type != TargetType.Player) return;
            if (_anchor.Transform == null) return;

            Player p = _anchor.Transform.GetComponent<Player>();
            if (p == null) return;

            // Read sleep state directly from ZDO rather than relying on SetSleeping patch
            bool isSleeping = p.m_nview?.GetZDO()?.GetBool(ZDOVars.s_inBed) ?? false;

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
            Mode = DroneCamMode.Disabled;
            SetGameCameraEnabled(true);

            // Restore HUD if it was hidden
            if (_hudHidden)
            {
                _hudHidden = false;
                Hud.instance?.gameObject.SetActive(true);
            }

            if (Player.m_localPlayer != null)
            {
                Vector3 groundPos = FindGroundPosition(transform.position);
                Player.m_localPlayer.m_body.position = groundPos;
                Player.m_localPlayer.m_body.linearVelocity = Vector3.zero;
                Player.m_localPlayer.SetVisible(true);
            }

            Notify("Disabled - normal camera restored.");
        }
        
        private GameObject _fakeAttachPoint;

        private bool _broadcastRealPosition = false;
        public bool BroadcastRealPosition => _broadcastRealPosition;
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

                // Force immediate ZDO flush to server so EverybodyIsTryingToSleep
                // sees s_inBed=true on our ZDO before it checks
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
                    UnityEngine.Object.Destroy(_fakeAttachPoint);
                    _fakeAttachPoint = null;
                }
            }

            SetGameCameraEnabled(false);
            Mode = _modeBeforeSleep;
            _modeBeforeSleep = DroneCamMode.Disabled;
            Notify("Awake - drone resumed.");
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
            if (_gameCamera != null)
                _gameCamera.enabled = state;
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
                    else if (modSpeed) _orbitSpeed = Mathf.Max(1f, _orbitSpeed + delta * 10f);
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

            List<Player> players = Player.GetAllPlayers()
                .Where(p => p != Player.m_localPlayer)
                .ToList();

            if (players.Count < 1) return;

            int current = players.FindIndex(p =>
                string.Equals(p.GetPlayerName(), _anchor.Name, StringComparison.OrdinalIgnoreCase));

            int next = current < 0 ? 0 : (current + dir + players.Count) % players.Count;
            string nextName = players[next].GetPlayerName();

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

            float speed = ZInput.GetButton(Btn.Fast)
                           ? DroneCamPlugin.FlySpeedFast.Value
                           : DroneCamPlugin.FlySpeed.Value;
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
                float yaw = delta.x * rotSpd * Time.deltaTime;
                float pitch = -delta.y * rotSpd * Time.deltaTime;
                Vector3 e = _droneRot.eulerAngles;
                _droneRot = Quaternion.Euler(e.x + pitch, e.y + yaw, 0f);
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
            Vector3 forward = _anchor.Transform != null ? _anchor.Transform.forward : transform.forward;
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
                if (_anchor == null || (!_anchor.IsValid && _anchorLastKnownPos == Vector3.zero)) return;
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

        private float _anchorReacquireTimer = 0f;
        private const float ReacquireStableTime = 0.5f; // player must be present for 500ms before snap fires


        // ── target management ─────────────────────────────────────────────────
        private void RefreshTarget()
        {
            if (_anchor == null || _anchor.Type == TargetType.Position) return;

            if (_anchor.Type == TargetType.Player)
            {
                // Remote players may not have a Player component on this client -
                // use ZDO as the primary source of truth for position
                ZDO zdo = FindPlayerZdoByName(_anchor.Name);

                if (zdo != null)
                {
                    Vector3 pos = zdo.GetPosition();

                    if (_anchorLastKnownPos != Vector3.zero &&
                        Vector3.Distance(pos, _anchorLastKnownPos) > DroneCamPlugin.TeleportDetectionDistance.Value)
                    {
                        DroneCamPlugin.Log.LogInfo("[DroneCam] Portal jump detected - snapping drone.");
                        SnapDroneTo(pos + _anchorLastRelOffset);
                    }

                    // Try to get the Player component for transform/forward reference
                    // but don't depend on it being present
                    Player p = FindPlayerByName(_anchor.Name);
                    _anchor.Transform = p != null ? p.transform : _anchor.Transform;
                    _anchorLastKnownPos = pos;
                    _anchorLastPos = pos;
                    _anchorWaiting = false;
                    _anchorLastRelOffset = transform.position - pos;
                    return;
                }

                if (!_anchorWaiting)
                {
                    _anchorLastRelOffset = transform.position - _anchorLastKnownPos;
                    _anchorWaiting = true;
                    DroneCamPlugin.Log.LogInfo($"[DroneCam] Player '{_anchor.Name}' ZDO lost - waiting.");
                }
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

        private Vector3 GetTargetCenter()
        {
            if (_anchor == null) return transform.position;

            if (_anchor.Type == TargetType.Player)
                return _anchorLastKnownPos != Vector3.zero
                    ? _anchorLastKnownPos
                    : transform.position;

            if (_anchor.IsValid)
                return _anchorLastKnownPos = _anchor.GetPosition();

            return _anchorLastKnownPos;
        }

        private Vector3 GetLookTarget()
        {
            Vector3 result;

            if (_lookTarget != null)
            {
                if (_lookTarget.Type == TargetType.Enemy)
                {
                    if (!_lookTarget.IsValid)
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
                }

                if (_lookTarget != null)
                {
                    result = _lookTarget.GetPosition() + Vector3.up * 1.5f;
                    _lastLookPosition = result;
                    return result;
                }
            }

            result = GetTargetCenter() + Vector3.up * 1.5f;
            _lastLookPosition = result;
            return result;
        }

        public void SnapToPlayer()
        {
            if (_anchor == null || _anchor.Type != TargetType.Player)
            {
                Notify("No player target set.");
                return;
            }

            Player p = FindPlayerByName(_anchor.Name);
            if (p == null)
            {
                Notify($"Player '{_anchor.Name}' not found.");
                return;
            }

            ZDO zdo = p.m_nview?.GetZDO();
            Vector3 playerPos = zdo != null ? zdo.GetPosition() : p.transform.position;

            transform.position = playerPos + _anchorLastRelOffset;
            _dronePos = transform.position;
            _smoothVelocity = Vector3.zero;
            _smoothVelRef = Vector3.zero;
            _anchorLastKnownPos = playerPos;
            _anchorLastPos = playerPos;

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.m_body.position = transform.position;

            Notify($"Snapped to {_anchor.Name}.");
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
            _anchorWaiting = false;
            _anchorReacquireTimer = 0f;
            _lastLookPosition = Vector3.zero;
            _targetWasSleeping = false;
        }

        // ── finders ───────────────────────────────────────────────────────────


        private static Player FindPlayerByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (Player p in Player.GetAllPlayers())
            {
                if (p == Player.m_localPlayer) continue; // drone can never target itself
                string pName = p.GetPlayerName();
                if (string.IsNullOrEmpty(pName) && p.m_nview != null)
                    pName = p.m_nview.GetZDO()?.GetString(ZDOVars.s_playerName) ?? "";
                if (string.Equals(pName, name, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        private static ZDO FindPlayerZdoByName(string name)
        {
            foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                string zdoName = zdo.GetString(ZDOVars.s_playerName);
                if (string.IsNullOrEmpty(zdoName)) continue;
//                DroneCamPlugin.Log.LogInfo($"[DroneCam] FindPlayerZdoByName: checking '{zdoName}' against '{name}'");
                if (string.Equals(zdoName, name, StringComparison.OrdinalIgnoreCase))
                    return zdo;
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

        // ── look-at position for pos commands ─────────────────────────────────
        public Vector3 GetLookAtPosition()
        {
            if (_lastLookPosition != Vector3.zero)
                return _lastLookPosition;

            // Fallback - project forward from camera if no look history yet
            return transform.position + transform.forward * _followDistance;
        }

        private static void Notify(string msg)
            => Chat.instance?.AddString($"[DroneCam] {msg}");

        // ── public API ────────────────────────────────────────────────────────

        public void SetFollow(string playerName, float distance, float heightOffset, float smoothTime)
        {
            ZDO zdo = FindPlayerZdoByName(playerName);
            if (zdo == null) { Notify($"Player '{playerName}' not found."); return; }

            ResetTargetState();

            // Try to get Player component for transform - may be null for remote players
            Player p = FindPlayerByName(playerName);
            _anchor = new DroneCamTarget
            {
                Type = TargetType.Player,
                Name = playerName,
                Transform = p?.transform,
            };

            _anchorLastKnownPos = zdo.GetPosition();
            _followDistance = distance;
            _followHeightOffset = new Vector3(0, heightOffset, 0);
            _followSmoothTime = smoothTime;
            Mode = DroneCamMode.Follow;
            EnterDroneMode();
            SetGameCameraEnabled(false);
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
            ZDO zdo = FindPlayerZdoByName(playerName);
            if (zdo == null) { Notify($"Player '{playerName}' not found."); return; }

            ResetTargetState();

            Player p = FindPlayerByName(playerName);
            _anchor = new DroneCamTarget
            {
                Type = TargetType.Player,
                Name = playerName,
                Transform = p?.transform,
            };

            _anchorLastKnownPos = zdo.GetPosition();
            _orbitRadius = radius;
            _orbitSpeed = speed;
            _orbitHeight = height;
            _orbitAngle = 0f;
            Mode = DroneCamMode.Orbit;
            EnterDroneMode();
            SetGameCameraEnabled(false);
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
            ZDO zdo = FindPlayerZdoByName(playerName);
            if (zdo == null) { Notify($"Player '{playerName}' not found."); return; }

            ResetTargetState();

            Player p = FindPlayerByName(playerName);
            _anchor = new DroneCamTarget
            {
                Type = TargetType.Player,
                Name = playerName,
                Transform = p?.transform,
            };

            _anchorLastKnownPos = zdo.GetPosition();
            _securityPos = transform.position;
            Mode = DroneCamMode.Security;
            EnterDroneMode();
            SetGameCameraEnabled(false);
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

        public static IEnumerable<string> GetPlayerNames()
            => Player.GetAllPlayers().Select(p => p.GetPlayerName());
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
    // Harmony – broadcast underground+offset position to other clients
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

            // Fixed remote location: underground, far from all play areas,
            // outside the 100m XZ difficulty scaling radius

            Vector3 realPos = Player.m_localPlayer.m_body.position;

            __result = new Vector3(
                realPos.x + 101f,
                realPos.y - 1000f,
                realPos.z);
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – intercept /dronecam and /dc commands
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

            DroneCamCommands.Handle(text);
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
            if (DroneCamController.Instance.HudHidden) return; // already hidden entirely
            __instance.m_crosshair.gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IsTargetPlayer helper (needed by sleep patch)
    // ─────────────────────────────────────────────────────────────────────────
    public partial class DroneCamController
    {
        public bool IsTargetPlayer(Player p)
        {
            if (_anchor == null || _anchor.Type != TargetType.Player) return false;
            string pName = p.GetPlayerName();
            if (string.IsNullOrEmpty(pName) && p.m_nview != null)
                pName = p.m_nview.GetZDO()?.GetString(ZDOVars.s_playerName) ?? "";
            return string.Equals(_anchor.Name, pName, StringComparison.OrdinalIgnoreCase);
        }
    }

    // prevent water effect on both client and server
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
    // Chat command parser + dispatcher
    // ─────────────────────────────────────────────────────────────────────────
    public static class DroneCamCommands
    {
        private static DroneCamController Ctrl => DroneCamController.Instance;

        private const string Help =
            "[DroneCam] Commands (/dronecam or /dc):\n" +
            "  /dc help\n" +
            "  /dc on | ff | freefly - enter free-fly setup\n" +
            "  /dc off - return to normal camera\n" +
            "  /dc players - list visible players\n" +
            "  /dc f p <player> [dist] [h] [sm]   follow a player\n" +
            "  /dc f e <enemy>  [dist] [h] [sm]   follow nearest named enemy\n" +
            "  /dc o p <n> [r] [spd] [h] - orbit a player\n" +
            "  /dc o e <n> [r] [spd] [h] - orbit nearest named enemy\n" +
            "  /dc o pos [r] [spd] [h] - orbit current look-at position\n" +
            "  /dc o s <deg/sec> - change orbit speed live\n" +
            "  /dc s p <n> - security cam, track player\n" +
            "  /dc s pos - security cam, track look-at position\n" +
            "  /dc te - target nearest enemy for look-at\n" +
            "  /dc te <name> - target named enemy for look-at\n" +
            "  /dc te c - clear enemy look-at target\n" +
            "  /dc hud - toggle HUD visibility\n" +
            "  /dc snap - snap drone to current player position\n" +
            "Player names with spaces must be quoted: /dc f p \"Big Viking\"\n" +
            "Wheel: dist/radius  Alt+wheel: height  Ctrl+wheel: orbit speed\n" +
            ". / , keys cycle next/prev player target  F8 toggle";

        private delegate void CmdHandler(string[] t);

        private static readonly Dictionary<string, CmdHandler> Commands =
            new Dictionary<string, CmdHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { "help",        t => Msg(Help) },
            { "on",          t => Ctrl.EnterFreeSetup() },
            { "off",         t => Ctrl.ExitDroneCam() },
            { "ff",          t => Ctrl.EnterFreeSetup() },
            { "freefly",     t => Ctrl.EnterFreeSetup() },
            { "players",     t => Msg($"[DroneCam] Players: {string.Join(", ", DroneCamController.GetPlayerNames())}") },
            { "follow",      HandleFollow },   { "f",  HandleFollow },
            { "orbit",       HandleOrbit },    { "o",  HandleOrbit },
            { "security",    HandleSecurity }, { "s",  HandleSecurity },
            { "targetenemy", HandleTargetEnemy }, { "te", HandleTargetEnemy },
            { "hud", t => Ctrl.ToggleHud() },
            { "snap", t => Ctrl.SnapToPlayer() },


        };

        public static void Handle(string raw)
        {
            if (Ctrl == null) { Msg("[DroneCam] Controller not ready."); return; }

            // Extract quoted name before splitting on spaces
            var match = Regex.Match(raw, "\"([^\"]+)\"");
            if (match.Success)
            {
                string quoted = match.Groups[1].Value;
                raw = raw.Replace(match.Value, quoted.Replace(' ', '\x01'));
            }

            string[] t = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < t.Length; i++)
                t[i] = t[i].Replace('\x01', ' ');

            string sub = t.Length > 1 ? t[1] : "help";

            if (Commands.TryGetValue(sub, out CmdHandler handler))
                handler(t);
            else
                Msg($"[DroneCam] Unknown command '{sub}'. Type /dc help.");
        }

        // ── sub-command handlers ──────────────────────────────────────────────

        private static void HandleFollow(string[] t)
        {
            if (t.Length < 3) { Msg("Usage: /dc f p <player> | e <enemy> [dist] [h] [smooth]"); return; }
            switch (t[2].ToLower())
            {
                case "p":
                case "player":
                    if (t.Length < 4) { Msg("Usage: /dc f p <player> [dist] [h] [smooth]"); return; }
                    Ctrl.SetFollow(t[3], F(t, 4, 5f), F(t, 5, 2f), F(t, 6, 0.1f));
                    break;
                case "e":
                case "enemy":
                    if (t.Length < 4) { Msg("Usage: /dc f e <enemy> [dist] [h] [smooth]"); return; }
                    Ctrl.SetFollowEnemy(t[3], F(t, 4, 5f), F(t, 5, 2f), F(t, 6, 0.1f));
                    break;
                default:
                    // Bare name - backwards compat, default to player
                    Ctrl.SetFollow(t[2], F(t, 3, 5f), F(t, 4, 2f), F(t, 5, 0.1f));
                    break;
            }
        }

        private static void HandleOrbit(string[] t)
        {
            if (t.Length < 3) { Msg("Usage: /dc o p <n> | e <n> | pos | s <val>"); return; }
            switch (t[2].ToLower())
            {
                case "p":
                case "player":
                    if (t.Length < 4) { Msg("Usage: /dc o p <n> [r] [spd] [h]"); return; }
                    Ctrl.SetOrbitPlayer(t[3], F(t, 4, 10f), F(t, 5, 30f), F(t, 6, 4f));
                    break;
                case "e":
                case "enemy":
                    if (t.Length < 4) { Msg("Usage: /dc o e <n> [r] [spd] [h]"); return; }
                    Ctrl.SetOrbitEnemy(t[3], F(t, 4, 10f), F(t, 5, 30f), F(t, 6, 4f));
                    break;
                case "pos":
                    Ctrl.SetOrbitPosition(Ctrl.GetLookAtPosition(), F(t, 3, 10f), F(t, 4, 30f), F(t, 5, 4f));
                    break;
                case "s":
                case "speed":
                    if (t.Length < 4) { Msg("Usage: /dc o s <deg/sec>"); return; }
                    Ctrl.SetOrbitSpeed(F(t, 3, 30f));
                    break;
                default:
                    Msg($"Unknown orbit sub-command '{t[2]}'.");
                    break;
            }
        }

        private static void HandleSecurity(string[] t)
        {
            if (t.Length < 3) { Msg("Usage: /dc s p <n> | pos"); return; }
            switch (t[2].ToLower())
            {
                case "p":
                case "player":
                    if (t.Length < 4) { Msg("Usage: /dc s p <n>"); return; }
                    Ctrl.SetSecurityPlayer(t[3]);
                    break;
                case "pos":
                    Ctrl.SetSecurityPosition(Ctrl.GetLookAtPosition());
                    break;
                default:
                    Msg($"Unknown security sub-command '{t[2]}'.");
                    break;
            }
        }

        private static void HandleTargetEnemy(string[] t)
        {
            if (t.Length < 3) { Ctrl.SetEnemyTargetNearest(); return; }
            switch (t[2].ToLower())
            {
                case "c":
                case "clear":
                    Ctrl.ClearEnemyTarget();
                    break;
                case "n":
                case "nearest":
                    Ctrl.SetEnemyTargetNearest();
                    break;
                default:
                    Ctrl.SetEnemyTargetNamed(t[2]);
                    break;
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static void Msg(string s)
        {
            Chat.instance?.AddString(s);
            DroneCamPlugin.Log.LogInfo(s);
        }

        private static float F(string[] t, int i, float fallback)
            => t.Length > i && float.TryParse(t[i], NumberStyles.Float,
               CultureInfo.InvariantCulture, out float v) ? v : fallback;

        private static float F(string s, float fallback)
            => float.TryParse(s, NumberStyles.Float,
               CultureInfo.InvariantCulture, out float v) ? v : fallback;
    }
}