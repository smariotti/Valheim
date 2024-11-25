using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using HarmonyLib;
using UnityEngine;

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
    }
}
