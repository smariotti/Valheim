using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using static Terminal;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Threading.Tasks;
using static CharacterDrop;
using UnityEngine.EventSystems;
using static Skills;
using System.Linq;
using System.Xml.Serialization;
using BepInEx.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
 
namespace SeasonheimMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public partial class Seasonheim : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.SeasonsheimMod";
        public const string PluginName = "SeasonsheimMod";


        public const string PluginVersion = "0.1.2";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        // Configuration variables
        private const Boolean DUMP_TROPHY_DATA = false;

        static public Seasonheim __m_seasonheimMod;

        public enum Biome
        {
            Meadows = 0,
            Forest = 1,
            Swamp = 2,
            Mountains = 3,
            Plains = 4,
            Mistlands = 5,
            Ashlands = 6,
            Ocean = 7,
            Hildir = 8,
            Bogwitch = 9,
        };


        public struct TrophyHuntData
        {
            public TrophyHuntData(string name, string prettyName, Biome biome, int value, float dropPercent, List<string> enemies)
            {
                m_name = name;
                m_prettyName = prettyName;
                m_biome = biome;
                m_value = value;
                m_dropPercent = dropPercent;
                m_enemies = enemies;
            }

            public string m_name;
            public string m_prettyName;
            public Biome m_biome;
            int m_value;
            public float m_dropPercent;
            public List<string> m_enemies;

            public int GetCurGameModeTrophyScoreValue()
            {
                int points = m_value;

                return points;
            }
        }
        const float DEFAULT_SCORE_FONT_SIZE = 36;
         

        static public TrophyHuntData[] __m_trophyHuntData = new TrophyHuntData[]
        {//                     Trophy Name                     Pretty Name         Biome               Score   Drop%   Dropping Enemy Name(s)
            new TrophyHuntData("TrophyAbomination",             "Abomination",      Biome.Swamp,        1,   50,     new List<string> { "$enemy_abomination" }),
            new TrophyHuntData("TrophyAsksvin",                 "Asksvin",          Biome.Ashlands,     1,   50,     new List<string> { "$enemy_asksvin" }),
            new TrophyHuntData("TrophyBlob",                    "Blob",             Biome.Swamp,        1,   10,     new List<string> { "$enemy_blob",       "$enemy_blobelite" }),
            new TrophyHuntData("TrophyBoar",                    "Boar",             Biome.Meadows,      1,   15,     new List<string> { "$enemy_boar" }),
            new TrophyHuntData("TrophyBjorn",                   "Bear",             Biome.Forest,       1,   10,      new List<string>  { "$enemy_bjorn" }),
            new TrophyHuntData("TrophyBjornUndead",             "Vile",             Biome.Plains,       1,   15,      new List<string>  { "$enemy_unbjorn" }),
            new TrophyHuntData("TrophyBonemass",                "Bonemass",         Biome.Swamp,        1,   100,    new List<string> { "$enemy_bonemass" }),
            new TrophyHuntData("TrophyBonemawSerpent",          "Bonemaw",          Biome.Ashlands,     1,   33,     new List<string> { "$enemy_bonemawserpent" }),
            new TrophyHuntData("TrophyCharredArcher",           "Charred Archer",   Biome.Ashlands,     1,   5,      new List<string> { "$enemy_charred_archer" }),
            new TrophyHuntData("TrophyCharredMage",             "Charred Warlock",  Biome.Ashlands,     1,   5,      new List<string> { "$enemy_charred_mage" }),
            new TrophyHuntData("TrophyCharredMelee",            "Charred Warrior",  Biome.Ashlands,     1,   5,      new List<string> { "$enemy_charred_melee" }),
            new TrophyHuntData("TrophyCultist",                 "Cultist",          Biome.Mountains,    1,   10,     new List<string> { "$enemy_fenringcultist" }),
            new TrophyHuntData("TrophyCultist_Hildir",          "Geirrhafa",        Biome.Hildir,       1,   100,    new List<string> { "$enemy_fenringcultist_hildir" }),
            new TrophyHuntData("TrophyDeathsquito",             "Deathsquito",      Biome.Plains,       1,   5,      new List<string> { "$enemy_deathsquito" }),
            new TrophyHuntData("TrophyDeer",                    "Deer",             Biome.Meadows,      1,   50,     new List<string> { "$enemy_deer" }),
            new TrophyHuntData("TrophyDragonQueen",             "Moder",            Biome.Mountains,    1,   100,    new List<string> { "$enemy_dragon" }),
            new TrophyHuntData("TrophyDraugr",                  "Draugr",           Biome.Swamp,        1,   10,     new List<string> { "$enemy_draugr" }),
            new TrophyHuntData("TrophyDraugrElite",             "Draugr Elite",     Biome.Swamp,        1,   10,     new List<string> { "$enemy_draugrelite" }),
            new TrophyHuntData("TrophyDvergr",                  "Dvergr",           Biome.Mistlands,    1,   5,      new List<string> { "$enemy_dvergr",     "$enemy_dvergr_mage" }),
            new TrophyHuntData("TrophyEikthyr",                 "Eikthyr",          Biome.Meadows,      1,   100,    new List<string> { "$enemy_eikthyr" }),
            new TrophyHuntData("TrophyFader",                   "Fader",            Biome.Ashlands,     1,   100,    new List<string> { "$enemy_fader" }),
            new TrophyHuntData("TrophyFallenValkyrie",          "Fallen Valkyrie",  Biome.Ashlands,     1,   5,      new List<string> { "$enemy_fallenvalkyrie" }),
            new TrophyHuntData("TrophyFenring",                 "Fenring",          Biome.Mountains,    1,   10,     new List<string> { "$enemy_fenring" }),
            new TrophyHuntData("TrophyFrostTroll",              "Troll",            Biome.Forest,       1,   50,     new List<string> { "$enemy_troll" }),
            new TrophyHuntData("TrophyGhost",                   "Ghost",            Biome.Forest,       1,   10,     new List<string> { "$enemy_ghost" }),
            new TrophyHuntData("TrophyGjall",                   "Gjall",            Biome.Mistlands,    1,   30,     new List<string> { "$enemy_gjall" }),
            new TrophyHuntData("TrophyGoblin",                  "Fuling",           Biome.Plains,       1,   10,     new List<string> { "$enemy_goblin" }),
            new TrophyHuntData("TrophyGoblinBrute",             "Fuling Berserker", Biome.Plains,       1,   5,      new List<string> { "$enemy_goblinbrute" }),
            new TrophyHuntData("TrophyGoblinBruteBrosBrute",    "Thungr",           Biome.Hildir,       1,   100,    new List<string> { "$enemy_goblinbrute_hildircombined" }),
            new TrophyHuntData("TrophyGoblinBruteBrosShaman",   "Zil",              Biome.Hildir,       1,   100,    new List<string> { "$enemy_goblin_hildir" }),
            new TrophyHuntData("TrophyGoblinKing",              "Yagluth",          Biome.Plains,       1,   100,    new List<string> { "$enemy_goblinking" }),
            new TrophyHuntData("TrophyGoblinShaman",            "Fuling Shaman",    Biome.Plains,       1,   10,     new List<string> { "$enemy_goblinshaman" }),
            new TrophyHuntData("TrophyGreydwarf",               "Greydwarf",        Biome.Forest,       1,   5,      new List<string> { "$enemy_greydwarf" }),
            new TrophyHuntData("TrophyGreydwarfBrute",          "Greydwarf Brute",  Biome.Forest,       1,   10,     new List<string> { "$enemy_greydwarfbrute" }),
            new TrophyHuntData("TrophyGreydwarfShaman",         "Greydwarf Shaman", Biome.Forest,       1,   10,     new List<string> { "$enemy_greydwarfshaman" }),
            new TrophyHuntData("TrophyGrowth",                  "Growth",           Biome.Plains,       1,   10,     new List<string> { "$enemy_blobtar" }),
            new TrophyHuntData("TrophyHare",                    "Misthare",         Biome.Mistlands,    1,   5,      new List<string> { "$enemy_hare" }),
            new TrophyHuntData("TrophyHatchling",               "Drake",            Biome.Mountains,    1,   10,     new List<string> { "$enemy_thehive",    "$enemy_drake" }),
            new TrophyHuntData("TrophyLeech",                   "Leech",            Biome.Swamp,        1,   10,     new List<string> { "$enemy_leech" }),
            new TrophyHuntData("TrophyLox",                     "Lox",              Biome.Plains,       1,   10,     new List<string> { "$enemy_lox" }),
            new TrophyHuntData("TrophyMorgen",                  "Morgen",           Biome.Ashlands,     1,   5,      new List<string> { "$enemy_morgen" }),
            new TrophyHuntData("TrophyNeck",                    "Neck",             Biome.Meadows,      1,   5,      new List<string> { "$enemy_neck" }),
            new TrophyHuntData("TrophySeeker",                  "Seeker",           Biome.Mistlands,    1,   10,     new List<string> { "$enemy_seeker" }),
            new TrophyHuntData("TrophySeekerBrute",             "Seeker Soldier",   Biome.Mistlands,    1,   5,      new List<string> { "$enemy_seekerbrute" }),
            new TrophyHuntData("TrophySeekerQueen",             "The Queen",        Biome.Mistlands,    1,   100,    new List<string> { "$enemy_seekerqueen" }),
            new TrophyHuntData("TrophySerpent",                 "Serpent",          Biome.Ocean,        1,   33,     new List<string> { "$enemy_serpent" }),
            new TrophyHuntData("TrophySGolem",                  "Stone Golem",      Biome.Mountains,    1,   5,      new List<string> { "$enemy_stonegolem" }),
            new TrophyHuntData("TrophySkeleton",                "Skeleton",         Biome.Forest,       1,   10,     new List<string> { "$enemy_skeleton" }),
            new TrophyHuntData("TrophySkeletonHildir",          "Brenna",           Biome.Hildir,       1,   100,    new List<string> { "$enemy_skeletonfire" }),
            new TrophyHuntData("TrophySkeletonPoison",          "Rancid Remains",   Biome.Forest,       1,   10,     new List<string> { "$enemy_skeletonpoison" }),
            new TrophyHuntData("TrophySurtling",                "Surtling",         Biome.Swamp,        1,   5,      new List<string> { "$enemy_surtling" }),
            new TrophyHuntData("TrophyTheElder",                "The Elder",        Biome.Forest,       1,   100,    new List<string> { "$enemy_gdking" }),
            new TrophyHuntData("TrophyTick",                    "Tick",             Biome.Mistlands,    1,   5,      new List<string> { "$enemy_tick" }),
            new TrophyHuntData("TrophyUlv",                     "Ulv",              Biome.Mountains,    1,   5,      new List<string> { "$enemy_ulv" }),
            new TrophyHuntData("TrophyVolture",                 "Volture",          Biome.Ashlands,     1,   50,     new List<string> { "$enemy_volture" }),
            new TrophyHuntData("TrophyWolf",                    "Wolf",             Biome.Mountains,    1,   10,     new List<string> { "$enemy_wolf" }),
            new TrophyHuntData("TrophyWraith",                  "Wraith",           Biome.Swamp,        1,   5,      new List<string> { "$enemy_wraith" }),
            new TrophyHuntData("TrophyKvastur",                 "Kvastur",          Biome.Bogwitch,     1,   100,    new List<string> { "$enemy_kvastur" })
        };



        // UI Elements
        static GameObject __m_scoreTextElement = null;
        static GameObject __m_scoreBGElement = null;
        static TMP_FontAsset __m_globalFontObject = null;

        static TextMeshProUGUI AddTextMeshProComponent(GameObject toThisObject)
        {
            TextMeshProUGUI textMeshComponent = toThisObject.AddComponent<TextMeshProUGUI>();
            textMeshComponent.font = __m_globalFontObject;
            textMeshComponent.material = __m_globalFontObject.material;

            return textMeshComponent;
        }

        // Trophy Icons
        static List<GameObject> __m_iconList = null;

        // Trophy Sprite
        static Sprite __m_trophySprite = null;

        // Trophy Display Settings
        static float __m_baseTrophyScale = 1.4f;
        static float __m_userIconScale = 1.2f;
        static float __m_userTextScale = 1.0f;
        static float __m_userTrophySpacing = 1.0f;

        // Cache for detecting newly arrived trophies and flashing the new ones
        static List<string> __m_trophyCache = new List<string>();

        // Player Path / Event Tracking
        static bool __m_pathAddedToMinimap = false;                                // are we showing the path on the minimap?
        static List<Minimap.PinData> __m_pathPins = new List<Minimap.PinData>();   // keep track of the special pins we add to the minimap so we can remove them
        static List<TrackEvent> __m_pendingEvents = new List<TrackEvent>();         // pending events waiting to be sent

        static bool __m_collectingPlayerPath = false;
        static float __m_playerPathCollectionInterval = 5.0f;
        static float __m_minPathPlayerMoveDistance = 10.0f;

        // Trophy Pins
        public class TrophyPin
        {
            public Vector3 m_pos;
            public string m_trophyName;
        }

        static List<TrophyPin> __m_trophyPins = new List<TrophyPin>();

        public enum TrophyGameMode
        {
            Seasonheim2026,
            Max
        }


        // TrophyHuntMod current Game Mode
        static TrophyGameMode __m_trophyGameMode = TrophyGameMode.Seasonheim2026;


        static public TrophyGameMode GetGameMode() { return __m_trophyGameMode; }
        static public string GetGameModeString(TrophyGameMode mode)
        {
            string modeString = "Unknown";
            switch (mode)
            {
                case TrophyGameMode.Seasonheim2026: modeString = "<color=orange>Seasonsheim 2026</color>"; break;
                default:
                    return "Unknown";
            }

            return modeString;
        }

        static bool __m_ignoreInvalidateUIChanges = false;

        // Currently computed score value
        static int __m_playerCurrentScore = 0;

        // Format per event: "<tag>=<secs>@<x>,<y>,<z>|<extra>;"
        // F=FirstInput  P=Snapshot  J=Jump(portal/respawn)  T=Trophy  D=Death  L=Logout  S=SlashDie
        [Serializable]
        public class TrackEvent
        {
            public string tag;   // single-letter code
            public int secs;     // seconds since tournament start
            public int x, y, z; // world position (rounded)
            public string extra; // optional extra data after the first |
        }

        private void Awake()
        {
            __m_seasonheimMod = this;

            // Patch with Harmony
            harmony.PatchAll();
        }


        private void Start()
        {
        }


        public static bool __m_showingTrophies = true;

        public static void ShowTrophies(bool show)
        {
            foreach (GameObject trophyIcon in __m_iconList)
            {
                trophyIcon.SetActive(show);
            }
        }

        public static TextMeshProUGUI __m_mainMenuText = null;

        public static string GetGameModeNameText()
        {
            string gameModeText = "???";

            switch (GetGameMode())
            {
                case TrophyGameMode.Seasonheim2026:
                    gameModeText = "Seasonsheim 2026";
                    break;
            }

            return gameModeText;
        }

        public static string GetGameModeText()
        {
            string text = "";

            text += $"<align=\"left\"><size=18>\nGame Mode: {GetGameModeString(GetGameMode())}</size>\n";
            switch (GetGameMode())
            {
                case TrophyGameMode.Seasonheim2026:
                    // Trophy Hunt game mode
                    break;
            }


            return text;
        }

        public static string GetTrophyHuntMainMenuText()
        {
            string textStr = $"<b><size=44><color=#FFB75B>Seasonsheim!</color></size></b>\n<size=18>           (Version: {PluginVersion})</size>";
            textStr += GetGameModeText();
            return textStr;
        }

        public static void ShowPlayerPath(bool showPlayerPath)
        {
            if (!showPlayerPath)
            {
                foreach (Minimap.PinData pinData in __m_pathPins)
                {
                    Minimap.instance.RemovePin(pinData);
                }

                __m_pathPins.Clear();

                __m_pathAddedToMinimap = false;
            }
            else
            {
                __m_pathPins.Clear();

                // Show all recorded positions as path markers (waypoints, events, snapshots)
                const float PATH_DISPLAY_MIN_DISTANCE = 50.0f;
                Vector3 lastPinned = Vector3.positiveInfinity;
                foreach (TrackEvent e in __m_pendingEvents)
                {
                    Vector3 pos = new Vector3(e.x, e.y, e.z);
                    if (Vector3.Distance(pos, lastPinned) < PATH_DISPLAY_MIN_DISTANCE)
                        continue;
                    lastPinned = pos;
                    Minimap.PinData newPin = Minimap.instance.AddPin(pos, Minimap.PinType.Icon3, "", save: false, isChecked: false);
                    __m_pathPins.Add(newPin);
                }

                __m_pathAddedToMinimap = true;
            }

        }

        // OnSpawned() is required instead of Awake
        //   this is because at Awake() time, Player.m_trophyList and Player.m_localPlayer haven't been initialized yet
        //
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public class Player_OnSpawned_Patch
        {
            static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                //                Debug.LogWarning("Local Player is Spawned!");

                // Sort the trophies by biome, score and name
                Array.Sort<TrophyHuntData>(__m_trophyHuntData, (x, y) => x.m_biome.CompareTo(y.m_biome) * 100000 + x.GetCurGameModeTrophyScoreValue().CompareTo(y.GetCurGameModeTrophyScoreValue()) * 10000 + x.m_name.CompareTo(y.m_name));

                //                Array.Sort<ConsumableData>(__m_cookedFoodData, (x, y) => x.m_regen.CompareTo(y.m_regen) * 100 + x.m_health.CompareTo(y.m_health) + x.m_stamina.CompareTo(y.m_stamina) + x.m_eitr.CompareTo(y.m_eitr) + x.m_prefabName.CompareTo(y.m_prefabName));

                // Dump loaded trophy data
                if (DUMP_TROPHY_DATA)
                {
                    foreach (var t in __m_trophyHuntData)
                    {
                        Debug.LogWarning($"{t.m_biome.ToString()}, {t.m_name}, {t.GetCurGameModeTrophyScoreValue()}");
                    }
                }

                // Cache already discovered trophies
                __m_trophyCache = Player.m_localPlayer.GetTrophies();


                // Create all the UI elements we need for this mod
                BuildUIElements();


                string workingDirectory = Directory.GetCurrentDirectory();

                // Do initial update of all UI elements to the current state of the game
                UpdateModUI(Player.m_localPlayer);

                ShowPlayerPath(false);

                StartCollectingPlayerPath();

                // Scan through minimap pins and fix them if they're trophy pins
                foreach (Minimap.PinData pin in Minimap.instance.m_pins)
                {
                    if (pin.m_name.StartsWith("Trophy"))
                    {
                        pin.m_icon = GetTrophySprite(pin.m_name);
                    }
                }

                FixTrophyPins();

                Debug.LogWarning($"Loading into Game Mode: {GetGameModeString(GetGameMode())}");


                // In case we've been playing around with Pacifist and have changed
                // recipes, do a recipe refresh on load in just in case things haven't
                // updated
                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    player.UpdateKnownRecipesList();
                }
            }
        }

        static public IEnumerator WaitForFirstInput()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                yield break;

            // Wait until the fly-in intro is finished
            while (player.m_intro)
                yield return null;

            // Now wait for the first real key/button/mouse input
            while (!Input.anyKeyDown && player.GetMoveDir() == Vector3.zero)
                yield return null;
        }

        public static void RaiseAllPlayerSkills(float skillLevel)
        {
            // Access the player's skills
            if (!Player.m_localPlayer)
            {
                return;
            }

            Skills skills = Player.m_localPlayer.GetSkills();

            // Loop through all the skills and set them to 10
            foreach (var skill in skills.m_skillData)
            {
                if (skill.Value.m_level < skillLevel)
                {
                    skill.Value.m_level = skillLevel;
                }
            }
        }



        public static void InitializeTrackedDataForNewPlayer()
        {
            //            Debug.LogError("INITIALIZING TRACKED DATA FOR NEW PLAYER");


            //                __m_gameTimerVisible = false;
//            TimerStart();

            __m_collectingPlayerPath = false;

            __m_trophyPins.Clear();
        }



        public static int CalculateTrophyPoints(bool displayToLog = false)
        {
            int score = 0;
            foreach (TrophyHuntData thData in __m_trophyHuntData)
            {
                if (__m_trophyCache.Contains(thData.m_name))
                {
                    score += thData.GetCurGameModeTrophyScoreValue();
                }
            }

            return score;
        }


        static void BuildUIElements()
        {
            if (Hud.instance == null || Hud.instance.m_rootObject == null)
            {
                Debug.LogError("TrophyHuntMod: Hud.instance.m_rootObject is NOT valid");

                return;
            }

            if (__m_scoreTextElement == null)
            {
                Transform healthPanelTransform = Hud.instance.transform.Find("hudroot/healthpanel");
                if (healthPanelTransform == null)
                {
                    Debug.LogError("Health panel transform not found.");

                    return;
                }

                if (__m_scoreTextElement == null)
                {
                    __m_scoreTextElement = CreateScoreTextElement(healthPanelTransform);
                }

                __m_iconList = new List<GameObject>();

                
                CreateTrophyIconElements(healthPanelTransform, __m_trophyHuntData, __m_iconList);

                // Create the hover text object
                CreateTrophyTooltip();

                CreateGameModeElements();

                SetScoreTextElementColor(Color.yellow);

                CreateScoreTooltip();
            }
        }

        public static void CreateGameModeElements()
        {
            Transform miniMapTransform = Hud.instance.transform.Find("hudroot/healthpanel");
            if (miniMapTransform == null)
            {
                Debug.LogError("Minimap transform not found.");

                return;
            }

            // Create a new GameObject for the text
            GameObject gameModeTextObject = new GameObject("GameModeText");

            // Set the parent to the HUD canvas
            gameModeTextObject.transform.SetParent(miniMapTransform);

            // Add RectTransform component for positioning
            RectTransform rectTransform = gameModeTextObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 100);
            rectTransform.anchoredPosition = new Vector2(-40, 260);
            rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            rectTransform.rotation = Quaternion.Euler(0, 0, 90);
            TMPro.TextMeshProUGUI tmText = AddTextMeshProComponent(gameModeTextObject);
            tmText.text = GetGameModeString(GetGameMode()) + $"<size=14>\n<color=white>v{PluginVersion}</color></size>";

            tmText.fontSize = 22;
            tmText.fontStyle = FontStyles.Bold;
            tmText.color = Color.yellow;
            tmText.raycastTarget = false;
            tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmText.outlineColor = Color.black;
            tmText.outlineWidth = 0.125f; // Adjust the thickness
            tmText.verticalAlignment = VerticalAlignmentOptions.Top;
            tmText.horizontalAlignment = HorizontalAlignmentOptions.Left;
        }
         
   
        static GameObject CreateScoreTextElement(Transform parentTransform)
        {
            __m_scoreBGElement = new GameObject("ScoreBG");
            __m_scoreBGElement.transform.SetParent(parentTransform);

            Vector2 scorePos = new Vector2(100, 90);
            Vector2 scoreSize = new Vector2(300, 42);

            RectTransform bgTransform = __m_scoreBGElement.AddComponent<RectTransform>();
            Vector2 scorePosBg = new Vector2(-70, 95);
            Vector2 scoreSizeBg = new Vector2(230, 72);
            bgTransform.sizeDelta = scoreSizeBg;
            bgTransform.anchoredPosition = scorePosBg;
            bgTransform.localScale = new Vector3(__m_userTextScale, __m_userTextScale, __m_userTextScale);
             
            __m_scoreBGElement.SetActive(false);

            //// Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_scoreBGElement.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 1f); // Semi-transparent black background

            // Create a new GameObject for the text
            GameObject scoreTextElement = new GameObject("ScoreText");
             
            // Set the parent to the HUD canvas
            scoreTextElement.transform.SetParent(parentTransform);


            // Add RectTransform component for positioning
            RectTransform rectTransform = scoreTextElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = scoreSize;
            rectTransform.anchoredPosition = scorePos;
            rectTransform.localScale = new Vector3(__m_userTextScale, __m_userTextScale, __m_userTextScale);

            int scoreValue = 9999;

            TMPro.TextMeshProUGUI tmText = AddTextMeshProComponent(scoreTextElement);

            tmText.text = $"Score: {scoreValue}";
            tmText.fontSize = DEFAULT_SCORE_FONT_SIZE;
            //                tmText.fontStyle = FontStyles.Bold;
            tmText.color = Color.yellow;
            tmText.alignment = TextAlignmentOptions.MidlineLeft;
            tmText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            tmText.raycastTarget = true;
            tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmText.outlineColor = Color.black;
            tmText.outlineWidth = 0.12f; // Adjust the thickness
                                          //               text.enableAutoSizing = true;
            AddTooltipTriggersToScoreObject(scoreTextElement);

            return scoreTextElement;
        }

        static public void SetScoreTextElementColor(Color color)
        {
            if (__m_ignoreInvalidateUIChanges)
            {
                return;
            }

            if (__m_scoreTextElement != null)
            {
                TMPro.TextMeshProUGUI tmText = __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>();
                tmText.color = color;
            }

            //            UpdateModUI(Player.m_localPlayer);
        }

        static GameObject CreateTrophyIconElement(Transform parentTransform, Sprite iconSprite, string iconName, Biome iconBiome, int index)
        {

            int iconSize = 33;
            int iconBorderSize = -1;
            int xOffset = -50;
            int yOffset = -140;

            int biomeIndex = (int)iconBiome;

            // Create a new GameObject for the icon
            GameObject iconElement = new GameObject(iconName);
            iconElement.transform.SetParent(parentTransform);

            // Add RectTransform component for positioning Sprite
            RectTransform iconRectTransform = iconElement.AddComponent<RectTransform>();
            iconRectTransform.sizeDelta = new Vector2(iconSize, iconSize); // Set size
            iconRectTransform.anchoredPosition = new Vector2(xOffset + index * (iconSize + iconBorderSize + __m_userTrophySpacing), yOffset); // Set position
            iconRectTransform.localScale = new Vector3(__m_baseTrophyScale, __m_baseTrophyScale, __m_baseTrophyScale) * __m_userIconScale;

            // Add an Image component for Sprite
            UnityEngine.UI.Image iconImage = iconElement.AddComponent<UnityEngine.UI.Image>();
            iconImage.sprite = iconSprite;
            iconImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            iconImage.raycastTarget = true;

            AddTooltipTriggersToTrophyIcon(iconElement);

            return iconElement;
        }

        public static void DeleteTrophyIconElements(List<GameObject> iconList)
        {
            foreach (GameObject trophyIconObject in iconList)
            {
                GameObject.Destroy(trophyIconObject);
            }

            iconList.Clear();
        }

        public static void CreateTrophyIconElements(Transform parentTransform, TrophyHuntData[] trophies, List<GameObject> iconList)
        {
            foreach (TrophyHuntData trophy in trophies)
            {
                Sprite trophySprite = GetTrophySprite(trophy.m_name);
                if (trophySprite == null)
                {
                    //ACK
                    Debug.LogError($"Unable to find trophy sprite for {trophy.m_name}");
                    continue;
                }

                GameObject iconElement = CreateTrophyIconElement(parentTransform, trophySprite, trophy.m_name, trophy.m_biome, iconList.Count);
                iconElement.name = trophy.m_name;

                iconList.Add(iconElement);
            }
        }

        static Sprite GetTrophySprite(string trophyPrefabName)
        {
            // Ensure the ObjectDB is loaded
            if (ObjectDB.instance == null)
            {
                Debug.LogError("ObjectDB is not loaded.");
                return null;
            }

            // Find the prefab for the specified trophy
            GameObject trophyPrefab = ObjectDB.instance.GetItemPrefab(trophyPrefabName);
            if (trophyPrefab == null)
            {
                Debug.LogError($"Trophy prefab '{trophyPrefabName}' not found.");
                return null;
            }

            // Extract the ItemDrop component and get the item's icon
            ItemDrop itemDrop = trophyPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                Debug.LogError($"ItemDrop component not found on prefab '{trophyPrefabName}'.");
                return null;
            }

            return itemDrop.m_itemData.m_shared.m_icons[0];
        }

        static void EnableTrophyHuntIcon(string trophyName)
        {
            // Find the UI element and bold it
            if (__m_iconList == null)
            {
                Debug.LogError("__m_iconList is null in EnableTrophyHuntIcon()");

                return;
            }

            GameObject iconGameObject = __m_iconList.Find(gameObject => gameObject.name == trophyName);

            if (iconGameObject != null)
            {
                UnityEngine.UI.Image image = iconGameObject.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.color = Color.white;
                }
            }
            else
            {
                Debug.LogError($"Unable to find {trophyName} in __m_iconList");
            }
        }
       
        static int CalculateTrophyScore(Player player)
        {
            int score = 0;
            foreach (string trophyName in player.GetTrophies())
            {
                TrophyHuntData trophyHuntData = Array.Find(__m_trophyHuntData, element => element.m_name == trophyName);

                if (trophyHuntData.m_name == trophyName)
                {
                    // Add the value to our score
                    score += trophyHuntData.GetCurGameModeTrophyScoreValue();
                }
            }

            return score;
        }

        public static void EnableTrophyHuntIcons(Player player)
        {
            // Enable found trophies
            foreach (string trophyName in player.GetTrophies())
            {
                EnableTrophyHuntIcon(trophyName);
            }
        }


        // Calculates the player's current score and updates __m_deaths and __m_playerCurrentScore.
        // Call this anywhere score is needed without triggering a full UI refresh.
        public static int CalculateCurrentScore(Player player)
        {
            if (player == null) return __m_playerCurrentScore;

            int score = 0;
            score = CalculateTrophyScore(player);
            __m_playerCurrentScore = score;
            return score;
        }

        public static void UpdateModUI(Player player)
        {
            // If there's no Hud yet, don't do anything here
            if (Hud.instance == null)
            {
                Debug.LogError("Hud.instance is null");
                return;
            }

            if (Hud.instance.m_rootObject == null)
            {
                Debug.LogError("Hud.instance.m_rootObject is null");
                return;
            }

            // If there's no player yet, or no trophy list, don't do anything here
            if (player == null)
            {
                Debug.LogError("Player.m_localPlayer is null");
                return;
            }

            if (player.m_trophies == null)
            {
                Debug.LogError("Player.m_localPlayer.m_trophies is null");
                return;
            }

            // Enable trophy/cooking icons (UI only)
            EnableTrophyHuntIcons(player);

            int score = CalculateCurrentScore(player);

            // Update score UI
            if (__m_scoreTextElement)
            {
               __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>().text = "Score: " + score.ToString();
            }
        }

        static IEnumerator FlashImage(UnityEngine.UI.Image targetImage, RectTransform imageRect)
        {
            float flashDuration = 0.809f;
            int numFlashes = 6;

            Vector2 originalAnchoredPosition = imageRect.anchoredPosition;
            Vector3 originalScale = imageRect.localScale;

            for (int i = 0; i < numFlashes; i++)
            {
                for (float t = 0.0f; t < flashDuration; t += Time.deltaTime)
                {
                    float interpValue = Math.Min(1.0f, t / flashDuration);

                    int flash = (int)(interpValue * 5.0f);
                    if (flash % 2 == 0)
                    {
                        targetImage.color = Color.white;
                    }
                    else
                    {
                        targetImage.color = Color.green;
                    }

                    float flashScale = 1 + (1.5f * interpValue);
                    imageRect.localScale = new Vector3(__m_baseTrophyScale, __m_baseTrophyScale, __m_baseTrophyScale) * flashScale * __m_userIconScale;
                    imageRect.anchoredPosition = originalAnchoredPosition + (new Vector2(0, 150.0f) * (float)Math.Sin((float)interpValue / 2f));

                    yield return null;
                }

                imageRect.anchoredPosition = originalAnchoredPosition;
            }

            targetImage.color = Color.white;
            imageRect.localScale = originalScale;
            imageRect.anchoredPosition = originalAnchoredPosition;
        }

        static IEnumerator FlashImage2(UnityEngine.UI.Image targetImage, RectTransform imageRect)
        {
            float flashDuration = 0.5f;
            int numFlashes = 4;

            Vector2 originalAnchoredPosition = imageRect.anchoredPosition;
            Vector3 originalScale = imageRect.localScale;

            float curAccel = 0.0f;
            float curVelocity = 0.0f;
            float curPosition = 0.0f;
            float timeElapsed = 0.0f;

            for (int i = 0; i < numFlashes; i++)
            {
                // Apply impulse
                curAccel = 10.0f; // m/sec
                curVelocity = 0.0f;
                curPosition = 0.0f;
                timeElapsed = 0.0f;

                while (curVelocity > 0.1f)
                {
                    float dt = Time.deltaTime;

                    // Do integration
                    curAccel += -10.0f * dt;
                    curVelocity = curVelocity + curAccel * dt;
                    curPosition = curPosition + curVelocity * dt;

                    float flashScale = 2 + (timeElapsed / flashDuration);

                    imageRect.localScale = new Vector3(__m_baseTrophyScale, __m_baseTrophyScale, __m_baseTrophyScale) * flashScale * __m_userIconScale;
                    imageRect.anchoredPosition = originalAnchoredPosition + (new Vector2(0, 200.0f) * curPosition);

                    yield return null;
                }
            }

            targetImage.color = Color.white;
            imageRect.localScale = originalScale;
            imageRect.anchoredPosition = originalAnchoredPosition;
        }

        static IEnumerator FlashBiomeImage(UnityEngine.UI.Image targetImage, RectTransform imageRect)
        {
            float flashDuration = 6f;

            Quaternion originalRotation = imageRect.rotation;

            for (float t = 0.0f; t < flashDuration; t += Time.deltaTime)
            {
                imageRect.localEulerAngles += new Vector3(0f, 0f, t);

                yield return null;
            }

            imageRect.rotation = originalRotation;
        }


        static void FlashTrophy(string trophyName)
        {
            GameObject iconGameObject = __m_iconList.Find(gameObject => gameObject.name == trophyName);

            if (iconGameObject != null)
            {
                UnityEngine.UI.Image image = iconGameObject.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    RectTransform imageRect = iconGameObject.GetComponent<RectTransform>();

                    if (imageRect != null)
                    {
                        // Flash it with a CoRoutine
                        __m_seasonheimMod.StartCoroutine(FlashImage(image, imageRect));
                        //                        __m_trophyHuntMod.StartCoroutine(DoFlashScore());
                    }
                }
            }
            else
            {
                Debug.LogError($"Unable to find {trophyName} in __m_iconList");
            }
        }


        [HarmonyPatch(typeof(Player), nameof(Player.AddTrophy), new[] { typeof(ItemDrop.ItemData) })]
        public static class Player_AddTrophy_Patch
        {
            public static void Postfix(Player __instance, ItemDrop.ItemData item)
            {
                var player = __instance;

                if (player != null && item != null)
                {
                    var name = item.m_dropPrefab.name;

                    // Check to see if this one's in the cache, if not, it's new to us
                    if (__m_trophyCache.Find(trophyName => trophyName == name) != name)
                    {
                        // Haven't collected this one before, flash the UI for it
                        FlashTrophy(name);

                        // Update Trophy cache
                        __m_trophyCache = player.GetTrophies();

                        UpdateModUI(player);

                        // Detect any bonuses triggered by this trophy before logging, so they combine into one event
                        string compositeBonusCode = null;

                        AddTrophyPin(player.transform.position, name);
                    }
                }
            }
        }

        // Track Event helpers
        static public void AddTrackEvent(string tag, Vector3 pos, string extra = null)
        {
            __m_pendingEvents.Add(new TrackEvent
            {
                tag = tag,
                secs = 0,
                x = Mathf.RoundToInt(pos.x),
                y = Mathf.RoundToInt(pos.y),
                z = Mathf.RoundToInt(pos.z),
                extra = extra
            });
        }
        public static void StartCollectingPlayerPath()
        {
            StopCollectingPlayerPath();
            __m_collectingPlayerPath = true;
            __m_seasonheimMod.StartCoroutine(CollectPlayerPath());
        }

        public static void StopCollectingPlayerPath()
        {
            __m_collectingPlayerPath = false;
        }

        static public IEnumerator CollectPlayerPath()
        {
            Vector3 lastPos = Vector3.positiveInfinity;
            while (__m_collectingPlayerPath)
            {
                yield return new WaitForSeconds(__m_playerPathCollectionInterval);
                if (!__m_collectingPlayerPath) yield break;
                Player player = Player.m_localPlayer;
                if (player == null) continue;
                Vector3 pos = player.transform.position;
                // Only record if the player has moved far enough from last recorded point
                // (also considers positions already recorded via events)
                Vector3 lastEventPos = lastPos;
                if (__m_pendingEvents.Count > 0)
                {
                    TrackEvent last = __m_pendingEvents[__m_pendingEvents.Count - 1];
                    lastEventPos = new Vector3(last.x, last.y, last.z);
                }
                if (Vector3.Distance(pos, lastEventPos) >= __m_minPathPlayerMoveDistance)
                {
                    AddTrackEvent("W", pos);
                    lastPos = pos;
                }
            }
        }

        public static void AddTrophyPin(Vector3 position, string trophyName, bool big = false)
        {
            Minimap.PinData pin = Minimap.instance.AddPin(position, Minimap.PinType.Boss, "", true, false);
            pin.m_icon = GetTrophySprite(trophyName);
            TrophyPin newPin = new TrophyPin();
            newPin.m_pos = position;
            newPin.m_trophyName = trophyName;
            if (big)
            {
                pin.m_doubleSize = true;
            }
            __m_trophyPins.Add(newPin);
        }

        public static void FixTrophyPins()
        {
            if (Minimap.instance == null)
            {
                return;
            }

            foreach (var trophyPin in __m_trophyPins)
            {
                foreach (var pinData in Minimap.instance.m_pins)
                {
                    if (Vector3.Distance(pinData.m_pos, trophyPin.m_pos) < 1.0f)
                    {
                        Minimap.instance.RemovePin(pinData);
                        break;
                    }
                }
            }

            foreach (var trophyPin in __m_trophyPins)
            {
                Minimap.instance.AddPin(trophyPin.m_pos, Minimap.PinType.Boss, "", true, false).m_icon = GetTrophySprite(trophyPin.m_trophyName);
            }
        }

        // Logout Tracking
        #region Logout Handling

        static float GetTotalOnFootDistance(Game game)
        {
            if (game == null)
            {
                Debug.LogError($"No Game object found in GetTotalOnFootDistance");

                return 0.0f;
            }

            PlayerProfile profile = game.GetPlayerProfile();
            if (profile != null)
            {
                PlayerProfile.PlayerStats stats = profile.m_playerStats;
                if (stats != null)
                {
                    float onFootDistance = stats[PlayerStatType.DistanceWalk] + stats[PlayerStatType.DistanceRun];

                    return onFootDistance;
                }
            }

            return 0.0f;
        }

        #endregion

        #region Tooltips

        // Score Tooltip
        static GameObject __m_scoreTooltipObject = null;
        static GameObject __m_scoreTooltipBackground = null;
        static TextMeshProUGUI __m_scoreTooltipText;
        static Vector2 __m_trophyHuntScoreTooltipWindowSize = new Vector2(240, 105);
        static Vector2 __m_scoreTooltipTextOffset = new Vector2(5, 2);

        static Dictionary<TrophyGameMode, Vector2> __toolTipSizes = new Dictionary<TrophyGameMode, Vector2>()
            {
                { TrophyGameMode.Seasonheim2026, new Vector2(240, 105) },
            };

        public static void CreateScoreTooltip()
        {
            // Tooltip Background
            __m_scoreTooltipBackground = new GameObject("Score Tooltip Background");

            Vector2 tooltipWindowSize = __m_trophyHuntScoreTooltipWindowSize;

            if (__toolTipSizes.ContainsKey(GetGameMode()))
            {
                tooltipWindowSize = __toolTipSizes[GetGameMode()];
            }

            // Set the parent to the HUD
            Transform hudrootTransform = Hud.instance.transform;
            __m_scoreTooltipBackground.transform.SetParent(hudrootTransform, false);

            RectTransform bgTransform = __m_scoreTooltipBackground.AddComponent<RectTransform>();
            bgTransform.sizeDelta = tooltipWindowSize;

            // Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_scoreTooltipBackground.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.95f); // Semi-transparent black background

            __m_scoreTooltipBackground.SetActive(false);

            // Create a new GameObject for the tooltip
            __m_scoreTooltipObject = new GameObject("Score Tooltip Text");
            __m_scoreTooltipObject.transform.SetParent(__m_scoreTooltipBackground.transform, false);

            // Add a RectTransform component for positioning
            RectTransform rectTransform = __m_scoreTooltipObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(tooltipWindowSize.x - __m_scoreTooltipTextOffset.x, tooltipWindowSize.y - __m_scoreTooltipTextOffset.y);

            // Add a TextMeshProUGUI component for displaying the tooltip text
            __m_scoreTooltipText = AddTextMeshProComponent(__m_scoreTooltipObject);
            __m_scoreTooltipText.fontSize = 14;
            __m_scoreTooltipText.alignment = TextAlignmentOptions.TopLeft;
            __m_scoreTooltipText.color = Color.yellow;

            // Initially hide the tooltip
            __m_scoreTooltipObject.SetActive(false);
        }

        public static void AddTooltipTriggersToScoreObject(GameObject uiObject)
        {
            // Add EventTrigger component if not already present
            EventTrigger trigger = uiObject.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                return;
            }

            trigger = uiObject.AddComponent<EventTrigger>();

            // Mouse Enter event (pointer enters the icon area)
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((eventData) => ShowScoreTooltip(uiObject));
            trigger.triggers.Add(entryEnter);

            // Mouse Exit event (pointer exits the icon area)
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((eventData) => HideScoreTooltip());
            trigger.triggers.Add(entryExit);
        }

        public static string BuildScoreTooltipText(GameObject uiObject)
        {
            string text = "<n/a>";

            string gameModeText = GetGameModeNameText();

            text = $"<size=20><b><color=#FFB75B>{gameModeText}</color><b></size>\n";

           
                int trophyCount = __m_trophyCache.Count;
                int earnedPoints = 0;
                    earnedPoints = CalculateTrophyPoints();

                text += $"<size=14><color=white>\n";
                text += $"  Trophies:\n    Num: <color=orange>{trophyCount}</color>/{__m_trophyHuntData.Length}\n";

                text += $"<size=17>  Earned Points: <color=orange>{earnedPoints}</color>\n</size>\n";

            text += $"</color></size>";

            return text;
        }


        public static void ShowScoreTooltip(GameObject uiObject)
        {
            if (uiObject == null)
                return;

            string text = BuildScoreTooltipText(uiObject);

            __m_scoreTooltipText.text = text;

            __m_scoreTooltipBackground.SetActive(true);
            __m_scoreTooltipObject.SetActive(true);

            Vector2 tooltipWindowSize = __m_trophyHuntScoreTooltipWindowSize;
            if (__toolTipSizes.ContainsKey(GetGameMode()))
            {
                tooltipWindowSize = __toolTipSizes[GetGameMode()];
            }

            Vector3 tooltipOffset = new Vector3(tooltipWindowSize.x / 2, tooltipWindowSize.y, 0);
            Vector3 mousePosition = Input.mousePosition;
            Vector3 desiredPosition = mousePosition + tooltipOffset;

            // Clamp the tooltip window onscreen
            if (desiredPosition.x < 200) desiredPosition.x = 200;
            if (desiredPosition.y < 200) desiredPosition.y = 200;
            if (desiredPosition.x > Screen.width - tooltipWindowSize.x)
                desiredPosition.x = Screen.width - tooltipWindowSize.x;
            if (desiredPosition.y > Screen.height - tooltipWindowSize.y)
                desiredPosition.y = Screen.height - tooltipWindowSize.y;

            //                Debug.LogWarning($"Luck Tooltip x={desiredPosition.x} y={desiredPosition.y}");

            __m_scoreTooltipBackground.transform.position = desiredPosition;
            __m_scoreTooltipObject.transform.position = new Vector3(desiredPosition.x + __m_scoreTooltipTextOffset.x, desiredPosition.y - __m_scoreTooltipTextOffset.y, 0f);
        }

        public static void HideScoreTooltip()
        {
            __m_scoreTooltipBackground.SetActive(false);
            __m_scoreTooltipObject.SetActive(false);
        }

        // Trophy Tooltips

        static GameObject __m_trophyTooltipObject = null;
        static GameObject __m_trophyTooltipBackground = null;
        static TextMeshProUGUI __m_trophyTooltip;
        static Vector2 __m_trophyTooltipWindowSize = new Vector2(240, 60);
        static Vector2 __m_trophyTooltipTextOffset = new Vector2(5, 2);

        public static void CreateTrophyTooltip()
        {
            //                Debug.LogWarning("Creating Tooltip object");

            Vector2 tooltipWindowSize = __m_trophyTooltipWindowSize;

            // Tooltip Background
            __m_trophyTooltipBackground = new GameObject("Tooltip Background");

            // Set %the parent to the HUD
            Transform hudrootTransform = Hud.instance.transform;
            __m_trophyTooltipBackground.transform.SetParent(hudrootTransform, false);

            RectTransform bgTransform = __m_trophyTooltipBackground.AddComponent<RectTransform>();
            bgTransform.sizeDelta = tooltipWindowSize;

            // Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_trophyTooltipBackground.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.85f); // Semi-transparent black background

            __m_trophyTooltipBackground.SetActive(false);

            // Create a new GameObject for the tooltip
            __m_trophyTooltipObject = new GameObject("Tooltip Text");
            __m_trophyTooltipObject.transform.SetParent(__m_trophyTooltipBackground.transform, false);

            // Add a RectTransform component for positioning
            RectTransform rectTransform = __m_trophyTooltipObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(tooltipWindowSize.x - __m_trophyTooltipTextOffset.x, tooltipWindowSize.y - __m_trophyTooltipTextOffset.y);

            // Add a TextMeshProUGUI component for displaying the tooltip text
            __m_trophyTooltip = AddTextMeshProComponent(__m_trophyTooltipObject);
            __m_trophyTooltip.fontSize = 14;
            __m_trophyTooltip.alignment = TextAlignmentOptions.TopLeft;
            __m_trophyTooltip.color = Color.yellow;

            // Initially hide the tooltip
            __m_trophyTooltipObject.SetActive(false);
        }

        public static void DeleteTrophyTooltip()
        {
            if (__m_trophyTooltipObject != null)
            {
                GameObject.DestroyImmediate(__m_trophyTooltipObject);
                __m_trophyTooltipObject = null;
            }

            if (__m_trophyTooltipBackground)
            {
                GameObject.DestroyImmediate(__m_trophyTooltipBackground);
                __m_trophyTooltipBackground = null;
            }
        }

        public static void AddTooltipTriggersToTrophyIcon(GameObject trophyIconObject)
        {
            // Add EventTrigger component if not already present
            EventTrigger trigger = trophyIconObject.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                return;
            }

            trigger = trophyIconObject.AddComponent<EventTrigger>();

            // Mouse Enter event (pointer enters the icon area)
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((eventData) => ShowTrophyTooltip(trophyIconObject));
            trigger.triggers.Add(entryEnter);

            // Mouse Exit event (pointer exits the icon area)
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((eventData) => HideTrophyTooltip());
            trigger.triggers.Add(entryExit);
        }



        public static string BuildTrophyTooltipText(GameObject uiObject)
        {
            if (uiObject == null)
            {
                return "Invalid";
            }

            string trophyName = uiObject.name;

            TrophyHuntData trophyHuntData = Array.Find(__m_trophyHuntData, element => element.m_name == trophyName);

            string text =
                $"<size=16><b><color=#FFB75B>{trophyHuntData.m_prettyName}</color><b></size>\n" +
                $"<color=white>Point Value: </color><color=green>{trophyHuntData.GetCurGameModeTrophyScoreValue()}</color>\n";
            return text;
        }

        public static void ShowTrophyTooltip(GameObject uiObject)
        {
            if (uiObject == null)
                return;

            string text = "";

            text = BuildTrophyTooltipText(uiObject);
            __m_trophyTooltip.text = text;

            __m_trophyTooltipBackground.SetActive(true);
            __m_trophyTooltipObject.SetActive(true);

            Vector2 tooltipSize = __m_trophyTooltipWindowSize;

            Vector3 tooltipOffset = new Vector3(tooltipSize.x / 2, tooltipSize.y, 0);
            Vector3 mousePosition = Input.mousePosition;
            Vector3 desiredPosition = mousePosition + tooltipOffset;

            // Clamp the tooltip window onscreen
            if (desiredPosition.x < 0) desiredPosition.x = 0;
            if (desiredPosition.y < 0) desiredPosition.y = 0;
            if (desiredPosition.x > Screen.width - tooltipSize.x)
                desiredPosition.x = Screen.width - tooltipSize.x;
            if (desiredPosition.y > Screen.height - tooltipSize.y)
                desiredPosition.y = Screen.height - tooltipSize.y;

            __m_trophyTooltipBackground.transform.position = desiredPosition;
            __m_trophyTooltipObject.transform.position = new Vector3(desiredPosition.x + __m_trophyTooltipTextOffset.x, desiredPosition.y - __m_trophyTooltipTextOffset.y, 0f);
        }

        public static void HideTrophyTooltip()
        {
            __m_trophyTooltipBackground.SetActive(false);
            __m_trophyTooltipObject.SetActive(false);
        }

        #endregion

        public static bool CharacterCanDropTrophies(string characterName)
        {
            int index = Array.FindIndex(__m_trophyHuntData, element => element.m_enemies.Contains(characterName));
            if (index >= 0) return true;
            return false;
        }

        public static string EnemyNameToTrophyName(string enemyName)
        {
            int index = Array.FindIndex(__m_trophyHuntData, element => element.m_enemies.Contains(enemyName));
            if (index < 0) return "Not Found";

            return __m_trophyHuntData[index].m_name;
        }

        /* ------------------------------------------ */

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
        public class FejdStartup_Start_Patch
        {
            static void Postfix()
            {
                //                    Debug.LogError("Main Menu Start method called");

                GameObject mainMenu = GameObject.Find("Menu");
                if (mainMenu != null)
                {
                    GameObject topicObject = GameObject.Find("Topic");
                    TextMeshProUGUI topicText = topicObject?.GetComponent<TextMeshProUGUI>();
                    __m_globalFontObject = topicText.font;

                    Transform logoTransform = mainMenu.transform.Find("Logo");
                    if (logoTransform != null)
                    {
                        GameObject textObject = new GameObject("SeasonheimLogoText");
                        textObject.transform.SetParent(logoTransform.parent);

                        // Set up the RectTransform for positioning
                        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
                        rectTransform.localScale = Vector3.one;
                        rectTransform.anchorMin = new Vector2(0.5f, 0.6f);
                        rectTransform.anchorMax = new Vector2(1.0f, 0.6f);
                        rectTransform.pivot = new Vector2(1.0f, 1.0f);
                        rectTransform.anchoredPosition = new Vector2(-20, 20); // Position below the logo
                        rectTransform.sizeDelta = new Vector2(-650, 185);

                        // Add a TextMeshProUGUI component
                        __m_mainMenuText = AddTextMeshProComponent(textObject);
                        __m_mainMenuText.font = __m_globalFontObject;
                        __m_mainMenuText.fontMaterial = __m_globalFontObject.material;
                        __m_mainMenuText.fontStyle = FontStyles.Bold;

                        __m_mainMenuText.text = GetTrophyHuntMainMenuText();
                        __m_mainMenuText.alignment = TextAlignmentOptions.Left;
                        // Enable outline
                        //                            __m_trophyHuntMainMenuText.fontMaterial.EnableKeyword("OUTLINE_ON");
                        __m_mainMenuText.lineSpacingAdjustment = -5;
                    }
                    else
                    {
                        Debug.LogWarning("Valheim logo not found!");
                    }
                }
                else
                {
                    Debug.LogWarning("Main menu not found!");
                }
            }
        }

        public static Sprite __m_healthSprite = null;

        [HarmonyPatch(typeof(LoadingIndicator), nameof(LoadingIndicator.Awake))]
        public static class LoadingIndicator_Awake_Patch
        {
            static void Postfix(LoadingIndicator __instance)
            {
                if (__instance != null)
                {
                    //                        Debug.LogWarning($"LoadingIndicator.Awake() {__instance.m_spinner.name} {__instance.m_spinner.sprite.name}");
                    IEnumerable<AssetBundle> loadedBundles = AssetBundle.GetAllLoadedAssetBundles();

                    foreach (var bundle in loadedBundles)
                    {
                        string assetName = "Assets/UI/textures/small/texts_button.png"; // rotating crow
                        if (bundle.Contains(assetName))
                        {
                            var asset = bundle.LoadAsset(assetName);
                            if (asset is Texture2D texture)
                            {
                                Sprite trophySprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                                __instance.m_spinner.sprite = trophySprite;
                                __instance.m_spinner.color = new Color(255f / 255f, 215f / 255f, 0, 1);
                                __instance.m_spinnerOriginalColor = __instance.m_spinner.color;

                                __m_trophySprite = trophySprite;

                                //Texture2D newTexture = CreateReadableTextureCopy(texture);
                                //byte[] pngData = newTexture.EncodeToPNG();
                                //File.WriteAllBytes("ValheimTrophyIcon", pngData);

                            }

                            break;
                        }
                        assetName = "Assets/UI/textures/small/health.png";
                        if (bundle.Contains(assetName))
                        {
                            Debug.LogError($"Found Health Sprite");

                            var asset = bundle.LoadAsset(assetName);
                            if (asset is Texture2D texture)
                            {
                                Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                                //__instance.m_spinner.sprite = newSprite;
                                //__instance.m_spinner.color = new Color(255f / 255f, 215f / 255f, 0, 1);
                                //__instance.m_spinnerOriginalColor = __instance.m_spinner.color;

                                __m_healthSprite = newSprite;

                                //Texture2D newTexture = CreateReadableTextureCopy(texture);
                                //byte[] pngData = newTexture.EncodeToPNG();
                                //File.WriteAllBytes("ValheimTrophyIcon", pngData);

                            }

                            break;
                        }
                    }
                }

                Texture2D CreateReadableTextureCopy(Texture2D texture)
                {
                    // Create a new Texture2D with the same width, height, and format as the original
                    Texture2D readableTexture = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 1);

                    // Copy the pixel data from the original to the new texture
                    RenderTexture tempRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
                    Graphics.Blit(texture, tempRenderTexture);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = tempRenderTexture;

                    // Read the pixels from the RenderTexture into the new Texture2D
                    readableTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    readableTexture.Apply();

                    // Restore the previous RenderTexture and release the temporary one
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(tempRenderTexture);

                    return readableTexture;
                }

            }
        }

    }
}
