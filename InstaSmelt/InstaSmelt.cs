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

            AddConsoleCommands();
        }

        static public void AddConsoleCommands()
        {
            ConsoleCommand createPinOnTeleport = new ConsoleCommand("createpinonteleport", "Add a portal pin to the minimap for each portal you pass through", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    return true;
                }

                __m_createPinOnTeleport = !__m_createPinOnTeleport;

                if (Chat.instance)
                {
                    if (__m_createPinOnTeleport)
                    {
                        Chat.instance.AddString("CreatePinOnTeleport ENABLED!");
                    }
                    else
                    {
                        Chat.instance.AddString("CreatePinOnTeleport DISABLED!");
                    }
                }

                return true;
            });

        }

        static void AddPortalPin(Vector3 pos, string text="")
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

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        public static class TeleportWorld_Teleport_Patch
        {
            public static void Postfix(TeleportWorld __instance, Player player)
            {
                if (__instance != null)
                {
                    if (__m_createPinOnTeleport)
                    {
 //                       Debug.LogWarning($"TeleportWorld.Teleport(): name: {__instance.GetText()} Pos: {__instance.transform.position}");
                        RenamePortalPin(__instance.transform.position, __instance.GetText());
                    }
                }
            }
        }
    }
}
