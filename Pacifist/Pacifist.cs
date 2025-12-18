using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TrophyHuntMod;
using TMPro;
using HarmonyLib.Tools;

namespace TrophyHuntMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public partial class TrophyHuntMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.Pacifist";
        public const string PluginName = "Pacifist";
        public const string PluginVersion = "0.1.0";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        public const float CHARMED_ENEMY_SPEED_MULTIPLIER = 3.0f;
        static TMP_FontAsset __m_globalFontObject = null;
        static public TrophyHuntMod __m_trophyHuntMod;

        public void Awake()
        {
            __m_trophyHuntMod = this;

            HarmonyFileLog.Enabled = true;

            harmony.PatchAll();

            //harmony.GetPatchedMethods().ToList().ForEach(method =>
            //{
            //    Debug.LogError("Patched: " + method.DeclaringType.FullName + "." + method.Name);
            //});
        }

        public static bool IsPacifist()
        {
            return true;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public class Player_OnSpawned_Patch
        {
            static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                CacheSprites();

                DoPacifistPostPlayerSpawnTasks();

                if (__m_thrallsWindowObject == null)
                {
                    Transform healthPanelTransform = Hud.instance.transform.Find("hudroot/healthpanel");
                    if (healthPanelTransform == null)
                    {
                        Debug.LogError("Health panel transform not found.");

                        return;
                    }
                    CreateThrallsWindow(healthPanelTransform);
                }

                StartCharmTimer();

                UpdateModUI(Player.m_localPlayer);
            }
        }

        public static TextMeshProUGUI AddTextMeshProComponent(GameObject toThisObject)
        {
            TextMeshProUGUI textMeshComponent = toThisObject.AddComponent<TextMeshProUGUI>();
            textMeshComponent.font = __m_globalFontObject;
            textMeshComponent.material = __m_globalFontObject.material;

            return textMeshComponent;
        }

        public static void UpdateModUI(Player player)
        {

        }
        static GameObject __m_thrallsWindowObject = null;
        static GameObject __m_thrallsWindowBackground = null;
        static TextMeshProUGUI __m_thrallsWindowText = null;
        static Vector2 __m_thrallsTooltipWindowSize = new Vector2(410, 170);
        static Vector2 __m_thrallsTooltipTextOffset = new Vector2(5, 2);

        public static void CreateThrallsWindow(Transform parentTransform)
        {
            // Tooltip Background
            __m_thrallsWindowBackground = new GameObject("Thrall Window Background");

            // Set %the parent to the HUD
            __m_thrallsWindowBackground.transform.SetParent(parentTransform, false);

            Vector2 windowPos = new Vector2(-90, 360);

            RectTransform bgTransform = __m_thrallsWindowBackground.AddComponent<RectTransform>();
            bgTransform.sizeDelta = __m_thrallsTooltipWindowSize;
            bgTransform.anchoredPosition = windowPos;
            bgTransform.pivot = new Vector2(0, 0);

            // Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_thrallsWindowBackground.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.90f); // Semi-transparent black background
            __m_thrallsWindowBackground.SetActive(false);

            // Create a new GameObject for the tooltip
            __m_thrallsWindowObject = new GameObject("Thrall Window Text");
            __m_thrallsWindowObject.transform.SetParent(parentTransform, false);

            // Add a RectTransform component for positioning
            RectTransform rectTransform = __m_thrallsWindowObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(__m_thrallsTooltipWindowSize.x - __m_thrallsTooltipTextOffset.x, __m_thrallsTooltipWindowSize.y - __m_thrallsTooltipTextOffset.y);
            rectTransform.anchoredPosition = windowPos + new Vector2(5, 0);
            rectTransform.pivot = new Vector2(0, 0);

            // Add a TextMeshProUGUI component for displaying the tooltip text
            __m_thrallsWindowText = AddTextMeshProComponent(__m_thrallsWindowObject);
            __m_thrallsWindowText.fontSize = 14;
            __m_thrallsWindowText.alignment = TextAlignmentOptions.TopLeft;
            __m_thrallsWindowText.color = Color.yellow;

            // Initially hide the tooltip
            __m_thrallsWindowObject.SetActive(true);
        }

        public static string BuildThrallsWindowText(ref int lines)
        {
            string text =
                $"<size=20><b><color=#FFB75B>Thralls</color><b></size>\n";

            text += $"\n<size=16><pos=0%><color=white><u>Friend</u></color><pos=35%><u><color=yellow>(Level)</color></u><pos=50%><color=red><u>Health</u></color><pos=78%><color=orange><u>Remain</u></color>\n";

            int lineCount = 0;
            foreach (var cc in __m_allCharmedCharacters)
            {
                Character c = GetCharacterFromGUID(cc.m_charmGUID);
                if (c == null)
                    continue;
                float remainingTime = cc.m_charmExpireTime - __m_charmTimerSeconds;
                DateTime remainTime = DateTime.MinValue.AddSeconds(remainingTime);
                string timeStr = remainTime.ToString("m'm 's's'");

                text += $"<color=yellow>{lineCount + 1}:</color> <pos=5%><color=white>{c.GetHoverName()}<pos=40%><color=yellow>({cc.m_charmLevel})</color><pos=50%><color=red>{(int)(c.GetHealthPercentage() * 100)}%</color><pos=78%></color><color=orange>{timeStr}</color></size>\n";
                lineCount++;
            }
            for (int i = MAX_NUM_THRALLS - lineCount; i > 0; i--)
            {
                text += $"<color=yellow>{lineCount + 1}:</color> <pos=5%><color=#505050> -- Unused -- <pos=40%><color=#505050>--<pos=50%><color=#505050>---<pos=78%><color=#505050>---</color></size>\n";
                lineCount++;
            }

            lines = lineCount;
            return text;
        }

        public static void ShowThrallsWindow(GameObject uiObject)
        {
            if (uiObject == null)
            {
                Debug.LogError("ShowThrallsWindow: uiObject is null!");

                return;
            }

            int lineCount = 0;

            string text = BuildThrallsWindowText(ref lineCount);

            __m_thrallsWindowText.text = text;
            __m_thrallsWindowText.ForceMeshUpdate(true, true);

            __m_thrallsWindowBackground.SetActive(true);
            __m_thrallsWindowObject.SetActive(true);

            __m_thrallsWindowText.ForceMeshUpdate(true, true);
        }

        public static void HideThrallsWindow()
        {
            __m_thrallsWindowBackground.SetActive(false);
            __m_thrallsWindowObject.SetActive(false);
        }

        public static TextMeshProUGUI __m_pacifistMainMenuText = null;

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
                        GameObject textObject = new GameObject("PacifistModLogoText");
                        textObject.transform.SetParent(logoTransform.parent);

                        // Set up the RectTransform for positioning
                        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
                        rectTransform.localScale = Vector3.one;
                        rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                        rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                        rectTransform.pivot = new Vector2(1.0f, 1.0f);
                        rectTransform.anchoredPosition = new Vector2(0,300);
                        rectTransform.sizeDelta = new Vector2(0, 0);
                        rectTransform.offsetMax = new Vector2(0, 0);
                        rectTransform.offsetMin = new Vector2(0, 0);

                        // Add a TextMeshProUGUI component
                        __m_pacifistMainMenuText = AddTextMeshProComponent(textObject);
                        __m_pacifistMainMenuText.font = __m_globalFontObject;
                        __m_pacifistMainMenuText.fontMaterial = __m_globalFontObject.material;
                        __m_pacifistMainMenuText.fontStyle = FontStyles.Bold;
                        __m_pacifistMainMenuText.raycastTarget = false;

                        __m_pacifistMainMenuText.text = "<color=#F387C5><size=64>Pacifist</size></color>" +
                            "<size=20>\nVersion " + PluginVersion + "</size>"+
                            "<size=32>\n🕊️ <color=yellow>You may not directly cause harm.</color> 🕊️\n</size>" +
                            "<size=28>→ <color=#FFFF0050>Use arrows to charm enemies.</color> ←" +
                            "\n→ <color=#FFFF0050>Your Thralls fight for you.</color> ←" +
                            "\n→ <color=#FFFF0050>Buff them with upgraded arrows.</color> ←" +
                            "\n→ <color=#FFFF0050>Level your thralls with Adrenaline.</color> ←" +
                            "\n→ <color=#FFFF0050>Defeat the forsaken without lifting a sword.</color> ←</size>";
                        __m_pacifistMainMenuText.alignment = TextAlignmentOptions.Center;
                        // Enable outline
                        //                            __m_trophyHuntMainMenuText.fontMaterial.EnableKeyword("OUTLINE_ON");
                        __m_pacifistMainMenuText.lineSpacingAdjustment = -5;
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

    }
}