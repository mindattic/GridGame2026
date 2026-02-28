using UnityEngine;

/// <summary>
/// MODE7CAMERACONTROLLER - Mode7-style perspective camera.
/// 
/// PURPOSE:
/// Controls camera position and rotation to create a retro
/// Mode7-like low-angle follow camera for overworld scenes.
/// 
/// VISUAL EFFECT:
/// ```
///    _____________________
///   /                     \   ← Horizon (vanishing point)
///  /      [distant]        \
/// |        terrain          |
/// |      [Hero]              |
/// |__________________________|  ← Ground plane
/// ```
/// 
/// CONFIGURATION:
/// - pitch: Camera angle (-45° typical)
/// - distance: Distance from target
/// - fieldOfView: Perspective FOV
/// - followLerp: Follow smoothness
/// 
/// BOUNDS CLAMPING:
/// Can clamp look-at point to terrain bounds
/// to prevent viewing outside the map.
/// 
/// RELATED FILES:
/// - OverworldHero.cs: Target to follow
/// - OverworldManager.cs: Overworld scene
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class Mode7CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float lookAtYOffset = 0f;

    [Header("Lens")]
    public bool enableMode7 = true;
    public float fieldOfView = 45f;

    [Header("Pose")]
    [Range(-89f, 89f)] public float pitch = -45;
    [Tooltip("Yaw is locked to 0 for comfort.")]
    public float yaw = 0f;
    public float distance = 10f;
    public float heightOffset = 0.5f;

    [Header("Follow")]
    public float followLerp = 10f;

    [Header("Bounds")]
    public SpriteRenderer terrain;
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
