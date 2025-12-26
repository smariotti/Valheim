using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CustomItemMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class CustomItemPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "com.yourname.customitemmod";
        private const string PluginName = "Custom Item Mod";
        private const string PluginVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(PluginGUID);

        void Awake()
        {
            Logger.LogInfo($"{PluginName} {PluginVersion} is loading...");

            harmony.PatchAll();

            Logger.LogInfo($"{PluginName} loaded successfully!");
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        static void Postfix(ObjectDB __instance)
        {
            if (__instance == null || __instance.m_items == null || __instance.m_items.Count == 0)
                return;

            AddCustomItems(__instance);
        }

        private static void AddCustomItems(ObjectDB objectDB)
        {
            // Check if already added
            if (objectDB.GetItemPrefab("MysticGem") != null)
                return;

            // Get the base item (Amber)
            GameObject amberPrefab = objectDB.GetItemPrefab("Amber");
            if (amberPrefab == null)
            {
                Debug.LogError("[CustomItemMod] Could not find Amber prefab!");
                return;
            }

            // Clone the prefab properly
            GameObject customItem = Object.Instantiate(amberPrefab);
            customItem.name = "MysticGem";

            // Modify the ItemDrop component
            ItemDrop itemDrop = customItem.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                // Create new ItemData to avoid reference issues
                ItemDrop.ItemData newItemData = itemDrop.m_itemData.Clone();
                itemDrop.m_itemData = newItemData;

                // Modify shared data
                newItemData.m_shared.m_name = "Mystic Gem";
                newItemData.m_shared.m_description = "A mysterious glowing gem that radiates ancient power.";
                newItemData.m_shared.m_maxStackSize = 50;
                newItemData.m_shared.m_value = 500;
                newItemData.m_shared.m_weight = 0.5f;

                // Optional: Change color
                Transform attachTransform = customItem.transform.Find("attach");
                if (attachTransform != null)
                {
                    MeshRenderer renderer = attachTransform.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        Material[] mats = renderer.materials;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            Material newMat = new Material(mats[i]);
                            newMat.color = new Color(0.5f, 0f, 1f, 1f);
                            if (newMat.HasProperty("_EmissionColor"))
                            {
                                newMat.SetColor("_EmissionColor", new Color(0.3f, 0f, 0.6f, 1f));
                                newMat.EnableKeyword("_EMISSION");
                            }
                            mats[i] = newMat;
                        }
                        renderer.materials = mats;
                    }
                }
            }

            // Properly register with ObjectDB
            objectDB.m_items.Add(customItem);
            objectDB.m_itemByHash[customItem.name.GetStableHashCode()] = customItem;

            Debug.Log("[CustomItemMod] Mystic Gem added successfully!");
        }
    }

    // Register with ZNetScene to prevent null reference errors
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            if (__instance == null)
                return;

            // Get our custom item
            GameObject customItem = ObjectDB.instance?.GetItemPrefab("MysticGem");
            if (customItem != null)
            {
                // Register with ZNetScene
                if (!__instance.m_namedPrefabs.ContainsKey(customItem.name.GetStableHashCode()))
                {
                    __instance.m_namedPrefabs.Add(customItem.name.GetStableHashCode(), customItem);
                    Debug.Log($"[CustomItemMod] Registered {customItem.name} with ZNetScene");
                }
            }
        }
    }

    // Console command
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class Terminal_Patch
    {
        private static bool commandAdded = false;

        static void Postfix()
        {
            if (commandAdded)
                return;

            new Terminal.ConsoleCommand("spawnmysticgem", "Spawns a Mystic Gem", args =>
            {
                Player player = Player.m_localPlayer;
                if (player == null)
                {
                    Debug.Log("[CustomItemMod] No local player found!");
                    return;
                }

                // Create the item directly in inventory
                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab("MysticGem");
                if (itemPrefab != null)
                {
                    ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                    if (itemDrop != null)
                    {
                        // Add using GameObject directly
                        bool added = player.GetInventory().AddItem(itemPrefab, 1);

                        if (added)
                        {
                            player.Message(MessageHud.MessageType.Center, "Mystic Gem added!");
                        }
                        else
                        {
                            player.Message(MessageHud.MessageType.Center, "Inventory full!");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[CustomItemMod] MysticGem prefab not found!");
                }
            });

            commandAdded = true;
        }
    }
}