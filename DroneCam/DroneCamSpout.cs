using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using Klak.Spout;
using UnityEngine.Windows;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class DroneCamSpout : BaseUnityPlugin
{
    private readonly List<DroneCamera> _drones = new List<DroneCamera>();
    private int _activeDrone = 0;
    private bool _enabled = true;
    public static DroneCamSpout m_droneCamSpout;

    public static GameObject m_droneObject;

    public const string PluginGUID = "com.oathorse.DroneCam";
    public const string PluginName = "Spout Drone Cam";
    public const string PluginVersion = "0.1.0";
    private readonly Harmony harmony = new Harmony(PluginGUID);

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
        float offset = Mathf.Sin(Time.time) * Time.deltaTime;
        m_droneObject.transform.position = m_droneObject.transform.position + new Vector3(0.0f, 0.0f, offset);
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
            RenderTexture renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            // 2. Create the Secondary Camera
            m_droneObject = new GameObject("SecondaryCamera");
            Camera secondaryCamera = m_droneObject.AddComponent<Camera>();
            secondaryCamera.targetTexture = renderTexture; // Direct output to texture

            // Position it (e.g., 10 units above the player)
            //           camGo.transform.position = Player.m_localPlayer.transform.position + Vector3.up * 10f;
            //           camGo.transform.LookAt(Player.m_localPlayer.transform);
            //camGo.transform.position = new Vector3(0.0f, 0.0f, 50.0f) + Vector3.up * 10f;
            //camGo.transform.LookAt(new Vector3(0.0f, 0.0f, 0.0f));
            m_droneObject.transform.position = Camera.main.transform.position;
            m_droneObject.transform.forward = Camera.main.transform.forward;
            secondaryCamera.fieldOfView = 60.0f;

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
        }
    }
}
  
public class ValheimSpoutDrone : MonoBehaviour
{
    public string spoutName = "ValheimDrone";
    public int width = 1280;
    public int height = 720;
    public float orbitSpeed = 15f;
    public Vector3 offset = new Vector3(0, 6, -10);

    Camera droneCamera;
    RenderTexture renderTexture;
    SpoutSender spoutSender;

    Transform target;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Debug.LogWarning("[SpoutDrone] Awake");
    }

    void Start()
    {
        CreateRenderTexture();
        CreateCamera();
        CreateSpoutSender();

        Debug.LogWarning("[SpoutDrone] Started");

    }

    void CreateRenderTexture()
    {
        renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        renderTexture.name = "SpoutDroneRT";
        renderTexture.Create();

        Debug.LogWarning("[SpoutDrone] Render Texture Created");

    }

    void CreateCamera()
    {
        var camGO = new GameObject("SpoutDroneCamera");
        camGO.transform.SetParent(transform);

        droneCamera = camGO.AddComponent<Camera>();
        droneCamera.CopyFrom(Camera.main);

        droneCamera.targetTexture = renderTexture;
        droneCamera.enabled = true;

        Debug.LogWarning("[SpoutDrone] Camera Created");

    }

    void CreateSpoutSender()
    {
        var senderGO = new GameObject("SpoutSender");
        senderGO.transform.SetParent(transform);

        spoutSender = senderGO.AddComponent<SpoutSender>();
        spoutSender.spoutName = spoutName;
        spoutSender.sourceTexture = renderTexture;
        spoutSender.enabled = true;

        Debug.LogWarning("[SpoutDrone] Spout Sender Created");

    }

    void LateUpdate()
    {
        if (!target) return;

        float angle = Time.time * orbitSpeed;
        Vector3 rotatedOffset = Quaternion.Euler(0, angle, 0) * offset;

        droneCamera.transform.position = target.position + rotatedOffset;
        droneCamera.transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        Debug.LogWarning("[SpoutDrone] Destroyed");

    }
}