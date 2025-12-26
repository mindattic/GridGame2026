using UnityEngine;

// Attach this to the Main Camera to get a Mode7-like low-angle follow camera.
// Keeps map logic intact; only camera pose is controlled here.
[ExecuteAlways]
[DisallowMultipleComponent]
public class Mode7CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // OverworldHero transform
    public float lookAtYOffset = 0f;         // Optional Y offset for look-at point

    [Header("Lens")]
    public bool enableMode7 = true;          // Toggle on/off
    public float fieldOfView = 45f;          // Perspective FOV in degrees

    [Header("Pose (relative to target)")]
    [Range(-89f, 89f)] public float pitch = -45;   // Positive pitches the ground away (horizon toward top)
    [Tooltip("Yaw is ignored to prevent rotating around Y (comfort)")]
    public float yaw = 0f;                          // Ignored (locked to 0)
    public float distance = 10f;                    // Distance from target along camera forward (spherical)
    public float heightOffset = 0.5f;               // Extra world Y offset applied after spherical placement

    [Header("Follow")]
    public float followLerp = 10f;                  // Higher = snappier follow

    [Header("Bounds (optional)")]
    public SpriteRenderer terrain;                  // If set, we will clamp look-at XY inside map bounds
    public bool clampLookAtToTerrain = true;

    private Camera _cam;

    private void Reset()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        if (_cam != null) { _cam.orthographic = false; _cam.fieldOfView = fieldOfView; }
        AutoFind();
    }

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        AutoFind();
    }

    private void OnEnable()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam != null) _cam.orthographic = false;
    }

    private void AutoFind()
    {
        if (target == null)
        {
            var hero = FindObjectOfType<OverworldHero>();
            if (hero != null) target = hero.transform;
        }
  
    }

    private void LateUpdate()
    {
        if (!enableMode7) return;
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam == null) return;
        _cam.orthographic = false;
        _cam.fieldOfView = Mathf.Clamp(fieldOfView, 1f, 179f);

        if (target == null)
        {
            AutoFind();
            if (target == null) return;
        }

        // Desired look-at point
        Vector3 lookAt = target.position + new Vector3(0f, lookAtYOffset, 0f);

        // Optionally clamp lookAt into terrain bounds (XY only)
        if (clampLookAtToTerrain && terrain != null)
        {
            Bounds b = terrain.bounds;
            float x = Mathf.Clamp(lookAt.x, b.min.x, b.max.x);
            float y = Mathf.Clamp(lookAt.y, b.min.y, b.max.y);
            lookAt = new Vector3(x, y, lookAt.z);
        }

        // Compute rotation: lock yaw to 0 to avoid rotating around Y
        Quaternion rot = Quaternion.Euler(-pitch, 0f, 0f);

        // Offset the camera behind the lookAt along this rotation's backward vector
        Vector3 baseOffset = rot * Vector3.back;
        if (baseOffset.sqrMagnitude < 1e-6f) baseOffset = Vector3.back;
        Vector3 desiredPos = lookAt + baseOffset.normalized * Mathf.Max(0.01f, distance);
        // Optional vertical offset in world Y (does not change yaw)
        desiredPos.y += heightOffset;

        // Smooth follow for position
        float t = Application.isPlaying ? (1f - Mathf.Exp(-Mathf.Max(0f, followLerp) * Time.deltaTime)) : 1f;
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);

        // Apply rotation with locked yaw
        transform.rotation = rot;

        // Keep near clip modest so we don't cut the plane when very low to ground
        _cam.nearClipPlane = Mathf.Max(0.01f, _cam.nearClipPlane);
    }

    // Utility: Convert a screen point to world point on the Z=planeZ (XY plane)
    public static Vector3 ScreenToWorldOnZPlane(Camera cam, Vector2 screenPos, float planeZ)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector3.zero;
        Ray r = cam.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
        float d;
        if (plane.Raycast(r, out d))
        {
            return r.GetPoint(d);
        }
        // Fallback to legacy distance method (rare if parallel)
        float zDist = Mathf.Abs((cam.transform.position.z) - planeZ);
        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
    }
}
