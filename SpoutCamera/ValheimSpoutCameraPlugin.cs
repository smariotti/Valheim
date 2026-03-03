using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections;

namespace ValheimSpoutCamera
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ValheimSpoutCameraPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.yourname.valheim.spoutcamera";
        public const string PluginName = "ValheimSpoutCamera";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        private static Harmony _harmony;

        // Config entries
        public static ConfigEntry<int> SpoutWidth;
        public static ConfigEntry<int> SpoutHeight;
        public static ConfigEntry<string> SpoutSenderName;
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<KeyboardShortcut> ToggleKey;
        public static ConfigEntry<float> CameraFOV;
        public static ConfigEntry<bool> FollowPlayer;
        public static ConfigEntry<float> FollowOffsetX;
        public static ConfigEntry<float> FollowOffsetY;
        public static ConfigEntry<float> FollowOffsetZ;

        private SpoutCameraManager _cameraManager;

        private void Awake()
        {
            Log = Logger;

            // --- Configuration ---
            EnableMod = Config.Bind("General", "EnableMod", true,
                "Enable or disable the Spout camera sender.");

            SpoutSenderName = Config.Bind("Spout", "SenderName", "ValheimCamera",
                "The name of the Spout sender as it will appear in OBS.");

            SpoutWidth = Config.Bind("Spout", "Width", 1920,
                "Width of the Spout output texture.");

            SpoutHeight = Config.Bind("Spout", "Height", 1080,
                "Height of the Spout output texture.");

            ToggleKey = Config.Bind("General", "ToggleKey",
                new KeyboardShortcut(KeyCode.F8),
                "Hotkey to toggle the Spout camera on/off.");

            CameraFOV = Config.Bind("Camera", "FOV", 60f,
                "Field of view for the custom camera (degrees).");

            FollowPlayer = Config.Bind("Camera", "FollowPlayer", true,
                "If true, the camera follows the local player.");

            FollowOffsetX = Config.Bind("Camera", "OffsetX", 0f,
                "X offset from player when following (world units).");

            FollowOffsetY = Config.Bind("Camera", "OffsetY", 2f,
                "Y offset from player when following (world units).");

            FollowOffsetZ = Config.Bind("Camera", "OffsetZ", -4f,
                "Z offset from player when following (world units).");

            if (!EnableMod.Value)
            {
                Log.LogInfo($"{PluginName} is disabled via config.");
                return;
            }

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded. Press {ToggleKey.Value} to toggle Spout camera.");
        }

        private void Start()
        {
            if (!EnableMod.Value) return;
            StartCoroutine(InitWhenReady());
        }

        private IEnumerator InitWhenReady()
        {
            // Wait until the game scene is properly loaded
            yield return new WaitUntil(() => Player.m_localPlayer != null);
            yield return new WaitForSeconds(1f);

            _cameraManager = gameObject.AddComponent<SpoutCameraManager>();
            Log.LogInfo("SpoutCameraManager initialised.");
        }

        private void Update()
        {
            if (!EnableMod.Value) return;
            if (ToggleKey.Value.IsDown() && _cameraManager != null)
            {
                _cameraManager.ToggleSpout();
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Log.LogInfo($"{PluginName} unloaded.");
        }
    }
}
