using UnityEngine;
using System.Collections;

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

    private void OnEnable()
    {
        TryCacheHero();
        // Keep rest orientation consistent on enable
        SetLocalEulerX(foldAngleX);
        heroInsideCount = 0;
        if (isVisible) StartIdleSwayIfAllowed();

        if (followHeroSorting && isVisible) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

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

    private void OnBecameVisible()
    {
        isVisible = true;
        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
        StartIdleSwayIfAllowed();
    }

    private void OnBecameInvisible()
    {
        isVisible = false;
        StopIdleSway();
    }

    private void Update()
    {
        if (!isVisible) return;
        if (!followHeroSorting) return;

        YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    private static void TryCacheHero()
    {
        if (hero == null) hero = Object.FindObjectOfType<OverworldHero>();
        if (hero != null && (heroSR == null || heroSR.gameObject != hero.gameObject))
            heroSR = hero.GetComponent<SpriteRenderer>();
    }

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

    private bool IsHeroCollider(Component other)
    {
        if (other == null) return false;
        var h = other.GetComponentInParent<OverworldHero>();
        return h != null;
    }

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

    private void StartIdleSwayIfAllowed()
    {
        if (!enableIdleSway) return;
        if (!isVisible) return;
        if (idleSwayRoutineRef != null) return;
        idleSwayRoutineRef = StartCoroutine(IdleSwayRoutine());
    }

    private void StopIdleSway()
    {
        if (idleSwayRoutineRef != null)
        {
            StopCoroutine(idleSwayRoutineRef);
            idleSwayRoutineRef = null;
        }
    }

    private void SetLocalEulerX(float x)
    {
        Vector3 e = transform.localEulerAngles;
        e.x = x;
        transform.localEulerAngles = e;
    }
}
