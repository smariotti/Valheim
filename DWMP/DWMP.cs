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

namespace DWMP
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class DWMP : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.DWMP";
        public const string PluginName = "Dude, Where's My Portal";
        public const string PluginVersion = "0.1.2";
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

            //ConsoleCommand analyzeMap = new ConsoleCommand("analyzemap", "Do map analysis", delegate (ConsoleEventArgs args)
            //{
            //    if (!Game.instance)
            //    {
            //        return true;
            //    }

            //    AnalyzeMap();

            //    return true;
            //});

        }
        //static void AnalyzeMap()
        //{
        //    float minX = -10500;
        //    float maxX = 10500;
        //    float xIncrement = 1000f;

        //    float minY = -10500;
        //    float maxY = 10500;
        //    float yIncrement = 1000f;

        //    //List<Heightmap> heightmaps = Heightmap.s_heightmaps;

        //    //foreach (Heightmap allHeightmap in heightmaps)
        //    //{
        //    //    float x = allHeightmap.m_bounds.extents.x;
        //    //    float y = allHeightmap.m_bounds.extents.y;
        //    //    float z = allHeightmap.m_bounds.extents.z;

        //    //    Debug.LogWarning($"Heightmap: {allHeightmap.name} {allHeightmap.m_bounds.extents}");
        //    //}

        //    if (Heightmap.s_heightmaps != null)
        //    {
        //        List<Heightmap> heightmaps = Heightmap.s_heightmaps;
        //        foreach (Heightmap hmap in heightmaps)
        //        {
        //            Debug.LogWarning($"{hmap.name}");
        //        }
        //    }

        //    for (float y = minY; y < maxY; y += yIncrement)
        //    {
        //        for (float x = minX; x < maxX; x += xIncrement)
        //        {
        //            Vector3 pos = new Vector3(x, 0, y);
        //            Vector3 normal = new Vector3();
        //            Heightmap.Biome biome;
        //            Heightmap.BiomeArea biomeArea;
        //            Heightmap hmap;

        //            ZoneSystem.instance.GetGroundData(ref pos, out normal, out biome, out biomeArea, out hmap);

        //            float height = 0;

        //            if (hmap.GetWorldHeight(pos, out height))
        //            {
        //                AddPortalPin(pos, height.ToString());
        //            }
        //        }
        //    }
        //}

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
