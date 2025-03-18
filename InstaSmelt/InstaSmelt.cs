using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using HarmonyLib;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using static Terminal;

namespace InstaSmelt
{ 
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class InstaSmelt : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.InstaSmelt";
        public const string PluginName = "InstaSmelt";
        public const string PluginVersion = "0.1.0";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        List<Minimap.PinData> __m_pins = new List<Minimap.PinData>();

        static public bool __m_createPinOnTeleport = false;

        public void Awake()
        {
            // Patch with Harmony
            harmony.PatchAll();

        }

        //
        // Trophy Saga Insta-Smelt
        //
        public static Dictionary<string, string> __m_oreNameToBarPrefabName = new Dictionary<string, string>()
            {
                { "CopperOre",          "Copper" },
                { "TinOre",             "Tin" },
                { "IronScrap",          "Iron" },
                { "SilverOre",          "Silver" },
                { "BlackMetalScrap",    "BlackMetal" },
                { "FlametalOreNew",     "FlametalNew" },
                { "BronzeScrap",        "Bronze" },
                { "CopperScrap",        "Copper" },
            };

        public static Dictionary<string, string> __m_oreNameToBarItemName = new Dictionary<string, string>()
            {
                { "CopperOre",          "$item_copper" },
                { "TinOre",             "$item_tin" },
                { "IronScrap",          "$item_iron" },
                { "SilverOre",          "$item_silver" },
                { "BlackMetalScrap",    "$item_blackmetal" },
                { "FlametalOreNew",     "$item_flametal" },
                { "BronzeScrap",        "$item_bronze" },
                { "CopperScrap",        "$item_copper" },
            };


        public static void ConvertMetal(ref ItemDrop.ItemData itemData)
        {
            if (itemData == null)
                return;

            ZNetScene zNetScene = ZNetScene.instance;
            if (zNetScene == null)
            {
                return;
            }

            //                Debug.LogWarning($"ConvertMetal(): Creating {itemData.ToString()} {itemData.m_dropPrefab.name}");

            string cookedMetalName;
            if (__m_oreNameToBarPrefabName.TryGetValue(itemData.m_dropPrefab.name, out cookedMetalName))
            {
                GameObject metalPrefab = zNetScene.GetPrefab(cookedMetalName);
                if (metalPrefab == null)
                {
                    return;
                }

                ItemDrop tempItemDrop = metalPrefab.GetComponent<ItemDrop>();
                if (tempItemDrop != null)
                {
                    int stackSize = itemData.m_stack;

                    // Replace the ore/scrap itemdata with the cooked metal itemdata
                    ItemDrop.ItemData tempItemData = tempItemDrop.m_itemData;

                    itemData = tempItemData.Clone();
                    itemData.m_stack = stackSize;
                    itemData.m_dropPrefab = metalPrefab;
                }
            }
        }

        // Patch GetWeight and GetNonStackedWeight to calculate Ore weights as the bar weights
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
        public class Humanoid_ItemDrop_ItemData_GetWeight_Patch
        {
            static bool Prefix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (__instance == null)
                    return true;

                if (__instance.m_dropPrefab == null)
                    return true;

                string cookedMetalName;
                if (__m_oreNameToBarPrefabName.TryGetValue(__instance.m_dropPrefab.name, out cookedMetalName))
                {
                    //                        Debug.LogWarning($"GetWeight(): Found {__instance.m_dropPrefab.name} => {cookedMetalName}");

                    GameObject ingotPrefab = ZNetScene.instance.GetPrefab(cookedMetalName);
                    ItemDrop.ItemData ingotItemData = ingotPrefab.GetComponent<ItemDrop>().m_itemData;
                    if (ingotItemData != null)
                    {
                        __result = ingotItemData.m_shared.m_weight * __instance.m_stack;
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetNonStackedWeight))]
        public class Humanoid_ItemDrop_ItemData_GetNonStackedWeight_Patch
        {
            static bool Prefix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (__instance == null)
                    return true;

                if (__instance.m_dropPrefab == null)
                    return true;

                string cookedMetalName;
                if (__m_oreNameToBarPrefabName.TryGetValue(__instance.m_dropPrefab.name, out cookedMetalName))
                {
                    //                        Debug.LogWarning($"GetNonStackedWeight(): Found {__instance.m_dropPrefab.name} => {cookedMetalName}");

                    GameObject ingotPrefab = ZNetScene.instance.GetPrefab(cookedMetalName);
                    ItemDrop.ItemData ingotItemData = ingotPrefab.GetComponent<ItemDrop>().m_itemData;
                    if (ingotItemData != null)
                    {
                        __result = ingotItemData.m_shared.m_weight;
                    }

                    return false;
                }

                return true;
            }
        }


        // This is called when items are picked up
        //
        // Insta-Smelt when moving items between inventories
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) })]
        public static class Inventory_AddItem_Patch
        {
            static void Prefix(Inventory __instance, ref ItemDrop.ItemData item, bool __result)
            {
                if (__instance != null && Player.m_localPlayer != null
                    && __instance == Player.m_localPlayer.GetInventory())
                {
                    ConvertMetal(ref item);
                }
            }
        }

        // Trick "CanAddItem" into thinking the ores are bars if you have bars in your inventory already, this fixes an auto-pickup bug
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), new[] { typeof(ItemDrop.ItemData), typeof(int) })]
        public static class Inventory_CanAddItem_Patch
        {
            static bool Prefix(Inventory __instance, ref ItemDrop.ItemData item, int stack, ref bool __result)
            {
                if (__instance != null && Player.m_localPlayer != null
                    && __instance == Player.m_localPlayer.GetInventory())
                {
                    if (item != null && item.m_dropPrefab != null)
                    {
                        string prefabName = item.m_dropPrefab.name;
                        string itemName;

                        if (__m_oreNameToBarItemName.TryGetValue(prefabName, out itemName))
                        {
                            if (stack <= 0)
                            {
                                stack = item.m_stack;
                            }
                            
                            __result = __instance.FindFreeStackSpace(itemName, 0) + (__instance.GetWidth() * __instance.GetHeight() - __instance.GetAllItems().Count) * item.m_shared.m_maxStackSize >= stack;

                            return false;
                        }
                    }
                }

                return true;
            }
        }

        // Called when an item is added to the player's inventory
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
        public class Humanoid_Pickup_Patch
        {
            // Used in Trophy Saga to auto-convert metals on pickup
            static void Prefix(Humanoid __instance, GameObject go, bool autoequip, bool autoPickupDelay, bool __result)
            {
                // Before pickup occurs, see if it's auto-smeltable ore and convert it
                if (__instance == null || __instance != Player.m_localPlayer)
                {
                    return;
                }

                ItemDrop itemDrop = go.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    ConvertMetal(ref itemDrop.m_itemData);
                }
            }
        }
    }
}
