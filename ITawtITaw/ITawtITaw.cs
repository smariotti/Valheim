﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace ITawtITaw
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ITawtITaw : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.ITIT";
        public const string PluginName = "I Tawt I Taw";
        public const string PluginVersion = "0.1.0";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        static public Hashtable m_aiObjects = new Hashtable();
        static float m_checkTimer = 0;
        static float m_checkFrequency = 2;
        static bool m_showEverything = false;
        static List<string> m_listOfPuddyTats = new List<string>();

        ConfigEntry<bool> m_configShowEverything;
        ConfigEntry<string> m_configListOfPuddyTats;
        ConfigEntry<float> m_configCheckFrequency;

        private void Awake()
        {
            // Patch with Harmony
            harmony.PatchAll();

            Debug.LogWarning("I Tawt I Taw is running.");

            m_configListOfPuddyTats = Config.Bind("General",
                                                   "ListOfPuddyTats",
                                                   "Serpent,BonemawSerpent",
                                                   "List of Characters to watch for, separated by commas. Defaults to 'Serpent,BonemawSerpent'");

            m_configShowEverything = Config.Bind("General",
                                                 "ShowEverything",
                                                 false,
                                                 "When enabled, will display all nearby characters on the minimap as they Enable/Disable. Default: FALSE");

            m_configCheckFrequency = Config.Bind("General",
                                                "CheckFrequency",
                                                2.0f, // seconds
                                                "Number of seconds between checks to see if you taw a puddy tat");


            m_showEverything = m_configShowEverything.Value;
            m_checkFrequency = m_configCheckFrequency.Value;
            string puddyTatList = m_configListOfPuddyTats.Value;

            m_listOfPuddyTats = puddyTatList.Split(',').ToList();
        }

        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.OnEnable))]
        public static class BaseAI_OnEnable_Patch
        {
            public static void Postfix(BaseAI __instance)
            {
                //                Debug.LogError($"BaseAI.OnEnable() Added BaseAI {__instance.gameObject} ({m_aiObjects.Count})");
                if (m_aiObjects.ContainsKey(__instance))
                {
                    return;
                }

                if (m_showEverything)
                {
                    Minimap.PinData newPin = Minimap.instance.AddPin(__instance.gameObject.transform.position, Minimap.PinType.Icon3, __instance.gameObject.name, save: true, isChecked: false);
                    m_aiObjects.Add(__instance, newPin);
                }
                else
                {
                    m_aiObjects.Add(__instance, null);
                }
            }
        }

        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.OnDisable))]
        public static class BaseAI_OnDestroy_Patch
        {
            public static void Postfix(BaseAI __instance)
            {
//                Debug.LogError($"BaseAI.OnDisable() Removing BaseAI {__instance.gameObject} ({m_aiObjects.Count})");
                Minimap.PinData pin = m_aiObjects[__instance] as Minimap.PinData;
                if (pin != null && m_showEverything)
                {
                    Minimap.instance.RemovePin(pin);
                }

                m_aiObjects.Remove(__instance);
            }
        }


        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.UpdateAI))]
        public static class BaseAI_UpdateAI_Patch
        {
            public static void Postfix(BaseAI __instance)
            {
                m_checkTimer += Time.deltaTime;
                if (m_checkTimer > m_checkFrequency)
                {
                    Character character = __instance.GetComponent<Character>();
                    if (character)
                    {
                        if (m_showEverything || m_listOfPuddyTats.Contains(character.GetHoverName()))
                        {
                            if (Player.m_localPlayer != null)
                            {
                                Vector3 playerPos = Player.m_localPlayer.transform.position;
                                Vector3 charPos = character.transform.position;
                                float distanceToCharacter = Vector3.Distance(Player.m_localPlayer.transform.position, character.transform.position);
                                if (m_showEverything || (distanceToCharacter < Minimap.instance.m_exploreRadius))
                                {
                                    Vector3 toChar = Vector3.Normalize(charPos - playerPos);
                                    Vector3 lookDir = Player.m_localPlayer.GetLookDir();
                                    float dot = Vector3.Dot(lookDir, toChar);
                                    float dotAngle = (float)Math.Acos((float)dot);
                                    float angleDegrees = dotAngle * 180.0f / (float)Math.PI;

                                    if (m_showEverything || (dot > 0.0f && angleDegrees < GameCamera.instance.m_fov/2))
                                    {
//                                        Debug.LogWarning($"{character.GetHoverName()} toChar {toChar} look {lookDir} dot {dot} distance {distanceToCharacter} dotAngle {dotAngle} degrees {angleDegrees}");
                                        if (m_aiObjects.ContainsKey(__instance))
                                        {
                                            Minimap.PinData pin = m_aiObjects[__instance] as Minimap.PinData;
                                            if (pin != null)
                                            {
                                                Minimap.instance.RemovePin(pin);
                                            }
                                            m_aiObjects.Remove(__instance);
                                        }

                                        Minimap.PinData newPin = Minimap.instance.AddPin(__instance.gameObject.transform.position, Minimap.PinType.Icon3, character.GetHoverName(), save: true, isChecked: false);
                                        m_aiObjects.Add(__instance, newPin);
                                    }
                                }
                            }
                        }
                    }

                    m_checkTimer = 0;
                }
            }
        }
    }
}

/*

namespace DWMP
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class DWMP : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.DWMP";
        public const string PluginName = "Dude, Where's My Portal";
        public const string PluginVersion = "0.1.0";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        List<Minimap.PinData> __m_pins = new List<Minimap.PinData>();


        private void Awake()
        {
            // Patch with Harmony
            harmony.PatchAll();
        }


        static void AddPortalPin(Vector3 pos, string text = "")
        {

            Minimap.PinData newPin = Minimap.instance.AddPin(pos, Minimap.PinType.Icon4, text, save: true, isChecked: false);
        }

        static void RemovePortalPin(Vector3 pos)
        {
            Minimap.instance.RemovePin(pos, 0.1f);
        }

        static void RenamePortalPin(Vector3 pos, string text)
        {
            RemovePortalPin(pos);
            AddPortalPin(pos, text);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece), new[] { typeof(Piece), typeof(Vector3), typeof(Quaternion), typeof(bool) })]
        public static class Player_PlacePiece_Patch
        {
            public static void Postfix(Player __instance, Piece piece, Vector3 pos, Quaternion rot, bool doAttack)
            {
                if (piece != null && (piece.name == "portal_wood" || piece.name == "portal_stone"))
                {
                    //                    Debug.LogWarning($"Placed Portal name: {piece.name} Pos: {pos}");

                    AddPortalPin(pos);
                }
            }
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
        public static class Piece_DropResources_Patch
        {
            public static void Postfix(Piece __instance)
            {
                Vector3 pos = __instance.transform.position;
                TeleportWorld tpWorld = __instance.GetComponent<TeleportWorld>();
                if (tpWorld != null)
                {
                    //                    Debug.LogWarning($"Piece.DropResources(): name: {__instance.name} Pos: {pos}");
                    RemovePortalPin(pos);
                }
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.SetText))]
        public static class TeleportWorld_SetText_Patch
        {
            public static void Postfix(TeleportWorld __instance, string text)
            {
                if (__instance != null)
                {
                    //                    Debug.LogWarning($"TeleportWorld.SetText(): name: {__instance.name} Pos: {__instance.transform.position} text: '{text}' ");
                    RenamePortalPin(__instance.transform.position, text);
                }
            }
        }
    }
}
*/