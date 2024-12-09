using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ScytheEverything
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ScytheEverything : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.ScytheEverything";
        public const string PluginName = "Scythe Everything";
        public const string PluginVersion = "0.1.0";
        private readonly Harmony harmony = new Harmony(PluginGUID);
        private void Awake()
        {
            // Patch with Harmony
            harmony.PatchAll();
        }

        //[HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
        //public class Patch_Pickable_Awake
        //{
        //    static void Postfix(Pickable __instance)
        //    {
        //        if (__instance != null && __instance.m_itemPrefab != null)
        //        {
        //            string name = __instance.m_itemPrefab.name;
        //            if (name == "MushroomMagecap" || name == "MushroomJotunPuffs")
        //            {
        //                __instance.m_harvestable = true;
        //                Debug.LogWarning($"Set Harvestable: {name}");

        //            }
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        public class Patch_Attack_DoMeleeAttack
        {
            static void Postfix(Attack __instance)
            {
                if (!__instance.m_harvest)
                {
                    return;
                }

                if (__instance.GetWeapon() != null)
                {
                    ItemDrop.ItemData weapon = __instance.GetWeapon();
                    if (weapon == null || weapon.m_dropPrefab.name != "Scythe")
                    {
                        return;
                    }

                    //// Modify the damage properties to include chop damage
                    ItemDrop.ItemData.SharedData shared = weapon.m_shared;

                    float bonkersDamage = 1000000;

                    shared.m_attack.m_attackRange = 20f;
                    shared.m_attack.m_harvestRadius = 20f;
                    shared.m_attack.m_harvestRadiusMaxLevel = 20f;

                    //                    shared.m_attack.m_hitTerrain = true;

                    shared.m_damages.m_damage = bonkersDamage;

                    //shared.m_damages.m_chop = bonkersDamage; // Adjust the chop damage value as needed
                    //shared.m_damages.m_pickaxe = bonkersDamage;
                    //shared.m_damages.m_fire = bonkersDamage;
                    //shared.m_damages.m_blunt = bonkersDamage;
                    //shared.m_damages.m_fire = bonkersDamage;
                    //shared.m_damages.m_frost = bonkersDamage;
                    //shared.m_damages.m_lightning = bonkersDamage;
                    //shared.m_damages.m_pierce = bonkersDamage;
                    //shared.m_damages.m_poison = bonkersDamage;
                    //shared.m_damages.m_slash = bonkersDamage;
                    //shared.m_damages.m_pierce = bonkersDamage;

                    ////                    shared.m_skillType = Skills.SkillType.WoodCutting;
                    shared.m_toolTier = 9;



//                    Debug.LogWarning($"DoMeleeAttack() harvest: {shared.m_attack.m_harvestRadiusMaxLevel} {weapon.m_shared.m_name}");
                }
            }
        }
    }
}
