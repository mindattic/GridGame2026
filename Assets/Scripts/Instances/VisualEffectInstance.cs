using Assets.Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using g = Assets.Helpers.GameHelper;
using Assets.Helpers;

/// <summary>
/// VISUALEFFECTINSTANCE - Runtime VFX behavior.
/// 
/// PURPOSE:
/// Controls a spawned visual effect including positioning,
/// scaling, sorting, and lifetime management.
/// 
/// NORMALIZATION:
/// - Scales VFX to match tile size
/// - Applies top-down rotation if needed
/// - Sets sorting layer for visibility
/// 
/// LIFETIME RULES:
/// - Duration > 0: Auto-despawn after Duration
/// - IsLoop: Keep alive until manually despawned
/// - Else: Wait for natural completion with safety timeout
/// 
/// RELATED FILES:
/// - VisualEffectManager.cs: Spawns VFX
/// - VisualEffectAsset.cs: VFX definition
/// - VisualEffectLibrary.cs: VFX registry
/// </summary>
public class VisualEffectInstance : MonoBehaviour
{
    #region Configuration

    [Header("Normalization")]
    [Tooltip("Authoring world units that equal one tile at scale 1.")]
    private float authoringUnitSize = 1.0f;

    [Tooltip("Extra global multiplier applied after tile scaling.")]
    private float extraScaleMultiplier = 1.0f;

    [Tooltip("Rotate X by this many degrees for top-down.")]
    private float topDownRotateX = 90.0f;

    [Tooltip("If true, applies the top-down rotation automatically.")]
    private bool applyTopDownRotation = false;

    [Tooltip("Sorting layer name to apply.")]
    private string sortingLayerName = SortingHelper.Layer.VFX;

    [Tooltip("Sorting order for renderers.")]
    private int sortingOrderOnTop = SortingHelper.Order.Max;

    [Tooltip("Unity layer name to assign recursively.")]
    private string unityLayerName = "Default";

    [Header("Defaults")]
    [Tooltip("Default tile size for Spawn without explicit tileSize.")]
    private float defaultTileSize = 1.0f;

    #endregion

    // Cached component references created at spawn time.
    private VisualEffect[] vfxComponents;
    private ParticleSystem[] particleSystems;
    private Renderer[] renderers;

    // -------------------------------------------------------------------------
    // Transform conveniences
    // -------------------------------------------------------------------------

    public Transform Parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }

    public Vector3 Position
    {
        get => gameObject.transform.position;
        set => gameObject.transform.position = value;
    }

    public Quaternion Rotation
    {
        get => gameObject.transform.rotation;
        set => gameObject.transform.rotation = value;
    }

    public Vector3 Scale
    {
        get => gameObject.transform.localScale;
        set => gameObject.transform.localScale = value;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fire and forget spawn at a world position using defaultTileSize.
    /// </summary>
    public void Spawn(VisualEffectAsset vfx, Vector3 position, IEnumerator routine = null)
    {
        StartCoroutine(SpawnRoutine(vfx, position, defaultTileSize, routine));
    }

    /// <summary>
    /// Fire and forget spawn at a world position using an explicit tileSize.
    /// </summary>
    public void Spawn(VisualEffectAsset vfx, Vector3 position, float tileSize, IEnumerator routine = null)
    {
        StartCoroutine(SpawnRoutine(vfx, position, tileSize, routine));
    }

    /// <summary>
    /// Yield until the configured Apex moment of the VFX. This is useful
    /// when gameplay needs to sync with a visual hit or burst.
    /// </summary>
    public IEnumerator WaitUntilTrigger(VisualEffectAsset vfx)
    {
        float wait = Mathf.Max(0f, (vfx?.Apex ?? 1f));
        if (wait <= 0f)
            yield break;

        float t = 0f;
        while (t < wait)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Yieldable spawn that instantiates the asset prefab as a child,
    /// normalizes transform, manages sorting, plays the effect, and handles lifetime.
    /// </summary>
    public IEnumerator SpawnRoutine(VisualEffectAsset vfx, Vector3 position, IEnumerator routine = null)
    {
        yield return SpawnRoutine(vfx, position, defaultTileSize, routine);
    }

    /// <summary>
    /// Yieldable spawn with explicit tileSize.
    /// Applies Delay, then plays. Handles Duration and completion rules as documented on the class.
    /// </summary>
    public IEnumerator SpawnRoutine(VisualEffectAsset vfx, Vector3 position, float tileSize, IEnumerator routine = null)
    {
        if (vfx == null || vfx.Prefab == null)
            yield break;

        // Important: keep the name assigned by the manager so Despawn works.
        string instanceName = name;

        // Clean any previous children then instantiate the effect under this wrapper.
        ClearChildren();
        var child = Instantiate(vfx.Prefab, transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        // Reassign Unity layer if requested so the gameplay camera can see it.
        //ApplyUnityLayerRecursively(unityLayerName);

        // Gather components for control and sorting.
        CacheComponents();

        // Normalize PS scaling to be affected by transform scale.
        NormalizeParticleSystems();

        // Normalize transform: rotation, position, and scale relative to tile size.
        ApplyTransform(vfx, position, tileSize);

        // Sorting push to ensure the effect appears on top.
        ForceSortingOnTop();

        // Play the effect across both systems defensively.
        PlayEffect();

        // Optional external routine during playback.
        if (routine != null)
            yield return routine;

        // Determine lifetime behavior.
        float duration = vfx.Duration;
        bool isLooping = vfx.IsLooping;

        if (duration > 0f)
        {
            // Explicit duration wins. Auto-despawn when done.
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // Return control to manager so it unregisters cleanly.
            g.VisualEffectManager.Despawn(instanceName);
            yield break;
        }

        if (isLooping)
        {
            // Looping with no duration must be explicitly despawned by reference.
            yield break;
        }

        // Non-looping with no duration: wait for natural completion or safety timeout.
        const float safetyTimeout = 8.0f;
        float elapsed = 0f;

        while (HasAliveParticles() && elapsed < safetyTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        g.VisualEffectManager.Despawn(instanceName);
    }

    /// <summary>
    /// Requests despawn from the manager.
    /// </summary>
    private void Despawn(string name)
    {
        g.VisualEffectManager.Despawn(name);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Removes all child GameObjects under this wrapper.
    /// </summary>
    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (Application.isPlaying)
                Destroy(c.gameObject);
            else
                DestroyImmediate(c.gameObject);
        }
    }

    /// <summary>
    /// Caches components from the instantiated child hierarchy.
    /// </summary>
    private void CacheComponents()
    {
        vfxComponents = GetComponentsInChildren<VisualEffect>(true);
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>
    /// Ensure ParticleSystems scale with transform to honor tile-based scaling.
    /// </summary>
    private void NormalizeParticleSystems()
    {
        if (particleSystems == null) return;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            if (ps == null) continue;
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
    }

    /// <summary>
    /// Applies rotation, position, and uniform tile scaling with asset offsets.
    /// </summary>
    private void ApplyTransform(VisualEffectAsset vfx, Vector3 worldPosition, float tileSize)
    {
        // Rotation combines top-down and asset rotation.
        Quaternion topDown = applyTopDownRotation ? Quaternion.Euler(topDownRotateX, 0f, 0f) : Quaternion.identity;
        Quaternion assetRot = Quaternion.Euler(vfx.AngularRotation);
        Rotation = topDown * assetRot;

        // Position uses world position plus any asset offset in world units.
        Position = worldPosition + vfx.RelativeOffset;

        // Scale is uniform by tile size then multiplied by per-asset relative scale.
        float baseScale = ComputeTileScale(tileSize);
        Vector3 relative = vfx.RelativeScale == Vector3.zero ? Vector3.one : vfx.RelativeScale;
        Scale = Vector3.one * baseScale;
        Scale = new Vector3(Scale.x * relative.x, Scale.y * relative.y, Scale.z * relative.z);
    }

    /// <summary>
    /// Returns a uniform scale factor so one authored unit maps to one tile at the given tileSize.
    /// </summary>
    private float ComputeTileScale(float tileSize)
    {
        float s = authoringUnitSize > 0.0001f ? tileSize / authoringUnitSize : 1.0f;
        s *= Mathf.Max(0.0001f, extraScaleMultiplier);
        return s;
    }

    /// <summary>
    /// Plays all found VisualEffect and ParticleSystem components defensively.
    /// </summary>
    private void PlayEffect()
    {
        if (vfxComponents != null)
        {
            for (int i = 0; i < vfxComponents.Length; i++)
            {
                var v = vfxComponents[i];
                if (v == null) continue;
                v.Reinit();
                v.Play();
            }
        }

        if (particleSystems != null)
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (ps == null) continue;
                ps.Play(true);
            }
        }
    }

    /// <summary>
    /// Returns true while any VisualEffect or ParticleSystem still has live particles.
    /// </summary>
    private bool HasAliveParticles()
    {
        bool anyAlive = false;

        if (vfxComponents != null)
        {
            for (int i = 0; i < vfxComponents.Length; i++)
            {
                var v = vfxComponents[i];
                if (v == null) continue;

#if UNITY_2019_3_OR_NEWER
                if (v.aliveParticleCount > 0)
                {
                    anyAlive = true;
                    break;
                }
#else
                // Fallback if aliveParticleCount is not available in the target version.
                anyAlive = true;
                break;
#endif
            }
        }

        if (!anyAlive && particleSystems != null)
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (ps == null) continue;
                if (ps.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }
        }

        return anyAlive;
    }

    /// <summary>
    /// Attempts to push all renderers in this hierarchy to the front visually.
    /// Applies sorting layer and order where supported and raises render queues for meshes.
    /// </summary>
    private void ForceSortingOnTop()
    {
        if (renderers == null || renderers.Length == 0) return;

        int layerId = 0;
        bool hasLayer = false;

        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            layerId = SortingLayer.NameToID(sortingLayerName);
            hasLayer = layerId != 0 || sortingLayerName == "Default";
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            // Sorting layer and order for SpriteRenderer and ParticleSystemRenderer.
            if (hasLayer)
                r.sortingLayerID = layerId;

            r.sortingOrder = sortingOrderOnTop;

            // Raise render queue for materials that do not support sorting order.
            // This instantiates per-renderer materials, which is intended here.
            var mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;

                if (mat.renderQueue < 5000)
                    mat.renderQueue = 5000;
            }
        }
    }

    /// <summary>
    /// Assign a Unity layer to this object and all children so the current camera can render it.
    /// </summary>
    private void ApplyUnityLayerRecursively(string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex < 0) return; // unknown layer; skip

        void Recurse(Transform t)
        {
            t.gameObject.layer = layerIndex;
            for (int i = 0; i < t.childCount; i++)
                Recurse(t.GetChild(i));
        }

        Recurse(transform);
    }
}
