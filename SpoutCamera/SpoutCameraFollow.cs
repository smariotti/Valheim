using UnityEngine;

namespace ValheimSpoutCamera
{
    /// <summary>
    /// Drives the position/rotation of the custom Spout camera each frame.
    ///
    /// Modes
    /// ─────
    ///  FollowPlayer = true  → camera stays at a configurable offset behind/above
    ///                         the local player, looking at them.
    ///  FollowPlayer = false → camera is parented to the main game camera and
    ///                         mirrors its transform exactly (pure clone view).
    /// </summary>
    public class SpoutCameraFollow : MonoBehaviour
    {
        private Transform _playerTransform;
        private Camera    _mainCamera;

        private void Update()
        {
            if (ValheimSpoutCameraPlugin.FollowPlayer.Value)
                FollowPlayerMode();
            else
                MirrorMainCamera();
        }

        // ── Mode A: follow local player ────────────────────────────────────────
        private void FollowPlayerMode()
        {
            // Resolve player reference lazily
            if (_playerTransform == null)
            {
                if (Player.m_localPlayer == null) return;
                _playerTransform = Player.m_localPlayer.transform;
            }

            float ox = ValheimSpoutCameraPlugin.FollowOffsetX.Value;
            float oy = ValheimSpoutCameraPlugin.FollowOffsetY.Value;
            float oz = ValheimSpoutCameraPlugin.FollowOffsetZ.Value;

            // Offset in the player's local space, then convert to world space
            Vector3 worldOffset = _playerTransform.TransformDirection(new Vector3(ox, oy, oz));
            Vector3 targetPos   = _playerTransform.position + worldOffset;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);

            // Always look at the player's head
            Vector3 lookAt = _playerTransform.position + Vector3.up * 1.6f;
            transform.LookAt(lookAt);
        }

        // ── Mode B: mirror main camera (same view, different stream) ───────────
        private void MirrorMainCamera()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            transform.SetPositionAndRotation(
                _mainCamera.transform.position,
                _mainCamera.transform.rotation);
        }
    }
}
