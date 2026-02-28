using UnityEngine;

/// <summary>
/// CLOUDSHADOWINSTANCE - Cloud shadow on the ground.
/// 
/// PURPOSE:
/// Drifts a shadow sprite across the terrain to simulate
/// clouds passing overhead. Respawns off-screen.
/// 
/// FEATURES:
/// - Synced with CloudInstance movement
/// - Randomized speed, scale, flip, and opacity
/// - Respawns at random Y within terrain bounds
/// 
/// RELATED FILES:
/// - CloudInstance.cs: Cloud sprite counterpart
/// - OverworldManager.cs: Overworld scene
/// </summary>
public class CloudShadowInstance : MonoBehaviour
{
    public enum CloudDirection { LeftToRight, RightToLeft }

    [Header("Scene References")]
    [Tooltip("Terrain SpriteRenderer that defines map bounds.")]
    public SpriteRenderer terrain;
    [Tooltip("Camera for on-screen region. Defaults to Camera.main.")]
    public Camera worldCamera;

    [Header("Movement")]
    [Tooltip("Drift direction across the map.")]
    public CloudDirection direction = CloudDirection.RightToLeft;
    [Tooltip("Cloud speed range (world units/second).")]
    public Vector2 speedRange = new Vector2(0.1f, 0.4f);
    [Tooltip("Randomize speed on each respawn.")]
    public bool randomizeSpeedEachRespawn = true;

    [Header("Appearance")]
    [Tooltip("Uniform scale range.")]
    public Vector2 scaleRange = new Vector2(1.0f, 1.25f);
    [Tooltip("Randomize scale on each respawn.")]
    public bool randomizeScale = false;
    [Tooltip("Randomize Flip X on spawn/respawn.")]
    public bool randomizeFlipX = false;
    [Tooltip("Randomize Flip Y on spawn/respawn.")]
    public bool randomizeFlipY = false;
    [Tooltip("Alpha range in 8-bit (0-255).")]
    public Vector2 alpha8bitRange = new Vector2(16f, 32f);

    [Header("Buffers")]
    [Tooltip("Extra units beyond terrain for exit trigger.")]
    public float mapEdgeBuffer = 1.0f;
    [Tooltip("Extra units outside camera for offscreen teleport.")]
    public float cameraFrustumBuffer = 2.0f;
    [Tooltip("Y padding inside terrain bounds.")]
    public float yPadding = 0.5f;

    [Header("Performance")]
    [Tooltip("Continue moving while offscreen.")]
    public bool UpdateWhileOffscreen = true;

    private float _speed;
    private Transform _t;
    private SpriteRenderer _sprite;
    // Visibility gate to save CPU when offscreen
    private bool _isVisible;

    private void Awake()
    {
        _t = transform;
        _sprite = GetComponent<SpriteRenderer>();
        if (worldCamera == null) worldCamera = Camera.main;
        if (terrain == null)
        {
            var map = GameObject.Find("Map");
            if (map != null)
            {
                var tr = map.transform.Find("Terrain");
                if (tr != null) terrain = tr.GetComponent<SpriteRenderer>();
            }
            if (terrain == null)
            {
                // As a fallback, pick the largest SpriteRenderer in the scene
                var srs = FindObjectsOfType<SpriteRenderer>();
                float bestArea = -1f; SpriteRenderer best = null;
                foreach (var sr in srs)
                {
                    var b = sr.bounds;
                    float area = Mathf.Abs((b.size.x) * (b.size.y));
                    if (area > bestArea) { bestArea = area; best = sr; }
                }
                terrain = best;
            }
        }
    }

    private void OnEnable()
    {
        NormalizeRanges();
        _speed = Random.Range(speedRange.x, speedRange.y);
        float s = Random.Range(scaleRange.x, scaleRange.y);
        _t.localScale = new Vector3(s, s, _t.localScale.z);

        // Initialize visibility gate from renderer state
        _isVisible = _sprite != null && _sprite.isVisible;

        // If starting inside the view, move it to the starting side off-screen so it drifts in
        if (terrain != null)
        {
            float planeZ = _t.position.z;
            float camXMin, camXMax; GetCameraXRangeOnPlane(worldCamera, planeZ, out camXMin, out camXMax);
            var b = terrain.bounds;
            float leftSpawn = Mathf.Min(b.min.x - mapEdgeBuffer, camXMin - cameraFrustumBuffer);
            float rightSpawn = Mathf.Max(b.max.x + mapEdgeBuffer, camXMax + cameraFrustumBuffer);

            bool insideX = _t.position.x > camXMin - cameraFrustumBuffer && _t.position.x < camXMax + cameraFrustumBuffer;
            if (insideX)
            {
                float yMin = b.min.y + yPadding, yMax = b.max.y - yPadding;
                float spawnY = (yMin <= yMax) ? Random.Range(yMin, yMax) : b.center.y;
                float spawnX = (direction == CloudDirection.RightToLeft) ? rightSpawn : leftSpawn;
                _t.position = new Vector3(spawnX, spawnY, planeZ);
            }
        }

        // Randomize flip at start if enabled
        MaybeRandomizeFlip();
        // Randomize alpha at start
        ApplyRandomAlpha();
    }

    private void Update()
    {
        // Early out when not visible by any camera unless we want to update offscreen
        if (!UpdateWhileOffscreen)
        {
            if (!_isVisible && (_sprite == null || !_sprite.isVisible)) return;
        }

        if (terrain == null) return;

        // Move steadily along X based on direction
        float dir = (direction == CloudDirection.LeftToRight) ? 1f : -1f;
        _t.position += new Vector3(dir * _speed * Time.deltaTime, 0f, 0f);

        // Trigger wrap only when fully outside both terrain side and camera frustum (with buffer)
        var b = terrain.bounds;
        float planeZ = _t.position.z;
        float camXMin, camXMax; GetCameraXRangeOnPlane(worldCamera, planeZ, out camXMin, out camXMax);
        float leftExit = b.min.x - mapEdgeBuffer;
        float rightExit = b.max.x + mapEdgeBuffer;
        float offscreenLeft = camXMin - cameraFrustumBuffer;
        float offscreenRight = camXMax + cameraFrustumBuffer;

        if (direction == CloudDirection.RightToLeft)
        {
            if (_t.position.x < Mathf.Min(leftExit, offscreenLeft))
            {
                RespawnToRight();
            }
        }
        else // LeftToRight
        {
            if (_t.position.x > Mathf.Max(rightExit, offscreenRight))
            {
                RespawnToLeft();
            }
        }
    }

    private void OnBecameVisible()
    {
        _isVisible = true;
    }

    private void OnBecameInvisible()
    {
        _isVisible = false;
    }

    private void RespawnToRight()
    {
        if (terrain == null) return;
        var b = terrain.bounds;
        float planeZ = _t.position.z;

        // Compute a spawn X outside both map and camera on the right
        float camXMin, camXMax; GetCameraXRangeOnPlane(worldCamera, planeZ, out camXMin, out camXMax);
        float rightSpawn = Mathf.Max(b.max.x + mapEdgeBuffer, camXMax + cameraFrustumBuffer);

        // Randomize Y within map bounds (with padding)
        float yMin = b.min.y + yPadding;
        float yMax = b.max.y - yPadding;
        float spawnY = (yMin <= yMax) ? Random.Range(yMin, yMax) : b.center.y;

        _t.position = new Vector3(rightSpawn, spawnY, planeZ);

        if (randomizeSpeedEachRespawn)
            _speed = Random.Range(speedRange.x, speedRange.y);
        if (randomizeScale)
        {
            float s = Random.Range(scaleRange.x, scaleRange.y);
            _t.localScale = new Vector3(s, s, _t.localScale.z);
        }

        // Randomize flip on teleport if enabled
        MaybeRandomizeFlip();
        // Always randomize alpha on teleport
        ApplyRandomAlpha();
    }

    private void RespawnToLeft()
    {
        if (terrain == null) return;
        var b = terrain.bounds;
        float planeZ = _t.position.z;

        // Compute a spawn X outside both map and camera on the left
        float camXMin, camXMax; GetCameraXRangeOnPlane(worldCamera, planeZ, out camXMin, out camXMax);
        float leftSpawn = Mathf.Min(b.min.x - mapEdgeBuffer, camXMin - cameraFrustumBuffer);

        // Randomize Y within map bounds (with padding)
        float yMin = b.min.y + yPadding;
        float yMax = b.max.y - yPadding;
        float spawnY = (yMin <= yMax) ? Random.Range(yMin, yMax) : b.center.y;

        _t.position = new Vector3(leftSpawn, spawnY, planeZ);

        if (randomizeSpeedEachRespawn)
            _speed = Random.Range(speedRange.x, speedRange.y);
        if (randomizeScale)
        {
            float s = Random.Range(scaleRange.x, scaleRange.y);
            _t.localScale = new Vector3(s, s, _t.localScale.z);
        }

        // Randomize flip on teleport if enabled
        MaybeRandomizeFlip();
        // Always randomize alpha on teleport
        ApplyRandomAlpha();
    }

    // Allow OverworldManager to prewarm clouds so they start distributed across the map
    public void PrewarmDistribute(int index, int total, Bounds mapBounds, Camera cam)
    {
        if (cam != null) worldCamera = cam;
        NormalizeRanges();
        if (randomizeSpeedEachRespawn || _speed <= 0f)
            _speed = Random.Range(speedRange.x, speedRange.y);
        if (randomizeScale)
        {
            float s = Random.Range(scaleRange.x, scaleRange.y);
            _t.localScale = new Vector3(s, s, _t.localScale.z);
        }

        float t = (total > 1) ? (index / Mathf.Max(1f, (total - 1))) : 0.5f;
        float width = mapBounds.size.x;
        float jitter = (total > 0) ? (width / (total * 3f)) : 0f; // small spacing jitter
        float baseX = Mathf.Lerp(mapBounds.min.x, mapBounds.max.x, t);
        float spawnX = baseX + Random.Range(-jitter, jitter);

        float yMin = mapBounds.min.y + yPadding;
        float yMax = mapBounds.max.y - yPadding;
        float spawnY = (yMin <= yMax) ? Random.Range(yMin, yMax) : mapBounds.center.y;

        _t.position = new Vector3(spawnX, spawnY, _t.position.z);
    }

    private static void GetCameraXRangeOnPlane(Camera cam, float planeZ, out float xMin, out float xMax)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            xMin = xMax = 0f; return;
        }
        Vector3 w00 = Mode7CameraController.ScreenToWorldOnZPlane(cam, new Vector2(0, 0), planeZ);
        Vector3 w10 = Mode7CameraController.ScreenToWorldOnZPlane(cam, new Vector2(Screen.width, 0), planeZ);
        Vector3 w01 = Mode7CameraController.ScreenToWorldOnZPlane(cam, new Vector2(0, Screen.height), planeZ);
        Vector3 w11 = Mode7CameraController.ScreenToWorldOnZPlane(cam, new Vector2(Screen.width, Screen.height), planeZ);
        xMin = Mathf.Min(w00.x, w10.x, w01.x, w11.x);
        xMax = Mathf.Max(w00.x, w10.x, w01.x, w11.x);
    }

    private void NormalizeRanges()
    {
        if (speedRange.x > speedRange.y) { float t = speedRange.x; speedRange.x = speedRange.y; speedRange.y = t; }
        if (scaleRange.x > scaleRange.y) { float t = scaleRange.x; scaleRange.x = scaleRange.y; scaleRange.y = t; }
        speedRange.x = Mathf.Max(0f, speedRange.x);
        speedRange.y = Mathf.Max(speedRange.x, speedRange.y);
        scaleRange.x = Mathf.Max(0.01f, scaleRange.x);
        scaleRange.y = Mathf.Max(scaleRange.x, scaleRange.y);
        // Alpha range (8-bit 0..255)
        if (alpha8bitRange.x > alpha8bitRange.y) { float t2 = alpha8bitRange.x; alpha8bitRange.x = alpha8bitRange.y; alpha8bitRange.y = t2; }
        alpha8bitRange.x = Mathf.Clamp(alpha8bitRange.x, 0f, 255f);
        alpha8bitRange.y = Mathf.Clamp(alpha8bitRange.y, alpha8bitRange.x, 255f);
    }

    private void MaybeRandomizeFlip()
    {
        if (_sprite == null) return;
        if (randomizeFlipX) _sprite.flipX = Random.value > 0.5f;
        if (randomizeFlipY) _sprite.flipY = Random.value > 0.5f;
    }

    private void ApplyRandomAlpha()
    {
        if (_sprite == null) return;
        float a8 = Random.Range(alpha8bitRange.x, alpha8bitRange.y);
        float a = Mathf.Clamp01(a8 / 255f);
        var c = _sprite.color;
        c.a = a;
        _sprite.color = c;
    }
}
