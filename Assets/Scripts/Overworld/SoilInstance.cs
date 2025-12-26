using UnityEngine;

/// <summary>
/// Helper to vary grass seed & optional wind per instance.
/// Keeps the soil/grass sprite locked to the ground plane so it does not drift with camera movement.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SoilInstance : MonoBehaviour
{
    [Header("Auto Seed")]
    [Tooltip("If true, assigns a deterministic seed from the object's position for variety.")]
    public bool autoSeed = true;

    [Tooltip("Extra seed offset for manual variation.")]
    public float seedOffset = 0f;

    [Header("Wind Jitter")]
    [Tooltip("Apply slight time scale variation for this instance.")]
    public bool windJitter = false;

    [Range(0.8f, 1.2f)] public float timeScale = 1f;

    [Header("Ground Lock")]
    [Tooltip("Force this object to stay on the ground plane (XY), preventing tilt that causes parallax drift.")]
    public bool lockToGroundPlane = true;

    [Tooltip("Z value of the ground plane to lock to (usually 0). Only used when Lock To Ground Plane is enabled.")]
    public float groundZ = 0f;

    MaterialPropertyBlock _props;
    SpriteRenderer _sr;

    static readonly int ID_Seed = Shader.PropertyToID("_Seed");

    void OnEnable()
    {
        if (_props == null) _props = new MaterialPropertyBlock();
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        ApplyGroundLock();
        Apply();
    }

    void Update()
    {
        if (windJitter)
        {
            timeScale = 1f + Mathf.Sin((float)Time.realtimeSinceStartup * 0.17f + transform.GetInstanceID() * 0.013f) * 0.05f;
        }

        ApplyGroundLock();
        Apply();
    }

    void ApplyGroundLock()
    {
        if (!lockToGroundPlane) return;

        // Ensure rotation does not tilt off the XY ground plane
        Vector3 e = transform.eulerAngles;
        if (e.x != 0f || e.z != 0f)
        {
            e.x = 0f; e.z = 0f; // keep any local yaw if authoring used it
            transform.eulerAngles = e;
        }

        // Keep on the configured ground Z
        if (!Mathf.Approximately(transform.position.z, groundZ))
        {
            Vector3 p = transform.position; p.z = groundZ; transform.position = p;
        }
    }

    void Apply()
    {
        if (_sr == null) return;

        _sr.GetPropertyBlock(_props);

        if (autoSeed)
        {
            float s = Mathf.Sin(transform.position.x * 12.9898f + transform.position.y * 78.233f) * 43758.5453f;
            float seed = Mathf.Abs(s % 10000f) + seedOffset;
            _props.SetFloat(ID_Seed, seed);
        }

        _sr.SetPropertyBlock(_props);
    }
}
