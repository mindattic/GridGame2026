using Assets.Helper;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using Assets.Scripts.Libraries;



// OverworldHero (world-space only)
// Movement types (mutually exclusive):
// - VirtualJoystick: analog stick movement only
// - ClickToMove: path or straight toward a click destination
// - DirectionalPress: hold near a point to move in that direction
// All three share the same collision and pathfinding helpers below.
[ExecuteAlways]
public partial class OverworldHero : MonoBehaviour
{
    // Bindings (resolved at runtime from hierarchy paths)
    private SpriteRenderer terrainSprite;         // Map SpriteRenderer used for world bounds
    public SpriteRenderer spriteRenderer;        // Hero's SpriteRenderer (for probe radius inference)
    //private MapTerrain collisionProvider;     // Central collision provider on Terrain
    private Camera worldCamera;               // Camera for screen->world and visibility tests

    // Movement tuning (set in code)
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;           // Units per second
    [SerializeField] private float snapThreshold = 0.05f;      // Stop distance to consider goal reached
    [SerializeField] private bool requireVisibleToMove = true; // Only move when visible in camera viewport
    [SerializeField] private bool ignoreClicksWhenOffscreen = false;
    [SerializeField] private bool allowVirtualJoystick = true; // Enable joystick/analog movement
    [SerializeField] private bool idleWhileOffscreen = true;   // Idle when offscreen and movement gated

    // Collision toggle
    [Header("Collision")]
    [SerializeField] private bool enableCollision = false;     // When false, hero moves freely without casts

    // Leader/follower
    [Header("Leader/Follow")]
    [Tooltip("If true, this hero is controlled by input. If false, it follows its assigned Leader.")]
    public bool IsLeader = true;
    [SerializeField, HideInInspector] private Transform leader;
    [SerializeField] private float followSpeed = 2.3f;
    [SerializeField] private float followDistance = 0.75f;
    [SerializeField] private float arriveBuffer = 0.05f;
    [SerializeField] private float catchupMultiplier = 2.0f;
    [SerializeField] private float teleportIfBeyond = 25f;

    [Header("Party Collision")]
    [Tooltip("Ignore collisions between all OverworldHero instances (party members).")]
    [SerializeField] private bool ignorePartyCollisions = true;

    // Sampling
    private float speedSampleAheadFactor = 0.7f; // Future-proof: speed zones, currently constant 1x


    // FollowCursor speed ramp: distance at which input magnitude reaches 1
    private float followSpeedRampDistance = 6.0f;

    // Input mode
 
    private float directionalClickMagnitude = 1f; // 0..1 strength fed into analog

   

    // Events
    public event Action<Vector2> OnHeroMoved;  // Invoked with world position after movement

    // Runtime state
    private bool isMoving;                 // True while following a MoveToPoint target
    private Vector2 targetPosition;        // Destination for MoveToPoint mode (world)

    // Analog input (-1..1). Set by OverworldManager each frame.
    private Vector2 analogInput;

    // Directional click override (-1..1). Latched while pressed.
    private bool directionalActive;
    private Vector2 directionalOverride;

    // Pathfinding (A*)
    private bool usePathfinding = true;
    private int navCellSize = 1;              // In world units
    private float navObstacleBuffer = 0.05f;   // Extra clearance from walls
    private int navMaxExpanded = 8000;        // Solver cap
    private float waypointArrive = 0.1f;      // Waypoint arrive distance

    private List<Vector2> _path; // world waypoints
    private int _pathIndex;

    // Collision center and radius
    // Always sample collisions at the Animator/Sprite pivot (transform.position) plus an optional feet offset.
    private Vector2 feetOffset = Vector2.zero; // local-space offset from pivot to feet (e.g., Vector2.down * 0.05f)
    [SerializeField, Min(0f)] private float collisionRadiusWorld = 0.1f;      // world units radius when fixed (adjust in inspector)

    // New: look-ahead coverage tunables
    [Header("Collision Look-Ahead")]
    [Tooltip("Block movement if forward probe coverage >= this fraction (0..1). 0.5 = 50%.")]
    [SerializeField, Range(0f, 1f)] private float forwardCoverageBlockThreshold = 0.5f;
    [Tooltip("Samples taken on the forward semicircle when computing coverage.")]
    [SerializeField, Min(1)] private int forwardCoverageSamples = 16;

    // Destination marker prefab to spawn on click
    private GameObject destinationMarkerPrefab;

    // 2D physics-based cast-and-slide (optional)
    [Header("Physics Collision (optional)")]
    [SerializeField] private float skin = 0.01f;
    [SerializeField] private int maxSlideIterations = 3;
    [Tooltip("Max distance per cast step. Displacements larger than this are subdivided to prevent tunneling through thin walls.")]
    [SerializeField] private float maxCastStepDistance = 0.25f;
    private Rigidbody2D rb;                      // Optional: if present, use shape cast to plan slides
    private ContactFilter2D contactFilter;       // Configured from object layer
    private RaycastHit2D[] hitBuffer;            // Reused hits buffer

    // Party collision cache
    private Collider2D[] selfColliders;

    private void Awake()
    {
        // Auto-binding core components using exact hierarchy paths
        worldCamera = Camera.main;

        // Map terrain (SpriteRenderer + MapTerrain provider)
        var terrainGo = GameObject.Find(GameObjectHelper.Overworld.Map.Terrain);
        if (terrainGo != null)
        {
            terrainSprite = terrainGo.GetComponent<SpriteRenderer>();
   
        }

        // Hero sprite and animator
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        // Optional Rigidbody2D for physics-based casting
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            hitBuffer = new RaycastHit2D[16];
            contactFilter.useTriggers = false;
            contactFilter.useLayerMask = true;
            // Include layers that collide with the hero layer, plus terrain layer explicitly (strong walls)
            int mask = Physics2D.GetLayerCollisionMask(gameObject.layer);
            if (terrainSprite != null)
                mask |= (1 << terrainSprite.gameObject.layer);
            contactFilter.SetLayerMask(mask);

            // Movement is driven manually via casts; lock rotation and smooth visuals
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (rb.bodyType == RigidbodyType2D.Dynamic)
                rb.bodyType = RigidbodyType2D.Kinematic;

            // Enforce a minimal skin to reduce clipping through thin edges
            skin = Mathf.Max(skin, 0.02f);
            maxCastStepDistance = Mathf.Max(0.05f, maxCastStepDistance);
        }

        // Initialize animator with default idle facing
        ApplyAnimatorParameters(lastLook, 0f);

        // Cache destination marker prefab
        if (PrefabLibrary.Prefabs.TryGetValue("DestinationMarkerPrefab", out var prefab))
            destinationMarkerPrefab = prefab;

        CacheSelfColliders();
    }

    private void OnEnable()
    {
        CacheSelfColliders();
        ApplyPartyIgnoreCollisions();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        CacheSelfColliders();
        ApplyPartyIgnoreCollisions();
    }

    private void Update()
    {
        if (IsLeader)
        {
            TickFollowCursor();
        }
        else
        {
            TickFollowLeader();
        }

        // Always keep Y-sort current for actors
        var sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        if (sr != null)
            PartySortHelper.ApplyActorYSort(sr, PartySortHelper.GlobalScale);
    }

 
    // ---------------- Visibility and clamping (world space) ----------------

    private bool IsVisible()
    {
        if (!requireVisibleToMove) return true;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
    }

    private Vector2 ClampToMap(Vector2 p)
    {
        // World-space clamp against sprite bounds
        Bounds b = terrainSprite.bounds;
        float cx = Mathf.Clamp(p.x, b.min.x, b.max.x);
        float cy = Mathf.Clamp(p.y, b.min.y, b.max.y);
        return new Vector2(cx, cy);
    }






    // World bindings
    public void BindWorld(SpriteRenderer map, Camera cam)
    {
        terrainSprite = map;
        worldCamera = cam;
        // Update collision mask to ensure terrain layer is included
        if (rb != null)
        {
            int mask = Physics2D.GetLayerCollisionMask(gameObject.layer);
            if (terrainSprite != null)
                mask |= (1 << terrainSprite.gameObject.layer);
            contactFilter.SetLayerMask(mask);
        }
    }



    // Speed sampling hook (placeholder for zones)
    private float GetSpeedMultiplier(Vector2 world)
    {
        return 1f; // constant speed (slow zones can be added later)
    }


    // ---------------- helpers ----------------

    private Vector2 GetPosition()
    {
        if (rb != null)
            return rb.position;
        return new Vector2(transform.position.x, transform.position.y);
    }

    private void SetPosition(Vector2 v)
    {
        // Keep Z from transform but drive both Transform and Rigidbody2D when available
        if (rb != null)
        {
            rb.position = v; // immediate update of physics body
        }
        transform.position = new Vector3(v.x, v.y, transform.position.z);
        Physics2D.SyncTransforms();
    }


    // Inspector toggles via code (optional helpers)
    public void SetMoveSpeed(int unitsPerSecond) => moveSpeed = Mathf.Max(0f, unitsPerSecond);
    public void SetSnapThreshold(int value) => snapThreshold = Mathf.Max(0f, value);
    public void SetPathfinding(bool enabled) => usePathfinding = enabled;

    // Exposed setters for tuning friction and clearance
    public void SetNavClearance(float value) => navObstacleBuffer = Mathf.Max(0f, value);
  
    public void SetFollowSpeedRampDistance(float dist) => followSpeedRampDistance = Mathf.Max(0.01f, dist);

    // New: control collision sampling relative to animator pivot
    public void SetFeetOffsetLocal(Vector2 offset) => feetOffset = offset;

    // --- Leader/follower API ---
    public void SetLeader(Transform t)
    {
        leader = t;
        if (leader != null) IsLeader = false;
        ApplyPartyIgnoreCollisions();
    }
    public void SetLeader(OverworldHero h) => SetLeader(h != null ? h.transform : null);
    public Transform GetLeader() => leader;
    public void SetAsLeader(bool value)
    {
        IsLeader = value;
        if (value) leader = null;
    }

    private void CacheSelfColliders()
    {
        selfColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void ApplyPartyIgnoreCollisions()
    {
        if (!Application.isPlaying) return;
        if (!ignorePartyCollisions) return;
        if (selfColliders == null || selfColliders.Length == 0) CacheSelfColliders();

        var all = FindObjectsOfType<OverworldHero>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var other = all[i];
            if (other == null || other == this) continue;
            var otherCols = other.GetComponentsInChildren<Collider2D>(true);
            for (int a = 0; a < selfColliders.Length; a++)
            {
                var ca = selfColliders[a]; if (ca == null) continue;
                for (int b = 0; b < otherCols.Length; b++)
                {
                    var cb = otherCols[b]; if (cb == null) continue;
                    Physics2D.IgnoreCollision(ca, cb, true);
                }
            }
        }
    }
}
