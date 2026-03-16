using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RafTris
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class RafTrisPlugin : BaseUnityPlugin
    {
        public const string PluginGUID    = "com.oathorse.raftris";
        public const string PluginName    = "RafTris";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        // Config entries
        public static ConfigEntry<KeyboardShortcut> ToggleKey;
        public static ConfigEntry<bool>             PauseGameWhilePlaying;
        public static ConfigEntry<float>            WindowScale;

        private Harmony _harmony;
        private GameObject _managerObject;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} loading…");

            ToggleKey = Config.Bind(
                "Controls", "ToggleKey",
                new KeyboardShortcut(KeyCode.F7),
                "Key to open/close the RafTris window");

            PauseGameWhilePlaying = Config.Bind(
                "Gameplay", "PauseGameWhilePlaying",
                false,
                "Pause Valheim time while the RafTris window is open");

            WindowScale = Config.Bind(
                "UI", "WindowScale",
                1.0f,
                new ConfigDescription("UI scale multiplier",
                    new AcceptableValueRange<float>(0.5f, 2.0f)));

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            _managerObject = new GameObject("RafTrisManager");
            DontDestroyOnLoad(_managerObject);
            _managerObject.AddComponent<RafTrisManager>();

            Log.LogInfo($"{PluginName} loaded successfully.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
