using UnityEngine;
using System.Collections;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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
/// TREEINSTANCE - Tree prop in overworld.
/// 
/// PURPOSE:
/// Decorative tree sprite with idle sway animation
/// and Y-based sorting relative to the hero.
/// 
/// FEATURES:
/// - Idle sway animation (gentle wind motion)
/// - Y-sorting: renders behind hero when below
/// - Configurable sway amplitude, period, phase
/// 
/// SORTING:
/// Uses YSortUtility to sort from sprite bottom
/// (trunk base controls render order).
/// 
/// RELATED FILES:
/// - GrassInstance.cs: Similar vegetation
/// - BushInstance.cs: Similar vegetation
/// - OverworldManager.cs: Overworld scene
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TreeInstance : MonoBehaviour
{
    [Header("Sorting")]
    [Tooltip("Match hero's sorting layer and sort by Y position.")]
    public bool followHeroSorting = true;

    [Header("Idle Sway")]
    [Tooltip("Enable gentle sway animation.")]
    public bool enableIdleSway = true;
    [Tooltip("Rest X-rotation (degrees).")]
    [Range(-80f, -5f)] public float foldAngleX = -45f;
    [Tooltip("Sway amplitude in degrees.")]
    [Range(0f, 45f)] public float swayAmplitude = 10f;
    [Tooltip("Seconds per full sway cycle.")]
    [Range(0.2f, 10f)] public float swayPeriod = 3.5f;
    [Tooltip("Randomize starting sway phase.")]
    public bool randomizeSwayPhase = true;

    private SpriteRenderer spriteRenderer;
    private static OverworldHero hero;
    private static SpriteRenderer heroSR;

    private Coroutine idleSwayRoutineRef;
    private float swayPhase;
    private bool isVisible;

    /// <summary>Initializes component references and state.</summary>
    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetLocalEulerX(foldAngleX);
        swayPhase = randomizeSwayPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        transform.position.SetZ(0f);

        isVisible = spriteRenderer != null && spriteRenderer.isVisible;

        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Called when the component becomes enabled and active.</summary>
    private void OnEnable()
    {
        TryCacheHero();
        SetLocalEulerX(foldAngleX);
        if (isVisible) StartIdleSwayIfAllowed();

        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Called when the component becomes disabled.</summary>
    private void OnDisable()
    {
        StopIdleSway();
        SetLocalEulerX(foldAngleX);
    }

    /// <summary>Called when the renderer becomes visible by any camera.</summary>
    private void OnBecameVisible()
    {
        isVisible = true;
        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
        StartIdleSwayIfAllowed();
    }

    /// <summary>Called when the renderer is no longer visible by any camera.</summary>
    private void OnBecameInvisible()
    {
        isVisible = false;
        StopIdleSway();
    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        if (!isVisible) return;
        if (!followHeroSorting) return;

        // Sort using the visual base (bounds.min.y)
        YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Try cache hero.</summary>
    private static void TryCacheHero()
    {
        if (hero == null) hero = Object.FindObjectOfType<OverworldHero>();
        if (hero != null && (heroSR == null || heroSR.gameObject != hero.gameObject))
            heroSR = hero.GetComponent<SpriteRenderer>();
    }

    // === Idle sway (no trigger required) ===
    /// <summary>Coroutine that executes the idle sway sequence.</summary>
    private IEnumerator IdleSwayRoutine()
    {
        float w = (swayPeriod <= 0f) ? 0f : (Mathf.PI * 2f) / Mathf.Max(0.01f, swayPeriod);
        while (enableIdleSway && isActiveAndEnabled && isVisible)
        {
            float angle = foldAngleX + Mathf.Sin((Time.time * w) + swayPhase) * swayAmplitude;
            SetLocalEulerX(angle);
            yield return null;
        }
        idleSwayRoutineRef = null;
    }

    /// <summary>Start idle sway if allowed.</summary>
    private void StartIdleSwayIfAllowed()
    {
        if (!enableIdleSway) return;
        if (!isVisible) return;
        if (idleSwayRoutineRef != null) return;
        idleSwayRoutineRef = StartCoroutine(IdleSwayRoutine());
    }

    /// <summary>Stop idle sway.</summary>
    private void StopIdleSway()
    {
        if (idleSwayRoutineRef != null)
        {
            StopCoroutine(idleSwayRoutineRef);
            idleSwayRoutineRef = null;
        }
    }

    /// <summary>Sets the local euler x.</summary>
    private void SetLocalEulerX(float x)
    {
        Vector3 e = transform.localEulerAngles;
        e.x = x;
        transform.localEulerAngles = e;
    }
}

}
