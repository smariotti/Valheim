
using BepInEx;
using static Terminal;
using UnityEngine;
using System.Collections.Generic;

namespace TrophyHuntMod
{
    public partial class TrophyHuntMod : BaseUnityPlugin
    {

        // New Console Commands for TrophyHuntMod
        #region Console Commands

        public static void PrintToConsole(string message)
        {
            if (Console.m_instance) Console.m_instance.AddString(message);
            if (Chat.m_instance) Chat.m_instance.AddString(message);
            Debug.Log(message);
        }

        void AddConsoleCommands()
        {
            ConsoleCommand trophyHuntCommand = new ConsoleCommand("trophyhunt", "Prints trophy hunt data", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'trophyhunt' console command can only be used in-game.");
                    return true;
                }

                PrintToConsole($"[Trophy Hunt Scoring]");

                PrintToConsole($"Trophies:");
                int score = CalculateTrophyPoints(true);
                PrintToConsole($"Trophy Score Total: {score}");
                int deathScore = CalculateDeathPenalty();
                int logoutScore = CalculateLogoutPenalty();
                PrintToConsole($"Penalties:");
                PrintToConsole($"  Deaths: {__m_deaths} Score: {deathScore}");
                PrintToConsole($"  Logouts: {__m_logoutCount} Score: {logoutScore}");

                int biomeBonus = 0;
                if (GetGameMode() == TrophyGameMode.TrophyRush)
                {
                    CalculateBiomeBonusScore(Player.m_localPlayer);
                    PrintToConsole($"Biome Bonus Total: {biomeBonus}");
                }
                score += deathScore;
                score += logoutScore;
                score += biomeBonus;
                PrintToConsole($"Total Score: {score}");

                if (args.Length > 1)
                {
                    string arg = args[1];
                    //if (arg == "xmal")
                    //{
                    //    SetScoreTextElementColor(Color.yellow);
                    //    __m_ignoreInvalidateUIChanges = true;
                    //}

                }
                return true;
            });

            //ConsoleCommand dumpFoodCommand = new ConsoleCommand("dumpfood", "Dump all consumable items", delegate (ConsoleEventArgs args)
            //{
            //        // Get the ObjectDB instance
            //    ObjectDB objectDB = ObjectDB.instance;

            //    if (objectDB == null)
            //    {
            //        Debug.LogError("ObjectDB is not initialized yet.");
            //        return;
            //    }

            //    // Iterate through all items in the ObjectDB
            //    foreach (GameObject prefab in objectDB.m_items)
            //    {
            //        if (prefab == null) continue;

            //        // Check if the prefab has an ItemDrop component
            //        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            //        if (itemDrop == null) continue;

            //        // Check if the item is of type Consumable
            //        if (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
            //        {
            //            string prefabName = prefab.name;
            //            string itemName = itemDrop.m_itemData.m_shared.m_name;
            //            string displayName = Localization.instance.Localize(itemName);
            //            float health = itemDrop.m_itemData.m_shared.m_food;
            //            float stamina = itemDrop.m_itemData.m_shared.m_foodStamina;
            //            float eitr = itemDrop.m_itemData.m_shared.m_foodEitr;
            //            float regen = itemDrop.m_itemData.m_shared.m_foodRegen;


            //            Debug.Log($"new ConsumableData({QWC(prefabName),-28}{QWC(itemName),-33}{QWC(displayName),-34}{QWC("Biome.Meadows"),-16}{0.ToString()+",",-5}{health.ToString() + ",",-5}{stamina.ToString() + ",",-5}{eitr.ToString() + ",",-5}{regen.ToString() + ",",-5})");
            //        }
            //    }

            //    string QWC(string s)
            //    {
            //        return "\"" + s + "\",";
            //    }
            //});

            ConsoleCommand dumpRecipes = new ConsoleCommand("dumprecipes", "Dump all recipes", delegate (ConsoleEventArgs args)
            {
                // Get the ObjectDB instance
                ObjectDB objectDB = ObjectDB.instance;

                if (objectDB == null)
                {
                    Debug.LogError("ObjectDB is not initialized yet.");
                    return;
                }

                // Iterate through all items in the ObjectDB
                foreach (Recipe recipe in objectDB.m_recipes)
                {
                    if (recipe == null) continue;

                    if (recipe.m_item != null)
                    {
                        CraftingStation station = recipe.m_craftingStation;
                        string stationName = "n/a";
                        if (station)
                        {
                            stationName = station.name;
                        }
                        int level = recipe.m_minStationLevel;

                        Debug.LogWarning($"{recipe.name} {recipe.m_item.m_itemData.m_shared.m_itemType}: {stationName} {level}");
                        foreach (Piece.Requirement req in recipe.m_resources)
                        {
                            Debug.LogWarning($"  req: {req.m_resItem.name} {req.m_amount}");
                        }
                    }
                }
            });


            ConsoleCommand showBossesCommand = new ConsoleCommand("showbosses", "Show all potential boss locations", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'showbosses' console command can only be used in-game.");
                }

                RevealAllBosses(Player.m_localPlayer);
            }, true);

            /*
            ConsoleCommand instaSmelt = new ConsoleCommand("instasmelt", "Toggle Insta-Smelt", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    return;
                }

                __m_instaSmelt = !__m_instaSmelt;

                PrintToConsole($"Instasmelt: {__m_instaSmelt}");

            });
            */

            ConsoleCommand ignoreLogoutsCommand = new ConsoleCommand("ignorelogouts", "Don't subtract points for logouts", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'/ignorelogouts' can only be used in gameplay.");
                    return;
                }

                __m_ignoreLogouts = !__m_ignoreLogouts;

                __m_invalidForTournamentPlay = true;

                if (__m_scoreTextElement != null)
                {
                    if (__m_ignoreLogouts)
                    {
                        TMPro.TextMeshProUGUI tmText = __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>();

                        tmText.color = Color.green;
                    }
                }

                if (__m_relogsTextElement != null)
                {
                    if (__m_ignoreLogouts)
                    {
                        TMPro.TextMeshProUGUI tmText = __m_relogsTextElement.GetComponent<TMPro.TextMeshProUGUI>();

                        tmText.color = Color.gray;
                    }
                }
            });


            ConsoleCommand showAllTrophyStats = new ConsoleCommand("showalltrophystats", "Toggle tracking ALL enemy deaths and trophies with JUST tracking player kills and trophies", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'/showalltrophystats' can only be used in gameplay.");
                    return;
                }

                ToggleShowAllTrophyStats();

                __m_invalidForTournamentPlay = true;

                if (__m_scoreTextElement != null)
                {
                    if (__m_showAllTrophyStats)
                    {
                        TMPro.TextMeshProUGUI tmText = __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>();

                        tmText.color = Color.green;
                    }
                }

                InitializeSagaDrops();
            });

            ConsoleCommand toggleScoreBGCommand = new ConsoleCommand("togglescorebackground", "Toggle black background underneath the score", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'togglescorebackground' console command can only be used in-game.");
                }


                RectTransform textTransform = __m_scoreBGElement.GetComponent<RectTransform>();

                __m_scoreBGElement.SetActive(!__m_scoreBGElement.activeSelf);
            });


            ConsoleCommand scoreScaleCommand = new ConsoleCommand("scorescale", "Scale the score text sizes (1.0 is default)", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'scorescale' console command can only be used in-game.");
                }

                // First argument is user trophy scale
                if (args.Length > 1)
                {
                    float userScale = float.Parse(args[1]);
                    if (userScale == 0) userScale = 1;
                    __m_userTextScale = userScale;

                }
                else
                {
                    // no arguments means reset
                    __m_userTextScale = 1.0f;
                }

                RectTransform textTransform = __m_scoreTextElement.GetComponent<RectTransform>();
                textTransform.localScale = new Vector3(__m_userTextScale, __m_userTextScale, __m_userTextScale);

                // Readjust the UI elements' trophy sizes
                //Player player = Player.m_localPlayer;
                //if (player != null)
                //{
                //    TextMeshProUGUI textElement = __m_scoreTextElement.GetComponent<TextMeshProUGUI>();
                //    if (textElement != null)
                //    {
                //        textElement.fontSize = DEFAULT_SCORE_FONT_SIZE * __m_userScoreScale;
                //    }
                //}
            });

            ConsoleCommand trophyScaleCommand = new ConsoleCommand("trophyscale", "Scale the trophy sizes (1.0 is default)", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'trophyscale' console command can only be used in-game.");
                }

                // First argument is user trophy scale
                if (args.Length > 1)
                {
                    float userScale = float.Parse(args[1]);
                    if (userScale == 0) userScale = 1;
                    __m_userIconScale = userScale;

                    // second argument is base trophy scale (for debugging)
                    if (args.Length > 2)
                    {
                        float baseScale = float.Parse(args[2]);
                        if (baseScale == 0) baseScale = 1;
                        __m_baseTrophyScale = baseScale;
                    }
                }
                else
                {
                    // no arguments means reset
                    __m_userIconScale = 1.0f;
                    __m_baseTrophyScale = 1.0f;
                }

                // Readjust the UI elements' trophy sizes
                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    List<string> discoveredTrophies = player.GetTrophies();
                    foreach (TrophyHuntData td in __m_trophyHuntData)
                    {
                        string trophyName = td.m_name;

                        GameObject iconGameObject = __m_iconList.Find(gameObject => gameObject.name == trophyName);

                        if (iconGameObject != null)
                        {
                            UnityEngine.UI.Image image = iconGameObject.GetComponent<UnityEngine.UI.Image>();
                            if (image != null)
                            {
                                RectTransform imageRect = iconGameObject.GetComponent<RectTransform>();

                                if (imageRect != null)
                                {
                                    imageRect.localScale = new Vector3(__m_baseTrophyScale, __m_baseTrophyScale, __m_baseTrophyScale) * __m_userIconScale;
                                }
                            }
                        }
                    }
                }
            });

            ConsoleCommand trophySpacingCommand = new ConsoleCommand("trophyspacing", "Space the trophies out (negative and positive numbers work)", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'trophyspacing' console command can only be used in-game.");
                    return;
                }

                if (Player.m_localPlayer == null)
                {
                    return;
                }

                Player player = Player.m_localPlayer;

                // First argument is user trophy scale
                if (args.Length > 1)
                {
                    float userSpacing = float.Parse(args[1]);
                    if (userSpacing == 0) userSpacing = 1;
                    __m_userTrophySpacing = userSpacing;
                }
                else
                {
                    // no arguments means reset
                    __m_userTrophySpacing = 0.0f;
                }

                Transform healthPanelTransform = Hud.instance.transform.Find("hudroot/healthpanel");
                if (healthPanelTransform == null)
                {
                    Debug.LogError("Health panel transform not found.");

                    return;
                }

                DeleteTrophyIconElements(__m_iconList);
                CreateTrophyIconElements(healthPanelTransform, __m_trophyHuntData, __m_iconList);
                EnableTrophyHuntIcons(player);
            });

            ConsoleCommand showTrophies = new ConsoleCommand("showtrophies", "Toggle Trophy Rush Mode on and off", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'/showtrophies' console command can only be used during gameplay.");
                    return;
                }

                __m_showingTrophies = !__m_showingTrophies;

                ShowTrophies(__m_showingTrophies);
            });

            ConsoleCommand showOnlyDeaths = new ConsoleCommand("showonlydeaths", "Hide all of the UI except for the death counter", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'/showonlydeaths' console command can only be used during gameplay.");
                    return;
                }

                __m_showOnlyDeaths = !__m_showOnlyDeaths;

                ShowOnlyDeaths(__m_showOnlyDeaths);
            });

            ConsoleCommand elderPowerCutsAllTrees = new ConsoleCommand("elderpowercutsalltrees", "All trees are choppable while elder power active", delegate (ConsoleEventArgs args)
            {
                __m_elderPowerCutsAllTrees = !__m_elderPowerCutsAllTrees;
                PrintToConsole($"elder power cuts all trees: {__m_elderPowerCutsAllTrees}");

                if (__m_elderPowerCutsAllTrees)
                {
                    __m_invalidForTournamentPlay = true;

                    if (__m_scoreTextElement != null)
                    {
                        TMPro.TextMeshProUGUI tmText = __m_scoreTextElement.GetComponent<TMPro.TextMeshProUGUI>();

                        tmText.color = Color.green;
                    }
                }
            });

            ConsoleCommand timerCommand = new ConsoleCommand("timer", "Control the Trophy Hunt Timer display (start/stop/reset/show/hide)", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'timer' console command can only be used in-game.");
                }

                if (__m_gameTimerTextElement == null)
                {
                    return;
                }

                // First argument is user trophy scale
                if (args.Length > 1)
                {
                    string timerCommand = args[1].Trim();
                    switch (timerCommand)
                    {
                        case "start": TimerStart(); break;
                        case "stop": TimerStop(); break;
                        case "reset": TimerReset(); break;
                        case "show": __m_gameTimerVisible = true; break;
                        case "hide": __m_gameTimerVisible = false; break;
                        case "set": TimerSet(args[2]); break;
                        case "toggle": TimerToggle(); break;
                    }
                }
                else
                {
                    // no arguments means show/hide
                }
            });

            //ConsoleCommand notACheater = new ConsoleCommand("iamnotacheater", "Reset PlayerStats to disable the cheated flag", delegate (ConsoleEventArgs args)
            //{
            //    if (!Game.instance)
            //    {
            //        PrintToConsole("'timer' console command can only be used in-game.");
            //    }
            //    Game.instance.m_playerProfile.m_usedCheats = false;
            //    Game.instance.m_playerProfile.m_playerStats[PlayerStatType.Cheats] = 0;
            //});

            ConsoleCommand boatSpeed = new ConsoleCommand("boatspeedmultiplier", "How fast do you want to go?", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'boatspeedmultiplier' console command can only be used in-game.");
                }
                __m_sagaSailingSpeedMultiplier = int.Parse(args[1]);
                UpdateModUI(Player.m_localPlayer);
            });

            ConsoleCommand showCharmList = new ConsoleCommand("showcharmlist", "Show the list of charmed enemies in the debug log", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'showcharmlist' console command can only be used in-game.");
                }

                __m_showCharmList = !__m_showCharmList;
            });

            ConsoleCommand releaseThralls = new ConsoleCommand("releasethralls", "On-charm all current Thralls", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'releasethralls' console command can only be used in-game.");
                }

                ReleaseAllThralls();
            }, true);

            ConsoleCommand charmLevel = new ConsoleCommand("charmLevel", "Set charm level of all thralls", delegate (ConsoleEventArgs args)
            {
                if (!Game.instance)
                {
                    PrintToConsole("'charmLevel' console command can only be used in-game.");
                }
                
                int level = 0;

                if (args.Length > 1)
                {
                    level = int.Parse(args[1]);
                }

                SetCharmLevel(level);
            }, true);

        }
        #endregion
    }
}