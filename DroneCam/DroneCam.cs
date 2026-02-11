using UnityEngine;
using Klak.Spout;

public enum DroneMode
{
    Follow,
    Spline
}

public class DroneCamera
{
    private readonly string _name;
    private Camera _cam;
    private RenderTexture _rt;
    private SpoutSender _spout;

    private Vector3 _followOffset;
    private Vector3 _defaultOffset;

    private DroneMode _mode = DroneMode.Follow;

    private Vector3[] _spline;
    private float _splineT;

    private float _fov = 60f;

    public DroneCamera(string name, Vector2Int res, Vector3 offset)
    {
        _name = name;
        _followOffset = offset;
        _defaultOffset = offset;
        _resolution = res;
    }

    private Vector2Int _resolution;

    public void Initialize()
    {
        var go = new GameObject(_name);
        Object.DontDestroyOnLoad(go);

        _cam = go.AddComponent<Camera>();
        _cam.CopyFrom(Camera.main);
        _cam.stereoTargetEye = StereoTargetEyeMask.None;
        _cam.depth = -20;
        _cam.fieldOfView = _fov;

        // Strip shake & audio
        Object.Destroy(go.GetComponent<AudioListener>());
//        Object.Destroy(go.GetComponent<CameraShake>());

        _rt = new RenderTexture(_resolution.x, _resolution.y, 24);
        _rt.Create();
        _cam.targetTexture = _rt;

        var spoutGO = new GameObject(_name + " Spout");
        Object.DontDestroyOnLoad(spoutGO);

        _spout = spoutGO.AddComponent<SpoutSender>();
        _spout.spoutName = _name;
        _spout.sourceTexture = _rt;

        Debug.LogWarning("[DroneCam] Initialized " + _name);
    }
    public void ViewCam()
    {
        var canvasGO = new GameObject("SpoutCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var rawImageGO = new GameObject("SpoutPreviewUI");
        rawImageGO.transform.SetParent(canvasGO.transform);

        var raw = rawImageGO.AddComponent<UnityEngine.UI.RawImage>();
        raw.texture = _cam.targetTexture;
        raw.rectTransform.sizeDelta = new Vector2(512, 512);
    }

    public void Update()
    {
        var player = Player.m_localPlayer;
        if (!player)
            return;

        Vector3 pos;
        Vector3 look;

        if (_mode == DroneMode.Follow)
        {
            pos = player.transform.TransformPoint(_followOffset);
            look = player.transform.position + Vector3.up * 1.5f;
        }
        else
        {
            pos = EvaluateSpline(player.transform.position);
            look = player.transform.position;
        }

        _cam.transform.position =
            Vector3.Lerp(_cam.transform.position, pos, Time.deltaTime * 4f);

        _cam.transform.rotation =
            Quaternion.Slerp(
                _cam.transform.rotation,
                Quaternion.LookRotation(look - _cam.transform.position),
                Time.deltaTime * 4f
            );

        _cam.fieldOfView = _fov;

        //        EnvMan.instance?.ApplyEnv(_cam);

        ViewCam();
    }

    Vector3 EvaluateSpline(Vector3 playerPos)
    {
        if (_spline == null || _spline.Length < 2)
            return playerPos + Vector3.up * 20;

        _splineT += Time.deltaTime * 0.1f;
        if (_splineT > 1f)
            _splineT -= 1f;

        int count = _spline.Length;
        float t = _splineT * count;
        int i = Mathf.FloorToInt(t) % count;
        int j = (i + 1) % count;

        return Vector3.Lerp(
            playerPos + _spline[i],
            playerPos + _spline[j],
            t - Mathf.Floor(t)
        );
    }

    // -------- Controls --------

    public void ToggleMode()
    {
        _mode = _mode == DroneMode.Follow
            ? DroneMode.Spline
            : DroneMode.Follow;
    }

    public void SetSpline(Vector3[] points)
    {
        _spline = points;
        _splineT = 0;
        _mode = DroneMode.Spline;
    }

    public void SetResolution(Vector2Int res)
    {
        if (_rt != null)
        {
            _rt.Release();
            Object.Destroy(_rt);
        }

        _resolution = res;
        _rt = new RenderTexture(res.x, res.y, 24);
        _rt.Create();
        _cam.targetTexture = _rt;
        _spout.sourceTexture = _rt;
    }

    public void AdjustFov(float delta)
    {
        _fov = Mathf.Clamp(_fov + delta, 20f, 100f);
    }

    public void ResetOffset()
    {
        _followOffset = _defaultOffset;
    }
}