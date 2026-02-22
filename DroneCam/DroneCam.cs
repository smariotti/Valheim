using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public const string PluginVersion = "0.1.2";

        internal static ManualLogSource Log;

        public static ConfigEntry<float> FlySpeed;
        public static ConfigEntry<float> FlySpeedFast;
        public static ConfigEntry<float> RotationSpeed;
        public static ConfigEntry<float> SmoothTime;

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        private void Awake()
        {
            Log = Logger;

            FlySpeed = Config.Bind("Camera", "FlySpeed", 10f, "Normal fly speed (units/sec).");
            FlySpeedFast = Config.Bind("Camera", "FlySpeedFast", 40f, "Fast fly speed (units/sec).");
            RotationSpeed = Config.Bind("Camera", "RotationSpeed", 90f, "Keyboard rotation speed (degrees/sec).");
            SmoothTime = Config.Bind("Camera", "SmoothTime", 0.25f, "Movement smoothing time.");

            _harmony.PatchAll();
            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Type /dronecam help in chat.");
        }

        private void OnDestroy() => _harmony.UnpatchSelf();
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
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Register buttons every time ZInput rebuilds its table (startup + options save)
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
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Drone cam modes
    // ─────────────────────────────────────────────────────────────────────────
    public enum DroneCamMode
    {
        Disabled,
        FreeSetup,  // manual free-fly, no automated movement
        Follow,     // chase a named player
        Orbit,      // circle a player or world position
        Security,   // fixed position, pan to track a target
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core controller – attached to the GameCamera GameObject by Harmony patch
    // ─────────────────────────────────────────────────────────────────────────
    public class DroneCamController : MonoBehaviour
    {
        public static DroneCamController Instance { get; private set; }

        public DroneCamMode Mode { get; private set; } = DroneCamMode.Disabled;

        // ── target ────────────────────────────────────────────────────────────
        private string _targetPlayerName;
        private Player _targetPlayer;
        private Vector3 _targetWorldPos;
        private bool _targetIsPlayer;

        // ── follow params ─────────────────────────────────────────────────────
        private float _followDistance = 5f;
        private Vector3 _followHeightOffset = new Vector3(0, 2f, 0);

        // ── orbit params ──────────────────────────────────────────────────────
        private float _orbitRadius = 10f;
        private float _orbitSpeed = 30f;   // degrees/sec
        private float _orbitHeight = 4f;
        private float _orbitAngle = 0f;

        // ── security ──────────────────────────────────────────────────────────
        private Vector3 _securityPos;

        // ── free-fly state ────────────────────────────────────────────────────
        private Vector3 _dronePos;
        private Quaternion _droneRot;
        private Vector3 _smoothVelocity = Vector3.zero;
        private Vector3 _smoothVelRef = Vector3.zero;

        // ── saved player state ────────────────────────────────────────────────
        private bool _wasGodMode;
        private bool _wasGhostMode;

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

            // Keep local player teleported to camera position so the world
            // loads correctly, and hide them so they don't appear in shot
            if (Mode != DroneCamMode.Disabled && Player.m_localPlayer != null)
            {
                Player.m_localPlayer.m_body.position = transform.position;
                Player.m_localPlayer.SetVisible(false);
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

            if (Player.m_localPlayer != null)
            {
                _wasGodMode = Player.m_localPlayer.m_godMode;
                _wasGhostMode = Player.m_localPlayer.m_ghostMode;
                Player.m_localPlayer.m_godMode = true;
                Player.m_localPlayer.m_ghostMode = true;
            }

            Notify("Free-fly active. WASD/QE move, Shift=fast, RMB/arrows rotate. F8 to exit.");
        }

        public void ExitDroneCam()
        {
            Mode = DroneCamMode.Disabled;
            SetGameCameraEnabled(true);

            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.m_godMode = _wasGodMode;
                Player.m_localPlayer.m_ghostMode = _wasGhostMode;
                Player.m_localPlayer.SetVisible(true);
            }

            Notify("Disabled - normal camera restored.");
        }

        private void SetGameCameraEnabled(bool state)
        {
            if (_gameCamera != null)
                _gameCamera.enabled = state;
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

            // Translation
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

            // Mouse look
            if (ZInput.GetButton(Btn.MouseLook))
            {
                Vector2 delta = ZInput.GetMouseDelta();
                float yaw = delta.x * rotSpd * Time.deltaTime;
                float pitch = -delta.y * rotSpd * Time.deltaTime;
                Vector3 e = _droneRot.eulerAngles;
                _droneRot = Quaternion.Euler(e.x + pitch, e.y + yaw, 0f);
            }

            // Arrow key rotation
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
            RefreshTargetPlayer();
            if (_targetPlayer == null) return;
            CheckForTargetTeleport();

            Vector3 targetPos = _targetPlayer.transform.position;
            Vector3 desiredPos = targetPos
                               + (-_targetPlayer.transform.forward * _followDistance)
                               + _followHeightOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref _smoothVelRef, DroneCamPlugin.SmoothTime.Value);

            LookSmoothAt(targetPos + Vector3.up * 1.5f);
        }

        // ── orbit ─────────────────────────────────────────────────────────────
        private void UpdateOrbit()
        {
            CheckForTargetTeleport();

            Vector3 center = GetTargetCenter();
            _orbitAngle += _orbitSpeed * Time.deltaTime;

            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(rad) * _orbitRadius,
                _orbitHeight,
                Mathf.Cos(rad) * _orbitRadius);

            transform.position = Vector3.SmoothDamp(
                transform.position, center + offset, ref _smoothVelRef, DroneCamPlugin.SmoothTime.Value);

            LookSmoothAt(center + Vector3.up * 1.5f);
        }

        // ── security ──────────────────────────────────────────────────────────
        private void UpdateSecurity()
        {
            CheckForTargetTeleport();

            transform.position = _securityPos;
            LookSmoothAt(GetTargetCenter() + Vector3.up * 1.5f, lerpSpeed: 3f);
        }

        // ── helpers ───────────────────────────────────────────────────────────
        private void LookSmoothAt(Vector3 worldPoint, float lerpSpeed = 5f)
        {
            Vector3 dir = worldPoint - transform.position;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * lerpSpeed);
        }

        private Vector3 GetTargetCenter()
        {
            if (_targetIsPlayer)
            {
                RefreshTargetPlayer();
                if (_targetPlayer != null)
                    return _targetPlayer.transform.position;
            }
            return _targetWorldPos;
        }

        private void RefreshTargetPlayer()
        {
            if (_targetPlayer != null && _targetPlayer.isActiveAndEnabled) return;
            _targetPlayer = FindPlayerByName(_targetPlayerName);
        }

        private static Player FindPlayerByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (Player p in Player.GetAllPlayers())
                if (string.Equals(p.GetPlayerName(), name, StringComparison.OrdinalIgnoreCase))
                    return p;
            return null;
        }

        // Returns the position the camera is currently looking at, or a point
        // projected forward by follow distance if no look-at target is set.
        public Vector3 GetLookAtPosition()
        {
            if (Mode == DroneCamMode.Security)
                return GetTargetCenter();

            return transform.position + transform.forward * _followDistance;
        }

        private static void Notify(string msg)
            => Chat.instance?.AddString($"[DroneCam] {msg}");

        // ── public API ────────────────────────────────────────────────────────

        public void SetFollow(string playerName, float distance, float heightOffset)
        {
            _targetPlayerName = playerName;
            _targetIsPlayer = true;
            _followDistance = distance;
            _followHeightOffset = new Vector3(0, heightOffset, 0);
            _targetPlayer = FindPlayerByName(playerName);
            _lastTargetPosition = Vector3.zero;

            if (_targetPlayer == null) { Notify($"Player '{playerName}' not found."); return; }

            Mode = DroneCamMode.Follow;
            SetGameCameraEnabled(false);
            Notify($"Following {playerName} - dist={distance} height={heightOffset}");
        }

        public void SetOrbitPlayer(string playerName, float radius, float speed, float height)
        {
            _targetPlayerName = playerName;
            _targetIsPlayer = true;
            _targetPlayer = FindPlayerByName(playerName);
            _orbitRadius = radius;
            _orbitSpeed = speed;
            _orbitHeight = height;
            _orbitAngle = 0f;
            _lastTargetPosition = Vector3.zero;

            if (_targetPlayer == null) { Notify($"Player '{playerName}' not found."); return; }

            Mode = DroneCamMode.Orbit;
            SetGameCameraEnabled(false);
            Notify($"Orbiting {playerName} - radius={radius} speed={speed} height={height}");
        }

        public void SetOrbitPosition(Vector3 pos, float radius, float speed, float height)
        {
            _targetIsPlayer = false;
            _targetWorldPos = pos;
            _orbitRadius = radius;
            _orbitSpeed = speed;
            _orbitHeight = height;
            _orbitAngle = 0f;
            _lastTargetPosition = Vector3.zero;

            Mode = DroneCamMode.Orbit;
            SetGameCameraEnabled(false);
            Notify($"Orbiting position - radius={radius} speed={speed} height={height}");
        }

        public void SetSecurityPlayer(string playerName)
        {
            _securityPos = transform.position;
            _targetPlayerName = playerName;
            _targetIsPlayer = true;
            _targetPlayer = FindPlayerByName(playerName);

            if (_targetPlayer == null) { Notify($"Player '{playerName}' not found."); return; }

            Mode = DroneCamMode.Security;
            SetGameCameraEnabled(false);
            Notify($"Security cam tracking {playerName}");
        }

        public void SetSecurityPosition(Vector3 pos)
        {
            _securityPos = transform.position;
            _targetIsPlayer = false;
            _targetWorldPos = pos;

            Mode = DroneCamMode.Security;
            SetGameCameraEnabled(false);
            Notify("Security cam tracking look-at position");
        }

        public void SetOrbitSpeed(float speed)
        {
            _orbitSpeed = speed;
            Notify($"Orbit speed set to {speed} deg/sec");
        }

        public static IEnumerable<string> GetPlayerNames()
            => Player.GetAllPlayers().Select(p => p.GetPlayerName());

        private Vector3 _lastTargetPosition;
        private const float TeleportDetectionDistance = 50f;

        private void CheckForTargetTeleport()
        {
            if (_targetPlayer == null) return;

            Vector3 currentPos = _targetPlayer.transform.position;
            float dist = Vector3.Distance(currentPos, _lastTargetPosition);

            if (dist > TeleportDetectionDistance && _lastTargetPosition != Vector3.zero)
            {
                // Target player teleported - snap drone to their new position
                SnapToTarget(currentPos);
            }

            _lastTargetPosition = currentPos;
        }

        private void SnapToTarget(Vector3 targetPos)
        {
            // Reset smooth velocity so we don't interpolate across the world
            _smoothVelocity = Vector3.zero;
            _smoothVelRef = Vector3.zero;

            switch (Mode)
            {
                case DroneCamMode.Follow:
                    // Snap to follow position behind player
                    transform.position = targetPos
                        + (-_targetPlayer.transform.forward * _followDistance)
                        + _followHeightOffset;
                    _dronePos = transform.position;
                    break;

                case DroneCamMode.Orbit:
                    // Snap orbit center to new position, reset angle
                    _orbitAngle = 0f;
                    transform.position = targetPos + new Vector3(_orbitRadius, _orbitHeight, 0f);
                    break;

                case DroneCamMode.Security:
                    // Snap security cam to near new position, keeping relative offset
                    _securityPos = targetPos + (transform.position - _lastTargetPosition).normalized * 10f;
                    transform.position = _securityPos;
                    break;
            }

            DroneCamPlugin.Log.LogInfo("[DroneCam] Target teleported - snapping drone to new position.");
        }
    }


    // ─────────────────────────────────────────────────────────────────────────
    // Harmony – attach DroneCamController to the GameCamera GameObject
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
    // Harmony – suppress GameCamera.LateUpdate while drone cam owns the camera
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
    // Harmony – intercept /dronecam commands typed into the chat box
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Chat), "InputText")]
    public static class Chat_InputText_Patch
    {
        static bool Prefix(Chat __instance)
        {
            string text = __instance.m_terminalInstance.m_input.text?.Trim();
            if (string.IsNullOrEmpty(text) ||
                !text.StartsWith("/dronecam", StringComparison.OrdinalIgnoreCase))
                return true; // not our command – let Valheim handle normally

            DroneCamCommands.Handle(text);
            return false; // swallow so it doesn't appear in network chat
        }
    }

    [HarmonyPatch(typeof(ZSyncTransform), "GetPosition")]
    public static class ZSyncTransform_GetPosition_Patch
    {
        static bool Prefix(ZSyncTransform __instance, ref Vector3 __result)
        {
            if (DroneCamController.Instance == null) return true;
            if (DroneCamController.Instance.Mode == DroneCamMode.Disabled) return true;
            if (Player.m_localPlayer == null) return true;
            if (__instance.gameObject != Player.m_localPlayer.gameObject) return true;

            // Return underground position so other clients render us below terrain,
            // while m_body.position remains at the camera for local chunk loading
            __result = Player.m_localPlayer.m_body.position + Vector3.down * 1000f;
            return false; // skip original
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Chat command parser + dispatcher
    // ─────────────────────────────────────────────────────────────────────────
    public static class DroneCamCommands
    {
        private static DroneCamController Ctrl => DroneCamController.Instance;

        private const string Help =
            "[DroneCam] Commands:\n" +
            "  /dronecam help\n" +
            "  /dronecam on                               enter free-fly setup\n" +
            "  /dronecam off                              return to normal camera\n" +
            "  /dronecam players                          list visible players\n" +
            "  /dronecam follow <player> [dist] [height]  chase a player\n" +
            "  /dronecam orbit player <n> [r] [spd] [h]  orbit a player\n" +
            "  /dronecam orbit pos [r] [spd] [h]         orbit current look-at position\n" +
            "  /dronecam orbit speed <deg/sec>            change orbit speed live\n" +
            "  /dronecam security player <n>              security cam, track player\n" +
            "  /dronecam security pos                     security cam, track look-at position\n" +
            "Free-fly: WASD move  QE up/down  Shift fast  RMB/arrows rotate  F8 toggle";

        public static void Handle(string raw)
        {
            if (Ctrl == null) { Msg("[DroneCam] Controller not ready."); return; }

            string[] t = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string sub = t.Length > 1 ? t[1].ToLower() : "help";

            switch (sub)
            {
                case "help":
                    Msg(Help);
                    break;

                case "on":
                    Ctrl.EnterFreeSetup();
                    break;

                case "off":
                    Ctrl.ExitDroneCam();
                    break;

                case "players":
                    {
                        string list = string.Join(", ", DroneCamController.GetPlayerNames());
                        Msg($"[DroneCam] Players: {(string.IsNullOrEmpty(list) ? "none" : list)}");
                        break;
                    }

                case "follow":
                    {
                        // /dronecam follow <player> [dist] [height]
                        if (t.Length < 3) { Msg("Usage: /dronecam follow <player> [dist] [height]"); break; }
                        float dist = t.Length > 3 ? F(t[3], 5f) : 5f;
                        float height = t.Length > 4 ? F(t[4], 2f) : 2f;
                        Ctrl.SetFollow(t[2], dist, height);
                        break;
                    }

                case "orbit":
                    {
                        if (t.Length < 3) { Msg("Usage: /dronecam orbit player <n> | pos | speed <val>"); break; }
                        string osub = t[2].ToLower();

                        if (osub == "player")
                        {
                            // /dronecam orbit player <n> [r] [spd] [h]
                            if (t.Length < 4) { Msg("Usage: /dronecam orbit player <n> [r] [spd] [h]"); break; }
                            float r = t.Length > 4 ? F(t[4], 10f) : 10f;
                            float spd = t.Length > 5 ? F(t[5], 30f) : 30f;
                            float h = t.Length > 6 ? F(t[6], 4f) : 4f;
                            Ctrl.SetOrbitPlayer(t[3], r, spd, h);
                        }
                        else if (osub == "pos")
                        {
                            // /dronecam orbit pos [r] [spd] [h]
                            Vector3 pos = Ctrl.GetLookAtPosition();
                            float r = t.Length > 3 ? F(t[3], 10f) : 10f;
                            float spd = t.Length > 4 ? F(t[4], 30f) : 30f;
                            float h = t.Length > 5 ? F(t[5], 4f) : 4f;
                            Ctrl.SetOrbitPosition(pos, r, spd, h);
                        }
                        else if (osub == "speed")
                        {
                            // /dronecam orbit speed <value>
                            if (t.Length < 4) { Msg("Usage: /dronecam orbit speed <deg/sec>"); break; }
                            Ctrl.SetOrbitSpeed(F(t[3], 30f));
                        }
                        else
                            Msg($"Unknown orbit sub-command '{osub}'.");

                        break;
                    }

                case "security":
                    {
                        if (t.Length < 3) { Msg("Usage: /dronecam security player <n> | pos"); break; }
                        string ssub = t[2].ToLower();

                        if (ssub == "player")
                        {
                            // /dronecam security player <n>
                            if (t.Length < 4) { Msg("Usage: /dronecam security player <n>"); break; }
                            Ctrl.SetSecurityPlayer(t[3]);
                        }
                        else if (ssub == "pos")
                        {
                            // /dronecam security pos
                            Ctrl.SetSecurityPosition(Ctrl.GetLookAtPosition());
                        }
                        else
                            Msg($"Unknown security sub-command '{ssub}'.");

                        break;
                    }

                default:
                    Msg($"[DroneCam] Unknown command '{sub}'. Type /dronecam help.");
                    break;
            }
        }

        private static void Msg(string s)
        {
            Chat.instance?.AddString(s);
            DroneCamPlugin.Log.LogInfo(s);
        }

        private static float F(string s, float fallback)
            => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
    }
}