using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using System.Runtime.InteropServices;
using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Security;
using UnityEngine.Experimental.Rendering;
using Spout.Interop;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class DroneCamSpout : BaseUnityPlugin
{
    public const string PluginGUID = "com.oathorse.DroneCam";
    public const string PluginName = "Spout Drone Cam";
    public const string PluginVersion = "0.1.0";
    private readonly Harmony harmony = new Harmony(PluginGUID);
    public const string m_senderName = "ValheimDroneCam";

    public static GameObject m_droneObject = null;
    public static Camera m_droneCamera = null;
    public static Spout.Interop.Spout m_spout = null;
    public static RenderTexture m_renderTexture = null;
    public static SpoutSender m_sender = null;

    public static class SpoutDXNative
    {
        private const string dllName = "SpoutDX.dll";

        // Initializes the DirectX 11 device for Spout
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool OpenDirectX11();

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetSenderName([MarshalAs(UnmanagedType.LPStr)] string name);

        // Closes the DirectX 11 device
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseDirectX11();

        // Sends a native ID3D11Texture2D pointer (Unity's GetNativeTexturePtr)
        // Note: 'name' is the sender name, 'pTexture' is the native pointer
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SendTexture([MarshalAs(UnmanagedType.LPStr)] string name, IntPtr pTexture);

        // Optional: Controls frame rate to match the receiver
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void HoldFps(uint fps);
    }

    void Awake()
    {
        harmony.PatchAll();
    }

    void LateUpdate()
    {
        if (m_droneObject != null)
        {
            float offset = Mathf.Sin(Time.time) * Time.deltaTime;
            m_droneObject.transform.position = m_droneObject.transform.position + new Vector3(0.0f, 0.0f, offset);

            System.IntPtr texPtr = m_droneCamera.targetTexture.GetNativeTexturePtr();

            if (m_renderTexture == null)
                return;

            if (!m_renderTexture.IsCreated())
                return;

            IntPtr dxPtr = m_renderTexture.GetNativeTexturePtr();

            if (dxPtr == IntPtr.Zero)
                return;

//            SpoutDXNative.SendTexture(m_senderName, dxPtr);
            // THIS is the correct DX11 call
//            m_spout.SendTexture((uint)dxPtr, (uint)RenderTextureFormat.BGRA32, (uint)m_renderTexture.width, (uint)m_renderTexture.height, false, 0);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupGui))]
    public class FejdStartup_Patch
    {
        static void Postfix(FejdStartup __instance)
        {
            Debug.LogWarning("[Startup] Creating Drones");

            // 1. Create the RenderTexture (Resolution: 512x512)
            RenderTexture renderTexture = new RenderTexture(512, 512, 16, GraphicsFormat.B8G8R8A8_UNorm);
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
        //Debug.LogWarning("Creating sender.");
        ////        m_spout = new Spout.Interop.Spout();

        //bool senderCreated = SpoutDXNative.OpenDirectX11();
        //if (!senderCreated)
        //{
        //    Debug.LogError("Failed to create Spout sender.");
        //    return;
        //}
        //Debug.LogWarning("Setting sender name.");
        //SpoutDXNative.SetSenderName(m_senderName);

        //Debug.LogWarning("Spout sender created.");

    }

    void OnDestroy()
    {
        if (m_renderTexture != null)
        {
            m_renderTexture.Release();
        }
        Debug.LogWarning("[] Destroyed");
    }
}
