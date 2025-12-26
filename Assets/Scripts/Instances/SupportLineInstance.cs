using Assets.Helper;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// Draws a support line between two ActorInstances,
/// handles overlay in/out, and initializes renderer settings.
/// </summary>
public class SupportLineInstance : MonoBehaviour
{
    /// <summary>
    /// Quick reference to the parent transform.
    /// Setting this moves the GameObject under the board in the hierarchy.
    /// </summary>
    public Transform parent
    {
        get => transform.parent;
        set => transform.SetParent(value, true);
    }

    /// <summary>
    /// Current alpha transparency of the line.
    /// </summary>
    public float alpha = 0f;

    /// <summary>
    /// Duration for overlay in and overlay out.
    /// </summary>

    private float fadeDuration = 0.05f;

    /// <summary>
    /// Minimum alpha value (fully transparent).
    /// </summary>
    private float minAlpha = Opacity.Transparent;

    /// <summary>
    /// Maximum alpha value (semi transparent).
    /// </summary>
    private float maxAlpha = Opacity.Transparent;

    /// <summary>
    /// First actor endpoint for the line.
    /// </summary>
    public ActorInstance supporter;

    /// <summary>
    /// Second actor endpoint for the line.
    /// </summary>
    public ActorInstance attacker;

    /// <summary>
    /// Base color of the line (green) with adjustable alpha.
    /// </summary>
    private Color color = ColorHelper.RGBA(48, 161, 49, 0);

    /// <summary>
    /// LineRenderer used to draw the line.
    /// </summary>
    private LineRenderer lineRenderer;

    /// <summary>
    /// If true, DespawnRoutine is skipped.
    /// </summary>
    public bool isStatic = false;

    /// <summary>
    /// SortingGroup accessor.
    /// </summary>
    public SortingGroup sortingGroup
    {
        get => GetComponent<SortingGroup>();
    }

    // Legacy cone particles (disabled by default)
    private ParticleSystem particles;
    private const float SparkSpeed = 6f; // units/sec along the line

    // Shared spark system with SynergyStrand
    private SynergySpark sparkSystem = new SynergySpark();

    // Cache last endpoints to avoid redundant work
    private Vector3 _lastP0, _lastP1;

    // Freeze updating endpoints (e.g., during fade-out after lane change)
    private bool _freezeEndpoints;

    // Track initial lanes (world Y at spawn) to detect lane changes
    private float _supporterLaneY;
    private float _attackerLaneY;


    /// <summary>
    /// Cache component and configure initial renderer properties.
    /// Ensure particle systems do not auto-play at origin.
    /// </summary>
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // Set line width relative to tile size
        lineRenderer.startWidth = g.TileSize * 0.25f;
        lineRenderer.endWidth = g.TileSize * 0.25f;

        // Ensure alignment faces camera
        lineRenderer.alignment = LineAlignment.View;

        lineRenderer.positionCount = 2;

        // Legacy particle system (if present) is force-disabled; SynergySpark replaces it
        particles = GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            var main = particles.main; main.playOnAwake = false;
            var emission = particles.emission; emission.enabled = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear(true);
        }

        // Initialize shared spark system
        sparkSystem.Init(transform);

        // Set initial tint similar to support line color
        var sparkTint = new Color(0.25f, 1f, 0.25f, 1f);
        sparkSystem.SetTint(sparkTint);
    }


    /// <summary>
    /// Configure sorting layer and order.
    /// </summary>
    public void SetSorting(string sortingLayer, int sortingOrder = 0)
    {
        sortingGroup.sortingLayerID = SortingLayer.NameToID(sortingLayer);
        sortingGroup.sortingOrder = sortingOrder;

        // Apply sorting to all particle renderers in self and children
        var renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (var pr in renderers)
        {
            pr.sortingLayerID = sortingGroup.sortingLayerID;
            pr.sortingOrder = sortingOrder;
        }

        // Sort SynergySpark above the line slightly
        sparkSystem.SetSorting(sortingLayer, sortingOrder + 1);
    }

    /// <summary>
    /// Initializes the support line between two actors.
    /// Positions are set to the centers of the actors' tiles, not their current transforms.
    /// </summary>
    public void Spawn(ActorInstance supporter, ActorInstance attacker)
    {
        this.supporter = supporter;
        this.attacker = attacker;

        // Parent under the board for organization
        parent = g.Board.transform;

        // Unique name for debugging
        name = $"SupportLine_{Guid.NewGuid():N}";

        // Record initial lanes (world Y) to detect lane changes later
        _supporterLaneY = (supporter?.currentTile != null ? supporter.currentTile.position.y : supporter.Position.y);
        _attackerLaneY = (attacker?.currentTile != null ? attacker.currentTile.position.y : attacker.Position.y);
        _freezeEndpoints = false;

        // Ensure legacy particles are cleared and disabled
        if (particles != null)
        {
            var emission = particles.emission; emission.enabled = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear(true);
        }

        UpdateEndpoints(force: true);

        g.SortingManager.OnSupportLineSpawn(this);

        // No fade-in: show at max alpha immediately
        alpha = maxAlpha;
        UpdateLineAlpha(alpha);

        // Prewarm the spark system a bit so it looks settled on first frame
        sparkSystem.Prewarm(1.0f, 8, false, t => transform.position, _ => 0f); // samplers replaced next update
    }

    /// <summary>
    /// Keep endpoints aligned to tile centers each frame.
    /// </summary>
    private void LateUpdate()
    {
        if (supporter == null || attacker == null)
            return;

        UpdateEndpoints();
    }

    /// <summary>
    /// Recompute endpoints to follow the actors' tile centers. Also updates the shared spark system along the straight path.
    /// Guard against invalid positions (e.g., Vector3.zero) to prevent particles from spawning at origin.
    /// </summary>
    public void UpdateEndpoints(bool force = false)
    {
        if (_freezeEndpoints) return;
        if (lineRenderer == null) return;

        var p0 = supporter?.currentTile != null ? supporter.currentTile.position : supporter.Position;
        var p1 = attacker?.currentTile != null ? attacker.currentTile.position : attacker.Position;

        // If either actor changed lanes (Y differs from spawn), freeze and destroy immediately without moving endpoints
        const float laneEpsilon = 0.01f;
        float curSupporterLaneY = supporter?.currentTile != null ? supporter.currentTile.position.y : supporter.Position.y;
        float curAttackerLaneY = attacker?.currentTile != null ? attacker.currentTile.position.y : attacker.Position.y;
        if (Mathf.Abs(curSupporterLaneY - _supporterLaneY) > laneEpsilon || Mathf.Abs(curAttackerLaneY - _attackerLaneY) > laneEpsilon)
        {
            _freezeEndpoints = true;
            // Disable visuals instantly
            if (lineRenderer != null) lineRenderer.enabled = false;
            StopAndClearParticles();
            sparkSystem.Clear();
            // Immediate destroy (no fade)
            Despawn();
            return;
        }

        // Skip if unchanged and not forced
        if (!force && (p0 == _lastP0) && (p1 == _lastP1))
        {
            // Update spark system even when points unchanged (so it animates)
            DriveSparksStraight(_lastP0, _lastP1);
            return;
        }

        _lastP0 = p0; _lastP1 = p1;

        // Block when any endpoint is at Vector3.zero to avoid spawning particles at origin
        bool invalidEndpoints = (p0 == Vector3.zero) || (p1 == Vector3.zero);
        if (invalidEndpoints)
        {
            if (lineRenderer.enabled) lineRenderer.enabled = false;
            StopAndClearParticles();
            sparkSystem.Clear();
            return; // keep previous endpoints until valid
        }
        else if (!lineRenderer.enabled)
        {
            lineRenderer.enabled = true;
        }

        lineRenderer.SetPosition(0, p0);
        lineRenderer.SetPosition(1, p1);

        // Drive Synergy sparks along the straight line
        DriveSparksStraight(p0, p1);
    }

    private void DriveSparksStraight(Vector3 p0, Vector3 p1)
    {
        // Ensure Z=0 for 2D
        p0.z = 0f; p1.z = 0f;
        float dist = (p1 - p0).magnitude;
        if (dist <= 0.001f)
        {
            sparkSystem.Clear();
            return;
        }

        System.Func<float, Vector3> sampler = (t) => Vector3.Lerp(p0, p1, Mathf.Clamp01(t));
        System.Func<float, float> radius = (t) => 0f; // centerline

        // Keep tint consistent with the line
        var sparkTint = new Color(0.25f, 1f, 0.25f, 1f);
        sparkSystem.SetTint(sparkTint);

        // Animate sparks
        sparkSystem.Tick(
            fade: 1f,
            revActive: false,
            samplePos: sampler,
            radiusAtT: radius,
            dt: Time.deltaTime
        );
    }

    /// <summary>
    /// ProcessRoutine overlay out and eventual destruction of the support line.
    /// </summary>
    public void Despawn()
    {
        if (isStatic)
            return;

        // Disable visuals immediately
        if (lineRenderer != null) lineRenderer.enabled = false;
        StopAndClearParticles();
        sparkSystem.Clear();

        // Immediate destroy; no fade-out
        g.SupportLineManager.Destroy(supporter, attacker);
    }

    private void StopAndClearParticles()
    {
        // Stop and clear all particle systems in the hierarchy to prevent lingering particles
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var em = ps.emission; em.enabled = false;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }
    }

    /// <summary>
    /// Applies new alpha to both ends of the LineRenderer color.
    /// </summary>
    private void UpdateLineAlpha(float a)
    {
        color.a = a;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    /// <summary>
    /// Destroys this GameObject when requested.
    /// </summary>
    public void Destroy()
    {
        Destroy(gameObject);
    }
}
