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
/// GRASSINSTANCE - Grass prop in overworld.
/// 
/// PURPOSE:
/// Decorative grass sprite with idle sway animation,
/// Y-based sorting, and interaction with hero movement.
/// 
/// FEATURES:
/// - Idle sway animation (wind motion)
/// - Y-sorting relative to hero
/// - Trigger-based hero interaction
/// 
/// RELATED FILES:
/// - TreeInstance.cs: Similar vegetation
/// - BushInstance.cs: Similar vegetation
/// - OverworldManager.cs: Overworld scene
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GrassInstance : MonoBehaviour
{
    [Header("Sorting")]
    [Tooltip("Match hero's sorting layer and sort by Y position.")]
    public bool followHeroSorting = true;

    [Header("Idle Sway")]
    public bool enableIdleSway = true;
    [Range(-80f, -5f)] public float foldAngleX = -60f;
    [Range(0f, 45f)] public float swayAmplitude = 6f;
    [Range(0.2f, 10f)] public float swayPeriod = 2.5f;
    public bool randomizeSwayPhase = true;

    private SpriteRenderer spriteRenderer;
    private Collider2D trigger;

    private static OverworldHero hero;
    private static SpriteRenderer heroSR;

    private Coroutine flapRoutineRef;
    private Coroutine idleSwayRoutineRef;
    private int heroInsideCount;
    private float swayPhase;
    private bool isVisible;

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trigger = GetComponent<Collider2D>();

        if (trigger != null && !trigger.isTrigger)
            trigger.isTrigger = true;

        SetLocalEulerX(foldAngleX);
        swayPhase = randomizeSwayPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        transform.position.SetZ(0f);

        isVisible = spriteRenderer != null && spriteRenderer.isVisible;

        if (followHeroSorting && isVisible) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Called when the component becomes enabled and active.</summary>
    private void OnEnable()
    {
        TryCacheHero();
        // Keep rest orientation consistent on enable
        SetLocalEulerX(foldAngleX);
        heroInsideCount = 0;
        if (isVisible) StartIdleSwayIfAllowed();

        if (followHeroSorting && isVisible) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Called when the component becomes disabled.</summary>
    private void OnDisable()
    {
        if (flapRoutineRef != null)
        {
            StopCoroutine(flapRoutineRef);
            flapRoutineRef = null;
        }
        if (idleSwayRoutineRef != null)
        {
            StopCoroutine(idleSwayRoutineRef);
            idleSwayRoutineRef = null;
        }
        heroInsideCount = 0;
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

        YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Try cache hero.</summary>
    private static void TryCacheHero()
    {
        if (hero == null) hero = Object.FindObjectOfType<OverworldHero>();
        if (hero != null && (heroSR == null || heroSR.gameObject != hero.gameObject))
            heroSR = hero.GetComponent<SpriteRenderer>();
    }

    /// <summary>Called when a 2D trigger collider is entered.</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to the hero entering
        if (!IsHeroCollider(other)) return;

        heroInsideCount++;
        if (heroInsideCount == 1)
        {
            // Stop idle sway while intersecting
            StopIdleSway();
            // Start a snap flatten and hold at 0
            if (flapRoutineRef != null) StopCoroutine(flapRoutineRef);
            flapRoutineRef = StartCoroutine(FlattenToZeroRoutine());
        }
    }

    /// <summary>Called when a 2D trigger collider is exited.</summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsHeroCollider(other)) return;

        heroInsideCount = Mathf.Max(0, heroInsideCount - 1);
        if (heroInsideCount == 0)
        {
            // Only when the hero fully leaves, return to rest then resume idle sway
            if (flapRoutineRef != null) StopCoroutine(flapRoutineRef);
            flapRoutineRef = StartCoroutine(ReturnToRestRoutine());
        }
    }

    /// <summary>Returns whether the is hero collider condition is met.</summary>
    private bool IsHeroCollider(Component other)
    {
        if (other == null) return false;
        var h = other.GetComponentInParent<OverworldHero>();
        return h != null;
    }

    /// <summary>Coroutine that executes the flatten to zero sequence.</summary>
    private IEnumerator FlattenToZeroRoutine()
    {
        float start = transform.localEulerAngles.x;
        float end = 0f;
        float t = 0f;
        float dur = 0.05f;
        while (t < dur && isVisible)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float angle = Mathf.LerpAngle(start, end, u);
            SetLocalEulerX(angle);
            yield return null;
        }
        // Hold flat while inside
        while (heroInsideCount > 0 && isVisible)
        {
            SetLocalEulerX(0f);
            yield return null;
        }
    }

    /// <summary>Coroutine that executes the return to rest sequence.</summary>
    private IEnumerator ReturnToRestRoutine()
    {
        float start = transform.localEulerAngles.x;
        float end = foldAngleX;
        float t = 0f;
        float dur = 0.15f;
        while (t < dur && isVisible)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float angle = Mathf.LerpAngle(start, end, u);
            SetLocalEulerX(angle);
            yield return null;
        }
        StartIdleSwayIfAllowed();
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
