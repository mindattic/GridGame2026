using Scripts.Helpers;
using Scripts.Factories;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Config;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Overworld
{
/// <summary>
/// OVERWORLDHERO - Player-controlled hero in overworld.
/// 
/// PURPOSE:
/// Controls hero movement, collision, and animation in the
/// world map exploration scene.
/// 
/// MOVEMENT MODES (mutually exclusive):
/// - VirtualJoystick: Analog stick movement
/// - ClickToMove: Pathfind to clicked position
/// - DirectionalPress: Hold to move in direction
/// 
/// PARTY SYSTEM:
/// - IsLeader: Controlled by input
/// - Followers: Follow the leader at distance
/// 
/// COLLISION:
/// Uses collision helpers to avoid obstacles.
/// Can be toggled via enableCollision.
/// 
/// PARTIAL CLASS FILES:
/// - OverworldHero.cs: Core movement
/// - OverworldHero.Animation.cs: Animation control
/// - OverworldHero.Collision.cs: Collision detection
/// - OverworldHero.FollowCursor.cs: Click-to-move
/// 
/// RELATED FILES:
/// - OverworldManager.cs: Scene controller
/// - Mode7CameraController.cs: Camera control
/// </summary>
[ExecuteAlways]
public partial class OverworldHero : MonoBehaviour
{
    // Bindings (resolved at runtime from hierarchy paths)
    private SpriteRenderer terrainSprite;         // Map SpriteRenderer used for world bounds
    public SpriteRenderer spriteRenderer;        // Hero's SpriteRenderer (for probe radius inference)
    //private MapTerrain collisionProvider;     // Central collision provider on Terrain
    private Camera worldCamera;               // Camera for screen->world and visibility tests

    // Movement tuning (initialized from OverworldHeroConfig; preserves runtime-mutation setters)
    private float moveSpeed                = OverworldHeroConfig.MoveSpeed;
    private float snapThreshold            = OverworldHeroConfig.SnapThreshold;
    private bool  requireVisibleToMove     = OverworldHeroConfig.RequireVisibleToMove;
    private bool  ignoreClicksWhenOffscreen = OverworldHeroConfig.IgnoreClicksWhenOffscreen;
    private bool  allowVirtualJoystick     = OverworldHeroConfig.AllowVirtualJoystick;
    private bool  idleWhileOffscreen       = OverworldHeroConfig.IdleWhileOffscreen;

    // Collision toggle
    private bool enableCollision = OverworldHeroConfig.EnableCollision;

    // Leader/follower
    public bool IsLeader = true;
    private Transform leader;                  // runtime reference; set via SetLeader()
    private float followSpeed        = OverworldHeroConfig.FollowSpeed;
    private float followDistance     = OverworldHeroConfig.FollowDistance;
    private float arriveBuffer       = OverworldHeroConfig.ArriveBuffer;
    private float catchupMultiplier  = OverworldHeroConfig.CatchupMultiplier;
    private float teleportIfBeyond   = OverworldHeroConfig.TeleportIfBeyond;

    // Party collision
    private bool ignorePartyCollisions = OverworldHeroConfig.IgnorePartyCollisions;

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
    private float collisionRadiusWorld = OverworldHeroConfig.CollisionRadiusWorld;

    // Look-ahead coverage tunables
    private float forwardCoverageBlockThreshold = OverworldHeroConfig.ForwardCoverageBlockThreshold;
    private int   forwardCoverageSamples        = OverworldHeroConfig.ForwardCoverageSamples;

    // Destination marker prefab to spawn on click
    // Note: Now using DestinationMarkerFactory instead of prefab

    // 2D physics-based cast-and-slide (optional). Awake() clamps skin/maxCastStepDistance.
    private float skin                = OverworldHeroConfig.Skin;
    private int   maxSlideIterations  = OverworldHeroConfig.MaxSlideIterations;
    private float maxCastStepDistance = OverworldHeroConfig.MaxCastStepDistance;
    private Rigidbody2D rb;                      // Optional: if present, use shape cast to plan slides
    private ContactFilter2D contactFilter;       // Configured from object layer
    private RaycastHit2D[] hitBuffer;            // Reused hits buffer

    // Party collision cache
    private Collider2D[] selfColliders;

    /// <summary>Initializes component references and state.</summary>
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

        // Note: Destination markers are now created via DestinationMarkerFactory.Create()

        CacheSelfColliders();
    }

    /// <summary>Called when the component becomes enabled and active.</summary>
    private void OnEnable()
    {
        CacheSelfColliders();
        ApplyPartyIgnoreCollisions();
    }

    /// <summary>Editor callback when inspector values change.</summary>
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        CacheSelfColliders();
        ApplyPartyIgnoreCollisions();
    }

    /// <summary>Runs per-frame update logic.</summary>
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

    /// <summary>Returns whether the is visible condition is met.</summary>
    private bool IsVisible()
    {
        if (!requireVisibleToMove) return true;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
    }

    /// <summary>Clamp to map.</summary>
    private Vector2 ClampToMap(Vector2 p)
    {
        // World-space clamp against sprite bounds
        Bounds b = terrainSprite.bounds;
        float cx = Mathf.Clamp(p.x, b.min.x, b.max.x);
        float cy = Mathf.Clamp(p.y, b.min.y, b.max.y);
        return new Vector2(cx, cy);
    }






    // World bindings
    /// <summary>Bind world.</summary>
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
    /// <summary>Gets the speed multiplier.</summary>
    private float GetSpeedMultiplier(Vector2 world)
    {
        return 1f; // constant speed (slow zones can be added later)
    }


    // ---------------- helpers ----------------

    /// <summary>Gets the position.</summary>
    private Vector2 GetPosition()
    {
        if (rb != null)
            return rb.position;
        return new Vector2(transform.position.x, transform.position.y);
    }

    /// <summary>Sets the position.</summary>
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
    /// <summary>Sets the move speed.</summary>
    public void SetMoveSpeed(int unitsPerSecond) => moveSpeed = Mathf.Max(0f, unitsPerSecond);
    /// <summary>Sets the snap threshold.</summary>
    public void SetSnapThreshold(int value) => snapThreshold = Mathf.Max(0f, value);
    /// <summary>Sets the pathfinding.</summary>
    public void SetPathfinding(bool enabled) => usePathfinding = enabled;

    // Exposed setters for tuning friction and clearance
    /// <summary>Sets the nav clearance.</summary>
    public void SetNavClearance(float value) => navObstacleBuffer = Mathf.Max(0f, value);
  
    /// <summary>Sets the follow speed ramp distance.</summary>
    public void SetFollowSpeedRampDistance(float dist) => followSpeedRampDistance = Mathf.Max(0.01f, dist);

    // New: control collision sampling relative to animator pivot
    /// <summary>Sets the feet offset local.</summary>
    public void SetFeetOffsetLocal(Vector2 offset) => feetOffset = offset;

    // --- Leader/follower API ---
    /// <summary>Sets the leader.</summary>
    public void SetLeader(Transform t)
    {
        leader = t;
        if (leader != null) IsLeader = false;
        ApplyPartyIgnoreCollisions();
    }
    /// <summary>Sets the leader.</summary>
    public void SetLeader(OverworldHero h) => SetLeader(h != null ? h.transform : null);
    /// <summary>Gets the leader.</summary>
    public Transform GetLeader() => leader;
    /// <summary>Sets the as leader.</summary>
    public void SetAsLeader(bool value)
    {
        IsLeader = value;
        if (value) leader = null;
    }

    /// <summary>Cache self colliders.</summary>
    private void CacheSelfColliders()
    {
        selfColliders = GetComponentsInChildren<Collider2D>(true);
    }

    /// <summary>Applies the party ignore collisions.</summary>
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

}
