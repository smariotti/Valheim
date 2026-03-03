using UnityEngine;
using BepInEx.Logging;
using System;

namespace ValheimSpoutCamera
{
    /// <summary>
    /// Manages a secondary Camera whose framebuffer is sent to OBS via Spout.
    ///
    /// Pipeline:
    ///   Camera → RenderTexture → Spout.Sender (KlakSpout / Spout4Unity)
    ///
    /// Requirements (place DLLs in BepInEx/plugins or alongside this assembly):
    ///   - KlakSpout  (https://github.com/keijiro/KlakSpout)
    ///       OR
    ///   - Spout4Unity (https://github.com/valyard/Spout4Unity)
    ///
    /// This file targets the KlakSpout API.  See the comment block at the bottom
    /// for the Spout4Unity alternative if you prefer that library.
    /// </summary>
    public class SpoutCameraManager : MonoBehaviour
    {
        private static ManualLogSource Log => ValheimSpoutCameraPlugin.Log;

        // ── Unity objects ──────────────────────────────────────────────────────
        private GameObject     _cameraGO;
        private Camera         _camera;
        private RenderTexture  _renderTexture;

        // ── Spout sender ───────────────────────────────────────────────────────
        // KlakSpout exposes a MonoBehaviour called "Klak.Spout.SpoutSender".
        // We reference it via Component/SendMessage to avoid a hard compile-time
        // dependency – swap the strings if you use a different Spout library.
        private Component _spoutSender;

        private bool _isRunning;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            CreateCamera();
            CreateSpoutSender();

            if (_spoutSender == null)
            {
                Log.LogWarning(
                    "Spout sender component not found.  Make sure KlakSpout (or your " +
                    "chosen Spout library) DLLs are present in BepInEx/plugins.");
            }
        }

        private void OnDestroy()
        {
            ShutDown();
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public void ToggleSpout()
        {
            if (_isRunning) ShutDown();
            else            StartUp();
        }

        public void StartUp()
        {
            if (_isRunning) return;

            _camera.enabled = true;

            if (_spoutSender != null)
                _spoutSender.SendMessage("SetEnabled", true, SendMessageOptions.DontRequireReceiver);

            _isRunning = true;
            Log.LogInfo($"Spout sender '{ValheimSpoutCameraPlugin.SpoutSenderName.Value}' started " +
                        $"({ValheimSpoutCameraPlugin.SpoutWidth.Value}×" +
                        $"{ValheimSpoutCameraPlugin.SpoutHeight.Value}).");
        }

        public void ShutDown()
        {
            if (!_isRunning) return;

            _camera.enabled = false;

            if (_spoutSender != null)
                _spoutSender.SendMessage("SetEnabled", false, SendMessageOptions.DontRequireReceiver);

            _isRunning = false;
            Log.LogInfo("Spout sender stopped.");
        }

        // ── Internal setup ─────────────────────────────────────────────────────
        private void CreateCamera()
        {
            int w = ValheimSpoutCameraPlugin.SpoutWidth.Value;
            int h = ValheimSpoutCameraPlugin.SpoutHeight.Value;

            // RenderTexture that will be the Spout source
            _renderTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2,
                filterMode   = FilterMode.Bilinear,
                name         = "SpoutCameraRT"
            };
            _renderTexture.Create();

            // Dedicated camera GameObject (persists across scene loads)
            _cameraGO = new GameObject("SpoutCustomCamera");
            DontDestroyOnLoad(_cameraGO);

            _camera = _cameraGO.AddComponent<Camera>();
            _camera.fieldOfView  = ValheimSpoutCameraPlugin.CameraFOV.Value;
            _camera.targetTexture = _renderTexture;
            _camera.depth        = 99;   // render after main camera
            _camera.enabled      = false; // off until toggled on

            // Copy main camera culling / clear flags so it looks identical
            Camera main = Camera.main;
            if (main != null)
            {
                _camera.cullingMask  = main.cullingMask;
                _camera.clearFlags   = main.clearFlags;
                _camera.backgroundColor = main.backgroundColor;
                _camera.nearClipPlane   = main.nearClipPlane;
                _camera.farClipPlane    = main.farClipPlane;
            }

            // Attach the follow logic
            _cameraGO.AddComponent<SpoutCameraFollow>();

            Log.LogInfo($"Custom camera created with RenderTexture {w}×{h}.");
        }

        private void CreateSpoutSender()
        {
            // ── KlakSpout ──────────────────────────────────────────────────────
            // Type name: "Klak.Spout.SpoutSender"
            // The assembly is usually "jp.keijiro.klak.spout" or "KlakSpout"
            Type senderType = FindType("Klak.Spout.SpoutSender");

            if (senderType == null)
            {
                // ── Spout4Unity fallback ───────────────────────────────────────
                // Type name: "Spout.SpoutSender"
                senderType = FindType("Spout.SpoutSender");
            }

            if (senderType == null)
            {
                Log.LogError("Could not find a Spout sender type. " +
                             "Install KlakSpout or Spout4Unity and place the DLLs in BepInEx/plugins.");
                return;
            }

            _spoutSender = _cameraGO.AddComponent(senderType);

            // KlakSpout API: set the source texture and sender name via reflection
            // so this file does not need a hard reference to the assembly.
            TrySetProperty(_spoutSender, "sourceTexture", _renderTexture);
            TrySetProperty(_spoutSender, "_sourceTexture", _renderTexture); // alt field name
            TrySetProperty(_spoutSender, "senderName",    ValheimSpoutCameraPlugin.SpoutSenderName.Value);
            TrySetProperty(_spoutSender, "_senderName",   ValheimSpoutCameraPlugin.SpoutSenderName.Value);

            Log.LogInfo($"Spout sender component attached ({senderType.FullName}).");
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (t != null) return t;
            }
            return null;
        }

        private static void TrySetProperty(object target, string name, object value)
        {
            if (target == null) return;
            Type t = target.GetType();

            var prop = t.GetProperty(name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
                return;
            }

            var field = t.GetField(name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            field?.SetValue(target, value);
        }
    }
}

/*
 ═══════════════════════════════════════════════════════════════════════════════
  ALTERNATIVE: using Spout4Unity directly (no reflection)
  If you add a project reference to Spout4Unity, replace CreateSpoutSender()
  with the code below.

    using Spout;

    private void CreateSpoutSender()
    {
        var sender = _cameraGO.AddComponent<SpoutSender>();
        sender.sharingName  = ValheimSpoutCameraPlugin.SpoutSenderName.Value;
        sender.captureSource = CaptureSource.RenderTexture;
        sender.renderTexture = _renderTexture;
        _spoutSender = sender;
    }
 ═══════════════════════════════════════════════════════════════════════════════
*/
