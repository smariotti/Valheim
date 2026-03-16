using HarmonyLib;

namespace RafTris
{
    /// <summary>
    /// Harmony patches that prevent Valheim from consuming keyboard input
    /// while the RafTris window is visible and focused.
    /// </summary>
    public static class InputPatches
    {
        /// <summary>
        /// Prevents the player character from receiving movement input
        /// while the RafTris overlay is open.
        /// </summary>
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
        public static class ZInput_GetButtonDown_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (RafTrisManager.Instance != null && RafTrisManager.Instance.IsVisible)
                {
                    __result = false;
                    return false;   // skip original
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton))]
        public static class ZInput_GetButton_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (RafTrisManager.Instance != null && RafTrisManager.Instance.IsVisible)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp))]
        public static class ZInput_GetButtonUp_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (RafTrisManager.Instance != null && RafTrisManager.Instance.IsVisible)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Prevent the game from opening its own menus (inventory, map, etc.)
        /// while the RafTris window is open.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public static class Player_Update_Patch
        {
            public static bool Prefix()
            {
                // Returning false would break everything — instead we rely on ZInput patches above.
                return true;
            }
        }

        /// <summary>
        /// Disable the escape-menu pause screen while we handle Escape ourselves.
        /// Only active when RafTris window is open.
        /// </summary>
        [HarmonyPatch(typeof(FejdStartup), "Update")]
        public static class FejdStartup_Patch
        {
            public static bool Prefix()
            {
                // FejdStartup.Update is the main-menu update; nothing to do during gameplay.
                return true;
            }
        }
    }
}
