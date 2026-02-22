using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using Spout.Interop;
using System.Runtime.InteropServices;
using System;
using static DroneCamSpout;
using OpenGL;
using System.Threading;
using System.Runtime.Remoting.Messaging;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class DroneCamSpout : BaseUnityPlugin
{
    public const string PluginGUID = "com.oathorse.DroneCam";
    public const string PluginName = "Spout Drone Cam";
    public const string PluginVersion = "0.1.0";
    private readonly Harmony harmony = new Harmony(PluginGUID);

    public static GameObject m_droneObject = null;
    public static Camera m_droneCamera = null;
    public static Spout m_spout = null;
    public static RenderTexture renderTexture = null;

    private readonly Vector2Int[] _resolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440)
    };

    private int _resIndex = 1;

    void Awake()
    {
        harmony.PatchAll();
    }
    void LateUpdate()
    {
        if (!senderCreated || spoutRT == null)
            return;

        if (!spoutRT.IsCreated())
            return;

        IntPtr dxPtr = spoutRT.GetNativeTexturePtr();

        if (dxPtr == IntPtr.Zero)
            return;

        // THIS is the correct DX11 call
        spout.SendTextureDX11(
            dxPtr,
            (uint)spoutRT.width,
            (uint)spoutRT.height,
            false   // no vertical flip
        );
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupGui))]
    public class FejdStartup_Patch
    {
        static void Postfix(FejdStartup __instance)
        {
            Debug.LogWarning("[Startup] Creating Drones");

            //            m_droneObject = new GameObject("ValheimSpoutDrone");
            //            m_droneObject.AddComponent<ValheimSpoutDrone>();

            // Create two example drones
            //            m_droneCamSpout._drones.Add(m_droneCamSpout.CreateDrone("Drone A", new Vector3(0, 18, -25)));
            //            m_droneCamSpout._drones.Add(m_droneCamSpout.CreateDrone("Drone B", new Vector3(25, 25, 0)));

            // 1. Create the RenderTexture (Resolution: 512x512)
            m_renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            // 2. Create the Secondary Camera
            m_droneObject = new GameObject("SecondaryCamera");
            m_droneCamera = m_droneObject.AddComponent<Camera>();
            m_droneCamera.targetTexture = renderTexture; // Direct output to texture

            // Position it (e.g., 10 units above the player)
            //           camGo.transform.position = Player.m_localPlayer.transform.position + Vector3.up * 10f;
            //           camGo.transform.LookAt(Player.m_localPlayer.transform);
            //camGo.transform.position = new Vector3(0.0f, 0.0f, 50.0f) + Vector3.up * 10f;
            //camGo.transform.LookAt(new Vector3(0.0f, 0.0f, 0.0f));
            m_droneObject.transform.position = Camera.main.transform.position;
            m_droneObject.transform.forward = Camera.main.transform.forward;
            m_droneCamera.fieldOfView = 60.0f;

            // 3. Create UI to display the texture
            GameObject uiPanel = new GameObject("MiniViewUI");
            Canvas canvas = uiPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiPanel.AddComponent<CanvasScaler>();
            uiPanel.AddComponent<GraphicRaycaster>();

            GameObject rawImageGo = new GameObject("RawImage");
            rawImageGo.transform.SetParent(uiPanel.transform);
            RawImage img = rawImageGo.AddComponent<RawImage>();
            img.texture = renderTexture; // Assign the captured view to the UI

            // Position UI in top-right corner
            RectTransform rt = rawImageGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(256, 256);
            rt.anchoredPosition = new Vector2(-20, -20);

            Debug.LogError("[Spout] Initialized Camera");

            InitSpout();
        }
    }

    static void InitSpout()
    {
        m_spout = new Spout();

        senderCreated = m_spout.CreateSender(
            SenderName,
            (uint)spoutRT.width,
            (uint)spoutRT.height
        );

        if (!senderCreated)
        {
            Debug.LogError("Failed to create Spout sender.");
            return;
        }

        Debug.LogInfo("Spout sender created.");

    }

    void OnDestroy()
    {
        Debug.LogWarning("[] Destroyed");
    }
}
