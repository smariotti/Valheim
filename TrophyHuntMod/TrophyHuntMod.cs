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
using static TrophyHuntMod.TrophyHuntMod.THMSaveData;
using System.Xml.Serialization;
using BepInEx.Configuration;
using Newtonsoft.Json;
using System.Net.Http;

namespace TrophyHuntMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public partial class TrophyHuntMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.TrophyHuntMod";
        public const string PluginName = "TrophyHuntMod";


        private const Boolean UPDATE_LEADERBOARD = false; // SET TO TRUE WHEN PTB IS LIVE

        public const string PluginVersion = "0.10.15";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        // Configuration variables
        private const Boolean DUMP_TROPHY_DATA = false;

        static public TrophyHuntMod __m_trophyHuntMod;

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


        public struct TrophyHuntDataScoreOverride
        {
            public TrophyHuntDataScoreOverride(TrophyGameMode gameMode, string trophyName, int score)
            {
                m_gameMode = gameMode;
                m_trophyName = trophyName;
                m_score = score;
            }

            public TrophyGameMode m_gameMode;
            public string m_trophyName;
            public int m_score;
        }

        static public TrophyHuntDataScoreOverride[] __m_trophyHuntScoreOverrides = new TrophyHuntDataScoreOverride[]
        {

            new TrophyHuntDataScoreOverride(TrophyGameMode.TrophySaga, "TrophyFader", 400),
            new TrophyHuntDataScoreOverride(TrophyGameMode.TrophySaga, "TrophySeekerQueen", 400),

            new TrophyHuntDataScoreOverride(TrophyGameMode.TrophyBlitz, "TrophyFader", 260),
            new TrophyHuntDataScoreOverride(TrophyGameMode.TrophyBlitz, "TrophySeekerQueen", 200),

            new TrophyHuntDataScoreOverride(TrophyGameMode.TrophyTrailblazer, "TrophyFader", 400),
            new TrophyHuntDataScoreOverride(TrophyGameMode.TrophyTrailblazer, "TrophySeekerQueen", 400),

            new TrophyHuntDataScoreOverride(TrophyGameMode.CasualSaga, "TrophyFader", 400),
            new TrophyHuntDataScoreOverride(TrophyGameMode.CasualSaga, "TrophySeekerQueen", 400),

            new TrophyHuntDataScoreOverride(TrophyGameMode.CulinarySaga, "TrophyFader", 400),
            new TrophyHuntDataScoreOverride(TrophyGameMode.CulinarySaga, "TrophySeekerQueen", 400)
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

                foreach (TrophyHuntDataScoreOverride thdso in __m_trophyHuntScoreOverrides)
                {
                    if (thdso.m_gameMode == GetGameMode() && thdso.m_trophyName == m_name)
                    {
                        points = thdso.m_score;
                        break;
                    }
                }

                return points;
            }
        }

        const int TROPHY_HUNT_DEATH_PENALTY = -20;
        const int TROPHY_HUNT_LOGOUT_PENALTY = -10;

        const int TROPHY_RUSH_DEATH_PENALTY = -10;
        const int TROPHY_RUSH_SLASHDIE_PENALTY = -10;
        const int TROPHY_RUSH_LOGOUT_PENALTY = -5;

        const int TROPHY_SAGA_DEATH_PENALTY = -30;
        const int TROPHY_SAGA_LOGOUT_PENALTY = -10;

        const int TROPHY_BLITZ_DEATH_PENALTY = -40;
        const int TROPHY_BLITZ_LOGOUT_PENALTY = -20;

        const int TROPHY_TRAILBLAZER_DEATH_PENALTY = -20;
        const int TROPHY_TRAILBLAZER_LOGOUT_PENALTY = -10;

        const int TROPHY_PACIFIST_DEATH_PENALTY = -20;
        const int TROPHY_PACIFIST_LOGOUT_PENALTY = -10;
        const float CHARMED_ENEMY_SPEED_MULTIPLIER = 3.5f;

        const int CULINARY_SAGA_DEATH_PENALTY = -30;
        const int CULINARY_SAGA_LOGOUT_PENALTY = -10;

        static float __m_sagaSailingSpeedMultiplier = 2.5f;
        static float __m_sagaPaddlingSpeedMultiplier = 2.0f;

        static float __m_blitzSailingSpeedMultiplier = 10.0f;
        static float __m_blitzPaddlingSpeedMultiplier = 8.0f;

        static float __m_trailblazerSailingSpeedMultiplier = 10.0f;
        static float __m_trailblazerPaddlingSpeedMultiplier = 8.0f;

        const float TROPHY_SAGA_TROPHY_DROP_MULTIPLIER = 2f;
        const float TROPHY_SAGA_BASE_SKILL_LEVEL = 20.0f;
        const int TROPHY_SAGA_MINING_MULTIPLIER = 2;

        const float TROPHY_BLITZ_BASE_SKILL_LEVEL = 100.0f;
        const float TROPHY_TRAILBLAZER_BASE_SKILL_LEVEL = 1.0f;
        const float TROPHY_TRAILBLAZER_SKILL_GAIN_RATE = 6.0f;

        const int EXTRA_MINUTE_SCORE_VALUE = 5;

        const string TROPHY_SAGA_INTRO_TEXT = "You were once a great warrior, though your memory of deeds past has long grown dim, shrouded by eons slumbering in the lands beyond death…\n\n\n\n" +
            "Ragnarok looms and the tenth world remains only for a few scant hours. You are reborn with one purpose: collect the heads of Odin's enemies before this cycle ends…\n\n\n\n" +
            "Odin will cast these heads into the well of Mimir where his lost eye still resides. With knowledge of the Forsaken he can finally banish them forever…\n\n\n" +
            "Bring Odin what he desires or be forced to repeat the cycle for eternity…\n\n\n" +
            "…in VALHEIM!";

        const string CULINARY_SAGA_INTRO_TEXT = "You were once a great warrior, though your memory of deeds past has long grown dim, shrouded by eons slumbering in the lands beyond death…\n\n\n\n" +
            "Ragnarok looms and the tenth world remains only for a few scant hours. You are reborn with one purpose: cook a whole bunch of delicious meals to appease Odin's insatiable hunger?\n\n\n\n" +
            "Yes. Somehow. Bring Odin what he desires or be forced to repeat the cycle for eternity…\n\n\n\n" +
            "…in VALHEIM!";

        const string TROPHY_BLITZ_INTRO_TEXT =
            "Onglay agoyay, ethay allfatheryay odinyay unitedyay ethay orldsway. Ehay ewthray down\n\n" +
            "his oesfay andyay astcay emthay intoyay ethay enthtay orldway, enthay itsplay the\n\n" +
            "boughs atthay eldhay eirthay isonpray otay ethay orld-treeway, andyay eftlay ityay to\n\n" +
            "drift unanchoredyay, ayay aceplay ofyay exileyay...\n\n" +
            "For enturiescay, isthay orldway umberedslay uneasilyyay, utbay ityay idday not\n\n" +
            "die... Asyay acialglay agesyay assedpay, ingdomskay oseray andyay ellfay outyay ofyay sight\n\n" +
            "of ethay odsgay.\n\n" +
            "When odinyay eardhay ishay enemiesyay ereway owinggray onceyay againyay in\n\n" +
            "strength, ehay ookedlay otay idgardmay andyay entsay ishay alkyriesvay otay scour\n\n" +
            "the attlefieldsbay orfay ethay eatestgray ofyay eirthay arriorsway. Eadday to\n\n" +
            "the orldway, eythay ouldway ebay ornbay againyay...\n\n" +
            "... inyay Alheimvay.";

        const string LEADERBOARD_URL = "https://valheim.help/api/trackhunt";

        const float LOGOUT_PENALTY_GRACE_DISTANCE = 50.0f;  // total distance you're allowed to walk/run from initial spawn and get a free logout to clear wet debuff

        const float DEFAULT_SCORE_FONT_SIZE = 25;

        const long NUM_SECONDS_IN_FOUR_HOURS = 4 * 60 * 60;
        const long NUM_SECONDS_IN_THREE_HOURS = 3 * 60 * 60;
        const long NUM_SECONDS_IN_TWO_HOURS = 2 * 60 * 60;


        //
        // Trophy Scores updated from Discord chat 08/18/24
        // Archy:
        //  *eik/elder/bonemass/moder/yag     -   40/60/80/100/120 pts 
        //  *hildir bosses trophies respectively -   25/45/65 pts
        //

        //            new TrophyHuntData("TrophyDraugrFem", Biome.Swamp, 20, new List<string> { "" }),
        //            new TrophyHuntData("TrophyForestTroll", Biome.Forest, 20, new List<string> { "" }),

        // Drop Percentages are from the Valheim Fandom Wiki: https://valheim.fandom.com/wiki/Trophies
        //

        static public TrophyHuntData[] __m_trophyHuntData = new TrophyHuntData[]
        {//                     Trophy Name                     Pretty Name         Biome               Score   Drop%   Dropping Enemy Name(s)
            new TrophyHuntData("TrophyAbomination",             "Abomination",      Biome.Swamp,        20,     50,     new List<string> { "$enemy_abomination" }),
            new TrophyHuntData("TrophyAsksvin",                 "Asksvin",          Biome.Ashlands,     50,     50,     new List<string> { "$enemy_asksvin" }),
            new TrophyHuntData("TrophyBlob",                    "Blob",             Biome.Swamp,        20,     10,     new List<string> { "$enemy_blob",       "$enemy_blobelite" }),
            new TrophyHuntData("TrophyBoar",                    "Boar",             Biome.Meadows,      10,     15,     new List<string> { "$enemy_boar" }),
            new TrophyHuntData("TrophyBjorn",                   "Bear",             Biome.Forest,       20,     10,      new List<string>  { "$enemy_bjorn" }),
            new TrophyHuntData("TrophyBjornUndead",             "Vile",             Biome.Plains,       30,     15,      new List<string>  { "$enemy_unbjorn" }),
            new TrophyHuntData("TrophyBonemass",                "Bonemass",         Biome.Swamp,        100,    100,    new List<string> { "$enemy_bonemass" }),
            new TrophyHuntData("TrophyBonemawSerpent",          "Bonemaw",          Biome.Ashlands,     50,     33,     new List<string> { "$enemy_bonemawserpent" }),
            new TrophyHuntData("TrophyCharredArcher",           "Charred Archer",   Biome.Ashlands,     50,     5,      new List<string> { "$enemy_charred_archer" }),
            new TrophyHuntData("TrophyCharredMage",             "Charred Warlock",  Biome.Ashlands,     50,     5,      new List<string> { "$enemy_charred_mage" }),
            new TrophyHuntData("TrophyCharredMelee",            "Charred Warrior",  Biome.Ashlands,     50,     5,      new List<string> { "$enemy_charred_melee" }),
            new TrophyHuntData("TrophyCultist",                 "Cultist",          Biome.Mountains,    30,     10,     new List<string> { "$enemy_fenringcultist" }),
            new TrophyHuntData("TrophyCultist_Hildir",          "Geirrhafa",        Biome.Hildir,       55,     100,    new List<string> { "$enemy_fenringcultist_hildir" }),
            new TrophyHuntData("TrophyDeathsquito",             "Deathsquito",      Biome.Plains,       30,     5,      new List<string> { "$enemy_deathsquito" }),
            new TrophyHuntData("TrophyDeer",                    "Deer",             Biome.Meadows,      10,     50,     new List<string> { "$enemy_deer" }),
            new TrophyHuntData("TrophyDragonQueen",             "Moder",            Biome.Mountains,    100,    100,    new List<string> { "$enemy_dragon" }),
            new TrophyHuntData("TrophyDraugr",                  "Draugr",           Biome.Swamp,        20,     10,     new List<string> { "$enemy_draugr" }),
            new TrophyHuntData("TrophyDraugrElite",             "Draugr Elite",     Biome.Swamp,        20,     10,     new List<string> { "$enemy_draugrelite" }),
            new TrophyHuntData("TrophyDvergr",                  "Dvergr",           Biome.Mistlands,    40,     5,      new List<string> { "$enemy_dvergr",     "$enemy_dvergr_mage" }),
            new TrophyHuntData("TrophyEikthyr",                 "Eikthyr",          Biome.Meadows,      40,     100,    new List<string> { "$enemy_eikthyr" }),
            new TrophyHuntData("TrophyFader",                   "Fader",            Biome.Ashlands,     1000,   100,    new List<string> { "$enemy_fader" }),
            new TrophyHuntData("TrophyFallenValkyrie",          "Fallen Valkyrie",  Biome.Ashlands,     50,     5,      new List<string> { "$enemy_fallenvalkyrie" }),
            new TrophyHuntData("TrophyFenring",                 "Fenring",          Biome.Mountains,    30,     10,     new List<string> { "$enemy_fenring" }),
            new TrophyHuntData("TrophyFrostTroll",              "Troll",            Biome.Forest,       20,     50,     new List<string> { "$enemy_troll" }),
            new TrophyHuntData("TrophyGhost",                   "Ghost",            Biome.Forest,       20,     10,     new List<string> { "$enemy_ghost" }),
            new TrophyHuntData("TrophyGjall",                   "Gjall",            Biome.Mistlands,    40,     30,     new List<string> { "$enemy_gjall" }),
            new TrophyHuntData("TrophyGoblin",                  "Fuling",           Biome.Plains,       30,     10,     new List<string> { "$enemy_goblin" }),
            new TrophyHuntData("TrophyGoblinBrute",             "Fuling Berserker", Biome.Plains,       30,     5,      new List<string> { "$enemy_goblinbrute" }),
            new TrophyHuntData("TrophyGoblinBruteBrosBrute",    "Thungr",           Biome.Hildir,       65,     100,    new List<string> { "$enemy_goblinbrute_hildircombined" }),
            new TrophyHuntData("TrophyGoblinBruteBrosShaman",   "Zil",              Biome.Hildir,       65,     100,    new List<string> { "$enemy_goblin_hildir" }),
            new TrophyHuntData("TrophyGoblinKing",              "Yagluth",          Biome.Plains,       160,    100,    new List<string> { "$enemy_goblinking" }),
            new TrophyHuntData("TrophyGoblinShaman",            "Fuling Shaman",    Biome.Plains,       30,     10,     new List<string> { "$enemy_goblinshaman" }),
            new TrophyHuntData("TrophyGreydwarf",               "Greydwarf",        Biome.Forest,       20,     5,      new List<string> { "$enemy_greydwarf" }),
            new TrophyHuntData("TrophyGreydwarfBrute",          "Greydwarf Brute",  Biome.Forest,       20,     10,     new List<string> { "$enemy_greydwarfbrute" }),
            new TrophyHuntData("TrophyGreydwarfShaman",         "Greydwarf Shaman", Biome.Forest,       20,     10,     new List<string> { "$enemy_greydwarfshaman" }),
            new TrophyHuntData("TrophyGrowth",                  "Growth",           Biome.Plains,       30,     10,     new List<string> { "$enemy_blobtar" }),
            new TrophyHuntData("TrophyHare",                    "Misthare",         Biome.Mistlands,    40,     5,      new List<string> { "$enemy_hare" }),
            new TrophyHuntData("TrophyHatchling",               "Drake",            Biome.Mountains,    30,     10,     new List<string> { "$enemy_thehive",    "$enemy_drake" }),
            new TrophyHuntData("TrophyLeech",                   "Leech",            Biome.Swamp,        20,     10,     new List<string> { "$enemy_leech" }),
            new TrophyHuntData("TrophyLox",                     "Lox",              Biome.Plains,       30,     10,     new List<string> { "$enemy_lox" }),
            new TrophyHuntData("TrophyMorgen",                  "Morgen",           Biome.Ashlands,     50,     5,      new List<string> { "$enemy_morgen" }),
            new TrophyHuntData("TrophyNeck",                    "Neck",             Biome.Meadows,      10,     5,      new List<string> { "$enemy_neck" }),
            new TrophyHuntData("TrophySeeker",                  "Seeker",           Biome.Mistlands,    40,     10,     new List<string> { "$enemy_seeker" }),
            new TrophyHuntData("TrophySeekerBrute",             "Seeker Soldier",   Biome.Mistlands,    40,     5,      new List<string> { "$enemy_seekerbrute" }),
            new TrophyHuntData("TrophySeekerQueen",             "The Queen",        Biome.Mistlands,    1000,   100,    new List<string> { "$enemy_seekerqueen" }),
            new TrophyHuntData("TrophySerpent",                 "Serpent",          Biome.Ocean,        45,     33,     new List<string> { "$enemy_serpent" }),
            new TrophyHuntData("TrophySGolem",                  "Stone Golem",      Biome.Mountains,    30,     5,      new List<string> { "$enemy_stonegolem" }),
            new TrophyHuntData("TrophySkeleton",                "Skeleton",         Biome.Forest,       20,     10,     new List<string> { "$enemy_skeleton" }),
            new TrophyHuntData("TrophySkeletonHildir",          "Brenna",           Biome.Hildir,       25,     100,    new List<string> { "$enemy_skeletonfire" }),
            new TrophyHuntData("TrophySkeletonPoison",          "Rancid Remains",   Biome.Forest,       20,     10,     new List<string> { "$enemy_skeletonpoison" }),
            new TrophyHuntData("TrophySurtling",                "Surtling",         Biome.Swamp,        20,     5,      new List<string> { "$enemy_surtling" }),
            new TrophyHuntData("TrophyTheElder",                "The Elder",        Biome.Forest,       60,     100,    new List<string> { "$enemy_gdking" }),
            new TrophyHuntData("TrophyTick",                    "Tick",             Biome.Mistlands,    40,     5,      new List<string> { "$enemy_tick" }),
            new TrophyHuntData("TrophyUlv",                     "Ulv",              Biome.Mountains,    30,     5,      new List<string> { "$enemy_ulv" }),
            new TrophyHuntData("TrophyVolture",                 "Volture",          Biome.Ashlands,     50,     50,     new List<string> { "$enemy_volture" }),
            new TrophyHuntData("TrophyWolf",                    "Wolf",             Biome.Mountains,    30,     10,     new List<string> { "$enemy_wolf" }),
            new TrophyHuntData("TrophyWraith",                  "Wraith",           Biome.Swamp,        20,     5,      new List<string> { "$enemy_wraith" }),
            new TrophyHuntData("TrophyKvastur",                 "Kvastur",          Biome.Bogwitch,     25,     100,    new List<string> { "$enemy_kvastur" })
        };


        static public Color[] __m_biomeColors = new Color[]
        {
            new Color(0.2f, 0.2f, 0.1f, 0.3f),  // Biome.Meadows
            new Color(0.0f, 0.2f, 0.0f, 0.3f),  // Biome.Forest   
            new Color(0.2f, 0.1f, 0.0f, 0.3f),  // Biome.Swamp
            new Color(0.2f, 0.2f, 0.2f, 0.3f),  // Biome.Mountains
            new Color(0.2f, 0.2f, 0.0f, 0.3f),  // Biome.Plains 
            new Color(0.2f, 0.1f, 0.2f, 0.3f),  // Biome.Mistlands
            new Color(0.2f, 0.0f, 0.0f, 0.3f),  // Biome.Ashlands 
            new Color(0.1f, 0.1f, 0.2f, 0.3f),  // Biome.Ocean    
            new Color(0.2f, 0.1f, 0.0f, 0.3f),  // Biome.Hildir
            new Color(0.2f, 0.1f, 0.0f, 0.3f),  // Biome.BogWitch
        };

        public struct BiomeBonus
        {
            public BiomeBonus(Biome biome, string biomeName, int bonus, List<string> trophies)
            {
                m_biome = biome;
                m_biomeName = biomeName;
                m_bonus = bonus;
                m_trophies = trophies;
            }

            public Biome m_biome;
            public string m_biomeName;
            public int m_bonus;
            public List<string> m_trophies;
        }

        static public BiomeBonus[] __m_biomeBonuses = new BiomeBonus[]
        {
            new BiomeBonus(Biome.Meadows,   "Meadows",        20,      new List<string> { "TrophyBoar", "TrophyDeer", "TrophyNeck" }),
            new BiomeBonus(Biome.Forest,    "Black Forest",   40,      new List<string> { "TrophyBjorn", "TrophyFrostTroll", "TrophyGhost", "TrophyGreydwarf", "TrophyGreydwarfBrute", "TrophyGreydwarfShaman", "TrophySkeleton", "TrophySkeletonPoison" }),
            new BiomeBonus(Biome.Swamp,     "Swamp",          40,      new List<string> { "TrophyAbomination", "TrophyBlob", "TrophyDraugr", "TrophyDraugrElite", "TrophyLeech", "TrophySurtling", "TrophyWraith" }),
            new BiomeBonus(Biome.Mountains, "Mountains",      60,      new List<string> { "TrophyCultist", "TrophyFenring", "TrophyHatchling", "TrophySGolem", "TrophyUlv", "TrophyWolf" }),
            new BiomeBonus(Biome.Plains,    "Plains",         60,      new List<string> { "TrophyBjornUndead", "TrophyDeathsquito", "TrophyGoblin", "TrophyGoblinBrute", "TrophyGoblinShaman", "TrophyGrowth", "TrophyLox" }),
            new BiomeBonus(Biome.Mistlands, "Mistlands",      80,      new List<string> { "TrophyDvergr", "TrophyGjall", "TrophyHare", "TrophySeeker", "TrophySeekerBrute", "TrophyTick" }),
            new BiomeBonus(Biome.Ashlands,  "Ashlands",       100,     new List<string> { "TrophyAsksvin", "TrophyBonemawSerpent", "TrophyCharredArcher", "TrophyCharredMage", "TrophyCharredMelee", "TrophyFallenValkyrie", "TrophyMorgen", "TrophyVolture" }),
        };

        // UI Elements
        static GameObject __m_scoreTextElement = null;
        static GameObject __m_scoreBGElement = null;
        static GameObject __m_deathsTextElement = null;
        static GameObject __m_relogsTextElement = null;
        static GameObject __m_relogsIconElement = null;
        static GameObject __m_gameTimerTextElement = null;
        static GameObject __m_luckOMeterElement = null;
        static GameObject __m_standingsElement = null;
        static GameObject __m_thrallsElement = null;
        static GameObject __m_thrallsTextElement = null;

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

        // Online Integration
        static DiscordOAuthFlow __m_discordAuthentication = new DiscordOAuthFlow();
        static bool __m_loggedInWithDiscord = false;
        static TextMeshProUGUI __m_discordLoginButtonText = null;
        static TextMeshProUGUI __m_onlineUsernameText = null;
        static TextMeshProUGUI __m_onlineStatusText = null;
        static UnityEngine.UI.Image __m_discordBackgroundImage = null;

        // In game timer
        static long __m_gameTimerElapsedSeconds = 0;
        static bool __m_gameTimerActive = false;
        static bool __m_gameTimerVisible = false;
        static bool __m_gameTimerCountdown = true;
        static long __m_internalTimerElapsedSeconds = 0;

        const long UPDATE_STANDINGS_INTERVAL = 30;  // Update standings every 30 seconds

        // Trophy Display Settings
        static float __m_baseTrophyScale = 1.4f;
        static float __m_userIconScale = 1.0f;
        static float __m_userTextScale = 1.0f;
        static float __m_userTrophySpacing = 0.0f;

        // Cache for detecting newly arrived trophies and flashing the new ones
        static List<string> __m_trophyCache = new List<string>();

        // Death counter
        static int __m_deaths = 0;
        static int __m_slashDieCount = 0;
        static int __m_logoutCount = 0;

        // Player Path
        static bool __m_pathAddedToMinimap = false;                                // are we showing the path on the minimap? 
        static List<Minimap.PinData> __m_pathPins = new List<Minimap.PinData>();   // keep track of the special pins we add to the minimap so we can remove them
        static List<Vector3> __m_playerPathData = new List<Vector3>();   // list of player positions during the session
        static bool __m_collectingPlayerPath = false;                           // are we actively asynchronously collecting the player position?
        static float __m_playerPathCollectionInterval = 8.0f;                   // seconds between checks to see if we can store the current player position
        static float __m_minPathPlayerMoveDistance = 30.0f;                     // the min distance the player has to have moved to consider storing the new path position
        static Vector3 __m_previousPlayerPos;                                   // last player position stored

        // Trophy Pins
        public class TrophyPin
        {
            public Vector3 m_pos;
            public string m_trophyName;
        }

        static List<TrophyPin> __m_trophyPins = new List<TrophyPin>();

        // Only mod running flag
        static bool __m_onlyModRunning = false;

        // Trophy rush flag
        public enum TrophyGameMode
        {
            TrophyHunt,
            TrophyRush,
            TrophySaga,
            TrophyBlitz,
            TrophyTrailblazer,
            TrophyPacifist,
            CulinarySaga,
            CasualSaga,
            TrophyFiesta,
            Max
        }

        //        static bool __m_trophyRushEnabled = false;

        // TrophyHuntMod current Game Mode
        static TrophyGameMode __m_trophyGameMode = TrophyGameMode.TrophyHunt;

        static public bool __m_pacifistEnabled = false;

        static public TrophyGameMode GetGameMode() { return __m_trophyGameMode; }
        static public string GetGameModeString(TrophyGameMode mode)
        {
            string modeString = "Unknown";
            switch (mode)
            {
                case TrophyGameMode.TrophyHunt: modeString = "<color=yellow>Trophy Hunt</color>"; break;
                case TrophyGameMode.TrophyRush: modeString = "<color=orange>Trophy Rush</color>"; break;
                case TrophyGameMode.TrophySaga: modeString = "<color=yellow>Trophy Saga</color>"; break;
                case TrophyGameMode.TrophyBlitz:
                    modeString =
                        "<color=#D00000>T</color>" +
                        "<color=#F00000>r</color>" +
                        "<color=#F05000>o</color>" +
                        "<color=#F0A000>p</color>" +
                        "<color=#F0F000>h</color>" +
                        "<color=#F0F0F0>y </color>" +
                        "<color=#F0F000>B</color>" +
                        "<color=#F0A000>l</color>" +
                        "<color=#F05000>i</color>" +
                        "<color=#F00000>t</color>" +
                        "<color=#D00000>z</color>";
                    break;
                case TrophyGameMode.TrophyTrailblazer: modeString = "<color=#D080FF>Trailblazer!</color>"; break;
                case TrophyGameMode.TrophyPacifist: modeString = "<color=#F387C5>Trophy Pacifist</color>"; break;
                case TrophyGameMode.CulinarySaga: modeString = "<color=#8080FF>Culinary Saga</color>"; break;
                case TrophyGameMode.CasualSaga: modeString = "<color=yellow>Casual Saga</color>"; break;
                case TrophyGameMode.TrophyFiesta: modeString = "<color=yellow>Trophy</color> <color=green>F</color><color=purple>i</color><color=red>e</color><color=yellow>s</color><color=orange>t</color><color=#8080FF>a</color>"; break;
                default:
                    return "Unknown";
            }

            if (IsPacifist() && mode != TrophyGameMode.TrophyPacifist)
            {
                modeString += " <color=#F387C5>Pacifist</color>";
            }
            return modeString;
        }

        static public bool IsSagaMode() { return __m_trophyGameMode == TrophyGameMode.CasualSaga || __m_trophyGameMode == TrophyGameMode.CulinarySaga || __m_trophyGameMode == TrophyGameMode.TrophySaga; }
        static public bool IsPacifist() { return __m_pacifistEnabled || GetGameMode() == TrophyGameMode.TrophyPacifist; }

        static bool __m_fiestaFlashing = false;
        static Color[] __m_fiestaColors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan,
            Color.magenta,
            Color.yellow,
        };

        static bool __m_blitzFlashing = false;

        // Track all enemy deaths and trophies flag
        static bool __m_showAllTrophyStats = false;
        static bool __m_invalidForTournamentPlay = false;
        static bool __m_ignoreLogouts = false;

        static bool __m_ignoreInvalidateUIChanges = false;

        static bool __m_introMessageDisplayed = false;

        // Used by TrophySaga, true if all ores turn into bars when entering inventory
        // also treats all ore weights as their bar weights across the game
        //
        static bool __m_instaSmelt = true;

        // If enabled, Elder power
        static bool __m_elderPowerCutsAllTrees = false;

        static bool __m_everythingUnlocked = false;

        // For tracking the unique ID for this user/player combo (unique to a given player character)
        static long __m_storedPlayerID = 0;
        static TrophyGameMode __m_storedGameMode = TrophyGameMode.Max;
        static string __m_storedWorldSeed = "";

        // Currently computed score value
        static int __m_playerCurrentScore = 0;
        static int __m_extraTimeScore = 0;

        // Log of in-game events we track
        static public List<PlayerEventLog> __m_playerEventLog = new List<PlayerEventLog>();
        static public bool __m_refreshLogsAndStandings = false;

        public struct DropInfo
        {
            public DropInfo()
            {
                m_numKilled = 0;
                m_trophies = 0;
            }

            public int m_numKilled = 0;
            public int m_trophies = 0;
        }

        // ALL the killed enemies and trophy drops that happen in the game
        static Dictionary<string, DropInfo> __m_allTrophyDropInfo = new Dictionary<string, DropInfo>();

        // Just the killed enemies and trophies dropped and picked up by the player
        static Dictionary<string, DropInfo> __m_playerTrophyDropInfo = new Dictionary<string, DropInfo>();

        // Biomes we've completed 
        static List<Biome> __m_completedBiomeBonuses = new List<Biome>();
        static bool __m_completedAllBiomeBonuses = false;
        static int ALL_BIOME_BONUS_SCORE = 50;

        void DoStackCrawl() 
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true); // true to capture file and line info
            foreach (System.Diagnostics.StackFrame sf in st.GetFrames())
            {
                Debug.LogWarning($" Method: {sf.GetMethod().Name}, File: {sf.GetFileName()}, Line: {sf.GetFileLineNumber()}");
            }
        }

        //
        // SAVE DATA SECTION
        //

        // BepInEx Mod Config File Data
        static ConfigEntry<string> __m_configDiscordId;
        static ConfigEntry<string> __m_configDiscordUser;
        static ConfigEntry<string> __m_configDiscordGlobalUser;
        static ConfigEntry<string> __m_configDiscordAvatar;
        static ConfigEntry<string> __m_configDiscordDiscriminator;

        // PlayerProfile save data
        //
        [HarmonyPatch(typeof(Game), nameof(Game.SavePlayerProfile))]
        public class Game_SavePlayerProfile_Patch
        {
            static void Prefix(PlayerProfile __instance, bool setLogoutPoint)
            {

                //              Debug.LogError($"Game.SavePlayerProfile() __m_logoutCount={__m_logoutCount}");

                SavePersistentData();
            }
        }

        static public string __m_saveDataVersionNumber = "7";

        // WARNING!
        //
        // ALL CHANGES MADE TO THIS MUST UPDATE __m_saveDataVersionNumber, or deserialize will be broken
        //
        public class THMSaveData
        {
            public class THMSaveDataDropInfo
            {
                public string m_name;
                public DropInfo m_dropInfo;
            }

            public class THMSaveDataSpecialSagaDropCount
            {
                public string m_name;
                public int m_dropCount;
                public int m_numPickedUp;
            }

            public class THMSaveDataCharmedCharacter
            {
                public Vector3 m_pos;
                public Minimap.PinType m_pinType;
                public long m_charmTimeRemaining;
                public Guid m_charmGUID;
                //                public ushort m_userKey;
                //                public uint m_ID;
                public Character.Faction m_originalFaction;
                public float m_swimSpeed;
                public int m_charmLevel;
            }

            public List<THMSaveDataDropInfo> m_playerTrophyDropInfos = null;
            public List<THMSaveDataDropInfo> m_allTrophyDropInfos = null;

            public List<Vector3> m_playerPathData = null;

            public List<TrophyPin> m_trophyPins;

            public long m_gameTimerElapsedSeconds;
            public long m_internalTimerElapsedSeconds;
            public bool m_gameTimerActive;
            public bool m_gameTimerVisible;
            public bool m_gameTimerCountdown;

            public long m_charmTimerSeconds;

            public List<THMSaveDataCharmedCharacter> m_charmedCharacters = null;


            public int m_slashDieCount;
            public int m_logoutCount;

            // Build list of string/bools key value pairs for special saga drops
            //Dictionary<string, List<SpecialSagaDrop>> m_specialSagaDrops;

            public List<THMSaveDataSpecialSagaDropCount> m_specialSagaDropCounts;

            public long m_storedPlayerID;
            public TrophyGameMode m_storedGameMode;
            public string m_storedWorldSeed;

            public List<string> m_cookedFoods = null;

            public List<PlayerEventLog> m_playerEventLog = null;
        }

        static string GetPersistentDataKey()
        {
            return __m_saveDataVersionNumber + __m_storedGameMode.ToString() + __m_storedWorldSeed + (IsPacifist() ? "P" : "_");
        }

        static void SavePersistentData()
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.m_customData == null)
            {
                return;
            }

            string dataKey = GetPersistentDataKey();

            //            Debug.LogWarning($"SaveData {dataKey}");

            THMSaveData saveData = new THMSaveData();

            // Extract dictionary to serializable list for player trophy drop data
            //
            saveData.m_playerTrophyDropInfos = new List<THMSaveDataDropInfo>();
            foreach (KeyValuePair<string, DropInfo> dictEntry in __m_playerTrophyDropInfo)
            {
                THMSaveDataDropInfo savedDropInfo = new THMSaveDataDropInfo();
                savedDropInfo.m_name = dictEntry.Key;
                savedDropInfo.m_dropInfo = dictEntry.Value;

                saveData.m_playerTrophyDropInfos.Add(savedDropInfo);
            }

            // Extract dictionary to serializable list for ALL trophy drop data
            //
            saveData.m_allTrophyDropInfos = new List<THMSaveDataDropInfo>();
            foreach (KeyValuePair<string, DropInfo> dictEntry in __m_allTrophyDropInfo)
            {
                THMSaveDataDropInfo savedDropInfo = new THMSaveDataDropInfo();
                savedDropInfo.m_name = dictEntry.Key;
                savedDropInfo.m_dropInfo = dictEntry.Value;

                saveData.m_allTrophyDropInfos.Add(savedDropInfo);
            }

            // Extract drop counts for special saga drops
            // public KeyValuePair<string, int> m_specialSagaDropCounts;

            saveData.m_specialSagaDropCounts = new List<THMSaveDataSpecialSagaDropCount>();
            foreach (KeyValuePair<string, List<SpecialSagaDrop>> sagaDrops in __m_specialSagaDrops)
            {
                foreach (SpecialSagaDrop sagaDrop in sagaDrops.Value)
                {
                    string sagaDropCountKey = sagaDrops.Key + "," + sagaDrop.m_itemName;
                    THMSaveDataSpecialSagaDropCount savedCount = new THMSaveDataSpecialSagaDropCount();
                    savedCount.m_name = sagaDropCountKey;
                    savedCount.m_dropCount = sagaDrop.m_numDropped;
                    savedCount.m_numPickedUp = sagaDrop.m_numPickedUp;
                    saveData.m_specialSagaDropCounts.Add(savedCount);
                }
            }

            // The /showpath path
            saveData.m_playerPathData = __m_playerPathData;

            saveData.m_trophyPins = __m_trophyPins;

            // Game timer settings
            saveData.m_gameTimerElapsedSeconds = __m_gameTimerElapsedSeconds;
            saveData.m_internalTimerElapsedSeconds = __m_internalTimerElapsedSeconds;
            saveData.m_gameTimerActive = __m_gameTimerActive;
            saveData.m_gameTimerVisible = __m_gameTimerVisible;
            saveData.m_gameTimerCountdown = __m_gameTimerCountdown;

            // Charming Enemies in Pacifist mode
            saveData.m_charmTimerSeconds = __m_charmTimerSeconds;

            saveData.m_charmedCharacters = new List<THMSaveDataCharmedCharacter>();
            foreach (var cc in __m_allCharmedCharacters)
            {
                THMSaveDataCharmedCharacter savedChar = new THMSaveDataCharmedCharacter();
                savedChar.m_pos = cc.m_pin.m_pos;
                savedChar.m_pinType = cc.m_pin.m_type;

                savedChar.m_charmGUID = cc.m_charmGUID;
//                savedChar.m_userKey = cc.m_zdoid.UserKey;
//                savedChar.m_ID = cc.m_zdoid.ID;

                savedChar.m_charmTimeRemaining = cc.m_charmExpireTime - __m_charmTimerSeconds;
                savedChar.m_originalFaction = cc.m_originalFaction;
                savedChar.m_swimSpeed = cc.m_swimSpeed;
                savedChar.m_charmLevel = cc.m_charmLevel;

                saveData.m_charmedCharacters.Add(savedChar);
            }
            //            saveData.m_charmedCharacters = __m_allCharmedCharacters;

            // Death and logout accounting
            saveData.m_slashDieCount = __m_slashDieCount;
            saveData.m_logoutCount = __m_logoutCount;

            // world and seed info
            saveData.m_storedPlayerID = __m_storedPlayerID;
            saveData.m_storedGameMode = __m_storedGameMode;
            saveData.m_storedWorldSeed = __m_storedWorldSeed;

            saveData.m_cookedFoods = __m_cookedFoods;

            saveData.m_playerEventLog = __m_playerEventLog;

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(THMSaveData));
            StringWriter stream = new StringWriter();
            xmlSerializer.Serialize(stream, saveData);

            string saveDataString = stream.ToString();

            Player.m_localPlayer.m_customData[dataKey] = saveDataString;

            //            Debug.LogWarning($"SaveData String:{Player.m_localPlayer.m_customData[dataKey]} LogoutCount {__m_logoutCount}");
        }

        static void LoadPersistentData()
        {
            string dataKey = GetPersistentDataKey();

            //            Debug.LogWarning($"LoadData {dataKey}");

            if (Player.m_localPlayer == null || !Player.m_localPlayer.m_customData.ContainsKey(dataKey))
            {
                return;
            }

            string data = Player.m_localPlayer.m_customData[dataKey];
            //            Debug.LogWarning($"LoadData String:{data}");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(THMSaveData));
            StringReader stream = new StringReader(data);

            THMSaveData saveData = xmlSerializer.Deserialize(stream) as THMSaveData;

            // The /showpath path
            if (__m_playerPathData != null && saveData.m_playerPathData != null && __m_playerPathData.Count < saveData.m_playerPathData.Count)
            {
                __m_playerPathData = saveData.m_playerPathData;
            }

            // The trophy map pins
            if (__m_trophyPins != null)
            {
                __m_trophyPins = saveData.m_trophyPins;
            }

            // Game timer settings
            if (__m_gameTimerElapsedSeconds < saveData.m_gameTimerElapsedSeconds)
            {
                __m_gameTimerElapsedSeconds = saveData.m_gameTimerElapsedSeconds;
            }
            if (__m_internalTimerElapsedSeconds < saveData.m_internalTimerElapsedSeconds)
            {
                __m_internalTimerElapsedSeconds = saveData.m_internalTimerElapsedSeconds;
            }
            __m_gameTimerActive = saveData.m_gameTimerActive;
            __m_gameTimerVisible = saveData.m_gameTimerVisible;
            __m_gameTimerCountdown = saveData.m_gameTimerCountdown;

            if (__m_charmTimerSeconds < saveData.m_charmTimerSeconds)
            {
                __m_charmTimerSeconds = saveData.m_charmTimerSeconds;
            }

            //            __m_allCharmedCharacters = saveData.m_charmedCharacters;
            __m_allCharmedCharacters.Clear();
            foreach (var thmsdcc in saveData.m_charmedCharacters)
            {
                CharmedCharacter cc = new CharmedCharacter();

                //cc.m_zdoid = new ZDOID();
                //cc.m_zdoid.UserKey = thmsdcc.m_userKey;
                //cc.m_zdoid.ID = thmsdcc.m_ID;

                cc.m_charmGUID = thmsdcc.m_charmGUID;
                cc.m_charmExpireTime = __m_charmTimerSeconds + thmsdcc.m_charmTimeRemaining;
                cc.m_originalFaction = thmsdcc.m_originalFaction;
                cc.m_pin = new Minimap.PinData();
                cc.m_pin.m_type = thmsdcc.m_pinType;
                cc.m_pin.m_pos = thmsdcc.m_pos;
                cc.m_swimSpeed = thmsdcc.m_swimSpeed;
                cc.m_charmLevel = thmsdcc.m_charmLevel;

                __m_allCharmedCharacters.Add(cc);
            }

            // Death and logout accounting
            __m_slashDieCount = saveData.m_slashDieCount;
            __m_logoutCount = saveData.m_logoutCount;

            // world and seed info
            __m_storedPlayerID = saveData.m_storedPlayerID;
            __m_storedGameMode = saveData.m_storedGameMode;
            __m_storedWorldSeed = saveData.m_storedWorldSeed;

            __m_cookedFoods = saveData.m_cookedFoods;

            __m_playerEventLog = saveData.m_playerEventLog;

            // Unpack dropinfos and update the Dictionary
            //
            foreach (THMSaveDataDropInfo saveDropInfo in saveData.m_playerTrophyDropInfos)
            {
                if (__m_playerTrophyDropInfo.ContainsKey(saveDropInfo.m_name))
                {
                    DropInfo dropInfo = saveDropInfo.m_dropInfo;
                    __m_playerTrophyDropInfo[saveDropInfo.m_name] = dropInfo;
                }
            }
            foreach (THMSaveDataDropInfo saveDropInfo in saveData.m_allTrophyDropInfos)
            {
                if (__m_allTrophyDropInfo.ContainsKey(saveDropInfo.m_name))
                {
                    DropInfo dropInfo = saveDropInfo.m_dropInfo;
                    __m_allTrophyDropInfo[saveDropInfo.m_name] = dropInfo;
                }
            }

            foreach (THMSaveDataSpecialSagaDropCount dropCounts in saveData.m_specialSagaDropCounts)
            {
                string[] sagaDropCountKey = dropCounts.m_name.Split(',');
                string enemyName = sagaDropCountKey[0];
                string itemName = sagaDropCountKey[1];

                List<SpecialSagaDrop> specials = __m_specialSagaDrops[enemyName];
                for (int i = 0; i < specials.Count; i++)
                {
                    SpecialSagaDrop sagaDrop = specials[i];

                    if (sagaDrop.m_itemName == itemName)
                    {
                        sagaDrop.m_numDropped = dropCounts.m_dropCount;
                        sagaDrop.m_numPickedUp = dropCounts.m_numPickedUp;
                        specials[i] = sagaDrop;
                        break;
                    }
                }
            }
        }

        private void Awake()
        {
            __m_trophyHuntMod = this;

            // Patch with Harmony
            harmony.PatchAll();

            AddConsoleCommands();

            // Create the drop data for collecting info about trophy drops vs. kills
            //
            InitializeTrophyDropInfo();

            __m_configDiscordId = Config.Bind("General", "DiscordUserId", "", "When signed in with Discord, the UserID of the Discord user");
            __m_configDiscordUser = Config.Bind("General", "DiscordUserName", "", "When signed in with Discord, the User Name of the Discord user");
            __m_configDiscordGlobalUser = Config.Bind("General", "DiscordGlobalUserName", "", "When signed in with Discord, the Global User Name of the Discord user");
            __m_configDiscordAvatar = Config.Bind("General", "DiscordAvatar", "", "When signed in with Discord, the Avatar id of the Discord user");
            __m_configDiscordDiscriminator = Config.Bind("General", "DiscordDiscriminator", "", "When signed in with Discord, the User Discriminator of the Discord user");

            Debug.Log($"Config __m_configDiscordId:{__m_configDiscordId.Value}");
            Debug.Log($"Config __m_configDiscordUser:{__m_configDiscordUser.Value}");
            Debug.Log($"Config __m_configDiscordGlobalUser:{__m_configDiscordGlobalUser.Value}");

            __m_loggedInWithDiscord = false;
            if (__m_configDiscordUser.Value != "")
            {
                __m_loggedInWithDiscord = true;
            }
        }

        private string[] __m_modWhiteList = new string[]
        {
            "org.bepinex.valheim.displayinfo",
            "com.oathorse.TrophyHuntMod",
            "com.oathorse.Tuba",
            "com.oathorse.Yakkity",
            "wearable_trophies",
//            "AzuAntiArthriticCrafting"
        };

        private void Start()
        {
            // Get the list of loaded plugins
            var loadedPlugins = BepInEx.Bootstrap.Chainloader.PluginInfos;

            Debug.LogError($"[TrophyHut Mod] Found Plugins: {loadedPlugins.Count}");

            __m_onlyModRunning = true;

            foreach (var plugin in loadedPlugins)
            {
                //               Debug.LogError($"{plugin.Key} : {plugin.Value.ToString()} : {plugin.Value.Metadata.Name}, {plugin.Value.Metadata.GUID}, {plugin.Value.Metadata.Version}, {plugin.Value.Metadata.TypeId}");
                if (!__m_modWhiteList.Contains(plugin.Value.Metadata.GUID))
                {
                    __m_onlyModRunning = false;
                    Debug.LogError($"[TrophyHuntMod] v{PluginVersion} detected unauthorized mod '{plugin.Value.Metadata.Name}'! Score will not be accepted with this mod enabled!");
                }
            }

            // Check if the count of loaded plugins is 1 and if it's this mod
            if (__m_onlyModRunning)
            {
                Debug.LogWarning($"[TrophyHuntMod] v{PluginVersion} is loaded and Valheim is running only authorized mods! Let's Hunt!");
            }
            else
            {
                Debug.LogError($"[TrophyHuntMod] v{PluginVersion} found unauthorized mods. Score will be cyan colored, indicating invalid entry.");
            }
        }
        public static void InitializeTrophyDropInfo()
        {
            __m_allTrophyDropInfo.Clear();
            __m_playerTrophyDropInfo.Clear();
            foreach (TrophyHuntData td in __m_trophyHuntData)
            {
                __m_allTrophyDropInfo.Add(td.m_name, new DropInfo());
                __m_playerTrophyDropInfo.Add(td.m_name, new DropInfo());
            }
            __m_completedBiomeBonuses.Clear();
        }

        public static bool __m_showingTrophies = true;
        public static bool __m_showOnlyDeaths = false;

        public static void ShowTrophies(bool show)
        {
            foreach (GameObject trophyIcon in __m_iconList)
            {
                trophyIcon.SetActive(show);
            }
        }

        public static void ShowOnlyDeaths(bool show)
        {
            foreach (GameObject trophyIcon in __m_iconList)
            {
                trophyIcon.SetActive(!show);
            }

            if (__m_luckOMeterElement != null)
            {
                __m_luckOMeterElement.SetActive(!show);
            }

            if (__m_standingsElement != null)
            {
                __m_standingsElement.SetActive(!show);
            }

            if (__m_thrallsElement != null)
            {
                __m_thrallsElement.SetActive(!show);
            }

            if (__m_thrallsTextElement != null)
            {
                __m_thrallsTextElement.SetActive(!show);
            }

            if (__m_relogsTextElement != null)
            {
                __m_relogsTextElement.SetActive(!show);
            }

            if (__m_relogsIconElement != null)
            {
                __m_relogsIconElement.SetActive(!show);
            }

            if (__m_scoreTextElement != null)
            {
                __m_scoreTextElement.SetActive(!show);
            }
        }

        public static void ToggleGameMode()
        {
            __m_trophyGameMode += 1;
            if (__m_trophyGameMode >= TrophyGameMode.TrophyFiesta)
            {
                __m_trophyGameMode = TrophyGameMode.TrophyHunt;
            }
            if (__m_trophyHuntMainMenuText != null)
            {
                __m_trophyHuntMainMenuText.text = GetTrophyHuntMainMenuText();
            }
        }

        public static void TogglePacifist()
        {
            __m_pacifistEnabled = !__m_pacifistEnabled;

            if (__m_trophyHuntMainMenuText != null)
            {
                __m_trophyHuntMainMenuText.text = GetTrophyHuntMainMenuText();
            }

        }

        public static void ToggleShowAllTrophyStats()
        {
            __m_showAllTrophyStats = !__m_showAllTrophyStats;

            if (__m_showAllTrophyStats)
            {
                PrintToConsole($"Displaying ALL enemy deaths for kills and trophies!");
                PrintToConsole($"WARNING: Not legal for Tournament Play!");
            }
            else
            {
                PrintToConsole($"Displaying ONLY Player enemy kills and picked up trophies!");
            }

            // If the game's running, fix the tooltip UI
            if (Game.instance)
            {
                DeleteTrophyTooltip();
                CreateTrophyTooltip();
            }

            if (__m_trophyHuntMainMenuText != null)
            {
                __m_trophyHuntMainMenuText.text = GetTrophyHuntMainMenuText();
            }
        }

        public static TextMeshProUGUI __m_trophyHuntMainMenuText = null;

        public static string GetSagaRulesText()
        {
            string text = "";

            text += $"<align=\"left\">      * Portals allow <color=orange>all items</color>\n";
            text += $"<align=\"left\">      * Raids are <color=orange>disabled</color>\n";
            text += $"<align=\"left\">      * Boat Speed is <color=orange>increased</color>\n";
            text += $"<align=\"left\">      * Ores <color=orange>Insta-smelt</color> on pickup\n";
            text += $"<align=\"left\">      * Speedy <color=orange>Production</color> and <color=orange>crops</color>\n";
            text += $"<align=\"left\">      * Biome minions can <color=orange>drop Boss Items</color>\n";
            text += $"<align=\"left\">      * Greylings/Trolls/Dvergr <color=orange>drop gifts</color>\n";
            text += $"<align=\"left\">      * Mining is <color=orange>more productive</color>\n";
            text += $"<align=\"left\">      * <color=orange>CheatDeath(tm)</color> within 3 sec.\n";

            return text;
        }

        public static string GetGameModeNameText()
        {
            string gameModeText = "???";

            switch (GetGameMode())
            {
                case TrophyGameMode.TrophyHunt:
                    gameModeText = "Trophy Hunt";
                    break;
                case TrophyGameMode.TrophyRush:
                    gameModeText = "Trophy Rush";
                    break;
                case TrophyGameMode.CasualSaga:
                    gameModeText = "Casual Saga";
                    break;
                case TrophyGameMode.TrophySaga:
                    gameModeText = "Trophy Saga";
                    break;
                case TrophyGameMode.TrophyBlitz:
                    gameModeText = "Trophy Blitz";
                    break;
                case TrophyGameMode.TrophyTrailblazer:
                    gameModeText = "Trailblazer";
                    break;
                case TrophyGameMode.TrophyPacifist:
                    gameModeText = "Pacifist";
                    break;
                case TrophyGameMode.CulinarySaga:
                    gameModeText = "Culinary Saga";
                    break;
                case TrophyGameMode.TrophyFiesta:
                    gameModeText = "Trophy Fiesta";
                    break;
            }

            return gameModeText;
        }

        public static string GetGameModeText()
        {
            string text = "";

            float resourceMultiplier = 1f;
            string combatDifficulty = "Normal";
            string dropRate = "Normal";
            bool hasBiomeBonuses = false;
            bool hasAdditionalSlashDiePenalty = false;
            string timeLimit = "None";

            text += $"<align=\"left\"><size=18>\nGame Mode: {GetGameModeString(GetGameMode())}</size>\n";
            switch (GetGameMode())
            {
                case TrophyGameMode.TrophyHunt:
                    // Trophy Hunt game mode
                    break;
                case TrophyGameMode.TrophyRush:
                    // Trophy Rush game mode
                    text += "<align=\"center\"><size=12>\n <color=yellow>NOTE:</color> To use existing world, change World Modifiers manually!</size>\n";
                    resourceMultiplier = 2f;
                    combatDifficulty = "Very Hard";
                    dropRate = "100%";
                    hasBiomeBonuses = true;
                    hasAdditionalSlashDiePenalty = true;
                    timeLimit = "4 Hours";
                    break;
                case TrophyGameMode.CasualSaga:
                    text += "<align=\"center\"><size=12>\n  <color=yellow>NOTE:</color> To use existing world, change World Modifiers manually!</size>\n";
                    resourceMultiplier = 2.0f;
                    combatDifficulty = "Normal";
                    dropRate = "100%";
                    hasBiomeBonuses = false;
                    timeLimit = "None";
                    break;
                case TrophyGameMode.TrophySaga:
                    text += "<align=\"center\"><size=12>\n  <color=yellow>NOTE:</color> To use existing world, change World Modifiers manually!</size>\n";
                    resourceMultiplier = 2.0f;
                    combatDifficulty = "Normal";
                    dropRate = "100%";
                    hasBiomeBonuses = false;
                    timeLimit = "4 Hours";
                    break;
                case TrophyGameMode.TrophyBlitz:
                    text += $"<align=\"center\"><size=12>\n  <color=yellow>NOTE:</color> To use existing world, change World Modifiers manually!</size>\n";
                    resourceMultiplier = 2.0f;
                    combatDifficulty = "Normal";
                    dropRate = "100%";
                    hasBiomeBonuses = true;
                    timeLimit = "2 Hours";
                    break;
                case TrophyGameMode.TrophyTrailblazer:
                    text += $"<align=\"center\"><size=12>\n  <color=yellow>NOTE:</color> To use existing world, change World Modifiers manually!</size>\n";
                    resourceMultiplier = 2.0f;
                    combatDifficulty = "Normal";
                    dropRate = "100%";
                    hasBiomeBonuses = true;
                    timeLimit = "3 Hours";
                    break;
                case TrophyGameMode.TrophyPacifist:
                    text += $"<align=\"left\"><size=14><color=red>                EXPERIMENTAL!</color></size>";
                    text += $"<align=\"center\"><size=12>\n  <color=yellow>NOTE:</color> To use existing world, change World Modifiers manually!</size>\n";
                    resourceMultiplier = 2.0f;
                    combatDifficulty = "Normal";
                    dropRate = "100%";
                    hasBiomeBonuses = true;
                    timeLimit = "4 Hours";
                    break;
                case TrophyGameMode.CulinarySaga:
                    text += $"<align=\"left\"><size=14><color=red>                EXPERIMENTAL!</color></size>";
                    text += $"<align=\"left\"><size=12>\n <color=yellow> NOTE:</color> To use existing world, change World Modifiers manually!</size>\n";
                    resourceMultiplier = 2.0f;
                    combatDifficulty = "Normal";
                    dropRate = "100%";
                    hasBiomeBonuses = false;
                    timeLimit = "4 Hours";
                    break;
                case TrophyGameMode.TrophyFiesta:
                    text += "<align=\"left\"><size=14>\n <color=yellow>            Nothing to see here.</color></size>\n";
                    timeLimit = "None";
                    break;
            }

            if (GetGameMode() != TrophyGameMode.TrophyFiesta)
            {
                text += "<align=\"left\"><size=14>    Rules:\n";
                text += $"<align=\"left\">      * Time Limit: <color=orange>{timeLimit}</color>\n";
                text += $"<align=\"left\">      * Resources: <color=orange>{resourceMultiplier.ToString("0.0")}x</color>\n";
                text += $"<align=\"left\">      * Combat Difficulty: <color=orange>{combatDifficulty}</color>\n";

                if (GetGameMode() != TrophyGameMode.CasualSaga)
                {
                    text += $"<align=\"left\">      * Trophy Drop Rate: <color=orange>{dropRate}</color>\n";
                }

                if (hasBiomeBonuses)
                {
                    text += $"<align=\"left\">      * <color=orange>Biome Bonuses</color> for trophy sets!\n";
                }

                if (GetGameMode() != TrophyGameMode.CasualSaga)
                {

                    text += $"<align=\"left\">      * Logout Penalty: <color=red>{GetLogoutPointCost()}</color>\n";
                    text += $"<align=\"left\">      * Death Penalty: <color=red>{GetDeathPointCost()}</color>\n";
                    if (hasAdditionalSlashDiePenalty)
                    {
                        text += $"<align=\"left\">      * '/die' Penalty: <color=red>{GetDeathPointCost() + GetSlashDiePointCost()}</color>\n";
                    }
                }
                if (GetGameMode() == TrophyGameMode.CulinarySaga)
                {
                    text += $"\n<align=\"left\"><size=14>      * You have four hours to cook food.\n</size>";
                    text += $"<align=\"left\"><size=14>      * Cook <color=orange>one of each food</color> to score big!\n</size>";
                    text += $"<align=\"left\"><size=14>      * Feasts are not (yet) included.\n</size>";
                }
                if (GetGameMode() == TrophyGameMode.TrophyRush)
                {
                    text += $"<align=\"left\">      * <color=orange>CheatDeath(tm)</color> within 3 sec.\n";

                }

                if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    text += $"<align=\"left\">      * NO BEDS ALLOWED!\n";
                    text += $"<align=\"left\">      * Fast Fermenters\n";
                    text += $"<align=\"left\">      * No Craft or Build Cost\n";
                    text += $"<align=\"left\">      * Sequential Boss Reveals\n";
                    text += $"<align=\"left\">      * Dangerously fast boats\n";
                    text += $"<align=\"left\">      * Keep Equipment on death\n";
                    text += $"<align=\"left\">      * Portal Everything\n";
                    text += $"<align=\"left\">      * Skills at 100\n";
                    text += $"<align=\"left\">      * Automatic Portal map pins\n";
                    text += $"<align=\"left\">      * <color=orange>CheatDeath(tm)</color> within 3 sec.\n";
                }

                if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    text += $"<align=\"left\">      * Fast Fermenters and Plantings\n";
                    text += $"<align=\"left\">      * No Craft or Build Cost for known recipes\n";
                    text += $"<align=\"left\">      * Sequential Boss Reveals\n";
                    text += $"<align=\"left\">      * Dangerously fast boats\n";
                    text += $"<align=\"left\">      * Keep Equipment on death\n";
                    text += $"<align=\"left\">      * Portal Everything\n";
                    text += $"<align=\"left\">      * Skills increase very rapidly\n";
                    text += $"<align=\"left\">      * Automatic Portal map pins\n";
                    text += $"<align=\"left\">      * <color=orange>CheatDeath(tm)</color> within 3 sec.\n";
                }
                if (IsPacifist())
                {
                    text += $"<align=\"left\">      * You <color=orange>can't attack</color> enemies!\n";
                    text += $"<align=\"left\">      * <color=orange>Wood Arrows</color> charm enemies!\n";
                    text += $"<align=\"left\">      * <color=orange>Bosses</color> cannot be charmed!\n";
                    text += $"<align=\"left\">      * Charmed enemies move <color=orange>super fast</color>!\n";
                }

                if (GetGameMode() == TrophyGameMode.CasualSaga)
                {
                    text += GetSagaRulesText();
                }
                else if (IsSagaMode())
                {
                    text += GetSagaRulesText(); // $"\n<align=\"left\">      * Saga rule set (see Casual Saga)\n";
                }

                text += "</size>";
            }

            return text;
        }

        public static string GetTrophyHuntMainMenuText()
        {
            string textStr = $"<b><size=34><color=#FFB75B>TrophyHuntMod</color></size></b>\n<size=18>           (Version: {PluginVersion})</size>";
            textStr += GetGameModeText();

            //if (__m_showAllTrophyStats)
            //{
            //    textStr += ("\n<size=18><color=orange>Tracking ALL enemy deaths and trophies!</color>" +
            //                "\n<color=red>NOT LEGAL FOR TOURNAMENT PLAY!</color></size>");
            //}

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

                foreach (Vector3 pathPos in __m_playerPathData)
                {
                    Minimap.PinType pinType = Minimap.PinType.Icon3;
                    Minimap.PinData newPin = Minimap.instance.AddPin(pathPos, pinType, "", save: false, isChecked: false);

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


                // If this is a different character, gamemode, or world seed, clear all in-memory stats
                if (__m_storedPlayerID != Player.m_localPlayer.GetPlayerID() ||
                    __m_storedGameMode != __m_trophyGameMode ||
                    __m_storedWorldSeed != WorldGenerator.instance.m_world.m_seedName)
                {
                    InitializeTrackedDataForNewPlayer();
                }


                if (__m_showAllTrophyStats ||
                    __m_ignoreLogouts ||
                    GetGameMode() == TrophyGameMode.TrophyFiesta ||
                    GetGameMode() == TrophyGameMode.CulinarySaga ||
                    GetGameMode() == TrophyGameMode.CasualSaga ||
                     Game.instance.GetPlayerProfile().m_usedCheats == true ||
                     Game.instance.GetPlayerProfile().m_playerStats[PlayerStatType.Cheats] > 0)
                {
                    __m_invalidForTournamentPlay = true;

                    //                    Debug.LogError($"INVALID FOR TOURNAMENT PLAY!: showstats={__m_showAllTrophyStats}, Mode={GetGameMode().ToString()}, usedCheats={Game.instance.GetPlayerProfile().m_usedCheats}, cheats={Game.instance.GetPlayerProfile().m_playerStats[PlayerStatType.Cheats]}");
                }

                __m_fiestaFlashing = false;

                // Create all the UI elements we need for this mod
                BuildUIElements();

                // Until the player has moved 10 meters, ignore logouts. This is a hack
                // to get around switching players and accounting for logouts in case the 
                // user was playing another character before starting the trophy hunt run
                //
                if (GetTotalOnFootDistance(Game.instance) < 10.0f)
                {
                    __m_logoutCount = 0;
                }

                //                Debug.LogWarning($"Stored PlayerID: {__m_currentPlayerID}, m_localPlayer PlayerID: {Player.m_localPlayer.GetPlayerID()}");


                //                Debug.LogWarning($"Total Logouts: {__m_logoutCount}");

                string workingDirectory = Directory.GetCurrentDirectory();
                //                Debug.Log($"Working Directory for Trophy Hunt Mod: {workingDirectory}");
                //                Debug.Log($"Steam username: {SteamFriends.GetPersonaName()}");

                // Store the current session data to help determine the player changing these
                // things at the main menu
                __m_storedPlayerID = Player.m_localPlayer.GetPlayerID();
                __m_storedGameMode = __m_trophyGameMode;
                __m_storedWorldSeed = WorldGenerator.instance.m_world.m_seedName;

                // Load persistent data
                LoadPersistentData();

                // Do initial update of all UI elements to the current state of the game
                UpdateModUI(Player.m_localPlayer);

                // Start collecting player position map pin data
                ShowPlayerPath(false);
                StopCollectingPlayerPath();
                StartCollectingPlayerPath();

                if (GetGameMode() == TrophyGameMode.TrophyFiesta)
                {
                    //                    TrophyFiesta.Initialize();
                }
                else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    //                    if (!__m_everythingUnlocked)
                    {
                        UnlockEverythingBlitz(Player.m_localPlayer);
                    }
                }
                else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    UnlockEverythingTrailblazer(Player.m_localPlayer);
                }

                __m_refreshLogsAndStandings = true;

                PostStandingsRequest();

                StartPeriodicTimer();

                if (IsPacifist())
                {
                    StartCharmTimer();
                }

                //                PostTrackHunt();

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

                if (IsPacifist())
                {
                    CacheSprites();

                    if (!__m_introMessageDisplayed)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "<color=#F387C5>Trophy Pacifist</color>\nUse Wood Arrows to charm enemies!");
                        __m_introMessageDisplayed = true;
                    }

                    DoPacifistPostPlayerSpawnTasks();
                }
            }
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
                    //                        Debug.Log($"Setting skill {skill.Key} from level {skill.Value.m_level} to level {skillLevel}");

                    skill.Value.m_level = skillLevel;
                }
            }
        }

        // Patch the Learn method in the Skills class to detect when a skill is added
        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkill))]
        public class Skills_Learn_Patch
        {
            static void Postfix(Skills.Skill __instance, SkillType skillType, ref Skill __result)
            {
                if (IsSagaMode())
                {
                    // Get the specific skill that was just learned or updated^
                    if (__result.m_level < TROPHY_SAGA_BASE_SKILL_LEVEL)
                    {
                        __result.m_level = TROPHY_SAGA_BASE_SKILL_LEVEL;
                        __result.m_accumulator = 0f;

                        //                        Debug.Log($"Setting skill {__result.m_info.m_skill.ToString()} to {TROPHY_SAGA_BASE_SKILL_LEVEL}");
                    }
                }
                else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    // Get the specific skill that was just learned or updated^
                    if (__result.m_level < TROPHY_BLITZ_BASE_SKILL_LEVEL)
                    {
                        __result.m_level = TROPHY_BLITZ_BASE_SKILL_LEVEL;
                        __result.m_accumulator = 0f;

                        //                        Debug.Log($"Setting skill {__result.m_info.m_skill.ToString()} to {TROPHY_BLITZ_BASE_SKILL_LEVEL}");
                    }
                }
                else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    // Get the specific skill that was just learned or updated^
                    if (__result.m_level < TROPHY_TRAILBLAZER_BASE_SKILL_LEVEL)
                    {
                        __result.m_level = TROPHY_TRAILBLAZER_BASE_SKILL_LEVEL;
                        __result.m_accumulator = 0f;

                        //                        Debug.Log($"Setting skill {__result.m_info.m_skill.ToString()} to {TROPHY_BLITZ_BASE_SKILL_LEVEL}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Skill), nameof(Skill.Raise))]
        public class Skills_RaiseSkill_Patch
        {
            static bool Prefix(Skill __instance, ref float factor, bool __result)
            {
                if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    factor = factor * TROPHY_TRAILBLAZER_SKILL_GAIN_RATE;
                }

                return true;
            }
        }

        public static void InitializeTrackedDataForNewPlayer()
        {
            //            Debug.LogError("INITIALIZING TRACKED DATA FOR NEW PLAYER");

            // Saga mode tracking, drop only one megingjord per session-player
            if (IsSagaMode())
            {
                InitializeSagaDrops();

                RaiseAllPlayerSkills(TROPHY_SAGA_BASE_SKILL_LEVEL);

                if (GetGameMode() == TrophyGameMode.CulinarySaga)
                {
                    __m_cookedFoods.Clear();
                }
            }

            if (GetGameMode() == TrophyGameMode.TrophyBlitz)
            {
                RaiseAllPlayerSkills(TROPHY_BLITZ_BASE_SKILL_LEVEL);
            }

            if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
            {
                RaiseAllPlayerSkills(TROPHY_TRAILBLAZER_BASE_SKILL_LEVEL);
            }

            // In-Game Timer 
            __m_gameTimerElapsedSeconds = 0;
            __m_internalTimerElapsedSeconds = 0;

            //                __m_gameTimerVisible = false;
            TimerStart();

            // Reset logout count
            __m_logoutCount = 0;

            // Reset logout ignoring for new character
            __m_ignoreLogouts = false;

            // Track how many times player has done "/die" command
            __m_slashDieCount = 0;

            // New players never start with show-all-stats
            __m_showAllTrophyStats = false;

            // Reset whether we've shown enemy deaths
            __m_invalidForTournamentPlay = false;

            // Clear the map screen pin player location data
            __m_playerPathData.Clear();

            // Clear the dropped trophies tracking data
            InitializeTrophyDropInfo();

            __m_completedAllBiomeBonuses = false;
            __m_completedBiomeBonuses.Clear();
            __m_extraTimeScore = 0;

            if (__m_playerEventLog != null)
            {
                __m_playerEventLog.Clear();
            }
            else
            {
                __m_playerEventLog = new List<PlayerEventLog>();
            }

            __m_trophyPins.Clear();

            __m_introMessageDisplayed = false;
        }

        static public int GetGameModeTimeLength()
        {
            int minutes = 0;
            switch (GetGameMode())
            {
                case TrophyGameMode.TrophyHunt: minutes = (int)NUM_SECONDS_IN_FOUR_HOURS / 60; break;
                case TrophyGameMode.TrophyRush: minutes = (int)NUM_SECONDS_IN_FOUR_HOURS / 60; break;
                case TrophyGameMode.TrophySaga: minutes = (int)NUM_SECONDS_IN_FOUR_HOURS / 60; break;
                case TrophyGameMode.TrophyBlitz: minutes = (int)NUM_SECONDS_IN_TWO_HOURS / 60; break;
                case TrophyGameMode.TrophyTrailblazer: minutes = (int)NUM_SECONDS_IN_THREE_HOURS / 60; break;
                case TrophyGameMode.TrophyPacifist: minutes = (int)NUM_SECONDS_IN_FOUR_HOURS / 60; break;
                case TrophyGameMode.CasualSaga: minutes = 0; break;
                case TrophyGameMode.CulinarySaga: minutes = (int)NUM_SECONDS_IN_FOUR_HOURS / 60; break;


            }
            return minutes;
        }

        public static int CalculateCookingPoints(bool displayToLog = false)
        {
            int score = 0;
            foreach (ConsumableData cd in __m_cookedFoodData)
            {
                if (__m_cookedFoods.Contains(cd.m_prefabName))
                {
                    if (displayToLog)
                    {
                        PrintToConsole($"  {cd.m_prefabName}: Score: {cd.m_points} Biome: {cd.m_biome.ToString()}");
                    }

                    score += cd.m_points;
                }
            }

            return score;
        }

        public static int CalculateTrophyPoints(bool displayToLog = false)
        {
            int score = 0;
            foreach (TrophyHuntData thData in __m_trophyHuntData)
            {
                if (__m_trophyCache.Contains(thData.m_name))
                {
                    if (displayToLog)
                    {
                        PrintToConsole($"  {thData.m_name}: Score: {thData.GetCurGameModeTrophyScoreValue()} Biome: {thData.m_biome.ToString()}");
                    }
                    score += thData.GetCurGameModeTrophyScoreValue();
                }
            }

            return score;
        }

        public static int GetDeathPointCost()
        {
            int deathCost = TROPHY_HUNT_DEATH_PENALTY;

            if (GetGameMode() == TrophyGameMode.TrophyRush)
                deathCost = TROPHY_RUSH_DEATH_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophySaga)
                deathCost = TROPHY_SAGA_DEATH_PENALTY;
            else if (GetGameMode() == TrophyGameMode.CulinarySaga)
                deathCost = CULINARY_SAGA_DEATH_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                deathCost = TROPHY_BLITZ_DEATH_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                deathCost = TROPHY_TRAILBLAZER_DEATH_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophyPacifist)
                deathCost = TROPHY_PACIFIST_DEATH_PENALTY;

            return deathCost;
        }

        public static int GetSlashDiePointCost()
        {
            int additionalCost = 0;

            if (GetGameMode() == TrophyGameMode.TrophyRush)
                additionalCost = TROPHY_RUSH_SLASHDIE_PENALTY;

            return additionalCost;
        }

        public static int CalculateDeathPenalty()
        {
            int deathScore = __m_deaths * GetDeathPointCost();

            deathScore += __m_slashDieCount * GetSlashDiePointCost();

            return deathScore;
        }

        public static int GetLogoutPointCost()
        {
            int logoutCost = TROPHY_HUNT_LOGOUT_PENALTY;

            if (GetGameMode() == TrophyGameMode.TrophyRush)
                logoutCost = TROPHY_RUSH_LOGOUT_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophySaga)
                logoutCost = TROPHY_SAGA_LOGOUT_PENALTY;
            else if (GetGameMode() == TrophyGameMode.CulinarySaga)
                logoutCost = CULINARY_SAGA_LOGOUT_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                logoutCost = TROPHY_BLITZ_LOGOUT_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                logoutCost = TROPHY_TRAILBLAZER_LOGOUT_PENALTY;
            else if (GetGameMode() == TrophyGameMode.TrophyPacifist)
                logoutCost = TROPHY_PACIFIST_LOGOUT_PENALTY;
            return logoutCost;
        }

        public static int CalculateLogoutPenalty()
        {
            int logoutScore = __m_logoutCount * GetLogoutPointCost();

            return logoutScore;
        }

        public static void CalculateExtraTimeScore()
        {
            // Called at the time when the trophies are all collected
            if (__m_tournamentStatus == TournamentStatus.Live)
            {
                int minutesToHour = 60 - DateTime.Now.Minute;
                __m_extraTimeScore = EXTRA_MINUTE_SCORE_VALUE * minutesToHour;// minutes until top of the hour
            }
            else
            {
                int totalMinutes = GetGameModeTimeLength();
                int minutesRemaining = totalMinutes - (int)__m_internalTimerElapsedSeconds / 60;
                __m_extraTimeScore = EXTRA_MINUTE_SCORE_VALUE * minutesRemaining;
            }

        }

        static void BuildUIElements()
        {
            if (Hud.instance == null || Hud.instance.m_rootObject == null)
            {
                Debug.LogError("TrophyHuntMod: Hud.instance.m_rootObject is NOT valid");

                return;
            }

            if (__m_deathsTextElement == null && __m_scoreTextElement == null)
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

                if (GetGameMode() != TrophyGameMode.CasualSaga)
                {
                    if (__m_deathsTextElement == null)
                    {
                        __m_deathsTextElement = CreateDeathsElement(healthPanelTransform);
                    }

                    if (__m_relogsTextElement == null)
                    {
                        __m_relogsTextElement = CreateRelogsElements(healthPanelTransform);
                    }

                    if (__m_gameTimerTextElement == null)
                    {
                        __m_gameTimerTextElement = CreateTimerElements(healthPanelTransform);
                    }

                    if (GetGameMode() == TrophyGameMode.CulinarySaga)
                    {
                        CreateCookingIconElements(healthPanelTransform, __m_cookedFoodData, __m_iconList);

                        CreateTrophyTooltip();
                    }
                    else
                    {
                        CreateTrophyIconElements(healthPanelTransform, __m_trophyHuntData, __m_iconList);

                        // Create the hover text object
                        CreateTrophyTooltip();

                        CreateLuckTooltip();

                        CreateStandingsTooltip();

                        CreateThrallsTooltip();

                        __m_luckOMeterElement = CreateLuckOMeterElements(healthPanelTransform);

                        __m_standingsElement = CreateStandingsElements(healthPanelTransform);
                        __m_standingsElement.SetActive(false);

                        __m_thrallsElement = CreateThrallsElements(healthPanelTransform);
                        __m_thrallsElement.SetActive(false);
                    }
                }

                CreateGameModeElements();

                SetScoreTextElementColor(Color.yellow);

                if (!__m_onlyModRunning)
                {
                    SetScoreTextElementColor(Color.cyan);
                }

                if (__m_showAllTrophyStats || __m_invalidForTournamentPlay)
                {
                    //                    Debug.LogError("SETTING SCORE GREEN!");
                    SetScoreTextElementColor(Color.green);
                }

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

        static IEnumerator TimerUpdate()
        {
            while (__m_gameTimerActive)
            {
                // Don't update seconds at main menu
                if (Game.instance)
                {
                    if (__m_gameTimerTextElement != null)
                    {

                        TMPro.TextMeshProUGUI tmText = __m_gameTimerTextElement.GetComponent<TMPro.TextMeshProUGUI>();

                        long timerValue = __m_gameTimerElapsedSeconds;
                        if (__m_gameTimerCountdown)
                        {
                            if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                            {
                                timerValue = NUM_SECONDS_IN_TWO_HOURS - timerValue;
                            }
                            else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                            {
                                timerValue = NUM_SECONDS_IN_THREE_HOURS - timerValue;
                            }
                            else
                            {
                                timerValue = NUM_SECONDS_IN_FOUR_HOURS - timerValue;
                            }
                        }

                        TimeSpan elapsed = TimeSpan.FromSeconds(timerValue);

                        if (__m_gameTimerVisible)
                        {
                            tmText.text = $"<mspace=0.5em>{elapsed.ToString()}</mspace>";

                            if (!__m_gameTimerCountdown)
                            {
                                tmText.color = Color.yellow;
                                tmText.outlineColor = Color.black;
                            }
                            else
                            {
                                tmText.color = Color.green;
                                tmText.outlineColor = Color.black;
                            }
                        }
                        else
                        {
                            tmText.text = "";
                        }
                    }

                    if (__m_internalTimerElapsedSeconds % UPDATE_STANDINGS_INTERVAL == 0)
                    {
                        PostStandingsRequest();
                    }

                    __m_gameTimerElapsedSeconds++;
                    __m_internalTimerElapsedSeconds++;
                }
                yield return new WaitForSeconds(1f);
            }
        }
        static public void TimerStart()
        {
            if (!__m_gameTimerActive)
            {
                __m_gameTimerActive = true;

                __m_trophyHuntMod.StartCoroutine(TimerUpdate());
            }
        }
        static public void TimerStop()
        {
            __m_gameTimerActive = false;
        }
        static public void TimerReset()
        {
            __m_gameTimerElapsedSeconds = 0;
        }
        static public void TimerSet(string timeStr)
        {
            TimeSpan requestedTime = TimeSpan.Parse(timeStr);

            __m_gameTimerElapsedSeconds = (long)requestedTime.TotalSeconds;
        }

        static public void TimerToggle()
        {
            __m_gameTimerCountdown = !__m_gameTimerCountdown;
        }

        static GameObject CreateTimerElements(Transform parentTransform)
        {
            GameObject timerElement = new GameObject("Timer");
            timerElement.transform.SetParent(parentTransform);

            RectTransform timerRectTransform = timerElement.AddComponent<RectTransform>();
            timerRectTransform.sizeDelta = new Vector2(120, 25);
            timerRectTransform.anchoredPosition = new Vector2(-43, 85);

            timerRectTransform.localScale = new Vector3(__m_userTextScale, __m_userTextScale, __m_userTextScale);

            TMPro.TextMeshProUGUI tmText = AddTextMeshProComponent(timerElement);

            tmText.text = $"<mspace=0.5em>00:00:00</mspace>";// {__m_gameTimer}";
            tmText.fontSize = 24;
            tmText.color = Color.yellow;
            tmText.alignment = TextAlignmentOptions.Center;
            tmText.raycastTarget = false;
            tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmText.outlineColor = Color.black;
            tmText.fontStyle = FontStyles.Bold;
            tmText.outlineWidth = 0.125f; // Adjust the thickness

            // HACK TEMP
            // Text Element
            //GameObject timerBGElement = new GameObject("Timer BG Element");
            //timerBGElement.transform.SetParent(timerElement.transform);

            //RectTransform bgRectTransform = timerBGElement.AddComponent<RectTransform>();
            //bgRectTransform.sizeDelta = timerRectTransform.sizeDelta;
            //bgRectTransform.anchoredPosition = new Vector2(0, 0);
            //bgRectTransform.localScale = timerRectTransform.localScale;

            //UnityEngine.UI.Image image = timerBGElement.AddComponent<UnityEngine.UI.Image>();
            //image.color = new Color(0, 0, 0, 0.75f);

            return timerElement;
        }

        static GameObject CreateRelogsElements(Transform parentTransform)
        {
            Sprite logSprite = GetTrophySprite("RoundLog");

            __m_relogsIconElement = new GameObject("RelogsIcon");
            __m_relogsIconElement.transform.SetParent(parentTransform);

            RectTransform rectTransform = __m_relogsIconElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(40, 40);
            rectTransform.anchoredPosition = new Vector2(-70, -105);
            rectTransform.localScale = new Vector3(__m_userIconScale, __m_userIconScale, __m_userIconScale);

            UnityEngine.UI.Image image = __m_relogsIconElement.AddComponent<UnityEngine.UI.Image>();
            image.sprite = logSprite;
            image.color = Color.white;

            // Text Element
            GameObject relogsElement = new GameObject("RelogsElement");
            relogsElement.transform.SetParent(parentTransform);

            // Add RectTransform component for positioning
            rectTransform = relogsElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(60, 20); // Set size
            rectTransform.anchoredPosition = new Vector2(-70, -105); // Set position
            rectTransform.localScale = new Vector3(__m_userTextScale, __m_userTextScale, __m_userTextScale);

            TMPro.TextMeshProUGUI tmText = AddTextMeshProComponent(relogsElement);

            tmText.text = $"{__m_logoutCount}";
            tmText.fontSize = 24;
            tmText.color = Color.yellow;
            tmText.alignment = TextAlignmentOptions.Center;
            tmText.raycastTarget = false;
            tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmText.outlineColor = Color.black;
            tmText.outlineWidth = 0.1f; // Adjust the thickness

            if (__m_ignoreLogouts)
            {
                tmText.color = Color.gray;
            }

            return relogsElement;
        }

        static GameObject CreateLuckOMeterElements(Transform parentTransform)
        {
            Sprite luckSprite = GetTrophySprite("HelmetMidsummerCrown");

            GameObject luckElement = new GameObject("LuckImage");
            luckElement.transform.SetParent(parentTransform);

            RectTransform rectTransform = luckElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(40, 40);
            rectTransform.anchoredPosition = new Vector2(-70, -20);
            rectTransform.localScale = new Vector3(__m_userIconScale, __m_userIconScale, __m_userIconScale);

            UnityEngine.UI.Image image = luckElement.AddComponent<UnityEngine.UI.Image>();
            image.sprite = luckSprite;
            image.color = Color.white;
            image.raycastTarget = true;

            AddTooltipTriggersToLuckObject(luckElement);

            return luckElement;
        }

        static GameObject CreateStandingsElements(Transform parentTransform)
        {
            Sprite standingsSprite = __m_trophySprite;

            GameObject standingsElement = new GameObject("StandingsImage");
            standingsElement.transform.SetParent(parentTransform);

            RectTransform rectTransform = standingsElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(40, 40);
            rectTransform.anchoredPosition = new Vector2(-70, 40);
            rectTransform.localScale = new Vector3(__m_userIconScale, __m_userIconScale, __m_userIconScale);

            UnityEngine.UI.Image image = standingsElement.AddComponent<UnityEngine.UI.Image>();
            image.sprite = standingsSprite;
            image.color = Color.yellow;
            image.raycastTarget = true;

            AddTooltipTriggersToStandingsObject(standingsElement);

            return standingsElement;
        }

        static GameObject CreateThrallsElements(Transform parentTransform)
        {
            Sprite thrallsSprite = GetTrophySprite("CookedVoltureMeat");

            GameObject thrallsElement = new GameObject("ThrallsImage");
            thrallsElement.transform.SetParent(parentTransform);

            RectTransform rectTransform = thrallsElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(40, 40);
            rectTransform.anchoredPosition = new Vector2(-70, 20);
            rectTransform.localScale = new Vector3(__m_userIconScale, __m_userIconScale, __m_userIconScale);

            UnityEngine.UI.Image image = thrallsElement.AddComponent<UnityEngine.UI.Image>();
            image.sprite = thrallsSprite;
            image.color = Color.yellow;
            image.raycastTarget = true;

            // Text Element
            __m_thrallsTextElement = new GameObject("RelogsElement");
            __m_thrallsTextElement.transform.SetParent(parentTransform);

            // Add RectTransform component for positioning
            rectTransform = __m_thrallsTextElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(60, 20); // Set size
            rectTransform.anchoredPosition = new Vector2(-70, 20); // Set position
            rectTransform.localScale = new Vector3(__m_userTextScale, __m_userTextScale, __m_userTextScale);

            TMPro.TextMeshProUGUI tmText = AddTextMeshProComponent(__m_thrallsTextElement);

            tmText.text = $"{__m_allCharmedCharacters.Count}";
            tmText.fontSize = 24;
            tmText.color = Color.yellow;
            tmText.alignment = TextAlignmentOptions.Center;
            tmText.raycastTarget = false;
            tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmText.outlineColor = Color.black;
            tmText.outlineWidth = 0.1f; // Adjust the thickness


            AddTooltipTriggersToThrallsObject(thrallsElement);

            return thrallsElement;
        }

        static GameObject CreateDeathsElement(Transform parentTransform)
        {
            // use the charred skull sprite for our Death count indicator in the UI
            Sprite skullSprite = GetTrophySprite("Charredskull");

            // Create the skullElement for deaths
            GameObject skullElement = new GameObject("DeathsIcon");
            skullElement.transform.SetParent(parentTransform);

            // Add RectTransform component for positioning
            RectTransform rectTransform = skullElement.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 50);
            rectTransform.anchoredPosition = new Vector2(-70, -65); // Set position
            rectTransform.localScale = new Vector3(__m_userIconScale, __m_userIconScale, __m_userIconScale);

            // Add an Image component
            UnityEngine.UI.Image image = skullElement.AddComponent<UnityEngine.UI.Image>();
            image.sprite = skullSprite;
            image.color = Color.white;
            image.raycastTarget = false;

            GameObject deathsTextElement = new GameObject("DeathsText");
            deathsTextElement.transform.SetParent(parentTransform);

            RectTransform deathsTextTransform = deathsTextElement.AddComponent<RectTransform>();
            deathsTextTransform.sizeDelta = new Vector2(40, 40);
            deathsTextTransform.anchoredPosition = rectTransform.anchoredPosition;
            deathsTextTransform.localScale = new Vector3(__m_userTextScale, __m_userTextScale, __m_userTextScale);

            TMPro.TextMeshProUGUI tmText = AddTextMeshProComponent(deathsTextElement);
            tmText.text = $"{__m_deaths}";
            tmText.fontSize = 24;
            tmText.color = Color.yellow;
            tmText.alignment = TextAlignmentOptions.Center;
            tmText.raycastTarget = false;
            tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmText.outlineColor = Color.black;
            tmText.outlineWidth = 0.1f; // Adjust the thickness

            return deathsTextElement;
        }

        static GameObject CreateScoreTextElement(Transform parentTransform)
        {
            __m_scoreBGElement = new GameObject("ScoreBG");
            __m_scoreBGElement.transform.SetParent(parentTransform);

            Vector2 scorePos = new Vector2(-65, -140);
            Vector2 scoreSize = new Vector2(70, 42);

            RectTransform bgTransform = __m_scoreBGElement.AddComponent<RectTransform>();
            Vector2 scorePosBg = new Vector2(-70, -143);
            Vector2 scoreSizeBg = new Vector2(70, 42);
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

            tmText.text = $"{scoreValue}";
            tmText.fontSize = DEFAULT_SCORE_FONT_SIZE;
            //                tmText.fontStyle = FontStyles.Bold;
            tmText.color = Color.yellow;
            tmText.alignment = TextAlignmentOptions.Center;
            tmText.raycastTarget = true;
            tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmText.outlineColor = Color.black;
            tmText.outlineWidth = 0.125f; // Adjust the thickness
                                          //               text.enableAutoSizing = true;
            tmText.enableAutoSizing = true;
            tmText.fontSizeMin = DEFAULT_SCORE_FONT_SIZE * 0.75f;
            tmText.fontSizeMax = DEFAULT_SCORE_FONT_SIZE + 2.0f;

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
            int xOffset = -20;
            int yOffset = -140;

            int biomeIndex = (int)iconBiome;
            Color backgroundColor = __m_biomeColors[biomeIndex];

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
            iconImage.color = new Color(0.0f, 0.2f, 0.1f, 0.95f);
            iconImage.raycastTarget = true;

            //                if (__m_trophyRushEnabled)
            if (GetGameMode() == TrophyGameMode.TrophyRush)
            {
                iconImage.color = new Color(0.5f, 0.0f, 0.0f);
            }
            else if (GetGameMode() == TrophyGameMode.TrophySaga)
            {
                iconImage.color = new Color(0f, 0f, 0.5f);
            }
            else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
            {
                iconImage.color = new Color(0.2f, 0.2f, 0.0f);
            }
            else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
            {
                iconImage.color = new Color(0.0f, 0.2f, 0.2f);
            }
            else if (GetGameMode() == TrophyGameMode.TrophyPacifist)
            {
                iconImage.color = new Color(0.3f, 0.1f, 0.2f);
            }

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

            if (GetGameMode() == TrophyGameMode.TrophyFiesta)
            {
                __m_fiestaFlashing = true;
                __m_trophyHuntMod.StartCoroutine(FlashTrophyFiesta());
            }
            else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
            {
                //                __m_blitzFlashing = true;
                //                __m_trophyHuntMod.StartCoroutine(FlashTrophyBlitz());
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
        static GameObject CreateCookingIconElement(Transform parentTransform, Sprite iconSprite, string iconName, Biome iconBiome, int index)
        {

            int iconSize = 33;
            int iconBorderSize = -1;
            int xOffset = -20;
            int yOffset = -140;

            int biomeIndex = (int)iconBiome;
            Color backgroundColor = __m_biomeColors[biomeIndex];

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
            iconImage.color = new Color(0.2f, 0.2f, 0.7f, 0.7f);
            iconImage.raycastTarget = true;

            AddTooltipTriggersToTrophyIcon(iconElement);

            return iconElement;
        }

        static void CreateCookingIconElements(Transform parentTransform, ConsumableData[] cookedFoodData, List<GameObject> iconList)
        {
            foreach (ConsumableData food in cookedFoodData)
            {
                string foodPrefabName = food.m_prefabName;

                Sprite foodSprite = GetTrophySprite(foodPrefabName);
                if (foodSprite == null)
                {
                    //ACK
                    Debug.LogError($"Unable to find cooked food sprite for {foodPrefabName}");
                    continue;
                }

                GameObject iconElement = CreateCookingIconElement(parentTransform, foodSprite, foodPrefabName, food.m_biome, iconList.Count);
                iconElement.name = foodPrefabName;

                iconList.Add(iconElement);
            }
        }
        static int CalculateCookingScore(Player player)
        {
            int score = 0;
            foreach (string foodName in __m_cookedFoods)
            {
                ConsumableData cookedFoodData = Array.Find(__m_cookedFoodData, element => element.m_prefabName == foodName);

                if (cookedFoodData != null && cookedFoodData.m_prefabName == foodName)
                {
                    // Add the value to our score
                    score += cookedFoodData.m_points;
                }
            }

            return score;
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

        static bool CalculateBiomeBonusStats(Biome biome, out int numCollected, out int numTotal, out int biomeScore)
        {
            BiomeBonus biomeBonus = Array.Find(__m_biomeBonuses, element => element.m_biome == biome);

            // Throws an exception accessing biomeBonus if not initialized (not found)
            try
            {
                numCollected = 0;
                numTotal = biomeBonus.m_trophies.Count;
                biomeScore = biomeBonus.m_bonus;

                foreach (string trophyName in biomeBonus.m_trophies)
                {
                    if (__m_trophyCache.Contains(trophyName))
                    {
                        numCollected++;
                    }
                }
            }
            catch (Exception ex)
            {
                numCollected = 0;
                numTotal = 0;
                biomeScore = 0;

                return false;
            }

            return true;
        }

        public static int CalculateBiomeBonusScore(Player player)
        {
            int bonusScore = 0;

            foreach (BiomeBonus biomeBonus in __m_biomeBonuses)
            {
                int numCollected = 0;
                int numTotal = 0;
                int biomeScore = 0;

                CalculateBiomeBonusStats(biomeBonus.m_biome, out numCollected, out numTotal, out biomeScore);

                if (numCollected == numTotal)
                {
                    bonusScore += biomeScore;
                }
            }

            if (__m_completedAllBiomeBonuses)
            {
                bonusScore += ALL_BIOME_BONUS_SCORE;
            }

            return bonusScore;
        }

        // Returns TRUE if the trophy completes the set for a biome and adds that biome to the list of completed ones
        public static bool UpdateBiomeBonusTrophies(string trophyName, ref Biome biome)
        {
            TrophyHuntData trophyHuntData = Array.Find(__m_trophyHuntData, element => element.m_name == trophyName);

            int numCollected = 0;
            int numTotal = 0;
            int biomeScore = 0;

            if (!CalculateBiomeBonusStats(trophyHuntData.m_biome, out numCollected, out numTotal, out biomeScore))
            {
                return false;
            }

            if (numCollected == numTotal && !__m_completedBiomeBonuses.Contains(trophyHuntData.m_biome))
            {
                biome = trophyHuntData.m_biome;

                PrintToConsole($"Biome Completed! {trophyHuntData.m_biome.ToString()}");

                __m_completedBiomeBonuses.Add(trophyHuntData.m_biome);

                return true;
            }

            return false;
        }
        public static void EnableTrophyHuntIcons(Player player)
        {
            // Enable found trophies
            foreach (string trophyName in player.GetTrophies())
            {
                EnableTrophyHuntIcon(trophyName);
            }
        }

        public static void EnableBiomes(Player player)
        {
            // Enable found trophies
            foreach (string trophyName in player.GetTrophies())
            {
                EnableTrophyHuntIcon(trophyName);
            }
        }

        public static void EnableCookingIcons(Player player)
        {
            foreach (string foodName in __m_cookedFoods)
            {
                EnableTrophyHuntIcon(foodName);
            }
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

            int score = 0;
            if (GetGameMode() == TrophyGameMode.CulinarySaga)
            {
                EnableCookingIcons(player);

                score = CalculateCookingScore(player);
            }
            else if (GetGameMode() != TrophyGameMode.CasualSaga)
            {
                EnableTrophyHuntIcons(player);

                EnableBiomes(player);

                score = CalculateTrophyScore(player);
            }

            // Update the deaths text and subtract deaths from score
            //
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile != null)
            {
                PlayerProfile.PlayerStats stats = profile.m_playerStats;
                if (stats != null)
                {
                    __m_deaths = (int)stats[PlayerStatType.Deaths];

                    //                        Debug.LogWarning($"Subtracting score for {__m_deaths} deaths.");

                    score += CalculateDeathPenalty();

                    if (__m_deathsTextElement)
                    {
                        // Update the UI element
                        TMPro.TextMeshProUGUI deathsText = __m_deathsTextElement.GetComponent<TMPro.TextMeshProUGUI>();
                        if (deathsText != null)
                        {
                            deathsText.SetText(__m_deaths.ToString());
                        }
                    }
                }
            }

            // Subtract points for logouts
            //                Debug.LogWarning($"Subtracting score for {__m_logoutCount} logouts.");
            if (!__m_ignoreLogouts)
            {
                score += CalculateLogoutPenalty();
            }

            if (GetGameMode() == TrophyGameMode.TrophyRush || GetGameMode() == TrophyGameMode.TrophyBlitz || GetGameMode() == TrophyGameMode.TrophyTrailblazer || GetGameMode() == TrophyGameMode.TrophyPacifist)
            {
                score += CalculateBiomeBonusScore(player);
            }

            score += __m_extraTimeScore;


            // Update the Score string
            if (__m_scoreTextElement)
            {
                if (GetGameMode() == TrophyGameMode.CasualSaga)
                {
                    __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>().text = "Saga";
                }
                else
                {
                    __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>().text = score.ToString();
                }
            }

            // Update the Logouts string
            if (__m_relogsTextElement)
            {
                __m_relogsTextElement.GetComponent<TMPro.TextMeshProUGUI>().text = __m_logoutCount.ToString();
            }

            if (IsPacifist())
            {
                __m_thrallsElement.SetActive(true);
                __m_thrallsTextElement.GetComponent<TMPro.TextMeshProUGUI>().text = __m_allCharmedCharacters.Count.ToString();
            }

            __m_playerCurrentScore = score;

            if (UPDATE_LEADERBOARD)
            {
                // Send the score to the web page
                if (GetGameMode() != TrophyGameMode.CasualSaga)
                {
                    //SendScoreToLeaderboard(score);
                    PostTrackHunt();
                }
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

                    float flashScale = 1 + (timeElapsed / flashDuration);

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

        static float blitzFlashTimer = 0.0f;

        static IEnumerator FlashTrophyBlitz()
        {
            blitzFlashTimer += 0.16f;

            while (__m_blitzFlashing)
            {
                int iconIndex = 0;
                foreach (GameObject go in __m_iconList)
                {
                    if (__m_trophyCache.Find(trophyName => trophyName == go.name) == go.name)
                    {
                        continue;
                    }

                    if (go != null)
                    {
                        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();

                        if (image != null)
                        {
                            float brightness = (float)Math.Sin((double)blitzFlashTimer) + 1.0f;

                            //                            brightness = __m_gameTimerElapsedSeconds % 2 == 0 ? 1.0f : 0.0f;

                            Color color = new Color(brightness, brightness, 0.0f);
                            image.color = color;
                        }
                    }

                    iconIndex++;
                }

                yield return new WaitForSeconds(0.16f);
            }
        }

        static IEnumerator FlashTrophyFiesta()
        {
            int startingColorIndex = 0;
            float elapsedTime = 0f;
            float flashInterval = 0.6f;

            while (__m_fiestaFlashing)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > flashInterval)
                {
                    elapsedTime = 0f;

                    int iconIndex = 0;
                    foreach (GameObject go in __m_iconList)
                    {
                        if (go != null)
                        {
                            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();

                            if (image != null)
                            {
                                int colorIndex = (startingColorIndex + iconIndex) % __m_fiestaColors.Length;

                                if (image.color != Color.white)
                                {
                                    Color color = __m_fiestaColors[colorIndex];
                                    color.a = 0.5f;
                                    image.color = color;
                                }
                            }
                        }

                        iconIndex++;
                    }

                    if (++startingColorIndex >= __m_fiestaColors.Length)
                    {
                        startingColorIndex = 0;
                    }
                }

                yield return null;
            }

            //foreach (GameObject go in __m_iconList)
            //{
            //    if (go != null)
            //    {
            //        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();

            //        if (image != null)
            //        {
            //            image.color = Color.white;
            //        }
            //    }
            //}
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
                        __m_trophyHuntMod.StartCoroutine(FlashImage(image, imageRect));
                        //                        __m_trophyHuntMod.StartCoroutine(DoFlashScore());
                    }
                }
            }
            else
            {
                Debug.LogError($"Unable to find {trophyName} in __m_iconList");
            }
        }

        static public bool __m_isFlashingScore = false;
        static IEnumerator DoFlashScore()
        {
            if (__m_isFlashingScore)
                yield return null;

            if (__m_scoreTextElement != null)
            {
                __m_isFlashingScore = true;
                TMPro.TextMeshProUGUI tmText = __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>();
                Color oldTextColor = tmText.color;
                float oldFontSize = tmText.fontSize;
                for (int count = 0; count < 8; count++)
                {
                    for (float x = 0.0f; x < Math.PI * 2; x += (float)(Math.PI / 6.0))
                    {
                        float scale = (1.0f + (float)Math.Sin((double)x)) * 0.5f;
                        tmText.color = Color.Lerp(Color.black, Color.white, scale); ;
                        tmText.fontSize = oldFontSize + (scale * 1.25f);
                        yield return new WaitForSeconds(0.033f);
                    }
                }
                tmText.color = oldTextColor;
                tmText.fontSize = oldFontSize;
                __m_isFlashingScore = false;
            }
        }

        static void FlashBiomeTrophies(string trophyName)
        {
            TrophyHuntData trophyHuntData = Array.Find(__m_trophyHuntData, element => element.m_name == trophyName);

            BiomeBonus biomeBonus = Array.Find(__m_biomeBonuses, element => element.m_biome == trophyHuntData.m_biome);

            foreach (string biomeTrophyName in biomeBonus.m_trophies)
            {
                GameObject iconGameObject = __m_iconList.Find(gameObject => gameObject.name == biomeTrophyName);
                if (iconGameObject != null)
                {
                    UnityEngine.UI.Image image = iconGameObject.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        RectTransform imageRect = iconGameObject.GetComponent<RectTransform>();

                        if (imageRect != null)
                        {
                            // Flash it with a CoRoutine
                            __m_trophyHuntMod.StartCoroutine(FlashBiomeImage(image, imageRect));
                        }
                    }
                }
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

                        AddPlayerEvent(PlayerEventType.Trophy, name, player.transform.position);

                        AddTrophyPin(player.transform.position, name);

                        if (GetGameMode() == TrophyGameMode.TrophyRush || GetGameMode() == TrophyGameMode.TrophyBlitz || GetGameMode() == TrophyGameMode.TrophyTrailblazer || GetGameMode() == TrophyGameMode.TrophyPacifist)
                        {
                            // Did we complete a biome bonus with this trophy?
                            Biome biome = Biome.Meadows;
                            if (UpdateBiomeBonusTrophies(name, ref biome))
                            {
                                MessageHud.instance.ShowBiomeFoundMsg("Biome Bonus", playStinger: true);

                                string bonusString = "Bonus" + biome.ToString();
                                UpdateModUI(Player.m_localPlayer);
                                AddPlayerEvent(PlayerEventType.Misc, bonusString, __instance.transform.position);
                                Debug.LogError("BIOME BONUS: " + bonusString);
                                player.Message(MessageHud.MessageType.TopLeft, "Biome Bonus: " + biome.ToString());
                                FlashBiomeTrophies(name);
                            }
                        }

                        if (IsSagaMode() || GetGameMode() == TrophyGameMode.TrophyRush || GetGameMode() == TrophyGameMode.TrophyBlitz || GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                        {
                            if (__m_trophyCache.Count == __m_trophyHuntData.Length && !__m_completedAllBiomeBonuses)
                            {
                                MessageHud.instance.ShowBiomeFoundMsg("Odin is Pleased", playStinger: true);
                                string bonusString = "BonusAll";
                                if (GetGameMode() == TrophyGameMode.TrophyBlitz || GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                                {
                                    CalculateExtraTimeScore();
                                }
                                __m_completedAllBiomeBonuses = true;
                                UpdateModUI(Player.m_localPlayer);
                                AddPlayerEvent(PlayerEventType.Misc, bonusString, __instance.transform.position);
                            }
                        }

                        if (MessageHud.instance)
                        {
                            Sprite trophyIcon = GetTrophySprite(name);
                            if (trophyIcon != null)
                            {
                                TrophyHuntData data = Array.Find(__m_trophyHuntData, element => element.m_name == name);
                                //MessageHud.instance.QueueUnlockMsg(trophyIcon, "Trophy Get!", data.m_prettyName + " Trophy");
                            }
                        }
                    }
                }
            }
        }

        // Player Path Collection
        #region Player Path Collection

        public static void StartCollectingPlayerPath()
        {
            if (!__m_collectingPlayerPath)
            {
                //                                    Debug.LogError("Starting Player Path collection");

                //                   AddPlayerPathUI();

                __m_previousPlayerPos = Player.m_localPlayer.transform.position;

                __m_collectingPlayerPath = true;

                __m_trophyHuntMod.StartCoroutine(CollectPlayerPath());
            }
        }

        public static void StopCollectingPlayerPath()
        {
            //                Debug.Log("Stopping Player Path collection");

            if (__m_collectingPlayerPath)
            {
                __m_trophyHuntMod.StopCoroutine(CollectPlayerPath());

                __m_collectingPlayerPath = false;
            }
        }

        public static IEnumerator CollectPlayerPath()
        {
            if (Player.m_localPlayer != null)
            {
                while (__m_collectingPlayerPath && Player.m_localPlayer != null)
                {
                    Vector3 curPlayerPos = Player.m_localPlayer.transform.position;
                    if (Vector3.Distance(curPlayerPos, __m_previousPlayerPos) > __m_minPathPlayerMoveDistance)
                    {
                        __m_playerPathData.Add(curPlayerPos);
                        __m_previousPlayerPos = curPlayerPos;

                        Debug.Log($"Collected player position at {curPlayerPos.ToString()}");
                    }

                    yield return new WaitForSeconds(__m_playerPathCollectionInterval);
                }
            }
        }
        #endregion

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

        // Periodic Timer
        #region Periodic Timer

        public static void StartPeriodicTimer()
        {
            StopPeriodicTimer();
            __m_trophyHuntMod.StartCoroutine(PeriodicTimer());
        }

        public static void StopPeriodicTimer()
        {
            __m_trophyHuntMod.StopCoroutine(PeriodicTimer());
        }

        public static IEnumerator PeriodicTimer()
        {
            if (Player.m_localPlayer != null)
            {
                PostTrackLogs();
                yield return new WaitForSeconds(UPDATE_STANDINGS_INTERVAL);
            }
        }
        #endregion

        // Leaderboard
        #region Leaderboard

        [System.Serializable]
        public class LeaderboardDataEx
        {
            public string event_name;
            public string event_data;
        }

        [System.Serializable]
        public class LeaderboardData
        {
            public string player_name;
            //                public string player_id;
            public int current_score;
            public string session_id;
            public string player_location;
            public string trophies;
            public int deaths;
            public int logouts;
            public string gamemode;
            //                public LeaderboardDataEx[] extra = new LeaderboardDataEx[] { };
        }

        private static void SendScoreToLeaderboard(int score)
        {
            if (!__m_loggedInWithDiscord)
            {
                return;
            }

            if (__m_invalidForTournamentPlay)
            {
                Debug.Log("Invalid for Tournament Play, not sending score to Tracker.");

                return;
            }

            string discordUser = __m_configDiscordUser.Value;
            string discordId = __m_configDiscordId.Value;

            string seed = WorldGenerator.instance.m_world.m_seedName;
            string sessionId = seed.ToString();
            string playerPos = Player.m_localPlayer.transform.position.ToString();
            string trophyList = string.Join(", ", __m_trophyCache);

            // Example data to send to the leaderboard
            var leaderboardData = new LeaderboardData
            {
                player_name = discordId,
                //                    player_id = discordId,
                current_score = score,
                session_id = sessionId,
                player_location = playerPos,
                trophies = trophyList,
                deaths = __m_deaths,
                logouts = __m_logoutCount,
                gamemode = GetGameMode().ToString(),
                //                    extra = new LeaderboardDataEx()
            };
            //LeaderboardDataEx extra = new LeaderboardDataEx();

            //extra.event_name = "trophy_get";
            //extra.event_data = "TrophyBoar";

            //                leaderboardData.extra.AddItem(extra);

            // Start the coroutine to post the data
            __m_trophyHuntMod.StartCoroutine(PostLeaderboardDataCoroutine(LEADERBOARD_URL, leaderboardData));
        }

        private static IEnumerator PostLeaderboardDataCoroutine(string url, LeaderboardData data)
        {
            // Convert the data to JSON
            string jsonData = JsonUtility.ToJson(data);

            //                Debug.Log(jsonData);

            // Create a UnityWebRequest for the POST operation
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //request.certificateHandler = new BypassCertificateHandler();

            // Send the request and wait for a response
            yield return request.SendWebRequest();

            // Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                //                    Debug.Log("Leaderboard POST successful! Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Leaderboard POST failed: " + request.error);
            }

            //                Debug.Log("Leaderboard Response: " + request.error);
            //                Debug.Log(request.downloadHandler.text);
        }

        #endregion Leaderboard

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

        // public void Logout(bool save = true, bool changeToStartScene = true)
        [HarmonyPatch(typeof(Game), nameof(Game.Logout), new[] { typeof(bool), typeof(bool) })]
        public static class Game_Logout_Patch
        {
            public static void Postfix(Game __instance, bool save, bool changeToStartScene)
            {
                if (Player.m_localPlayer == null)
                {
                    return;
                }

                float onFootDistance = GetTotalOnFootDistance(__instance);
                //                    Debug.LogError($"Total on-foot distance moved: {onFootDistance}");

                // If you've never logged out, and your total run/walk distance is less than the max grace distance, no penalty
                if (__m_logoutCount < 1 && onFootDistance < LOGOUT_PENALTY_GRACE_DISTANCE)
                {
                    // ignore this logout
                    return;
                }

                if (!__m_ignoreLogouts)
                {
                    __m_logoutCount++;

                    AddPlayerEvent(PlayerEventType.Misc, "PenaltyLogout", Player.m_localPlayer.transform.position);

                    //                        Debug.LogError($"Game.Logout() logoutCount = {__m_logoutCount}");

                    if (Game.instance != null)
                    {
                        Game.instance.SavePlayerProfile(GetGameMode() != TrophyGameMode.TrophyHunt);
                    }
                }

                StopPeriodicTimer();
                if (IsPacifist())
                {
                    StopCharmTimer();
                }
            }
        }

        #endregion

        #region Tooltips

        // Score Tooltip
        static GameObject __m_scoreTooltipObject = null;
        static GameObject __m_scoreTooltipBackground = null;
        static TextMeshProUGUI __m_scoreTooltipText;
        static Vector2 __m_trophyHuntScoreTooltipWindowSize = new Vector2(240, 215);
        static Vector2 __m_scoreTooltipTextOffset = new Vector2(5, 2);

        static Dictionary<TrophyGameMode, Vector2> __toolTipSizes = new Dictionary<TrophyGameMode, Vector2>()
            {
                { TrophyGameMode.TrophyHunt, new Vector2(240, 215) },
                { TrophyGameMode.TrophyRush, new Vector2(290, 400) },
                { TrophyGameMode.TrophyBlitz, new Vector2(290, 400) },
                { TrophyGameMode.TrophyTrailblazer, new Vector2(290, 400) },
                { TrophyGameMode.TrophyPacifist, new Vector2(290, 400) },
                { TrophyGameMode.CasualSaga, new Vector2(300, 170) },
                { TrophyGameMode.TrophySaga, new Vector2(290, 215) },
                { TrophyGameMode.CulinarySaga, new Vector2(240, 215) },
                { TrophyGameMode.TrophyFiesta, new Vector2(240, 215) }
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

            if (GetGameMode() == TrophyGameMode.CasualSaga)
            {
                text += "<color=white>" + GetSagaRulesText() + "</color>";
            }
            else
            {
                int trophyCount = __m_trophyCache.Count;
                int earnedPoints = 0;
                if (GetGameMode() == TrophyGameMode.CulinarySaga)
                {
                    earnedPoints = CalculateCookingPoints();
                }
                else
                {
                    earnedPoints = CalculateTrophyPoints();
                }

                int penaltyPoints = CalculateLogoutPenalty() + CalculateDeathPenalty();

                text += $"<size=14><color=white>\n";
                if (GetGameMode() == TrophyGameMode.CulinarySaga)
                {
                    text += $"  Dishes Prepared:\n    Num: <color=orange>{__m_cookedFoods.Count}</color> <color=yellow>({earnedPoints} Points)</color>\n";
                }
                else
                {
                    text += $"  Trophies:\n    Num: <color=orange>{trophyCount}</color> <color=yellow>({CalculateTrophyPoints().ToString()} Points)</color>\n";
                }

                text += $"  Logouts: (Penalty: <color=red>{GetLogoutPointCost()}</color>)\n    Num: <color=orange>{__m_logoutCount}</color> <color=yellow>({CalculateLogoutPenalty().ToString()} Points)</color>\n";
                text += $"  Deaths: (Penalty: <color=red>{GetDeathPointCost()}</color>)\n    Num: <color=orange>{__m_deaths}</color> <color=yellow>({CalculateDeathPenalty().ToString()} Points)</color>\n";
                if (GetGameMode() == TrophyGameMode.TrophyRush ||
                    GetGameMode() == TrophyGameMode.TrophyBlitz ||
                    GetGameMode() == TrophyGameMode.TrophyTrailblazer ||
                    GetGameMode() == TrophyGameMode.TrophyPacifist)
                {
                    if (GetGameMode() == TrophyGameMode.TrophyRush)
                    {
                        text += $"  /die's: (Penalty: <color=red>{TROPHY_RUSH_SLASHDIE_PENALTY}</color>)\n    Num: <color=orange>{__m_slashDieCount}</color> <color=yellow>({__m_slashDieCount * TROPHY_RUSH_SLASHDIE_PENALTY} Points)</color>\n";
                        penaltyPoints += __m_slashDieCount * TROPHY_RUSH_SLASHDIE_PENALTY;
                    }
                    text += $"  Biome Bonuses:\n";
                    foreach (BiomeBonus biomeBonus in __m_biomeBonuses)
                    {
                        int numCollected, numTotal, biomeScore;

                        CalculateBiomeBonusStats(biomeBonus.m_biome, out numCollected, out numTotal, out biomeScore);

                        int bonusScore = 0;
                        if (numCollected == numTotal)
                        {
                            bonusScore = biomeScore;
                        }
                        text += $"    {biomeBonus.m_biomeName} (+{biomeBonus.m_bonus}): <color=orange>{numCollected}/{numTotal}</color> <color=yellow>(+{bonusScore} Points)</color>\n";

                        earnedPoints += bonusScore;

                    }
                    if (__m_completedAllBiomeBonuses)
                    {
                        text += $"    All Biomes: <color=orange>{ALL_BIOME_BONUS_SCORE}</color>\n";
                        earnedPoints += ALL_BIOME_BONUS_SCORE;
                    }
                    if (__m_extraTimeScore > 0)
                    {
                        text += $"    Extra Time: <color=orange>{__m_extraTimeScore / EXTRA_MINUTE_SCORE_VALUE}</color> min <color=yellow>(+{__m_extraTimeScore} Points)</color>\n";
                        earnedPoints += __m_extraTimeScore;
                    }
                }

                text += $"<size=17>  Earned Points: <color=orange>{earnedPoints}</color>\n  Penalties: <color=orange>{penaltyPoints}</color></size>\n";
            }

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


        // Luck Tooltips

        static GameObject __m_luckTooltipObject = null;
        static GameObject __m_luckTooltipBackground = null;
        static TextMeshProUGUI __m_luckTooltip;
        static Vector2 __m_luckTooltipWindowSize = new Vector2(220, 135);
        static Vector2 __m_luckTooltipTextOffset = new Vector2(5, 2);

        public static void CreateLuckTooltip()
        {
            // Tooltip Background
            __m_luckTooltipBackground = new GameObject("Luck Tooltip Background");

            // Set %the parent to the HUD
            Transform hudrootTransform = Hud.instance.transform;
            __m_luckTooltipBackground.transform.SetParent(hudrootTransform, false);

            RectTransform bgTransform = __m_luckTooltipBackground.AddComponent<RectTransform>();
            bgTransform.sizeDelta = __m_luckTooltipWindowSize;

            // Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_luckTooltipBackground.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.85f); // Semi-transparent black background

            __m_luckTooltipBackground.SetActive(false);

            // Create a new GameObject for the tooltip
            __m_luckTooltipObject = new GameObject("Luck Tooltip Text");
            __m_luckTooltipObject.transform.SetParent(__m_luckTooltipBackground.transform, false);

            // Add a RectTransform component for positioning
            RectTransform rectTransform = __m_luckTooltipObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(__m_luckTooltipWindowSize.x - __m_luckTooltipTextOffset.x, __m_luckTooltipWindowSize.y - __m_luckTooltipTextOffset.y);

            // Add a TextMeshProUGUI component for displaying the tooltip text
            __m_luckTooltip = AddTextMeshProComponent(__m_luckTooltipObject);
            __m_luckTooltip.fontSize = 14;
            __m_luckTooltip.alignment = TextAlignmentOptions.TopLeft;
            __m_luckTooltip.color = Color.yellow;

            // Initially hide the tooltip
            __m_luckTooltipObject.SetActive(false);
        }

        public static void AddTooltipTriggersToLuckObject(GameObject uiObject)
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
            entryEnter.callback.AddListener((eventData) => ShowLuckTooltip(uiObject));
            trigger.triggers.Add(entryEnter);

            // Mouse Exit event (pointer exits the icon area)
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((eventData) => HideLuckTooltip());
            trigger.triggers.Add(entryExit);
        }

        public struct LuckRating
        {
            public LuckRating(float percent, string luckString, string colorStr)
            {
                m_percent = percent;
                m_luckString = luckString;
                m_colorString = colorStr;
            }
            public float m_percent = 0;
            public string m_luckString = "<n/a>";
            public string m_colorString = "white";
        }

        public static LuckRating[] __m_luckRatingTable = new LuckRating[]
            {
                    new LuckRating (70.0f,      "Bad",          "#BF6000"),
                    new LuckRating (100.0f,     "Average",      "#BFBF00"),
                    new LuckRating (140.0f,     "Good",         "#00BF00"),
                    new LuckRating (9999.0f,    "Bonkers",      "#6000BF"),
            };

        public static int GetLuckRatingIndex(float luckPercentage)
        {
            int index = 0;
            foreach (LuckRating rating in __m_luckRatingTable)
            {
                if (luckPercentage <= rating.m_percent)
                {
                    return index;
                }

                index++;
            }

            return 0;
        }

        public static string GetLuckRatingUIString(float luckPercentage)
        {
            int ratingIndex = GetLuckRatingIndex(luckPercentage);

            LuckRating luckRating = __m_luckRatingTable[ratingIndex];

            return $"<color={luckRating.m_colorString}>{luckRating.m_luckString}</color>";
        }

        public static string BuildLuckTooltipText(GameObject uiObject)
        {
            if (uiObject == null)
            {
                return "Invalid";
            }

            int numTrophyTypesKilled = 0;
            float cumulativeDropRatio = 0f;

            float luckiestScore = float.MinValue;
            string luckiestTrophy = "<n/a>";
            float luckiestActualPercent = 0f;
            float luckiestExpectedPercent = 0f;
            float luckiestRatio = 0f;
            float unluckiestScore = float.MaxValue;
            string unluckiestTrophy = "<n/a>";
            float unluckiestActualPercent = 0f;
            float unluckiestExpectedPercent = 0f;
            float unluckiestRatio = 0f;

            // Compute Luck
            foreach (KeyValuePair<string, DropInfo> entry in __m_allTrophyDropInfo)
            {
                DropInfo di = entry.Value;
                if (di.m_numKilled == 0)
                {
                    continue;
                }

                string trophyName = entry.Key;
                TrophyHuntData data = Array.Find(__m_trophyHuntData, element => element.m_name == trophyName);

                // Ignore 100% drop trophies
                if (data.m_dropPercent >= 100)
                {
                    continue;
                }

                // Ignore if you haven't killed enough to get a drop
                if (di.m_trophies == 0 ||
                    di.m_numKilled < (100 / data.m_dropPercent))
                {
                    continue;
                }

                float actualDropPercent = 100.0f * (float)di.m_trophies / (float)di.m_numKilled;
                float wikiDropPercent = data.m_dropPercent;

                float dropRatio = actualDropPercent / wikiDropPercent;

                if (dropRatio > luckiestScore)
                {
                    luckiestScore = dropRatio;
                    luckiestTrophy = data.m_prettyName;
                    luckiestActualPercent = actualDropPercent;
                    luckiestExpectedPercent = data.m_dropPercent;
                    luckiestRatio = luckiestActualPercent / luckiestExpectedPercent * 100.0f;
                }
                if (dropRatio < unluckiestScore)
                {
                    unluckiestScore = dropRatio;
                    unluckiestTrophy = data.m_prettyName;
                    unluckiestActualPercent = actualDropPercent;
                    unluckiestExpectedPercent = data.m_dropPercent;
                    unluckiestRatio = unluckiestActualPercent / unluckiestExpectedPercent * 100.0f;
                }
                //                    Debug.LogWarning($"Drop: {trophyName}: {dropRatio}");

                cumulativeDropRatio += dropRatio;

                numTrophyTypesKilled++;
            }


            string luckPercentStr = "<n/a>";
            string luckRatingStr = "<n/a>";
            float luckPercentage = 0.0f;
            //                int luckRatingIndex = -1;

            if (numTrophyTypesKilled > 0)
            {
                luckPercentage = (100.0f * (cumulativeDropRatio / (float)numTrophyTypesKilled));
                luckPercentStr = luckPercentage.ToString("0.0");
                luckRatingStr = GetLuckRatingUIString(luckPercentage);
                //                    luckRatingIndex = GetLuckRatingIndex(luckPercentage);

            }

            string text =
                $"<size=16><b><color=#FFB75B>Luck-O-Meter</color><b></size>\n" +
                $"<color=white>  Player Luck Score: </color><color=orange>{luckPercentStr}</color>\n" +
                $"<color=white>  Player Luck Rating: </color>{luckRatingStr}\n";

            //int index = 0;
            //foreach (LuckRating luckRating in __m_luckRatingTable)
            //{
            //    string colorStr = "#606060";
            //    if (index == luckRatingIndex)
            //    {
            //        colorStr = luckRating.m_colorString;
            //    }
            //    text += $"      <color={colorStr}>{luckRating.m_luckString}</color>\n";

            //    index++;
            //}

            string luckiestColor = __m_luckRatingTable[GetLuckRatingIndex(luckiestRatio)].m_colorString;
            string unluckiestColor = __m_luckRatingTable[GetLuckRatingIndex(unluckiestRatio)].m_colorString;

            // Luckiest and Unluckiest
            text += $"<color=white>  Luckiest:</color>\n";
            text += $"    <color={luckiestColor}>{luckiestTrophy}</color> <color=orange>{luckiestActualPercent.ToString("0.0")}%</color> (<color=yellow>{luckiestExpectedPercent}%)</color>\n";
            text += $"<color=white>  Unluckiest:</color>\n";
            text += $"    <color={unluckiestColor}>{unluckiestTrophy}</color> <color=orange>{unluckiestActualPercent.ToString("0.0")}%</color> (<color=yellow>{unluckiestExpectedPercent}%)</color>\n";

            return text;
        }

        public static void ShowLuckTooltip(GameObject uiObject)
        {
            if (uiObject == null)
                return;

            string text = BuildLuckTooltipText(uiObject);

            __m_luckTooltip.text = text;

            __m_luckTooltipBackground.SetActive(true);
            __m_luckTooltipObject.SetActive(true);

            Vector3 tooltipOffset = new Vector3(__m_luckTooltipWindowSize.x / 2, __m_luckTooltipWindowSize.y, 0);
            Vector3 mousePosition = Input.mousePosition;
            Vector3 desiredPosition = mousePosition + tooltipOffset;

            // Clamp the tooltip window onscreen
            if (desiredPosition.x < 150) desiredPosition.x = 150;
            if (desiredPosition.y < 150) desiredPosition.y = 150;
            if (desiredPosition.x > Screen.width - __m_luckTooltipWindowSize.x)
                desiredPosition.x = Screen.width - __m_luckTooltipWindowSize.x;
            if (desiredPosition.y > Screen.height - __m_luckTooltipWindowSize.y)
                desiredPosition.y = Screen.height - __m_luckTooltipWindowSize.y;

            //                Debug.LogWarning($"Luck Tooltip x={desiredPosition.x} y={desiredPosition.y}");

            __m_luckTooltipBackground.transform.position = desiredPosition;
            __m_luckTooltipObject.transform.position = new Vector3(desiredPosition.x + __m_luckTooltipTextOffset.x, desiredPosition.y - __m_luckTooltipTextOffset.y, 0f);
        }

        public static void HideLuckTooltip()
        {
            __m_luckTooltipBackground.SetActive(false);
            __m_luckTooltipObject.SetActive(false);
        }

        // Standings Tooltips

        static GameObject __m_standingsTooltipObject = null;
        static GameObject __m_standingsTooltipBackground = null;
        static TextMeshProUGUI __m_standingsTooltip;
        static Vector2 __m_standingsTooltipWindowSize = new Vector2(250, 300);
        static Vector2 __m_standingsTooltipTextOffset = new Vector2(5, 2);

        public static void CreateStandingsTooltip()
        {
            // Tooltip Background
            __m_standingsTooltipBackground = new GameObject("Standings Tooltip Background");

            // Set %the parent to the HUD
            Transform hudrootTransform = Hud.instance.transform;
            __m_standingsTooltipBackground.transform.SetParent(hudrootTransform, false);

            RectTransform bgTransform = __m_standingsTooltipBackground.AddComponent<RectTransform>();
            bgTransform.sizeDelta = __m_standingsTooltipWindowSize;

            // Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_standingsTooltipBackground.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.90f); // Semi-transparent black background

            __m_standingsTooltipBackground.SetActive(false);

            // Create a new GameObject for the tooltip
            __m_standingsTooltipObject = new GameObject("Standings Tooltip Text");
            __m_standingsTooltipObject.transform.SetParent(__m_standingsTooltipBackground.transform, false);

            // Add a RectTransform component for positioning
            RectTransform rectTransform = __m_standingsTooltipObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(__m_standingsTooltipWindowSize.x - __m_standingsTooltipTextOffset.x, __m_standingsTooltipWindowSize.y - __m_standingsTooltipTextOffset.y);

            // Add a TextMeshProUGUI component for displaying the tooltip text
            __m_standingsTooltip = AddTextMeshProComponent(__m_standingsTooltipObject);
            __m_standingsTooltip.fontSize = 14;
            __m_standingsTooltip.alignment = TextAlignmentOptions.TopLeft;
            __m_standingsTooltip.color = Color.yellow;

            // Initially hide the tooltip
            __m_standingsTooltipObject.SetActive(false);
        }

        public static void AddTooltipTriggersToStandingsObject(GameObject uiObject)
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
            entryEnter.callback.AddListener((eventData) => ShowStandingsTooltip(uiObject));
            trigger.triggers.Add(entryEnter);

            // Mouse Exit event (pointer exits the icon area)
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((eventData) => HideStandingsTooltip());
            trigger.triggers.Add(entryExit);
        }


        public static string BuildStandingsTooltipText(GameObject uiObject)
        {
            string nameString = "<color=yellow>No Tournament Active</color>";
            string modeString = "";

            if (__m_tournamentStatus != TournamentStatus.NotRunning)
            {
                nameString = __m_tournamentName;
                modeString = __m_tournamentMode;
                if (modeString == "")
                {
                    modeString = GetGameMode().ToString();
                }

            }
            string statusString = "<color=red>Not Running</color>";
            if (__m_tournamentStatus == TournamentStatus.Live)
            {
                statusString = "<color=green>Live</color>";
            }
            else
            if (__m_tournamentStatus == TournamentStatus.Over)
            {
                statusString = "<color=yellow>Ended</color>";
            }

            string tooltipText = $"<color=#FFB75B><size=24> Leaderboard</size></color>";
            tooltipText += $"\n   <size=18><color=white> Name: '<color=orange>{nameString}</color>'</color></size>";
            tooltipText += $"\n   <size=16><color=white> Game: <color=orange>{modeString}</color> [</color=yellow>{statusString}</color>]</size></color>\n";

            int size = 20;

            // Sort the list before displaying
            __m_tournamentPlayerInfo.Sort((p1, p2) => p2.score.CompareTo(p1.score));

            foreach (TournamentPlayerInfo info in __m_tournamentPlayerInfo)
            {
                int score = info.score;
                if (info.id == __m_configDiscordId.Value)
                {
                    score = __m_playerCurrentScore;
                }
                tooltipText += $"<indent=10%><size={size}><color=white>{info.name}</color></size><indent=70%><size={size + 2}><color=yellow>{score}</color>\n";
            }

            return tooltipText;
        }

        public static void ShowStandingsTooltip(GameObject uiObject)
        {
            if (uiObject == null)
                return;

            string text = BuildStandingsTooltipText(uiObject);

            __m_standingsTooltip.text = text;

            __m_standingsTooltipBackground.SetActive(true);
            __m_standingsTooltipObject.SetActive(true);

            __m_standingsTooltip.ForceMeshUpdate(true, true);

            Bounds bounds = __m_standingsTooltip.textBounds;

            RectTransform objRectTransform = __m_standingsTooltipObject.GetComponent<RectTransform>();
            RectTransform bgRectTransform = __m_standingsTooltipBackground.GetComponent<RectTransform>();

            Vector2 size = new Vector2(bounds.size.x + 20, bounds.size.y + 10);
            __m_standingsTooltipWindowSize = size;

            objRectTransform.sizeDelta = size;
            bgRectTransform.sizeDelta = size;

            __m_standingsTooltip.ForceMeshUpdate(true, true);

            Vector3 tooltipOffset = new Vector3(__m_standingsTooltipWindowSize.x / 2, __m_standingsTooltipWindowSize.y, 0);
            Vector3 mousePosition = Input.mousePosition;
            Vector3 desiredPosition = mousePosition + tooltipOffset;

            // Clamp the tooltip window onscreen
            if (desiredPosition.x < 150) desiredPosition.x = 150;
            if (desiredPosition.y < 150) desiredPosition.y = 150;
            if (desiredPosition.x > Screen.width - __m_standingsTooltipWindowSize.x)
                desiredPosition.x = Screen.width - __m_standingsTooltipWindowSize.x;
            if (desiredPosition.y > Screen.height - __m_standingsTooltipWindowSize.y)
                desiredPosition.y = Screen.height - __m_standingsTooltipWindowSize.y;

            __m_standingsTooltipBackground.transform.position = desiredPosition;
            __m_standingsTooltipObject.transform.position = new Vector3(desiredPosition.x + __m_standingsTooltipTextOffset.x, desiredPosition.y - __m_standingsTooltipTextOffset.y, 0f);
        }

        public static void HideStandingsTooltip()
        {
            __m_standingsTooltipBackground.SetActive(false);
            __m_standingsTooltipObject.SetActive(false);
        }



        // thralls Tooltips

        static GameObject __m_thrallsTooltipObject = null;
        static GameObject __m_thrallsTooltipBackground = null;
        static TextMeshProUGUI __m_thrallsTooltip;
        static Vector2 __m_thrallsTooltipWindowSize = new Vector2(250, 300);
        static Vector2 __m_thrallsTooltipTextOffset = new Vector2(5, 2);

        public static void CreateThrallsTooltip()
        {
            // Tooltip Background
            __m_thrallsTooltipBackground = new GameObject("thralls Tooltip Background");

            // Set %the parent to the HUD
            Transform hudrootTransform = Hud.instance.transform;
            __m_thrallsTooltipBackground.transform.SetParent(hudrootTransform, false);

            RectTransform bgTransform = __m_thrallsTooltipBackground.AddComponent<RectTransform>();
            bgTransform.sizeDelta = __m_thrallsTooltipWindowSize;

            // Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_thrallsTooltipBackground.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.90f); // Semi-transparent black background

            __m_thrallsTooltipBackground.SetActive(false);

            // Create a new GameObject for the tooltip
            __m_thrallsTooltipObject = new GameObject("thralls Tooltip Text");
            __m_thrallsTooltipObject.transform.SetParent(__m_thrallsTooltipBackground.transform, false);

            // Add a RectTransform component for positioning
            RectTransform rectTransform = __m_thrallsTooltipObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(__m_thrallsTooltipWindowSize.x - __m_thrallsTooltipTextOffset.x, __m_thrallsTooltipWindowSize.y - __m_thrallsTooltipTextOffset.y);

            // Add a TextMeshProUGUI component for displaying the tooltip text
            __m_thrallsTooltip = AddTextMeshProComponent(__m_thrallsTooltipObject);
            __m_thrallsTooltip.fontSize = 14;
            __m_thrallsTooltip.alignment = TextAlignmentOptions.TopLeft;
            __m_thrallsTooltip.color = Color.yellow;

            // Initially hide the tooltip
            __m_thrallsTooltipObject.SetActive(false);
        }
        public static void AddTooltipTriggersToThrallsObject(GameObject uiObject)
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
            entryEnter.callback.AddListener((eventData) => ShowThrallsTooltip(uiObject));
            trigger.triggers.Add(entryEnter);

            // Mouse Exit event (pointer exits the icon area)
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((eventData) => HideThrallsTooltip());
            trigger.triggers.Add(entryExit);
        }

        public static string BuildThrallsTooltipText(GameObject uiObject)
        {
            string text =
                $"<size=18><b><color=#FFB75B>Thralls</color><b></size>\n";

            text += $"\n<size=14><pos=0%><color=white><u>Thrall</u></color><pos=31%><u><color=yellow>(Level)</color></u><pos=50%><color=red><u>Health</u></color><pos=70%><color=orange><u>Remaining</u></color>\n";

            foreach (var cc in __m_allCharmedCharacters)
            {
                Character c = GetCharacterFromGUID(cc.m_charmGUID);
                float remainingTime = cc.m_charmExpireTime - __m_charmTimerSeconds;
                DateTime remainTime = DateTime.MinValue.AddSeconds(remainingTime);
                string timeStr = remainTime.ToString("m'm 's's'");

                text += $"<pos=5%><color=white>{c.GetHoverName()}<pos=40%><color=yellow>({cc.m_charmLevel})</color><pos=50%><color=red>{(int)(c.GetHealthPercentage()*100)}%</color><pos=70%></color><color=orange>{timeStr}</color></size>\n";
            }

            return text;
        }

        public static void ShowThrallsTooltip(GameObject uiObject)
        {
            if (uiObject == null)
                return;

            string text = BuildThrallsTooltipText(uiObject);

            __m_thrallsTooltip.text = text;

            __m_thrallsTooltipBackground.SetActive(true);
            __m_thrallsTooltipObject.SetActive(true);

            __m_thrallsTooltip.ForceMeshUpdate(true, true);

            Bounds bounds = __m_thrallsTooltip.textBounds;

            RectTransform objRectTransform = __m_thrallsTooltipObject.GetComponent<RectTransform>();
            RectTransform bgRectTransform = __m_thrallsTooltipBackground.GetComponent<RectTransform>();

            Vector2 size = new Vector2(bounds.size.x + 20, bounds.size.y + 10);
            __m_thrallsTooltipWindowSize = size;

            objRectTransform.sizeDelta = size;
            bgRectTransform.sizeDelta = size;

            __m_thrallsTooltip.ForceMeshUpdate(true, true);

            Vector3 tooltipOffset = new Vector3(__m_thrallsTooltipWindowSize.x / 2, __m_thrallsTooltipWindowSize.y, 0);
            Vector3 mousePosition = Input.mousePosition;
            Vector3 desiredPosition = mousePosition + tooltipOffset;

            // Clamp the tooltip window onscreen
            if (desiredPosition.x < 150) desiredPosition.x = 150;
            if (desiredPosition.y < 150) desiredPosition.y = 150;
            if (desiredPosition.x > Screen.width - __m_thrallsTooltipWindowSize.x)
                desiredPosition.x = Screen.width - __m_thrallsTooltipWindowSize.x;
            if (desiredPosition.y > Screen.height - __m_thrallsTooltipWindowSize.y)
                desiredPosition.y = Screen.height - __m_thrallsTooltipWindowSize.y;

            __m_thrallsTooltipBackground.transform.position = desiredPosition;
            __m_thrallsTooltipObject.transform.position = new Vector3(desiredPosition.x + __m_thrallsTooltipTextOffset.x, desiredPosition.y - __m_thrallsTooltipTextOffset.y, 0f);
        }

        public static void HideThrallsTooltip()
        {
            __m_thrallsTooltipBackground.SetActive(false);
            __m_thrallsTooltipObject.SetActive(false);
        }


        // Trophy Tooltips

        static GameObject __m_trophyTooltipObject = null;
        static GameObject __m_trophyTooltipBackground = null;
        static TextMeshProUGUI __m_trophyTooltip;
        static Vector2 __m_trophyTooltipWindowSize = new Vector2(240, 125);
        static Vector2 __m_trophyTooltipTextOffset = new Vector2(5, 2);
        static Vector2 __m_trophyTooltipAllTrophyStatsWindowSize = new Vector2(240, 195);

        public static void CreateTrophyTooltip()
        {
            //                Debug.LogWarning("Creating Tooltip object");

            Vector2 tooltipWindowSize = __m_trophyTooltipWindowSize;
            if (__m_showAllTrophyStats)
            {
                tooltipWindowSize = __m_trophyTooltipAllTrophyStatsWindowSize;
            }

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

        public static void CalculateDropPercentAndRating(TrophyHuntData trophyHuntData, DropInfo dropInfo, out string dropPercentStr, out string dropRatingStr)
        {
            dropPercentStr = "0";
            dropRatingStr = "<n/a>";

            if (dropInfo.m_numKilled > 0)
            {
                float dropPercent = 0.0f;
                float expectedDropPercent = trophyHuntData.m_dropPercent;

                dropPercent = (100.0f * ((float)dropInfo.m_trophies / (float)dropInfo.m_numKilled));
                dropPercentStr = dropPercent.ToString("0.0");

                // Don't compute for 100% drop enemies
                if (trophyHuntData.m_dropPercent < 100)
                {
                    if (dropInfo.m_trophies > 0 &&
                        dropInfo.m_numKilled >= (100 / expectedDropPercent))
                    {
                        float ratingPercent = 100 * (dropPercent / expectedDropPercent);
                        dropRatingStr = GetLuckRatingUIString(ratingPercent);
                    }
                }
            }
        }

        public static string BuildCookingTooltipText(GameObject uiObject)
        {
            if (uiObject == null)
            {
                return "ERROR";
            }

            ConsumableData cookedFoodData = Array.Find(__m_cookedFoodData, element => element.m_prefabName == uiObject.name);

            string text =
                $"<size=16><b><color=#FFB75B>{cookedFoodData.m_displayName}</color><b></size>\n" +
                $"<color=white>Point Value: </color><color=green>{cookedFoodData.m_points}</color>\n" +
                $"<color=white>  Health: </color><color=orange>{cookedFoodData.m_health}</color>\n" +
                $"<color=white>  Stamina: </color><color=orange>{cookedFoodData.m_stamina}</color>\n" +
                $"<color=white>  Regen: </color><color=orange>{cookedFoodData.m_regen}</color>/sec\n" +
                $"<color=white>  Eitr: <color=orange>{cookedFoodData.m_eitr}</color>\n" +
                $"<color=white>  Biome: <color=orange>{cookedFoodData.m_biome.ToString()}</color>\n";

            return text;
        }

        public static string BuildTrophyTooltipText(GameObject uiObject)
        {
            if (uiObject == null)
            {
                return "Invalid";
            }

            string trophyName = uiObject.name;

            TrophyHuntData trophyHuntData = Array.Find(__m_trophyHuntData, element => element.m_name == trophyName);

            DropInfo allTrophyDropInfo = __m_allTrophyDropInfo[trophyName];
            DropInfo playerDropInfo = __m_playerTrophyDropInfo[trophyName];

            //                Debug.LogWarning($"dropped: {dropInfo.m_trophiesDropped} killed: {dropInfo.m_numKilled} percent:{trophyHuntData.m_dropPercent}");

            string playerDropPercentStr = "0";
            string playerDropRatingStr = "<n/a>";

            CalculateDropPercentAndRating(trophyHuntData, playerDropInfo, out playerDropPercentStr, out playerDropRatingStr);

            string allTrophyDropPercentStr = "0";
            string allTrophyDropRatingStr = "<n/a>";

            CalculateDropPercentAndRating(trophyHuntData, allTrophyDropInfo, out allTrophyDropPercentStr, out allTrophyDropRatingStr);

            string dropWikiPercentStr = trophyHuntData.m_dropPercent.ToString();

            string text =
                $"<size=16><b><color=#FFB75B>{trophyHuntData.m_prettyName}</color><b></size>\n" +
                $"<color=white>Point Value: </color><color=green>{trophyHuntData.GetCurGameModeTrophyScoreValue()}</color>\n" +
                $"<color=white>Player Kills: </color><color=orange>{playerDropInfo.m_numKilled}</color>\n" +
                $"<color=white>Trophies Picked Up: </color><color=orange>{playerDropInfo.m_trophies}</color>\n" +
                $"<color=white>Kill/Pickup Rate: </color><color=orange>{playerDropPercentStr}%</color>\n" +
                $"<color=white>Wiki Trophy Drop Rate: (<color=orange>{dropWikiPercentStr}%)</color>\n" +
                $"<color=white>Player Luck Rating: <color=yellow>{playerDropRatingStr}</color>\n";

            if (__m_showAllTrophyStats)
            {
                text = text +
                $"<color=white>Actual Kills: </color><color=orange>{allTrophyDropInfo.m_numKilled}</color>\n" +
                $"<color=white>Actual Trophies: </color><color=orange>{allTrophyDropInfo.m_trophies}</color>\n" +
                $"<color=white>Actual Drop Rate: </color><color=orange>{allTrophyDropPercentStr}%</color> (<color=yellow>{dropWikiPercentStr}%)</color>\n" +
                $"<color=white>Actual Luck Rating: <color=yellow>{allTrophyDropRatingStr}</color>\n";

            }
            return text;
        }

        public static void ShowTrophyTooltip(GameObject uiObject)
        {
            if (uiObject == null)
                return;

            string text = "";

            if (GetGameMode() == TrophyGameMode.CulinarySaga)
            {
                text = BuildCookingTooltipText(uiObject);
            }
            else
            {
                text = BuildTrophyTooltipText(uiObject);
            }
            __m_trophyTooltip.text = text;

            __m_trophyTooltipBackground.SetActive(true);
            __m_trophyTooltipObject.SetActive(true);

            Vector2 tooltipSize = __m_trophyTooltipWindowSize;
            if (__m_showAllTrophyStats)
                tooltipSize = __m_trophyTooltipAllTrophyStatsWindowSize;

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

        public static void RecordDroppedTrophy(string characterName, string trophyName)
        {
            DropInfo drop = __m_allTrophyDropInfo[trophyName];
            drop.m_trophies++;
            __m_allTrophyDropInfo[trophyName] = drop;
        }

        public static string EnemyNameToTrophyName(string enemyName)
        {
            int index = Array.FindIndex(__m_trophyHuntData, element => element.m_enemies.Contains(enemyName));
            if (index < 0) return "Not Found";

            return __m_trophyHuntData[index].m_name;
        }

        public static bool RecordPlayerPickedUpTrophy(string trophyName)
        {
            if (__m_playerTrophyDropInfo.ContainsKey(trophyName))
            {
                DropInfo drop = __m_playerTrophyDropInfo[trophyName];
                drop.m_trophies++;
                __m_playerTrophyDropInfo[trophyName] = drop;

                return true;
            }

            return false;
        }

        public static void RecordTrophyCapableKill(string characterName, bool killedByPlayer)
        {
            string trophyName = EnemyNameToTrophyName(characterName);

            if (killedByPlayer)
            {
                //                    Debug.Log($"{characterName} killed by Player");

                DropInfo drop = __m_playerTrophyDropInfo[trophyName];
                drop.m_numKilled++;

                __m_playerTrophyDropInfo[trophyName] = drop;

            }
            else
            {
                //                    Debug.Log($"{characterName} killed not by Player");

                DropInfo drop = __m_allTrophyDropInfo[trophyName];
                drop.m_numKilled++;

                __m_allTrophyDropInfo[trophyName] = drop;
            }
        }

        public struct SpecialSagaDrop
        {
            public SpecialSagaDrop(string itemName, float dropPercent, int dropAmountMin, int dropAmountMax, bool dropOnlyOne = false, bool stopDroppingOnPickup = false, TrophyGameMode onlyInMode = TrophyGameMode.Max)
            {
                m_itemName = itemName;
                m_dropPercent = dropPercent;
                m_dropAmountMin = dropAmountMin;
                m_dropAmountMax = dropAmountMax;
                m_dropOnlyOne = dropOnlyOne;
                m_stopDroppingOnPickup = stopDroppingOnPickup;
                m_numDropped = 0;
                m_numPickedUp = 0;
                m_onlyInMode = onlyInMode;
            }

            public string m_itemName;
            public float m_dropPercent;
            public int m_dropAmountMin;
            public int m_dropAmountMax;
            public bool m_dropOnlyOne;
            public bool m_stopDroppingOnPickup;
            public TrophyGameMode m_onlyInMode;
            public int m_numDropped;
            public int m_numPickedUp;
        }

        static public Dictionary<string, List<SpecialSagaDrop>> __m_specialSagaDrops = new Dictionary<string, List<SpecialSagaDrop>>
            {
                {
                    "$enemy_greyling",          new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FineWood",        50,  3, 10, false),
                                                    new SpecialSagaDrop("Coal",             5,  4, 4, false),
                                                    new SpecialSagaDrop("TrophyDeer",       5,  1, 1, false),
                                                    new SpecialSagaDrop("RoundLog",        10,  2, 7, false),
//                                                    new SpecialSagaDrop("ArrowFlint",       5,  2, 4, false),
                                                    new SpecialSagaDrop("BoneFragments",    8,  1, 3, false),
                                                    new SpecialSagaDrop("Flint",            8,  1, 3, false),
                                                    new SpecialSagaDrop("LeatherScraps",    10, 2, 3, false),
                                                    new SpecialSagaDrop("DeerHide",         4,  1, 3, false),
                                                    new SpecialSagaDrop("Feathers",         20, 4, 8, false),
//                                                    new SpecialSagaDrop("CookedDeerMeat",   8,  1, 2, false),
                                                    new SpecialSagaDrop("Acorn",            3,  1, 2, false),
                                                    new SpecialSagaDrop("CarrotSeeds",      8,  1, 3, false),
                                                    new SpecialSagaDrop("TurnipSeeds",      4,  1, 3, false),
                                                    new SpecialSagaDrop("QueenBee",         6,  1, 1, false),
                                                    new SpecialSagaDrop("Honey",            8,  2, 3, false),
                                                    new SpecialSagaDrop("Blueberries",      7,  2, 4, false),

                                                    new SpecialSagaDrop("BeltStrength",     15,  1, 1, false, true)
                                                }
                },
                {
                    "$enemy_neck",              new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FishingBait", 25,  1, 5, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },

                // The Elder Boss Item Drop
                {
                    "$enemy_greydwarfbrute",    new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("CryptKey",        100,  1, 1, false, true),
                                                    new SpecialSagaDrop("FishingRod",      100,  1, 1, true, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                {
                    "$enemy_troll",             new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("BeltStrength",    100,  1, 1, false, true),
                                                    new SpecialSagaDrop("TrollHide",       100,  5, 5, false),
                                                    new SpecialSagaDrop("FishingBaitForest",      50,  1, 5, true, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                {
                    "$enemy_skeletonfire",      new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("CryptKey",        100, 1, 1, true),
                                                }
                },
                {
                    "$enemy_skeletonpoison",    new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("MaceIron",        100,  1, 1, true),
                                                }
                },

                // Bonemass Boss Item Drop
                {
                    "$enemy_blobelite",         new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("Wishbone",       100,  1, 1, false, true),
                                                    new SpecialSagaDrop("Ooze",           100,  2, 5, false),
                                                    new SpecialSagaDrop("FishingRod",      100,  1, 1, true, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                {
                    "$enemy_blob",         new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("Ooze",           100,  2, 5, false),
                                                }
                },
                {
                    "$enemy_abomination",       new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FishingBaitSwamp", 25,  1, 5, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                // $enemy_abomination

                // Moder Boss Item Drop
                {
                    // Drake
                    "$enemy_drake",           new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("DragonTear",      100,  1, 2, false),
                                                    new SpecialSagaDrop("FishingRod",      100,  1, 1, true, false, TrophyGameMode.CulinarySaga),
                                                    new SpecialSagaDrop("FishingBaitDeepNorth", 15,  1, 5, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                {
                    // Geirrhafa
                    "$enemy_fenringcultist_hildir", new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("DragonTear",      100,  2, 3, true),
                                                }
                },
                {
                    "$enemy_fenring",           new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FishingBaitCave", 25,  1, 10, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },

                // Yagluth Boss Item Drop
                {
                    "$enemy_goblin",      new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FishingBaitPlains", 15,  1, 5, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },
               {
                    "$enemy_goblinshaman",      new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YagluthDrop",     100,  1, 1, false, true),
                                                    new SpecialSagaDrop("FishingRod",      100,  1, 1, true, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                {
                    "$enemy_goblinbrute",       new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YagluthDrop",     100,  1, 1, false, true),
                                                    new SpecialSagaDrop("FishingRod",      100,  1, 1, true, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                {
                    "$enemy_goblin_hildir",     new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YagluthDrop",     100,  1, 1, true),
                                                }
                },
                {
                    "$enemy_goblinbrute_hildircombined", new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YagluthDrop",     100,  1, 1, true),
                                                }
                },
                {
                    "$enemy_lox",               new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FishingBaitMistlands", 45,  1, 5, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },

                // Queen Boss Item Drop
                {
                    "$enemy_seekerbrute",    new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("QueenDrop",       100,  1, 1, false, true),
                                                    new SpecialSagaDrop("FishingRod",      100,  1, 1, true, false, TrophyGameMode.CulinarySaga),
                                                }
                },

                {
                    "$enemy_dvergr",    new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YggdrasilWood",   100,  10, 20, false),
//                                                    new SpecialSagaDrop("BlackCore",       100,  2, 3, false),
                                                }
                },
                {
                    "$enemy_dvergr_mage",    new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YggdrasilWood",   100,  10, 20, false),
//                                                    new SpecialSagaDrop("BlackCore",       100,  2, 3, false),
                                                }
                },
                {
                    "$enemy_serpent",               new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FishingBaitOcean", 100,  20, 20, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },
                {
                    "$enemy_charred_melee",               new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("FishingBaitAshlands", 25,  1, 5, false, false, TrophyGameMode.CulinarySaga),
                                                }
                },

                {
                    "$enemy_gdking",            new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YmirRemains",     100,  10, 10, true),
                                                }
                },
                {
                    "$enemy_bonemass",            new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YmirRemains",     100,  10, 10, true),
                                                }
                },
                {
                    "$enemy_dragon",            new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YmirRemains",     100,  10, 10, true),
                                                }
                },
                {
                    "$enemy_seekerqueen",            new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YmirRemains",     100,  10, 10, true),
                                                }
                },
                {
                    "$enemy_fader",            new List<SpecialSagaDrop>
                                                {
                                                    new SpecialSagaDrop("YmirRemains",     100,  10, 10, true),
                                                }
                },

            };

        public static void InitializeSagaDrops()
        {
            List<string> keys = new List<string>(__m_specialSagaDrops.Keys);
            foreach (string key in keys)
            {
                List<SpecialSagaDrop> dropList = __m_specialSagaDrops[key];
                for (int i = 0; i < dropList.Count; i++)
                {
                    SpecialSagaDrop drop = dropList[i];

                    drop.m_numDropped = 0;
                    drop.m_numPickedUp = 0;
                    dropList[i] = drop;
                }
                __m_specialSagaDrops[key] = dropList;
            }
        }

        public static bool HasAnyoneDropped(string itemName)
        {
            bool hasDropped = false;

            foreach (KeyValuePair<string, List<SpecialSagaDrop>> specialDrops in __m_specialSagaDrops)
            {
                foreach (SpecialSagaDrop sagaDrop in specialDrops.Value)
                {
                    if (sagaDrop.m_itemName == itemName)
                    {
                        if (sagaDrop.m_numDropped > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return hasDropped;
        }
        public static bool HasBeenPickedUp(string itemName)
        {
            bool hasDropped = false;

            foreach (KeyValuePair<string, List<SpecialSagaDrop>> specialDrops in __m_specialSagaDrops)
            {
                foreach (SpecialSagaDrop sagaDrop in specialDrops.Value)
                {
                    if (sagaDrop.m_itemName == itemName)
                    {
                        if (sagaDrop.m_numPickedUp > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return hasDropped;
        }


        // Watch character drops and see what characters drop what items (actual dropped items)
        //
        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        class CharacterDrop_GenerateDropList_Patch
        {
            static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
            {
                if (__instance != null)
                {
                    Character character = __instance.GetComponent<Character>();

                    string characterName = character.m_name;

                    //                    Debug.LogError($"CharacterDrop_GenerateDropList_Patch: {characterName} has dropped items: {__result?.Count}");

                    // See if this is a trophy-dropper and handle any special trophy rules for the various game modes
                    //
                    if (CharacterCanDropTrophies(characterName))
                    {

                        //                              Debug.Log($"Trophy-capable character {characterName} has dropped items:");

                        RecordTrophyCapableKill(characterName, false);

                        bool droppedTrophy = false;

                        // Check if there are any dropped items
                        if (__result != null)
                        {
                            foreach (KeyValuePair<GameObject, int> droppedItem in __result)
                            {
                                // Get the item's name
                                string itemName = droppedItem.Key.name;

                                // Log or process the dropped item
                                //                                    Debug.Log($"Dropped item: {itemName} count: {droppedItem.Value}");

                                if (itemName.Contains("Trophy"))
                                {
                                    //                                        Debug.Log($"Trophy {itemName} Dropped by {characterName}");

                                    RecordDroppedTrophy(characterName, itemName);

                                    droppedTrophy = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Debug.Log($"Trophy-capable character {characterName} had null drop list");
                        }

                        if (!droppedTrophy)
                        {
                            float dropPercentage = 0f;

                            if (GetGameMode() == TrophyGameMode.TrophyRush ||
                                GetGameMode() == TrophyGameMode.TrophyBlitz ||
                                GetGameMode() == TrophyGameMode.TrophyTrailblazer ||
                                GetGameMode() == TrophyGameMode.TrophyPacifist ||
                                (IsSagaMode() && GetGameMode() != TrophyGameMode.CasualSaga))
                            {
                                string trophyName = EnemyNameToTrophyName(characterName);
                                if (!__m_trophyCache.Contains(trophyName) || trophyName == "TrophyDeer")
                                {
                                    dropPercentage = 100f;
                                }
                            }
                            //else if (GetGameMode() == TrophyGameMode.TrophySaga)
                            //{
                            //    int index = Array.FindIndex(__m_trophyHuntData, element => element.m_enemies.Contains(characterName));
                            //    if (index >= 0)
                            //    {
                            //        float wikiDropPercent = __m_trophyHuntData[index].m_dropPercent;

                            //        // Cap at 50% drop rate
                            //        dropPercentage = Math.Min(wikiDropPercent * TROPHY_SAGA_TROPHY_DROP_MULTIPLIER, 50f);
                            //    }
                            //}

                            // Roll the dice
                            System.Random randomizer = new System.Random();
                            float randValue = (float)randomizer.NextDouble() * 100f;

                            // If we rolled below drop percentage, drop a trophy
                            if (randValue < dropPercentage)
                            {
                                string trophyName = EnemyNameToTrophyName(characterName);

                                List<Drop> dropList = __instance.m_drops;

                                Drop trophyDrop = dropList.Find(theDrop => theDrop.m_prefab.name == trophyName);

                                if (trophyDrop != null)
                                {
                                    KeyValuePair<GameObject, int> newDropItem = new KeyValuePair<GameObject, int>(trophyDrop.m_prefab, 1);

                                    if (__result == null)
                                    {
                                        __result = new List<KeyValuePair<GameObject, int>>();
                                    }
                                    __result.Add(newDropItem);

                                    RecordDroppedTrophy(characterName, trophyName);
                                }
                            }
                        }
                    }

                    // Check to see if we need to add any special drops to this character
                    // ex: Saga only Greyling drops
                    if (IsSagaMode())
                    {
                        //                            Debug.LogWarning($"Saga drops for {characterName}?");

                        if (__m_specialSagaDrops.ContainsKey(characterName))
                        {
                            List<SpecialSagaDrop> enemySagaDrops = __m_specialSagaDrops[characterName];

                            System.Random randomizer = new System.Random(Guid.NewGuid().GetHashCode());

                            for (int i = 0; i < enemySagaDrops.Count; i++)
                            {
                                SpecialSagaDrop sagaDrop = enemySagaDrops[i];

                                //                                    Debug.LogWarning($"{sagaDrop.m_itemName} {sagaDrop.m_onlyInMode.ToString()} ({GetGameMode()}");

                                // If it only drops in a specific game mode
                                if (sagaDrop.m_onlyInMode != TrophyGameMode.Max)
                                {
                                    // And we're not in that mode currently
                                    if (GetGameMode() != sagaDrop.m_onlyInMode)
                                    {
                                        //                                        Debug.LogWarning($"{sagaDrop.m_itemName} Ignored");
                                        continue;
                                    }
                                }

                                bool alreadyDropped = false;
                                //                                    Debug.LogWarning($"{characterName} {sagaDrop.m_itemName} numDrops: {sagaDrop.m_numDropped}");
                                if (sagaDrop.m_dropOnlyOne)
                                {
                                    alreadyDropped = HasAnyoneDropped(sagaDrop.m_itemName);
                                }

                                if (sagaDrop.m_stopDroppingOnPickup)
                                {
                                    alreadyDropped = HasBeenPickedUp(sagaDrop.m_itemName);
                                }
                                // If it's only meant to drop once, just ignore additional drops
                                if (alreadyDropped)
                                {
                                    //                                        Debug.LogWarning($"{characterName} already dropped {sagaDrop.m_itemName}");

                                    continue;
                                }

                                float randValue = (float)randomizer.NextDouble() * 100f;

                                if (randValue < sagaDrop.m_dropPercent)
                                {
                                    //                                        Debug.LogWarning($"{characterName} passed check to drop {sagaDrop.m_itemName}");
                                    GameObject prefab = ObjectDB.instance.GetItemPrefab(sagaDrop.m_itemName);
                                    if (prefab != null)
                                    {
                                        int itemCount = randomizer.Next(sagaDrop.m_dropAmountMin, sagaDrop.m_dropAmountMax);

                                        KeyValuePair<GameObject, int> newDropItem = new KeyValuePair<GameObject, int>(prefab, itemCount);

                                        if (__result != null)
                                        {
                                            __result.Add(newDropItem);

                                            //                                            Debug.LogWarning($"{characterName} dropping {itemCount} {sagaDrop.m_itemName}");

                                            sagaDrop.m_numDropped += itemCount;

                                            enemySagaDrops[i] = sagaDrop;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        public class Character_OnDeath_Patch
        {
            static bool Prefix(Character __instance)
            {
                if (IsPacifist())
                {
                    if (IsCharmed(__instance))
                    {
//                        Debug.LogWarning($"Trophy Pacifist Mode: {__instance.m_name} was charmed and has died.");

                        // Post player message saying charmed enemy died
                        if (Player.m_localPlayer != null)
                        {
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Your thrall {__instance.m_name} has fallen!");
                        }
                        RemoveFromCharmedList(__instance);
                    }
                }

                return true;
            }

            static void Postfix(Character __instance)
            {
                if (GetGameMode() == TrophyGameMode.CulinarySaga)
                    return;

                Character character = __instance;

                // Check if the attacker is the local player
                bool playerHit = false;
                if (Player.m_localPlayer != null &&
                    character.m_lastHit != null &&
                    character.m_lastHit.GetAttacker() == Player.m_localPlayer)
                {
                    playerHit = true;
                }

                if (playerHit)
                {
                    // The local player killed this enemy
                    //                        Debug.Log($"Player killed {__instance.name}");

                    string characterName = __instance.m_name;
                    if (CharacterCanDropTrophies(characterName))
                    {
                        //                            Debug.Log($"Trophy-capable character {characterName} was killed by Player.");

                        RecordTrophyCapableKill(characterName, true);
                    }
                }

                if (GetGameMode() == TrophyGameMode.TrophyBlitz || GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    // Check for bosses dying and unlock future boss locations
                    RevealNextBoss(__instance.m_name);
                }
            }
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

//                { "Sap",                "Eitr"}
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

//                { "Sap",                "Eitr"}
            };


        public static void ConvertMetal(ref ItemDrop.ItemData itemData)
        {
            if (!IsSagaMode() || !__m_instaSmelt)
                return;

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
                if (!IsSagaMode())
                {
                    return true;
                }

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
                if (!IsSagaMode())
                {
                    return true;
                }

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

        public static void ConvertMetalOresIfNecessary(ref ItemDrop.ItemData item)
        {
            if (IsSagaMode())
            {
                // Item successfully added to inventory
                if (__m_instaSmelt)
                {
                    ConvertMetal(ref item);
                }

                if (GetGameMode() == TrophyGameMode.CulinarySaga)
                {
                    bool isFood = false;
                    foreach (ConsumableData cd in __m_cookedFoodData)
                    {
                        if (cd.m_prefabName == item.m_dropPrefab.name)
                        {
                            isFood = true;
                            break;
                        }
                    }

                    if (isFood)
                    {
                        if (!__m_cookedFoods.Contains(item.m_dropPrefab.name))
                        {
                            FlashTrophy(item.m_dropPrefab.name);

                            __m_cookedFoods.Add(item.m_dropPrefab.name);

                            if (__m_cookedFoods.Count == __m_cookedFoodData.Length)
                            {
                                MessageHud.instance.ShowBiomeFoundMsg("Odin is Sated", playStinger: true);
                            }
                        }
                    }
                    UpdateModUI(Player.m_localPlayer);
                }
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
                //            Debug.LogWarning($"Inventory.AddItem() {item.m_dropPrefab.name}");
                if (IsSagaMode() && __m_instaSmelt)
                {
                    if (__instance != null && Player.m_localPlayer != null
                        && __instance == Player.m_localPlayer.GetInventory())
                    {
                        ConvertMetalOresIfNecessary(ref item);
                    }
                }
            }
        }

        // this is called when dropped into an inventory slot with the mouse
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
        public class Inventory_AddItem_4_Patch
        {
            static void Postfix(Inventory __instance, ItemDrop.ItemData item, int amount, int x, int y, bool __result)
            {
                //               Debug.LogWarning($"Inventory.AddItem4() Postfix {amount} {x} {y}");

                if (__instance != null && Player.m_localPlayer != null
                    && __instance == Player.m_localPlayer.GetInventory() && item != null && item.m_dropPrefab != null)
                {
                    string itemName = item.m_dropPrefab.name;

                    if (item.m_quality > 1)
                    {
                        itemName += item.m_quality.ToString();
                    }

                    AddPlayerEvent(PlayerEventType.Item, itemName, Player.m_localPlayer.transform.position);
                }
            }


            static bool Prefix(Inventory __instance, ItemDrop.ItemData item, int amount, int x, int y, ref bool __result)
            {
                //                Debug.LogWarning($"Inventory.AddItem() Prefix {item.m_dropPrefab.name} stack={amount}, pos=({x},{y})");

                if (!IsSagaMode())
                {
                    // Run original function with no modifications
                    return true;
                }

                if (Player.m_localPlayer == null)
                {
                    // Run original function with no modifications
                    return true;
                }

                if (__instance != Player.m_localPlayer.GetInventory())
                {
                    // Run original function with no modifications
                    return true;
                }

                //if (__instance == null || item == null || item.m_dropPrefab == null)
                //{
                //    __result = false;
                //    return false;
                //}

                amount = Mathf.Min(amount, item.m_stack);
                if (x < 0 || y < 0 || x >= __instance.m_width || y >= __instance.m_height)
                {
                    __result = false;
                    return false;
                }
                bool flag = false;
                ItemDrop.ItemData itemAt = __instance.GetItemAt(x, y);
                if (itemAt != null)
                {
                    if (itemAt.m_shared.m_name != item.m_shared.m_name || itemAt.m_worldLevel != item.m_worldLevel || (itemAt.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality))
                    {
                        __result = false;
                        return false;
                    }
                    int num = itemAt.m_shared.m_maxStackSize - itemAt.m_stack;
                    if (num <= 0)
                    {
                        __result = false;
                        return false;
                    }
                    int num2 = Mathf.Min(num, amount);
                    itemAt.m_stack += num2;
                    item.m_stack -= num2;
                    flag = num2 == amount;
                    ZLog.Log((object)("Added to stack" + itemAt.m_stack + " " + item.m_stack));
                }
                else
                {
                    ItemDrop.ItemData itemData = item.Clone();
                    ConvertMetalOresIfNecessary(ref itemData);
                    itemData.m_stack = amount;
                    itemData.m_gridPos = new Vector2i(x, y);
                    __instance.m_inventory.Add(itemData);
                    item.m_stack -= amount;
                    flag = true;
                }

                __instance.Changed();

                __result = flag;

                return false;
            }
            /* TESTING SHORTCUT OF LOGIC
            static bool Prefix(Inventory __instance, ref ItemDrop.ItemData item, int amount, int x, int y, ref bool __result)
            {
                if (__instance != null && Player.m_localPlayer != null
                    && __instance == Player.m_localPlayer.GetInventory())
                {
                    ConvertMetalOresIfNecessary(ref item);
                    __result = false;
                }

                return true;
            }
            */

            /* ORIGINAL FUNCTION
            static bool Prefix(Inventory __instance, ItemDrop.ItemData item, int amount, int x, int y, ref bool __result)
            {
                amount = Mathf.Min(amount, item.m_stack);
                if (x < 0 || y < 0 || x >= __instance.m_width || y >= __instance.m_height)
                {
                    __result = false;
                    return false;
                }
                bool flag = false;
                ItemDrop.ItemData itemAt = __instance.GetItemAt(x, y);
                if (itemAt != null)
                {
                    if (itemAt.m_shared.m_name != item.m_shared.m_name || itemAt.m_worldLevel != item.m_worldLevel || (itemAt.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality))
                    {
                        __result = false;
                        return false;
                    }
                    int num = itemAt.m_shared.m_maxStackSize - itemAt.m_stack;
                    if (num <= 0)
                    {
                        __result = false;
                        return false;
                    }
                    int num2 = Mathf.Min(num, amount);
                    itemAt.m_stack += num2;
                    item.m_stack -= num2;
                    flag = num2 == amount;
                    ZLog.Log((object)("Added to stack" + itemAt.m_stack + " " + item.m_stack));
                }
                else
                {
                    ItemDrop.ItemData itemData = item.Clone();
                    itemData.m_stack = amount;
                    itemData.m_gridPos = new Vector2i(x, y);
                    __instance.m_inventory.Add(itemData);
                    item.m_stack -= amount;
                    flag = true;
                }
                __instance.Changed();
                __result = flag;
                
                return false;
            }
            */
        }
        // This is called when items are upgraded, so need to log upgrades as well as pickups
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool) })]
        public class Inventory_AddItem_2_Patch
        {
            static void Postfix(Inventory __instance, string name, int stack, int quality, int variant, long crafterID, string crafterName, Vector2i position, bool pickedUp)
            {

                //                Debug.LogWarning($"Inventory.AddItem2() {name}");

                if (__instance != null)
                {
                    string itemName = name;

                    if (quality > 1)
                    {
                        itemName += quality.ToString();
                    }

                    AddPlayerEvent(PlayerEventType.Item, itemName, Player.m_localPlayer.transform.position);
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
                    //                    Debug.LogWarning($"Inventory.CanAddItem() {item.m_dropPrefab.name}");

                    if (IsSagaMode())
                    {
                        // Item successfully added to inventory
                        if (__m_instaSmelt)
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

                                    __result = __instance.FindFreeStackSpace(itemName, 0) + (__instance.m_width * __instance.m_height - __instance.m_inventory.Count) * item.m_shared.m_maxStackSize >= stack;

                                    //                                        Debug.LogWarning($"CanAddItem {prefabName} result {__result} : {itemName}");

                                    return false;
                                }
                            }
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
                // Before pickup occurs, see if it's auto-smeltable ore and convert it^
                if (__instance == null || __instance != Player.m_localPlayer)
                {
                    return;
                }

                ItemDrop itemDrop = go.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (IsSagaMode())
                    {
                        if (__m_instaSmelt)
                        {
                            ConvertMetal(ref itemDrop.m_itemData);
                        }

                        // Check to see if we picked up something that's a SpecialSagaDrop
                        if (itemDrop.m_itemData != null && itemDrop.m_itemData.m_dropPrefab != null)
                        {
                            string itemName = itemDrop.m_itemData.m_dropPrefab.name;

                            foreach (KeyValuePair<string, List<SpecialSagaDrop>> specialDrops in __m_specialSagaDrops)
                            {
                                string merbName = specialDrops.Key;

                                List<SpecialSagaDrop> merbDrop = __m_specialSagaDrops[merbName];

                                for (int i = 0; i < merbDrop.Count; i++)
                                {
                                    SpecialSagaDrop sagaDrop = merbDrop[i];
                                    if (sagaDrop.m_itemName == itemName && sagaDrop.m_stopDroppingOnPickup)
                                    {
                                        //                                        Debug.LogError($"Humanoid.Pickup() SpecialSagaDrop for {itemName} found in list for {merbName}");

                                        //                                        Debug.LogError($"Player has picked up {sagaDrop.m_numPickedUp} {itemName}");

                                        sagaDrop.m_numPickedUp++;
                                    }

                                    merbDrop[i] = sagaDrop;
                                }

                                List<SpecialSagaDrop> verifyList = __m_specialSagaDrops[merbName];

                                foreach (SpecialSagaDrop sd in verifyList)
                                {
                                    if (sd.m_itemName == itemName && sd.m_stopDroppingOnPickup)
                                    {
                                        //                                        Debug.LogError($"{merbName} m_numPickedUp for {sd.m_itemName} is {sd.m_numPickedUp}");

                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Check picked up item to see if Trophy
            static void Postfix(Humanoid __instance, GameObject go, bool autoequip, bool autoPickupDelay, bool __result)
            {
                if (__instance == null || go == null)
                {
                    return;
                }

                if (GetGameMode() == TrophyGameMode.CulinarySaga)
                {
                    return;
                }

                ItemDrop component = go.GetComponent<ItemDrop>();
                ItemDrop.ItemData item = component.m_itemData;

                if (__result && item != null && item.m_dropPrefab != null)
                {
                    // Log the item name to the console when the player picks it up
                    // You can add further logic here to check the item type or trigger specific events
                    if (RecordPlayerPickedUpTrophy(item.m_dropPrefab.name))
                    {
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "PlacePiece")]
        public class PlacePiecePatch
        {
            static bool Prefix(Player __instance, Piece piece)
            {
                if (__instance == null || piece == null)
                {
                    return true;
                }

                if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    if (piece.m_name.ToLower().Contains("bed"))
                    {
                        //                        Debug.LogError($"Player {__instance.GetPlayerName()} tried to make bed: {piece.m_name}");

                        __instance.Message(MessageHud.MessageType.Center, "Beds are not allowed in Trophy Blitz!");

                        return false;
                    }
                }

                return true;
            }


            static void Postfix(Player __instance, Piece piece)
            {
                if (__instance == null)
                {
                    return;
                }

                if (piece != null)
                {
                    //                        Debug.Log($"Player {__instance.GetPlayerName()} placed a building: {piece.m_name}");

                    AddPlayerEvent(PlayerEventType.Build, piece.m_name, __instance.transform.position);
                }

            }
        }

        public static void AddToggleGameModeButton(Transform parentTransform)
        {
            // Clone the existing button
            GameObject toggleGameModeButton = new GameObject("ToggleGameModeButton");
            toggleGameModeButton.transform.SetParent(parentTransform);

            // The UI RectTransform for the button
            RectTransform rectTransform = toggleGameModeButton.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            rectTransform.pivot = new Vector2(1.0f, 0.0f);
            rectTransform.anchoredPosition = new Vector2(-170, -200); // Position below the logo
            rectTransform.sizeDelta = new Vector2(200, 25);

            // Add the Button component
            UnityEngine.UI.Button button = toggleGameModeButton.AddComponent<UnityEngine.UI.Button>();

            // Add an Image component for the button background
            UnityEngine.UI.Image image = toggleGameModeButton.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white; // Set background color

            // Create a sub-object for the text because the GameObject can't have an Image and a Text object
            GameObject textObject = new GameObject("ToggleGameModeButtonText");
            textObject.transform.SetParent(toggleGameModeButton.transform);

            // Set the Text RectTransform
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 0);

            // Change the button's text
            //TextMeshProUGUI buttonText = textObject.AddComponent<TextMeshProUGUI>();
            TextMeshProUGUI buttonText = AddTextMeshProComponent(textObject);

            buttonText.text = "<b>Switch Game Mode<b>";
            buttonText.fontSize = 18;
            buttonText.color = Color.black;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;

            // Set up the click listener
            button.onClick.AddListener(ToggleGameModeButtonClick);
        }
        public static TextMeshProUGUI __m_pacifistButtonText = null;
        public static void AddTogglePacifistButton(Transform parentTransform)
        {
            // Clone the existing button
            GameObject togglePacifistButton = new GameObject("TogglePacifistButton");
            togglePacifistButton.transform.SetParent(parentTransform);

            // The UI RectTransform for the button
            RectTransform rectTransform = togglePacifistButton.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            rectTransform.pivot = new Vector2(1.0f, 0.0f);
            rectTransform.anchoredPosition = new Vector2(-10, -200); // Position below the logo
            rectTransform.sizeDelta = new Vector2(150, 25);
            
            // Add the Button component
            UnityEngine.UI.Button button = togglePacifistButton.AddComponent<UnityEngine.UI.Button>();

            // Add an Image component for the button background
            UnityEngine.UI.Image image = togglePacifistButton.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.25f, 0.25f, 0.25f); // Set background color

            // Create a sub-object for the text because the GameObject can't have an Image and a Text object
            GameObject textObject = new GameObject("TogglePacifistButtonText");
            textObject.transform.SetParent(togglePacifistButton.transform);

            // Set the Text RectTransform
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 0);

            // Change the button's text
            //TextMeshProUGUI buttonText = textObject.AddComponent<TextMeshProUGUI>();
            __m_pacifistButtonText = AddTextMeshProComponent(textObject);

            __m_pacifistButtonText.text = "<b><color=red>Pacifist: OFF</color><b>";
            __m_pacifistButtonText.fontSize = 18;
            __m_pacifistButtonText.color = Color.black;
            __m_pacifistButtonText.alignment = TextAlignmentOptions.Center;
            __m_pacifistButtonText.fontStyle = FontStyles.Bold;

            // Set up the click listener
            button.onClick.AddListener(TogglePacifistButtonClick);
        }


        public static void AddLoginWithDiscordButton(Transform parentTransform)
        {
            // Clone the existing button
            GameObject loginButton = new GameObject("LoginButton");
            loginButton.transform.SetParent(parentTransform);

            // The UI RectTransform for the button
            RectTransform rectTransform = loginButton.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            rectTransform.pivot = new Vector2(1.0f, 0.0f);
            rectTransform.anchoredPosition = new Vector2(-140, -240); // Position below the logo
            rectTransform.sizeDelta = new Vector2(140, 22);

            // Add the Button component
            UnityEngine.UI.Button button = loginButton.AddComponent<UnityEngine.UI.Button>();

            ColorBlock cb = button.colors;
            cb.normalColor = new Color(88, 101, 242);
            cb.highlightedColor = Color.yellow;  // When hovering
            cb.pressedColor = Color.red;      // When pressed
            cb.selectedColor = Color.white;   // When selected
            button.colors = cb;

            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            // Add an Image component for the button background
            UnityEngine.UI.Image image = loginButton.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(88f / 256f, 101f / 256f, 242f / 256f); // Set background color
                                                                           //                image.color = Color.blue;

            // Create a sub-object for the text because the GameObject can't have an Image and a Text object
            GameObject textObject = new GameObject("LoginButtonText");
            textObject.transform.SetParent(loginButton.transform);

            // Set the Text RectTransform
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 0);

            // Change the button's text
            __m_discordLoginButtonText = AddTextMeshProComponent(textObject);

            if (__m_loggedInWithDiscord)
            {
                __m_discordLoginButtonText.text = "Discord Logout";
            }
            else
            {
                __m_discordLoginButtonText.text = "Discord Login";
            }

            __m_discordLoginButtonText.fontSize = 16;
            __m_discordLoginButtonText.color = Color.white;
            __m_discordLoginButtonText.alignment = TextAlignmentOptions.Center;

            // Set up the click listener
            button.onClick.AddListener(LoginButtonClick);

            // Online Status Text
            GameObject statusTextObject = new GameObject("StatusText");
            statusTextObject.transform.SetParent(parentTransform);

            // The UI RectTransform for the button
            RectTransform statusRectTransform = statusTextObject.AddComponent<RectTransform>();
            statusRectTransform.localScale = Vector3.one;
            statusRectTransform.anchorMin = new Vector2(1.0f, 0.0f);
            statusRectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            statusRectTransform.pivot = new Vector2(1.0f, 0.0f);
            statusRectTransform.anchoredPosition = new Vector2(-5, -240);
            statusRectTransform.sizeDelta = new Vector2(140, 22);

            // Change the button's text
            __m_onlineStatusText = AddTextMeshProComponent(statusTextObject);
            __m_onlineStatusText.fontSize = 16;
            __m_onlineStatusText.color = Color.white;
            __m_onlineStatusText.alignment = TextAlignmentOptions.Center;
            __m_onlineStatusText.raycastTarget = false;

            __m_onlineStatusText.text = "Status: <Unknown>";

            // Username Text
            GameObject usernameTextObject = new GameObject("usernameText");
            usernameTextObject.transform.SetParent(parentTransform);

            // The UI RectTransform for the button
            RectTransform usernameRectTransform = usernameTextObject.AddComponent<RectTransform>();
            usernameRectTransform.localScale = Vector3.one;
            usernameRectTransform.anchorMin = new Vector2(1.0f, 0.0f);
            usernameRectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            usernameRectTransform.pivot = new Vector2(1.0f, 0.0f);
            usernameRectTransform.anchoredPosition = new Vector2(-40, -265);
            usernameRectTransform.sizeDelta = new Vector2(250, 20);

            // Change the button's text
            __m_onlineUsernameText = AddTextMeshProComponent(usernameTextObject);
            __m_onlineUsernameText.fontSize = 16;
            __m_onlineUsernameText.color = Color.white;
            __m_onlineUsernameText.alignment = TextAlignmentOptions.Center;
            __m_onlineUsernameText.raycastTarget = false;

            __m_onlineUsernameText.text = "Discord User: <Unknown>";

            UpdateOnlineStatus();
        }




        static private async Task FetchDiscordAvatar(string userId, string avatarId)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string imageUrl = $"https://cdn.discordapp.com/avatars/{userId}/{avatarId}.png";

                // Download the image as a byte array
                byte[] imageData = await httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);

                //File.WriteAllBytes("avatar.png", imageData);

                MainThreadDispatcher.Instance.Enqueue(() =>
                {

                    // Create a Texture2D from the downloaded data
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    //                        if (texture.LoadImage(imageData, true))
                    {
                        // Create a sprite from the texture
                        Sprite sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        // Assign the sprite to the target image
                        //                        if (__m_discordBackgroundImage != null)
                        //                            __m_discordBackgroundImage.sprite = sprite;
                    }
                });
            }
        }



        public static void UpdateOnlineStatus()
        {

            DiscordUserResponse response = __m_discordAuthentication.GetUserResponse();
            if (response != null)
            {
                __m_loggedInWithDiscord = true;

                __m_configDiscordId.SetSerializedValue(response.id);
                __m_configDiscordUser.SetSerializedValue(response.username);
                __m_configDiscordGlobalUser.SetSerializedValue(response.global_name);
                __m_configDiscordAvatar.SetSerializedValue(response.avatar);
                __m_configDiscordDiscriminator.SetSerializedValue(response.discriminator);

                __m_trophyHuntMod.Config.Save();
            }

            //                System.Diagnostics.Debug.WriteLine($"UpdateOnlineStatus {__m_loggedInWithDiscord}");
            //                Debug.Log($"UpdateOnlineStatus: {__m_loggedInWithDiscord} updating");


            string onlineText = "n/a";
            if (__m_loggedInWithDiscord)
            {
                __m_onlineUsernameText.text = $"Discord User: <color=yellow>{__m_configDiscordUser.Value}</color>";
                onlineText = "<color=green>Online</color>";
                __m_discordLoginButtonText.text = "Discord Logout";

                //if (__m_discordBackgroundImage != null)
                //{
                //    __m_discordBackgroundImage.color = new Color(1, 1, 1, 1);
                //}
                //                Task.Run(() => FetchDiscordAvatar(__m_configDiscordId.Value, __m_configDiscordAvatar.Value));
            }
            else
            {
                onlineText = "<color=red>Offline</color>";
                __m_onlineUsernameText.text = $"Discord User: <color=grey>n/a</color>";
                __m_discordLoginButtonText.text = "Discord Login";

                //if (__m_discordBackgroundImage != null)
                //{
                //    __m_discordBackgroundImage.color = new Color(0, 0, 0, 0);
                //}
            }

            __m_onlineStatusText.text = $"Status: {onlineText}";
        }

        public static void ToggleGameModeButtonClick()
        {
            ToggleGameMode();
        }

        public static void TogglePacifistButtonClick()
        {
            TogglePacifist();
            if (IsPacifist())
            {
                __m_pacifistButtonText.text = "<b><color=green>Pacifist: ON</color><b>";
            }
            else
            {
                __m_pacifistButtonText.text = "<b><color=red>Pacifist: OFF</color><b>";
            }
        }

        public static void LoginButtonClick()
        {
            if (!__m_loggedInWithDiscord)
            {
                string clientId = "1328474573334642728";
                string redirectUri = "http://localhost:5000/callback";

                __m_discordAuthentication.StartOAuthFlow(clientId, redirectUri, UpdateOnlineStatus);
            }
            else
            {
                __m_loggedInWithDiscord = false;
                //                    Debug.Log("__m_loggedInWithDiscord = false");
                __m_discordAuthentication.ClearUserResponse();
                __m_configDiscordId.SetSerializedValue("");
                __m_configDiscordUser.SetSerializedValue("");
                __m_configDiscordGlobalUser.SetSerializedValue("");
                __m_configDiscordAvatar.SetSerializedValue("");
                __m_configDiscordDiscriminator.SetSerializedValue("");

                __m_trophyHuntMod.Config.Save();
            }

            UpdateOnlineStatus();
        }

        public class BossDetails
        {
            public BossDetails(string bossCharacterId, string bossLocationName, string bossName)
            {
                m_bossCharacterId = bossCharacterId;
                m_bossLocationName = bossLocationName;
                m_bossName = bossName;
            }

            public string m_bossCharacterId;    // ex: "$enemy_eikthyr"
            public string m_bossLocationName;
            public string m_bossName;
        }

        static public BossDetails[] __m_bossNames = new BossDetails[]
        {
                new BossDetails("$enemy_eikthyr","Eikthyrnir", "Eikthyr"),
                new BossDetails("$enemy_gdking","GDKing", "Elder"),
                new BossDetails("$enemy_bonemass","Bonemass", "Bonemass"),
                new BossDetails("$enemy_dragon","Dragonqueen", "Moder"),
                new BossDetails("$enemy_goblinking","GoblinKing", "Yagluth"),
                new BossDetails("$enemy_seekerqueen","Mistlands_DvergrBossEntrance1", "The Queen"),
                new BossDetails("$enemy_fader","FaderLocation", "Fader")
        };

        private static void RevealByName(string locationName, string pinName, Minimap.PinType pinType, float distance)
        {
            // Add boss pins to minimap
            var zs = ZoneSystem.instance;
            var mm = Minimap.instance;

            if (zs == null || mm == null)
            {
                Debug.LogWarning("RevealBossLocations: ZoneSystem or Minimap not initialized.");
                return;
            }

            foreach (var pair in zs.m_locationInstances)
            {
                var loc = pair.Value.m_location;
                if (loc == null) continue;

                string prefabName = loc.m_prefabName;

                if (locationName == prefabName)
                {
                    Debug.Log("RevealBoss: Found boss location name " + prefabName);

                    mm.DiscoverLocation(pair.Value.m_position, pinType, pinName, false);
                    mm.Explore(pair.Value.m_position, distance);
                }
            }
        }

        private static void RevealBoss(string bossCharacterId)
        {
//            Debug.Log("RevealBoss: " + bossCharacterId);
            BossDetails foundDetails = __m_bossNames.FirstOrDefault<BossDetails>(t => t.m_bossCharacterId == bossCharacterId);
            if (foundDetails == default)
            {
                Debug.Log("RevealBoss: Did not find details.");

                return;
            }

//            Debug.Log("RevealBoss: Found details for " + foundDetails.m_bossName + " at " + foundDetails.m_bossLocationName + " with id" + foundDetails.m_bossCharacterId);

            RevealByName(foundDetails.m_bossLocationName, foundDetails.m_bossName, Minimap.PinType.Boss, 500);
        }

        private static void RevealNextBoss(string enemyKilled)
        {
            bool wasABoss = true;
            switch (enemyKilled)
            {
                case "$enemy_eikthyr":
                    RevealBoss("$enemy_gdking");
                    RevealByName("Vendor_BlackForest", "Haldor", Minimap.PinType.Icon3, 100);
                    if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                        RaiseAllPlayerSkills(20);
                    break;
                case "$enemy_gdking":
                    RevealBoss("$enemy_bonemass");
                    RevealByName("BogWitch_Camp", "BogWitch", Minimap.PinType.Icon3, 100);
                    if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                        RaiseAllPlayerSkills(30);
                    break;
                case "$enemy_bonemass":
                    RevealBoss("$enemy_dragon");
                    RevealByName("Hildir_camp", "Hildir", Minimap.PinType.Icon3, 100);
                    if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                        RaiseAllPlayerSkills(40);
                    break;
                case "$enemy_dragon":
                    RevealBoss("$enemy_goblinking");
                    if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                        RaiseAllPlayerSkills(50);
                    break;
                case "$enemy_goblinking":
                    RevealBoss("$enemy_seekerqueen");
                    if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                        RaiseAllPlayerSkills(60);
                    break;
                case "$enemy_seekerqueen":
                    RevealBoss("$enemy_fader");
                    if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                        RaiseAllPlayerSkills(80);
                    break;

                default:
                    wasABoss = false;
                    break;
            }

            if (Player.m_localPlayer != null && wasABoss)
            {
                if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You have gained knowledge.");
                else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You have gained wisdom and knowledge.");
            }
        }

        private static void RevealAllBosses(Player player)
        {
            foreach (BossDetails bossDetails in __m_bossNames)
            {
                RevealBoss(bossDetails.m_bossCharacterId);
            }
        }

        // Enable to build all items at level 1 workbenches
        //
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
        public static class Player_HaveRequirements_Patch
        {
            static bool Prefix(Player __instance, Recipe recipe, bool discover, int qualityLevel, int amount, ref bool __result)
            {
                if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    __result = true;

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.CanRepair), new[] { typeof(ItemDrop.ItemData) })]
        public static class InventoryGui_CanRepair_Patch
        {
            static bool Prefix(InventoryGui __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                    if (currentCraftingStation == null)
                    {
                        return true;
                    }

                    Recipe recipe = ObjectDB.instance.GetRecipe(item);
                    if ((recipe.m_repairStation != null && recipe.m_repairStation.m_name == currentCraftingStation.m_name) ||
                        (recipe.m_craftingStation != null && recipe.m_craftingStation.m_name == currentCraftingStation.m_name) ||
                        item.m_worldLevel < Game.m_worldLevel)
                    {
                        __result = true;
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.SetQuality), new[] { typeof(int) })]
        public static class ItemDrop_SetQuality_Patch
        {
            static void Postfix(ItemDrop __instance, int quality)
            {
                if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    bool doUpgrade = false;
                    ItemDrop.ItemData.ItemType itemType = __instance.m_itemData.m_shared.m_itemType;
                    switch (itemType)
                    {
                        case ItemDrop.ItemData.ItemType.Material:
                        case ItemDrop.ItemData.ItemType.Consumable:
                        case ItemDrop.ItemData.ItemType.Ammo:
                        case ItemDrop.ItemData.ItemType.Customization:
                        case ItemDrop.ItemData.ItemType.Trophy:
                        case ItemDrop.ItemData.ItemType.Torch:
                        case ItemDrop.ItemData.ItemType.Misc:
                        case ItemDrop.ItemData.ItemType.Utility:
                        case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                        case ItemDrop.ItemData.ItemType.Fish:
                        case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                        case ItemDrop.ItemData.ItemType.Trinket:
                            doUpgrade = false;
                            break;

                        case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                        case ItemDrop.ItemData.ItemType.Bow:
                        case ItemDrop.ItemData.ItemType.Shield:
                        case ItemDrop.ItemData.ItemType.Helmet:
                        case ItemDrop.ItemData.ItemType.Chest:
                        case ItemDrop.ItemData.ItemType.Legs:
                        case ItemDrop.ItemData.ItemType.Hands:
                        case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                        case ItemDrop.ItemData.ItemType.Shoulder:
                        case ItemDrop.ItemData.ItemType.Tool:
                        case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                            doUpgrade = true;
                            break;
                    }
                    if (doUpgrade)
                    {
                        __instance.m_itemData.m_quality = __instance.m_itemData.m_shared.m_maxQuality;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SE_Cozy), nameof(SE_Cozy.Setup), new[] { typeof(Character) })]
        public static class SE_Cozy_Setup_Patch
        {
            static void Postfix(SE_Cozy __instance, Character character)
            {
                if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    if (__instance != null)
                    {
                        __instance.m_delay = 9.0f;

//                        Debug.LogError("SE_Cozy_Setup_Patch: StatusEffect: " + __instance.m_statusEffect);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SE_Puke), nameof(SE_Puke.Setup), new[] { typeof(Character) })]
        public static class SE_Puke_Setup_Patch
        {
            static void Postfix(SE_Puke __instance, Character character)
            {
                if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    if (__instance != null && character != null)
                    {
                        int statusEffectHash = StringExtensionMethods.GetStableHashCode("Rested");
                        character.GetSEMan().AddStatusEffect(statusEffectHash, resetTime: true);
                    }
                }
            }
        }
        //static int __m_oldCraftedItemQuality = 1;
        //[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem))]
        //public static class Inventory_AddItem2_Patch
        //{
        //    static bool Prefix(Inventory __instance, string name, int stack, int quality, int variant, long crafterID, string crafterName, Vector2i position, bool pickedUp = false)

        //    //static bool Prefix(Inventory __instance, string name, int stack, int quality, int variant, long crafterID, string crafterName, Vector2i position, bool pickedUp, ItemDrop.ItemData __result)
        //    {
        //        // I don't think this is necessary, but we want to return things as we found them
        //        if (__instance == null)
        //        {
        //            return true;
        //        }


        //        //if (GetGameMode() == TrophyGameMode.TrophyBlitz)
        //        //{
        //        //    __result = __instance.AddItem(name, stack, quality, variant, crafterID, crafterName, position, pickedUp);
        //        //    __result.m_quality = __result.m_shared.m_maxQuality;
        //        //    return false;
        //        //}

        //        return true;
        //    }
        //}

        static bool __m_overrideGlobalKeysForPieceDrops = false;

        [HarmonyPatch(typeof(Piece), nameof(Piece.FreeBuildKey))]
        public static class Piece_FreeBuildKey_Patch
        {
            static bool Prefix(ref GlobalKeys __result)
            {
                if (GetGameMode() == TrophyGameMode.TrophyTrailblazer && __m_overrideGlobalKeysForPieceDrops)
                {
                    __result = GlobalKeys.NoPortals; // return something guaranteed to not be found in this mode
                    return false;
                }

                return true;
            }
        }

        private static void UnlockEverythingBlitz(Player player)
        {
            if (player == null)
            {
                return;
            }

            MessageHud.instance.enabled = false;

            ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoWorkbench);
            ZoneSystem.instance.SetGlobalKey(GlobalKeys.AllPiecesUnlocked);
            ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoCraftCost);

            // This adds EVERYTHING to the tab menu
            // player.m_noPlacementCost = true;

            foreach (var prefab in ObjectDB.instance.m_items)
            {
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null)
                    continue;

                ItemDrop.ItemData itemData = drop.m_itemData;
                if (itemData == null)
                    continue;

                ItemDrop.ItemData.ItemType itemType = itemData.m_shared.m_itemType;

                //if (itemType != ItemDrop.ItemData.ItemType.Material &&
                //    itemType != ItemDrop.ItemData.ItemType.Consumable &&
                //    itemType != ItemDrop.ItemData.ItemType.Customization &&
                //    itemType != ItemDrop.ItemData.ItemType.Misc &&
                //    itemType != ItemDrop.ItemData.ItemType.Utility)
                //    continue;

                if (!player.m_knownMaterial.Contains(itemData.m_shared.m_name))
                {
                    player.m_knownMaterial.Add(itemData.m_shared.m_name);
                }
            }

            // Unlock all recipes
            //foreach (var recipe in ObjectDB.instance.m_recipes)
            //{
            //    if (recipe == null)
            //        continue;

            //    if (!player.m_knownRecipes.Contains(recipe.name))
            //    {
            //        player.m_knownRecipes.Add(recipe.name);
            //    }
            //}


            player.UpdateKnownRecipesList();
            player.UpdateAvailablePiecesList();

            // Clear notification queue to prevent pop-up message spam
            MessageHud.instance.m_unlockMsgQueue.Clear();
            MessageHud.instance.m_unlockMsgCount = 0;

            __m_everythingUnlocked = true;

            MessageHud.instance.enabled = true;

            if (!__m_introMessageDisplayed)
            {
                player.Message(MessageHud.MessageType.Center, "Blitz mode engaged!");
                __m_introMessageDisplayed = true;
            }
        }

        private static void UnlockEverythingTrailblazer(Player player)
        {
            if (player == null)
            {
                return;
            }

            ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoWorkbench);
            //            ZoneSystem.instance.SetGlobalKey(GlobalKeys.AllPiecesUnlocked);
            ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoCraftCost);

            __m_everythingUnlocked = true;

            if (!__m_introMessageDisplayed)
            {
                player.Message(MessageHud.MessageType.Center, "Blaze a trail, Trailblazer!");
                __m_introMessageDisplayed = true;
            }
        }

        // Player Log
        //
        // Log of events encountered during gameplay
        //
        public enum PlayerEventType
        {
            None,
            Trophy,
            Build,
            Item,
            Misc,
            Max
        }

        [Serializable]
        public class PlayerEventLog
        {
            public PlayerEventLog()
            {
                eventType = PlayerEventType.None;
                eventName = "";
                eventPos = Vector3.zero;
                eventTime = DateTime.MinValue;
            }
            public PlayerEventLog(PlayerEventType _eventType, string _eventName, Vector3 _eventPos, DateTime _eventTime)
            {
                eventType = _eventType;
                eventName = _eventName;
                eventPos = _eventPos;
                eventTime = _eventTime;
            }

            public PlayerEventType eventType = PlayerEventType.None;
            public string eventName = "";
            public Vector3 eventPos = Vector3.zero;
            public DateTime eventTime = DateTime.MinValue;
        }

        // Legal Events
        public struct EventDescription
        {
            public EventDescription(PlayerEventType _eventType, List<string> _legalEvents)
            {
                eventType = _eventType;
                legalEvents = _legalEvents;
            }

            public PlayerEventType eventType;
            public List<string> legalEvents;
        }

        static public readonly Dictionary<PlayerEventType, List<string>> __m_eventDescriptions = new Dictionary<PlayerEventType, List<string>>()
            {
                {
                    PlayerEventType.Trophy, null    // pass all trophy events (no whitelist)
                },
                {
                    PlayerEventType.Build, new List<string>
                    { 
                        // Base pieces
                        "$piece_workbench",
                        
                        // Plantables
                        "$piece_sapling_turnip",
                        "$piece_sapling_onion",

                        // Misc
                        "$piece_bonfire"
                    }
                },
                {
                    PlayerEventType.Item, new List<string>
                    {
                        // Wood types
                        "RoundLog",
                        "Finewood",
                        "ElderBark",

                        // Weapon types
                        "SpearFlint",
                        "SpearFlint2",
                        "SpearFlint3",
                        "SpearFlint4",

                        // Armor types
                        "ArmorTrollLeatherChest",
                        "ArmorTrollLeatherChest2",
                        "ArmorTrollLeatherChest3",
                        "ArmorRootChest",
                        "ArmorRootChest2",
                    }
                },
                {
                    PlayerEventType.Misc, null      // pass all misc events, no whitelist
                },
            };

        public static bool IsLoggableEvent(PlayerEventType eventType, string eventName)
        {
            List<string> trackedEvents = __m_eventDescriptions[eventType];

            // If no list, all events are considered loggable
            if (trackedEvents == null)
                return true;

            return trackedEvents.Contains(eventName);
        }

        public static bool IsAlreadyLogged(PlayerEventType eventType, string eventName)
        {
            PlayerEventLog existingEntry = __m_playerEventLog.Find(x => x.eventName == eventName);
            if (existingEntry != null)
            {
                return true;
            }

            return false;
        }

        public static void AddPlayerEvent(PlayerEventType eventType, string eventName, Vector3 eventPos)
        {

            if (!IsLoggableEvent(eventType, eventName))
            {
                return;
            }

            // Some events we only want to log the first time they occur
            if (eventType != PlayerEventType.Trophy)
            {
                if (IsAlreadyLogged(eventType, eventName))
                {
                    return;
                }
            }

//            Debug.LogWarning($"AddPlayerEvent() Logging Event: {eventType.ToString()}, {eventName}, {eventPos}");

            // Add the event to our internal tracking log
            __m_playerEventLog.Add(new PlayerEventLog(eventType, eventName, eventPos, DateTime.UtcNow));

            PostTrackLogEntry(eventName, __m_playerCurrentScore);
        }

        static public bool CanPostToTracker(bool force = false)
        {
            if (__m_invalidForTournamentPlay)
            {
                return false;
            }

            if (!__m_loggedInWithDiscord)
            {
                return false;
            }

            if (force == false)
            {
                if (__m_tournamentStatus != TournamentStatus.Live)
                {
                    return false;
                }

                if (DateTime.Now > __m_tournamentEndTime)
                {
                    return false;
                }
            }

            switch (GetGameMode())
            {
                case TrophyGameMode.CasualSaga:
                case TrophyGameMode.CulinarySaga:
                case TrophyGameMode.TrophyFiesta:
                    return false;
                    break;
            }

            return true;
        }

        // Tracker Logs

        // Single Log update
        [Serializable]
        public class TrackLogEntry
        {
            public string id;       // discord id
            public string seed;     // game seed
            public int score;       // Current score
            public string code;     // event name
                                    //                public string at;       // UTC time
        }

        // List of Logs update
        [Serializable]
        public class TrackLogs
        {
            public string id;
            public string user;
            public string seed;
            public string mode;
            public int score;
            public List<TrackLogsElement> logs;
        }

        [Serializable]
        public class TrackLogsElement
        {
            public string code;     // event name
            public string at;
        }


        [Serializable]
        public class TrackHunt
        {
            // List of Logs update
            public string id;
            public string user;
            public string seed;
            public string mode;
            public int score;
            public int deaths;
            public int relogs;
            public int slashdies;
            public List<string> trophies;
        }

        static public IEnumerator UnityPostRequest(string url, string json)
        {
            //                            Debug.LogWarning($"UnityPostRequest(): {url}\n{json}");

            UnityWebRequest request = UnityWebRequest.Post(url, json, "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
            }

            //                            Debug.LogWarning($"UnityPostRequest(): Result: {request.result.ToString()}");

        }

        public static void PostTrackLogs(bool force = false)
        {
            if (!CanPostToTracker(force))
            {
                return;
            }

            //            Debug.LogWarning($"PostTrackLogs(): { force }");

            TrackLogs trackLogs = new TrackLogs();

            trackLogs.id = __m_configDiscordId.Value;
            trackLogs.user = __m_configDiscordUser.Value;
            trackLogs.seed = __m_storedWorldSeed;
            trackLogs.mode = GetGameMode().ToString();
            trackLogs.score = __m_playerCurrentScore;
            trackLogs.logs = new List<TrackLogsElement>();

            foreach (PlayerEventLog logEntry in __m_playerEventLog)
            {
                TrackLogsElement elem = new TrackLogsElement();
                elem.code = logEntry.eventName;
                elem.at = logEntry.eventTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                trackLogs.logs.Add(elem);
            }

            string json = JsonConvert.SerializeObject(trackLogs);

            //                            Debug.LogWarning(json);

            string url = "https://valheim.help/api/track/logs";

            __m_trophyHuntMod.StartCoroutine(UnityPostRequest(url, json));
        }

        public static void PostTrackLogEntry(string eventName, int score)
        {
            if (!CanPostToTracker())
                return;

            //            Debug.LogWarning($"PostTrackLogEntry(): {eventName}, {score}");

            TrackLogEntry entry = new TrackLogEntry();

            entry.id = __m_configDiscordId.Value;
            entry.seed = __m_storedWorldSeed;
            entry.score = score;
            entry.code = eventName;

            string json = JsonConvert.SerializeObject(entry);

//            Debug.LogWarning($"PostTrackLogEntry: {json}");

            string url = "https://valheim.help/api/track/log";

            __m_trophyHuntMod.StartCoroutine(UnityPostRequest(url, json));
        }

        public static void PostTrackHunt()
        {
            if (__m_invalidForTournamentPlay)
            {
                return;
            }

            if (!__m_loggedInWithDiscord)
            {
                return;
            }

            if (__m_tournamentStatus != TournamentStatus.Live)
            {
                return;
            }

            if (DateTime.Now > __m_tournamentEndTime)
            {
                return;
            }

            TrackHunt trackHunt = new TrackHunt();

            trackHunt.id = __m_configDiscordId.Value;
            trackHunt.user = __m_configDiscordUser.Value;
            trackHunt.seed = __m_storedWorldSeed;
            trackHunt.mode = GetGameMode().ToString();
            trackHunt.score = __m_playerCurrentScore;
            trackHunt.trophies = __m_trophyCache.ToList();
            trackHunt.deaths = __m_deaths;
            trackHunt.slashdies = __m_slashDieCount;
            trackHunt.relogs = __m_logoutCount;

            string json = JsonConvert.SerializeObject(trackHunt);

            string url = "https://valheim.help/api/track/hunt";

            __m_trophyHuntMod.StartCoroutine(UnityPostRequest(url, json));
        }

        // Tracker Standings
        static public TournamentStatus __m_tournamentStatus = TournamentStatus.NotRunning;
        static public string __m_tournamentName = "";
        static public string __m_tournamentMode = "";
        static public DateTime __m_tournamentEndTime;

        public enum TournamentStatus
        {
            NotRunning = 0,
            Live = 20,
            Over = 30
        }
        public class TournamentPlayerInfo
        {
            public TournamentPlayerInfo(string _name, int _score, string _id)
            {
                name = _name;
                score = _score;
                id = _id;
            }
            public string name;
            public int score;
            public string id;
        }

        static public List<TournamentPlayerInfo> __m_tournamentPlayerInfo = new List<TournamentPlayerInfo>();


        [Serializable]
        public class TrackStandingsPlayer
        {
            public string name = "";
            public string id = "";
            public string avatarUrl = "";
            public int score = 0;
        }

        [Serializable]
        public class TrackStandings
        {
            public string name = ""; // tournament event name
            public string mode = ""; // tournament event game mode
            public string startAt = ""; // start time in UTC
            public string endAt = ""; // end time in UTC
            public int status = 0;
            public List<TrackStandingsPlayer> players = new List<TrackStandingsPlayer>();

        }

        public static IEnumerator UnityGetStandingsRequest(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    //                                            Debug.LogWarning($"Standings Recieved: {webRequest.downloadHandler.text}");

                    string responseText = webRequest.downloadHandler.text;

                    TrackStandings standings = JsonConvert.DeserializeObject<TrackStandings>(webRequest.downloadHandler.text);

                    __m_tournamentStatus = (TournamentStatus)standings.status;
                    __m_tournamentName = standings.name;
                    __m_tournamentMode = standings.mode;

                    __m_standingsElement.SetActive(__m_tournamentStatus != TournamentStatus.NotRunning);

                    DateTime endTime;
                    if (DateTime.TryParse(standings.endAt, out endTime))
                    {
                        __m_tournamentEndTime = endTime;
                    }

                    __m_tournamentPlayerInfo.Clear();

                    //                        for (int i = 0; i < 8; i++)
                    {
                        foreach (TrackStandingsPlayer player in standings.players)
                        {
                            __m_tournamentPlayerInfo.Add(new TournamentPlayerInfo(player.name, player.score, player.id));
                        }
                    }

                    //Debug.LogWarning($"Tournament Standings");
                    //Debug.LogWarning($" Name: {__m_tournamentName}");
                    //Debug.LogWarning($" Mode: {__m_tournamentMode}");
                    //Debug.LogWarning($" Status: {__m_tournamentStatus}");
                    //foreach (TournamentPlayerInfo pi in __m_tournamentPlayerInfo)
                    //{
                    //    Debug.LogWarning($" - {pi.score} - {pi.name}");
                    //}

                    if (__m_refreshLogsAndStandings)
                    {
                        PostTrackLogs();
                        if (Player.m_localPlayer != null)
                        {
                            UpdateModUI(Player.m_localPlayer);
                            __m_refreshLogsAndStandings = false;
                        }
                    }
                }
                else
                {
                    switch (webRequest.result)
                    {
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.DataProcessingError:
                            Debug.LogError("Error: " + webRequest.error);
                            __m_tournamentStatus = TournamentStatus.NotRunning;
                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            Debug.LogError("HTTP Error: " + webRequest.error);
                            __m_tournamentStatus = TournamentStatus.NotRunning;
                            break;
                    }
                }
            }

        }

        public static void PostStandingsRequest()
        {
            if (__m_invalidForTournamentPlay)
            {
                return;
            }

            if (!__m_loggedInWithDiscord)
            {
                return;
            }

            string standingsUrl = "https://valhelp.azurewebsites.net/api/track/standings";

            string seed = __m_storedWorldSeed;
            string mode = GetGameMode().ToString();
            string url = $"{standingsUrl}?seed={seed}&mode={mode}";
            //                            Debug.LogWarning($"Standings Request: {url}");
            __m_trophyHuntMod.StartCoroutine(UnityGetStandingsRequest(url));
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
                        GameObject textObject = new GameObject("TrophyHuntModLogoText");
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
                        __m_trophyHuntMainMenuText = AddTextMeshProComponent(textObject);
                        __m_trophyHuntMainMenuText.font = __m_globalFontObject;
                        __m_trophyHuntMainMenuText.fontMaterial = __m_globalFontObject.material;
                        __m_trophyHuntMainMenuText.fontStyle = FontStyles.Bold;

                        __m_trophyHuntMainMenuText.text = GetTrophyHuntMainMenuText();
                        __m_trophyHuntMainMenuText.alignment = TextAlignmentOptions.Left;
                        // Enable outline
                        //                            __m_trophyHuntMainMenuText.fontMaterial.EnableKeyword("OUTLINE_ON");
                        __m_trophyHuntMainMenuText.lineSpacingAdjustment = -5;
                        // Set outline color and thickness
                        //                            __m_trophyHuntMainMenuText.outlineColor = Color.black;
                        //                            __m_trophyHuntMainMenuText.outlineWidth = 0.05f; // Adjust the thickness


                        AddToggleGameModeButton(textObject.transform);
                        AddTogglePacifistButton(textObject.transform);

                        GameObject discordBackground = new GameObject("AvatarObject");
                        discordBackground.transform.SetParent(textObject.transform);

                        RectTransform discordTransform = discordBackground.AddComponent<RectTransform>();
                        discordTransform.sizeDelta = new Vector2(320, 78);
                        discordTransform.anchoredPosition = new Vector2(30, -335);
                        discordTransform.localScale = Vector3.one;

                        __m_discordBackgroundImage = discordBackground.AddComponent<UnityEngine.UI.Image>();
                        __m_discordBackgroundImage.raycastTarget = false;
                        __m_discordBackgroundImage.color = Color.black;
                        __m_discordBackgroundImage.CrossFadeAlpha(1.0f, 20.0f, false);

                        AddLoginWithDiscordButton(textObject.transform);


                        // Don't bother adding this button to the main menu, but keep the code around for new buttons
                        //
                        //AddShowAllTrophyStatsButton(textObject.transform);

                        // HACK
                        //GameObject copperPrefab = GameObject.Find("Copper");

                        //                            FiestaTrophies.Initialize();

                        //foreach (GameObject go in ObjectDB.m_instance.m_items)
                        //{
                        //    Debug.Log(go);
                        //}

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


        // Oh, this is sketchy, but it seems to work.
        //
        // Patch the New World creation dialogue to poke in world defaults for trophy rush automatically

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewWorldDone), new[] { typeof(bool) })]
        public class FejdStartup_OnNewWorldDone_Patch
        {
            static void Postfix(FejdStartup __instance, bool forceLocal)
            {
                //                    Debug.LogError("FejdStartup.OnNewWorldDone:");

                if (FejdStartup.m_instance.m_world != null)
                {
                    if (GetGameMode() == TrophyGameMode.TrophyRush)
                    {
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Clear();

                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("playerdamage 70");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemydamage 200");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyspeedsize 120");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyleveluprate 140");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("resourcerate 200");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("preset combat_veryhard:deathpenalty_default: resources_muchmore: raids_default: portals_default");
                        FejdStartup.m_instance.m_world.SaveWorldMetaData(DateTime.Now);
                        __instance.UpdateWorldList(centerSelection: true);
                    }
                    else if (IsSagaMode())
                    {
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Clear();

                        // Trying new tack with World Modifiers: portal everything, normal combat, no raids, double resources
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("resourcerate 200");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("eventrate 0");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("teleportall");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("preset combat_default:deathpenalty_default:resources_muchmore:raids_none:portals_casual");

                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("playerdamage 85");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemydamage 150");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyspeedsize 110");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyleveluprate 120");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("resourcerate 200");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("eventrate 0");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("preset combat_hard:deathpenalty_default: resources_muchmore: raids_none: portals_default");

                        FejdStartup.m_instance.m_world.SaveWorldMetaData(DateTime.Now);
                        __instance.UpdateWorldList(centerSelection: true);
                    }
                    else if (GetGameMode() == TrophyGameMode.TrophyFiesta)
                    {
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Clear();

                        // Trying new tack with World Modifiers: portal everything, normal combat, no raids, double resources
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyspeedsize 200");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyleveluprate 300");

                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("playerdamage 85");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemydamage 150");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyspeedsize 110");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("enemyleveluprate 120");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("resourcerate 200");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("eventrate 0");
                        //FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("preset combat_hard:deathpenalty_default: resources_muchmore: raids_none: portals_default");

                        FejdStartup.m_instance.m_world.SaveWorldMetaData(DateTime.Now);
                        __instance.UpdateWorldList(centerSelection: true);
                    }
                    else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                    {
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Clear();
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("deathkeepequip");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("skillreductionrate 15");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("resourcerate 200");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("eventrate 0");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("teleportall");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("nobuildcost");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("preset combat_default:deathpenalty_casual: resources_muchmore: raids_none: portals_casual");

                        FejdStartup.m_instance.m_world.SaveWorldMetaData(DateTime.Now);
                        __instance.UpdateWorldList(centerSelection: true);
                    }
                    else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                    {
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Clear();
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("deathkeepequip");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("skillreductionrate 15");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("resourcerate 200");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("eventrate 0");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("teleportall");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("nobuildcost");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("preset combat_default:deathpenalty_casual: resources_muchmore: raids_none: portals_casual");

                        FejdStartup.m_instance.m_world.SaveWorldMetaData(DateTime.Now);
                        __instance.UpdateWorldList(centerSelection: true);
                    }
                    else if (GetGameMode() == TrophyGameMode.TrophyPacifist)
                    {
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("resourcerate 200");
                        FejdStartup.m_instance.m_world.m_startingGlobalKeys.Add("preset combat_default:deathpenalty_default:resources_muchmore:raids_none:portals_default");
                        FejdStartup.m_instance.m_world.SaveWorldMetaData(DateTime.Now);
                        __instance.UpdateWorldList(centerSelection: true);
                    }
                }
            }
        }
        /*
                // Uncomment to inspect current world modifiers when hitting World Modifiers button
                                       [HarmonyPatch (typeof(FejdStartup), nameof(FejdStartup.OnServerOptions))]
                                       public class ServerOptionsGUI_Initizalize_Patch
                                       {
                                           static void Postfix(FejdStartup __instance)
                                           {
                                               ServerOptionsGUI serverOptionsGUI = __instance.m_serverOptions;

                                               Debug.LogError("OnServerOptions:");

                                               foreach (KeyUI entry in ServerOptionsGUI.m_modifiers)
                                               {
                                                   Debug.LogWarning($"  KeyUI: {entry.ToString()}");
                                                   if (entry.GetType() == typeof(KeySlider))
                                                   {
                                                       KeySlider slider = entry as KeySlider;


                                                       Debug.LogWarning($"  {slider.m_modifier.ToString()}");

                                                       foreach (KeySlider.SliderSetting setting in slider.m_settings)
                                                       {
                                                           Debug.LogWarning($"    {setting.m_name}, {setting.m_modifierValue.ToString()}");

                                                           foreach(string key in setting.m_keys)
                                                           {
                                                               Debug.LogWarning($"      {key}");
                                                           }
                                                       }
                                                   }
                                               }

                                               World world = FejdStartup.m_instance.m_world;
                                               if (world != null)
                                               {
                                                   Debug.LogWarning("FejdStartup.m_instance.m_world.m_startingGlobalKeys");
                                                   foreach (string key in world.m_startingGlobalKeys)
                                                   {
                                                       Debug.LogWarning($"  world key: {key}");
                                                   }
                                               }
                                           }
                                       }
        */
        // Catch /die console command to track it
        [HarmonyPatch(typeof(ConsoleCommand), nameof(ConsoleCommand.RunAction), new[] { typeof(ConsoleEventArgs) })]
        public static class ConsoleCommand_RunAction_Patch
        {
            static void Postfix(ConsoleEventArgs args)
            {
                if (Player.m_localPlayer != null)
                {
                    if (args.Length > 0 && args[0] == "die")
                    {
                        __m_slashDieCount += 1;

                        AddPlayerEvent(PlayerEventType.Misc, "PenaltySlashDie", Player.m_localPlayer.transform.position);
                    }
                    if (args.Length > 0 && args[0] == "devcommands")
                    {
                        Debug.LogError($"INVALID FOR TOURNAMENT PLAY!: devcommands USED");

                        __m_invalidForTournamentPlay = true;

                        SetScoreTextElementColor(Color.green);

                        UpdateModUI(Player.m_localPlayer);
                    }
                    if (Game.instance.GetPlayerProfile().m_usedCheats == true ||
                     Game.instance.GetPlayerProfile().m_playerStats[PlayerStatType.Cheats] > 0)
                    {
                        Debug.LogError($"INVALID FOR TOURNAMENT PLAY!: cheats USED");

                        __m_invalidForTournamentPlay = true;

                        SetScoreTextElementColor(Color.green);

                        UpdateModUI(Player.m_localPlayer);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        public static class Patch_Player_OnDeath
        {
            static void Prefix(Player __instance)
            {
                if (__instance != null)
                {
                    AddPlayerEvent(PlayerEventType.Misc, "PenaltyDeath", __instance.transform.position);
                }
            }
        }

        // Increase sailing speed
        //
        // Informed by "Sailing Speed" mod by Smoothbrain
        [HarmonyPatch(typeof(Ship), nameof(Ship.GetSailForce))]
        public class Ship_GetSailForce_Patch
        {
            static void Postfix(ref Vector3 __result)
            {
                if (IsSagaMode())
                {
                    __result *= __m_sagaSailingSpeedMultiplier;
                }
                else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    __result *= __m_blitzSailingSpeedMultiplier;
                }
                else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    __result *= __m_trailblazerSailingSpeedMultiplier;
                }
            }
        }

        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
        public class Ship_Awake_Patch
        {
            static void Postfix(Ship __instance)
            {
                if (IsSagaMode())
                {
                    __instance.m_backwardForce *= __m_sagaPaddlingSpeedMultiplier;
                }
                else if (GetGameMode() == TrophyGameMode.TrophyBlitz)
                {
                    __instance.m_backwardForce *= __m_blitzPaddlingSpeedMultiplier;
                }
                else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    __instance.m_backwardForce *= __m_trailblazerPaddlingSpeedMultiplier;
                }
            }
        }

        // Ability to chop down any tree with any axe if the elder power is active
        //
        [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
        public static class TreeBase_Damage_Patch
        {
            static void Prefix(TreeBase __instance, ref HitData hit)
            {

                if (__m_elderPowerCutsAllTrees)
                {
                    Player player = Player.m_localPlayer;

                    if (player != null && player.GetGuardianPowerName() == "GP_TheElder" && player.m_guardianPowerCooldown > 0.0f)
                    {
                        hit.m_toolTier = (short)__instance.m_minToolTier;
                    }
                }
                //Debug.LogWarning($"Guardian Power: {player.GetGuardianPowerName()}");
                //Debug.LogWarning($"Treebase.m_minToolTier: {__instance.m_minToolTier}");
                //Debug.LogWarning($"HitData.m_toolTier: {hit.m_toolTier}");
            }
        }
        [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
        public static class TreeLog_Damage_Patch
        {
            static void Prefix(TreeLog __instance, ref HitData hit)
            {

                if (__m_elderPowerCutsAllTrees)
                {
                    Player player = Player.m_localPlayer;

                    if (player != null && player.GetGuardianPowerName() == "GP_TheElder" && player.m_guardianPowerCooldown > 0.0f)
                    {
                        hit.m_toolTier = (short)__instance.m_minToolTier;
                    }
                }
                //Debug.LogWarning($"Guardian Power: {player.GetGuardianPowerName()}");
                //Debug.LogWarning($"Treebase.m_minToolTier: {__instance.m_minToolTier}");
                //Debug.LogWarning($"HitData.m_toolTier: {hit.m_toolTier}");
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Awake))]
        public static class Fermenter_AddItem_Patch
        {
            static void Postfix(Fermenter __instance)
            {
                if (__instance != null && (IsSagaMode() || GetGameMode() == TrophyGameMode.TrophyBlitz) || GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    __instance.m_fermentationDuration = 10;
                }
            }
        }

        // In trophy saga, fermenter output is doubled
        //
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DelayedTap))]
        public static class Fermenter_DelayedTap_Patch
        {
            static void Prefix(Fermenter __instance)
            {
                if (__instance != null && (IsSagaMode() || GetGameMode() == TrophyGameMode.TrophyBlitz) || GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {

                    Fermenter.ItemConversion itemConversion = __instance.GetItemConversion(__instance.m_delayedTapItem);
                    if (itemConversion != null)
                    {
                        itemConversion.m_producedItems = 9;
                    }
                }
            }
        }

        // In trophy saga, Planted plants grow to maturity as soon as possible
        //
        [HarmonyPatch(typeof(Plant), nameof(Plant.TimeSincePlanted))]
        public static class Plant_GetGrowTime_Patch
        {
            static void Postfix(Plant __instance, ref double __result)
            {
                if (__instance != null)
                {
                    if (IsSagaMode())
                    {
                        //                        Debug.LogWarning("Plant.TimeSincePlanted()");

                        __result = (double)__instance.m_growTimeMax + 1;
                    }
                    else if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                    {
                        __result = (double)__instance.m_growTimeMax + 1;
                    }
                }
            }
        }

        // Let's not fuck with cooking stations
        //
        //[HarmonyPatch(typeof(CookingStation), MethodType.Constructor)]
        //public static class CookingStation_Constructor_Patch
        //{
        //    static void Postfix(CookingStation __instance)
        //    {
        //        if (__instance != null && GetGameMode() == TrophyGameMode.TrophySaga)
        //        {
        //            Debug.LogWarning($"CookingStation() {__instance.m_name}");
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake))]
        public static class Smelter_Awake_Patch
        {
            static void Postfix(Smelter __instance)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    //Debug.LogWarning($"Smelter.Awake() {__instance.m_name}");
                    //foreach (Smelter.ItemConversion item in __instance.m_conversion)
                    //{
                    //    Debug.LogWarning($" {item.m_from.name} to {item.m_to.name}");
                    //}

                    if (__instance.m_name.Contains("eitr"))
                    {
                        __instance.m_secPerProduct = 1f;
                    }
                    else if (__instance.m_name.Contains("bathtub") || __instance.m_name.Contains("batteringram"))
                    {
                        // Do nothing to the hot tub or the battering ram

                    }
                    else
                    {
                        __instance.m_secPerProduct = 0.03f;
                    }
                }
            }
        }

        // If it's an Eitr Refiner, auto-add the "ore" (Softtissue) when Sap is added to remove Softtisue requirement
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddFuel))]
        public static class Smelter_OnAddFuel_Patch
        {
            static void Postfix(Smelter __instance, Switch sw, Humanoid user, ItemDrop.ItemData item, bool __result)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    //                        Debug.LogWarning($"Smelter.OnAddFuel() {__instance.m_name}");

                    if (__instance.m_name.Contains("eitr"))
                    {
                        // Add ore if not full
                        if (__instance.GetQueueSize() < __instance.m_maxOre)
                        {
                            __instance.m_nview.InvokeRPC("RPC_AddOre", "Softtissue");
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.Awake))]
        public static class SapCollector_Awake_Patch
        {
            static void Postfix(SapCollector __instance)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    //                        Debug.LogWarning($"SapCollector.Awake() {__instance.m_name}");

                    __instance.m_secPerUnit = 0.1f;
                }
            }
        }

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Awake))]
        public static class Beehive_Awake_Patch
        {
            static void Postfix(Beehive __instance)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    __instance.m_secPerUnit = 5f;
                    __instance.m_maxHoney = 4;
                }
            }
        }
        /*
        [HarmonyPatch(typeof(Game), nameof(Game.ShowIntro))]
        public static class Game_ShowIntro_Patch
        {
            static string m_originalText;

            static void Prefix(Game __instance)
            {
                if (__instance != null)
                {
                    m_originalText = __instance.m_introText;

                    if (GetGameMode() == TrophyGameMode.CulinarySaga)
                    {
                        __instance.m_introText = CULINARY_SAGA_INTRO_TEXT;
                    }
                    else
                    {
                        __instance.m_introText = TROPHY_SAGA_INTRO_TEXT;
                    }
                }
            }
            static void Postfix(Game __instance)
            {
                //                    Debug.LogError($"Intro Text: {__instance.m_introText}");

                if (__instance != null)
                {
                    __instance.m_introText = m_originalText;
                }
            }
        }
        */
        // Mining all veins are more productive
        //
        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Awake))]
        public static class MineRock5_Awake_Patch
        {
            static void Postfix(MineRock5 __instance)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    __instance.m_dropItems.m_dropMin *= TROPHY_SAGA_MINING_MULTIPLIER;
                    __instance.m_dropItems.m_dropMax *= (TROPHY_SAGA_MINING_MULTIPLIER + 1);
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
                        string assetName = "Assets/UI/textures/small/trophies.png";
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


        // Eitr Refinery
        // "eitrrefinery"
        // insta-sap, Sap (0.2 weight to Eitr 5.0 weight)
        // Soft Tissue: "SoftTissue" "$item_softtissue"
        // Sap: "Sap" "$item_sap" 0.2 weight
        // Eitr: "Eitr" "$item_eitr" 5.0 weight
        //
        // Sap converts to Eitr when picked up?
        //

        // Boss Drops
        //
        // Eikthyr
        //  Hard Antler: "HardAntler" "$item_hardantler"
        //  Not dropped by anyone else
        //
        // Elder
        //  Crypt Key: "CryptKey" "$item_cryptkey"
        //  Also dropped by Greyling Brutes? 25%
        //
        // Bonemass
        //  Wishbone: "Wishbone" "$item_wishbone"
        //  Dropped by Oozers, 50%?
        //
        // Moder
        //  Dragon Tear: "DragonTear" "$item_dragontear"
        //  Dropped by Drakes, 10%?
        //
        // Yagluth
        //  Torn Spirit: "YagluthDrop" "$item_yagluththing"
        //  Dropped by Fuling Shaman, 25%?
        //
        // Queen
        //  Majestic Carapace: "QueenDrop" "$item_seekerqueen_drop"
        //  Dropped by Seeker Soldiers, 25%?
        //


        // Spinning Wheel
        // "piece_spinningwheel"
        //   

        // Windmill
        // "Windmill"
        // Windmill(), has m_smelter that makes it?


        // Oven
        // "piece_oven"
        // CookingStation()
        // m_smelter
        //

        [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.Start))]
        public static class EggGrow_Start_Patch
        {
            static void Postfix(EggGrow __instance)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    __instance.m_growTime = 2f;
                }
            }
        }

        [HarmonyPatch(typeof(Growup), nameof(Growup.Start))]
        public static class Growup_Start_Patch
        {
            static void Postfix(Growup __instance)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    __instance.m_growTime = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(Procreation), nameof(Procreation.Awake))]
        public static class Procreation_Awake_Patch
        {
            static void Postfix(Procreation __instance)
            {
                if (__instance != null && (IsSagaMode()))
                {
                    if (__instance.name.Contains("Hen"))
                    {
                        //                        Debug.LogWarning($"Procreation.Start: {__instance.name} {__instance.m_character.name}");
                        __instance.m_pregnancyDuration = 0.1f;
                        __instance.m_pregnancyChance = 0;
                        __instance.m_updateInterval = 1;
                    }
                }
            }
        }




        // END Harmony Patch area


        static public List<string> __m_cookedFoods = new List<string>();

        public class ConsumableData
        {
            public ConsumableData(string prefab, string item, string display, Biome biome, int points, float health, float stamina, float eitr, float regen)
            {
                m_prefabName = prefab;
                m_itemName = item;
                m_displayName = display;
                m_biome = biome;
                m_points = points;
                m_health = health;
                m_stamina = stamina;
                m_eitr = eitr;
                m_regen = regen;
            }

            public string m_prefabName;
            public string m_itemName;
            public string m_displayName;
            public Biome m_biome;
            public int m_points;
            public float m_health;
            public float m_stamina;
            public float m_eitr;
            public float m_regen;
        }

        static public ConsumableData[] __m_rawFoodData = new ConsumableData[]
        {
                new ConsumableData("Blueberries",              "$item_blueberries",             "Blueberries",                    Biome.Meadows,   0,   8,   25,  0,   1),
                new ConsumableData("Carrot",                   "$item_carrot",                  "Carrot",                         Biome.Meadows,   0,   10,  32,  0,   1),
                new ConsumableData("Cloudberry",               "$item_cloudberries",            "Cloudberries",                   Biome.Meadows,   0,   13,  40,  0,   1),
                new ConsumableData("Fiddleheadfern",           "$item_fiddleheadfern",          "Fiddlehead",                     Biome.Meadows,   0,   30,  30,  0,   1),
                new ConsumableData("Mushroom",                 "$item_mushroomcommon",          "Mushroom",                       Biome.Meadows,   0,   15,  15,  0,   1),
                new ConsumableData("MushroomBlue",             "$item_mushroomblue",            "Blue Mushroom",                  Biome.Meadows,   0,   20,  20,  0,   1),
                new ConsumableData("MushroomBzerker",          "$item_mushroom_bzerker",        "Toadstool",                      Biome.Meadows,   0,   0,   0,   0,   1),
                new ConsumableData("MushroomJotunPuffs",       "$item_jotunpuffs",              "Jotun Puffs",                    Biome.Meadows,   0,   25,  25,  0,   1),
                new ConsumableData("MushroomMagecap",          "$item_magecap",                 "Magecap",                        Biome.Meadows,   0,   25,  25,  25,  1),
                new ConsumableData("MushroomSmokePuff",        "$item_smokepuff",               "Smoke Puff",                     Biome.Meadows,   0,   15,  15,  0,   1),
                new ConsumableData("MushroomYellow",           "$item_mushroomyellow",          "Yellow Mushroom",                Biome.Meadows,   0,   10,  30,  0,   1),
                new ConsumableData("Honey",                    "$item_honey",                   "Honey",                          Biome.Meadows,   0,   8,   35,  0,   1),
                new ConsumableData("Onion",                    "$item_onion",                   "Onion",                          Biome.Meadows,   0,   13,  40,  0,   1),
                new ConsumableData("Pukeberries",              "$item_pukeberries",             "Bukeperries",                    Biome.Meadows,   0,   0,   0,   0,   1),
                new ConsumableData("Raspberry",                "$item_raspberries",             "Raspberries",                    Biome.Meadows,   0,   7,   20,  0,   1),
                new ConsumableData("RottenMeat",               "$item_meat_rotten",             "Rotten Meat",                    Biome.Meadows,   0,   0,   0,   0,   1),
                new ConsumableData("RoyalJelly",               "$item_royaljelly",              "Royal Jelly",                    Biome.Meadows,   0,   15,  15,  0,   1),
                new ConsumableData("Vineberry",                "$item_vineberry",               "Vineberry Cluster",              Biome.Meadows,   0,   30,  30,  30,  1),
        };

        static public ConsumableData[] __m_drinkData = new ConsumableData[]
        {
                new ConsumableData("BarleyWine",               "$item_barleywine",              "Fire Resistance Barley Wine",    Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadBugRepellent",         "$item_mead_bugrepellent",       "Anti-Sting Concoction",          Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadBzerker",              "$item_mead_bzerker",            "Berserkir Mead",                 Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadEitrLingering",        "$item_mead_eitr_lingering",     "Lingering Eitr Mead",            Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadEitrMinor",            "$item_mead_eitr_minor",         "Minor Eitr Mead",                Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadFrostResist",          "$item_mead_frostres",           "Frost Resistance Mead",          Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadHasty",                "$item_mead_hasty",              "Tonic of Ratatosk",              Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadHealthLingering",      "$item_mead_hp_lingering",       "Lingering Healing Mead",         Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadHealthMajor",          "$item_mead_hp_major",           "Major Healing Mead",             Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadHealthMedium",         "$item_mead_hp_medium",          "Medium Healing Mead",            Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadHealthMinor",          "$item_mead_hp_minor",           "Minor Healing Mead",             Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadLightfoot",            "$item_mead_lightfoot",          "Lightfoot Mead",                 Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadPoisonResist",         "$item_mead_poisonres",          "Poison Resistance Mead",         Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadStaminaLingering",     "$item_mead_stamina_lingering",  "Lingering Stamina Mead",         Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadStaminaMedium",        "$item_mead_stamina_medium",     "Medium Stamina Mead",            Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadStaminaMinor",         "$item_mead_stamina_minor",      "Minor Stamina Mead",             Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadStrength",             "$item_mead_strength",           "Mead of Troll Endurance",        Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadSwimmer",              "$item_mead_swimmer",            "Draught of Vananidir",           Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadTamer",                "$item_mead_tamer",              "Brew of Animal Whispers",        Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadTasty",                "$item_mead_tasty",              "Tasty Mead",                     Biome.Meadows,   0,   0,   0,   0,   0),
                new ConsumableData("MeadTrollPheromones",      "$item_mead_trollpheromones",    "Love Potion",                    Biome.Meadows,   0,   0,   0,   0,   0),
        };

        static public ConsumableData[] __m_feastData = new ConsumableData[]
        {
                new ConsumableData("FeastAshlands",            "$item_feastashlands",           "Ashlands Gourmet Bowl",          Biome.Meadows,   0,   75,  75,  38,  6),
                new ConsumableData("FeastBlackforest",         "$item_feastblackforest",        "Black Forest Buffet Platter",    Biome.Meadows,   0,   35,  35,  0,   3),
                new ConsumableData("FeastMeadows",             "$item_feastmeadows",            "Whole Roasted Meadow Boar",      Biome.Meadows,   0,   35,  35,  0,   2),
                new ConsumableData("FeastMistlands",           "$item_feastmistlands",          "Mushrooms Galore á la Mistlands",Biome.Meadows,   0,   65,  65,  33,  5),
                new ConsumableData("FeastMountains",           "$item_feastmountains",          "Hearty Mountain Logger's Stew",  Biome.Meadows,   0,   45,  45,  0,   3),
                new ConsumableData("FeastOceans",              "$item_feastoceans",             "Sailor's Bounty",                Biome.Meadows,   0,   45,  45,  0,   3),
                new ConsumableData("FeastPlains",              "$item_feastplains",             "Plains Pie Picnic",              Biome.Meadows,   0,   55,  55,  0,   4),
                new ConsumableData("FeastSwamps",              "$item_feastswamps",             "Swamp Dweller's Delight",        Biome.Meadows,   0,   35,  35,  0,   3),

        };

        static public ConsumableData[] __m_cookedFoodData = new ConsumableData[]
        {
                new ConsumableData("NeckTailGrilled",          "$item_necktailgrilled",         "Grilled Neck Tail",              Biome.Meadows,   10,   25,  8,   0,   2),
                new ConsumableData("CookedMeat",               "$item_boar_meat_cooked",        "Cooked Boar Meat",               Biome.Meadows,   10,   30,  10,  0,   2),
                new ConsumableData("CookedDeerMeat",           "$item_deer_meat_cooked",        "Cooked Deer Meat",               Biome.Meadows,   10,   35,  12,  0,   2),
                new ConsumableData("QueensJam",                "$item_queensjam",               "Queen's Jam",                    Biome.Meadows,   10,   14,  40,  0,   2),

                new ConsumableData("BoarJerky",                "$item_boarjerky",               "Boar Jerky",                     Biome.Forest,    20,   23,  23,  0,   2),
                new ConsumableData("DeerStew",                 "$item_deerstew",                "Deer Stew",                      Biome.Forest,    20,   45,  15,  0,   3),
                new ConsumableData("CarrotSoup",               "$item_carrotsoup",              "Carrot Soup",                    Biome.Forest,    20,   15,  45,  0,   2),
                new ConsumableData("MinceMeatSauce",           "$item_mincemeatsauce",          "Minced Meat Sauce",              Biome.Forest,    20,   40,  13,  0,   3),
                new ConsumableData("CookedBjornMeat",          "$item_bjorn_meat_cooked",       "Cooked Bear Meat",               Biome.Forest,    20,   40,  13,  0,   2),

                new ConsumableData("Sausages",                 "$item_sausages",                "Sausages",                       Biome.Swamp,     30,   55,  18,  0,   3),
                new ConsumableData("ShocklateSmoothie",        "$item_shocklatesmoothie",       "Muckshake",                      Biome.Swamp,     30,   16,  50,  0,   1),
                new ConsumableData("TurnipStew",               "$item_turnipstew",              "Turnip Stew",                    Biome.Swamp,     30,   18,  55,  0,   2),
                new ConsumableData("BlackSoup",                "$item_blacksoup",               "Black Soup",                     Biome.Swamp,     30,   50,  17,  0,   3),

                new ConsumableData("OnionSoup",                "$item_onionsoup",               "Onion Soup",                     Biome.Mountains, 30,   20,  60,  0,   1),
                new ConsumableData("CookedWolfMeat",           "$item_wolf_meat_cooked",        "Cooked Wolf Meat",               Biome.Mountains, 30,   45,  15,  0,   3),
                new ConsumableData("WolfJerky",                "$item_wolfjerky",               "Wolf Jerky",                     Biome.Mountains, 30,   33,  33,  0,   3),
                new ConsumableData("WolfMeatSkewer",           "$item_wolf_skewer",             "Wolf Skewer",                    Biome.Mountains, 30,   65,  21,  0,   3),
                new ConsumableData("Eyescream",                "$item_eyescream",               "Eyescream",                      Biome.Mountains, 30,   21,  65,  0,   1),

                new ConsumableData("FishCooked",               "$item_fish_cooked",             "Cooked Fish",                    Biome.Ocean,     40,   45,  15,  0,   2),
                new ConsumableData("SerpentMeatCooked",        "$item_serpentmeatcooked",       "Cooked Serpent Meat",            Biome.Ocean,     40,   70,  23,  0,   3),
                new ConsumableData("SerpentStew",              "$item_serpentstew",             "Serpent Stew",                   Biome.Ocean,     40,   80,  26,  0,   4),

                new ConsumableData("CookedLoxMeat",            "$item_loxmeat_cooked",          "Cooked Lox Meat",                Biome.Plains,    40,   50,  16,  0,   4),
                new ConsumableData("FishWraps",                "$item_fishwraps",               "Fish Wraps",                     Biome.Plains,    40,   70,  23,  0,   4),
                new ConsumableData("LoxPie",                   "$item_loxpie",                  "Lox Meat Pie",                   Biome.Plains,    40,   75,  24,  0,   4),
                new ConsumableData("BloodPudding",             "$item_bloodpudding",            "Blood Pudding",                  Biome.Plains,    40,   25,  75,  0,   2),
                new ConsumableData("Bread",                    "$item_bread",                   "Bread",                          Biome.Plains,    40,   23,  70,  0,   2),
                new ConsumableData("CookedEgg",                "$item_egg_cooked",              "Cooked Egg",                     Biome.Plains,    40,   35,  12,  0,   2),
                new ConsumableData("CookedChickenMeat",        "$item_chicken_meat_cooked",     "Cooked Chicken Meat",            Biome.Plains,    40,   60,  20,  0,   5),

                new ConsumableData("CookedHareMeat",           "$item_hare_meat_cooked",        "Cooked Hare Meat",               Biome.Mistlands, 50,   60,  20,  0,   5),
                new ConsumableData("CookedBugMeat",            "$item_bug_meat_cooked",         "Cooked Seeker Meat",             Biome.Mistlands, 50,   60,  20,  0,   5),
                new ConsumableData("MeatPlatter",              "$item_meatplatter",             "Meat Platter",                   Biome.Mistlands, 50,   80,  26,  0,   5),
                new ConsumableData("HoneyGlazedChicken",       "$item_honeyglazedchicken",      "Honey Glazed Chicken",           Biome.Mistlands, 50,   80,  26,  0,   5),
                new ConsumableData("MisthareSupreme",          "$item_mistharesupreme",         "Misthare Supreme",               Biome.Mistlands, 50,   85,  28,  0,   5),
                new ConsumableData("Salad",                    "$item_salad",                   "Salad",                          Biome.Mistlands, 50,   26,  80,  0,   3),
                new ConsumableData("MushroomOmelette",         "$item_mushroomomelette",        "Mushroom Omelette",              Biome.Mistlands, 50,   28,  85,  0,   3),
                new ConsumableData("FishAndBread",             "$item_fishandbread",            "Fish 'n' Bread",                 Biome.Mistlands, 50,   30,  90,  0,   3),
                new ConsumableData("MagicallyStuffedShroom",   "$item_magicallystuffedmushroom","Stuffed Mushroom",               Biome.Mistlands, 50,   25,  12,  75,  3),
                new ConsumableData("YggdrasilPorridge",        "$item_yggdrasilporridge",       "Yggdrasil Porridge",             Biome.Mistlands, 50,   27,  13,  80,  3),
                new ConsumableData("SeekerAspic",              "$item_seekeraspic",             "Seeker Aspic",                   Biome.Mistlands, 50,   28,  14,  85,  3),

                new ConsumableData("CookedAsksvinMeat",        "$item_asksvin_meat_cooked",     "Cooked Asksvin Tail",            Biome.Ashlands,  60,   70,  24,  0,   6),
                new ConsumableData("CookedVoltureMeat",        "$item_volture_meat_cooked",     "Cooked Volture Meat",            Biome.Ashlands,  60,   70,  24,  0,   6),
                new ConsumableData("CookedBoneMawSerpentMeat", "$item_bonemawmeat_cooked",      "Cooked Bonemaw Meat",            Biome.Ashlands,  60,   90,  30,  0,   6),
                new ConsumableData("FierySvinstew",            "$item_fierysvinstew",           "Fiery Svinstew",                 Biome.Ashlands,  60,   95,  32,  0,   6),
                new ConsumableData("MashedMeat",               "$item_mashedmeat",              "Mashed Meat",                    Biome.Ashlands,  60,   100, 34,  0,   6),
                new ConsumableData("PiquantPie",               "$item_piquantpie",              "Piquant Pie",                    Biome.Ashlands,  60,   105, 35,  0,   6),
                new ConsumableData("SpicyMarmalade",           "$item_spicymarmalade",          "Spicy Marmalade",                Biome.Ashlands,  60,   30,  90,  0,   4),
                new ConsumableData("ScorchingMedley",          "$item_scorchingmedley",         "Scorching Medley",               Biome.Ashlands,  60,   32,  95,  0,   4),
                new ConsumableData("RoastedCrustPie",          "$item_roastedcrustpie",         "Roasted Crust Pie",              Biome.Ashlands,  60,   34,  100, 0,   4),
                new ConsumableData("SizzlingBerryBroth",       "$item_sizzlingberrybroth",      "Sizzling Berry Broth",           Biome.Ashlands,  60,   28,  14,  85,  4),
                new ConsumableData("SparklingShroomshake",     "$item_sparklingshroomshake",    "Sparkling Shroomshake",          Biome.Ashlands,  60,   30,  15,  90,  4),
                new ConsumableData("MarinatedGreens",          "$item_marinatedgreens",         "Marinated Greens",               Biome.Ashlands,  60,   32,  16,  95,  4),

            //new ConsumableData("HealthUpgrade_Bonemass",   "Bonemass heart",                "Bonemass heart",                 Biome.Meadows,   0,   0,   0,   0,   0),
            //new ConsumableData("HealthUpgrade_GDKing",     "Elder heart",                   "Elder heart",                    Biome.Meadows,   0,   0,   0,   0,   0),
            //new ConsumableData("StaminaUpgrade_Greydwarf", "Stamina Greydwarf",             "Stamina Greydwarf",              Biome.Meadows,   0,   0,   0,   0,   0),
            //new ConsumableData("StaminaUpgrade_Troll",     "Stamina Troll",                 "Stamina Troll",                  Biome.Meadows,   0,   0,   0,   0,   0),
            //new ConsumableData("StaminaUpgrade_Wraith",    "Stamina Wraith",                "Stamina Wraith",                 Biome.Meadows,   0,   0,   0,   0,   0),
        };



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
                if (GetGameMode() != TrophyGameMode.TrophyBlitz && GetGameMode() != TrophyGameMode.TrophyTrailblazer)
                {
                    return;
                }

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
            public static bool Prefix(Piece __instance, HitData hitData)
            {
                if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    if (!__instance.IsPlacedByPlayer())
                    {
                        __m_overrideGlobalKeysForPieceDrops = true;
                    }
                }
                return true;
            }

            public static void Postfix(Piece __instance)
            {
                if (GetGameMode() != TrophyGameMode.TrophyBlitz && GetGameMode() != TrophyGameMode.TrophyTrailblazer)
                {
                    return;
                }

                if (GetGameMode() == TrophyGameMode.TrophyTrailblazer)
                {
                    __m_overrideGlobalKeysForPieceDrops = false;
                }

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
                if (GetGameMode() != TrophyGameMode.TrophyBlitz && GetGameMode() != TrophyGameMode.TrophyTrailblazer)
                {
                    return;
                }

                if (__instance != null)
                {
                    //                    Debug.LogWarning($"TeleportWorld.SetText(): name: {__instance.name} Pos: {__instance.transform.position} text: '{text}' ");
                    RenamePortalPin(__instance.transform.position, text);
                }
            }
        }
        /*
                static bool PlaceAPortalHere(Vector3 pos, string name)
                {
                    if (Player.m_localPlayer != null)
                    {
                        var allPieces = Resources.FindObjectsOfTypeAll<Piece>();

                        Piece portalPiece = null;
                        foreach (var piece in allPieces)
                        {
                            //                        Debug.LogWarning($"Piece: {piece.m_name}");
                            if (piece.m_name == ("$piece_portal"))
                            {
                                portalPiece = piece;
                                Debug.LogWarning($"Found Portal Piece");
                                break;
                            }
                        }f
                        if (portalPiece)
                        {
                            float floor = 0;
                            Vector3 placePos = pos;
                            if (ZoneSystem.instance.GetGroundHeight(pos, out floor))
                            {
                                WaterVolume waterVolume = null;
                                float waterLevel = Floating.GetWaterLevel(pos, ref waterVolume);
                                Debug.LogWarning($"Ground height at {pos} is {floor}, water level is {waterLevel}");
                                floor = Math.Max(floor, waterLevel);
                                placePos.y = floor;
                            }
                            Debug.LogWarning($"Placing Portal {portalPiece.m_name} at {placePos} (playerPos: {Player.m_localPlayer.transform.position})");

                            Player.m_localPlayer.PlacePiece(portalPiece, placePos, Quaternion.identity, false);

                            return true;
                        }
                    }

                    return false;
                }

                static PinData __m_lastNamePin = null;

                [HarmonyPatch(typeof(Minimap), nameof(Minimap.HidePinTextInput))]
                public static class Minimap_HidePinTextInput_Patch
                {
                    public static bool Prefix(Minimap __instance, bool delayTextInput)
                    {
                        Debug.LogWarning($"Minimap.HidePinTextInput()");
                        __m_lastNamePin = __instance.m_namePin;
                        return true;
                    }
                }

                [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnPinTextEntered))]
                public static class Minimap_OnPinTextEntered_Patch
                {
                    public static void Postfix(Minimap __instance, string t)
                    {
                        if (__instance == null)
                        {
                            return;
                        }

                        Debug.LogWarning($"Minimap.OnPinTextEntered()");

                        PinData pinData = __m_lastNamePin;
                        if (pinData == null)
                        {
                            Debug.LogWarning($"No PinData");

                            return;
                        }

                        if (pinData.m_type == PinType.Icon4)
                        {
                            PlaceAPortalHere(pinData.m_pos, __m_lastNamePin.m_name);
                        }
                        __m_lastNamePin = null;
                    }
                }
        */
    }
}


/*

Trophy Saga

* Trophy drop rate increased by 50%, capped at 50%
* All metal ores and scrap insta-smelt upon pickup
* All boats are twice as fast as normal
* Combat on Hard
* Resources at 1.5x

The goal would be to push through the slow points in the progression and encourage exploration and travel. Right now the first hour in Hunt and the first two hours in Rush seem to drag.

Any thoughts?   
Maybe also:

* No biome bonuses

? Two star enemies have a chance to drop Megingjord

*/

/*
 * 
 * Up the ooze drop rate
 * Make ores 3x when mining
 * trolls drop meginjord
 * greylings drop lots of finewood
 * Queen is 180? 200?
 * Sealbreaker x2 doesn't work
 * 
 */

/*
 * Culinary Saga
 * 
 * Fishing Bait droppers
 
FishingBait					$item_fishingbait			Fishing Bait		    Neck            
FishingBaitAshlands			$item_fishingbait_ashlands	Hot Fishing Bait	    Charred Warrior     $enemy_charred_melee
FishingBaitCave				$item_fishingbait_cave		Cold Fishing Bait	    Fenring             $enemy_fenring
FishingBaitDeepNorth		$item_fishingbait_deepnorth	Frosty Fishing Bait	    Drake               $enemy_drake
FishingBaitForest			$item_fishingbait_forest	Mossy Fishing Bait	    Troll               $enemy_troll
FishingBaitMistlands		$item_fishingbait_mistlands	Misty Fishing Bait	    Lox                 $enemy_lox
FishingBaitOcean			$item_fishingbait_ocean		Heavy Fishing Bait	    Serpent             $enemy_serpent
FishingBaitPlains			$item_fishingbait_plains	Stingy Fishing Bait	    Fuling              $enemy_goblin
FishingBaitSwamp			$item_fishingbait_swamp		Sticky Fishing Bait	    Abomination         $enemy_abomination

 */

