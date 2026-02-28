using System.Collections;
using UnityEngine;

/// <summary>
/// BUSHINSTANCE - Bush prop in overworld.
/// 
/// PURPOSE:
/// Decorative bush sprite with rustle animation when hero
/// passes through, plus idle sway and Y-based sorting.
/// 
/// FEATURES:
/// - Rustle effect (squash + shake) on hero collision
/// - Idle sway animation when not colliding
/// - Y-sorting relative to hero
/// 
/// RUSTLE ANIMATION:
/// ```
/// [Hero enters] → Squash down → Shake → Return to rest
/// ```
/// 
/// RELATED FILES:
/// - TreeInstance.cs: Similar vegetation
/// - GrassInstance.cs: Similar vegetation
/// - OverworldManager.cs: Overworld scene
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BushInstance : MonoBehaviour
{
    [Header("Rustle Effect")]
    [Range(0.1f, 1f)] public float squashY = 0.8f;
    [Range(0.01f, 0.5f)] public float squashInTime = 0.06f;
    [Range(0.01f, 0.5f)] public float squashOutTime = 0.12f;

    [Header("Shake")]
    [Range(0.0f, 0.5f)] public float shakeAmplitude = 0.02f;
    [Range(1, 12)] public int shakeCycles = 3;
    [Range(0.0f, 1.0f)] public float crossYProximity = 0.25f;

    [Header("Idle Sway")]
    public bool enableIdleSway = true;
    [Range(-80f, -5f)] public float foldAngleX = -45f;
    [Range(0f, 45f)] public float swayAmplitude = 10f;
    [Range(0.2f, 10f)] public float swayPeriod = 3.5f;
    public bool randomizeSwayPhase = true;

    [Header("Sorting")]
    [Tooltip("Match hero's sorting layer and sort by Y position.")]
    public bool followHeroSorting = true;

    private SpriteRenderer spriteRenderer;

    private static OverworldHero hero;
    private static SpriteRenderer heroSR;

    private bool isVisible;
    private bool? heroWasBelow;
    private Coroutine rustleRoutineRef;

    // Idle sway state
    private Coroutine idleSwayRoutineRef;
    private float swayPhase;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Ensure we start at rest angle
        SetLocalEulerX(foldAngleX);
        swayPhase = randomizeSwayPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        transform.position.SetZ(0f); // ensure on Z=0 plane

        isVisible = spriteRenderer != null && spriteRenderer.isVisible;

        if (followHeroSorting && isVisible) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    private void OnEnable()
    {
        TryCacheHero();
        // Initialize side to avoid false-positive first-frame rustle
        if (hero != null)
            heroWasBelow = hero.transform.position.y < transform.position.y;

        // Keep rest orientation consistent on enable
        SetLocalEulerX(foldAngleX);
        if (isVisible) StartIdleSwayIfAllowed();

        if (followHeroSorting && isVisible) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    private void OnDisable()
    {
        if (rustleRoutineRef != null)
        {
            StopCoroutine(rustleRoutineRef);
            rustleRoutineRef = null;
        }
        StopIdleSway();
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
        // Y-sort for this bush instance using bottom-of-bounds
        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);

        // Use nearest party member by Y to detect pass-through for rustle
        var anchor = PartySortHelper.GetClosestToY(transform.position.y);
        if (anchor == null) return;

        var anchorPos = anchor.transform.position;
        var bushPos = transform.position;

        bool isAnchorBelow = anchorPos.y < bushPos.y;

        // Detect passing through the bush: side flip + horizontal overlap + near pivot Y
        if (heroWasBelow.HasValue && heroWasBelow.Value != isAnchorBelow && IsOverlappingHorizontally(anchorPos) && IsNearPivotY(anchorPos))
        {
            // Trigger scale/shake rustle
            if (rustleRoutineRef != null) StopCoroutine(rustleRoutineRef);
            rustleRoutineRef = StartCoroutine(RustleRoutine());
        }

        heroWasBelow = isAnchorBelow;
    }

    private static void TryCacheHero()
    {
        if (hero == null) hero = Object.FindObjectOfType<OverworldHero>();
        if (hero != null && (heroSR == null || heroSR.gameObject != hero.gameObject))
            heroSR = hero.GetComponent<SpriteRenderer>();
    }

    private bool IsOverlappingHorizontally(Vector3 heroPos)
    {
        // Consider overlap within the bush bounds in X
        var b = spriteRenderer.bounds;
        return heroPos.x >= b.min.x && heroPos.x <= b.max.x;
    }

    private bool IsNearPivotY(Vector3 heroPos)
    {
        // Near the bush pivot Y within a fraction of the bush height
        var b = spriteRenderer.bounds;
        float tolerance = Mathf.Clamp01(crossYProximity) * (b.size.y > 0f ? b.size.y : 0.2f);
        return Mathf.Abs(heroPos.y - transform.position.y) <= Mathf.Max(0.02f, tolerance);
    }

    private IEnumerator RustleRoutine()
    {
        // Cache start transforms
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.localPosition;

        // Targets
        float minY = Mathf.Clamp(squashY, 0.1f, 1f);
        Vector3 squashed = new Vector3(startScale.x * (1f + (1f - minY) * 0.15f), // slight widen on X for volume conservation
                                       startScale.y * minY,
                                       startScale.z);

        float durIn = Mathf.Max(0.01f, squashInTime);
        float durOut = Mathf.Max(0.01f, squashOutTime);
        float totalDur = durIn + durOut;

        // Randomize initial shake phase for variation
        float randomPhase = Random.Range(0f, Mathf.PI * 2f);

        float t = 0f;
        while (t < totalDur && isVisible)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / totalDur);

            // Squash envelope: quick impact then recover
            float squashBlend;
            if (t <= durIn)
            {
                float pin = Mathf.Clamp01(t / durIn);
                float easeOut = 1f - Mathf.Pow(1f - pin, 2f);
                squashBlend = easeOut; // towards squashed
            }
            else
            {
                float pout = Mathf.Clamp01((t - durIn) / durOut);
                float easeIn = pout * pout;
                squashBlend = 1f - easeIn; // back to start
            }
            transform.localScale = Vector3.LerpUnclamped(startScale, squashed, squashBlend);

            // Shake: decaying horizontal sine, centered on startPos
            float amp = shakeAmplitude * (1f - u);
            float cycles = Mathf.Max(1, shakeCycles);
            float xOffset = Mathf.Sin((u * cycles * Mathf.PI * 2f) + randomPhase) * amp;
            transform.localPosition = new Vector3(startPos.x + xOffset, startPos.y, startPos.z);

            yield return null;
        }

        // Restore
        transform.localScale = startScale;
        transform.localPosition = startPos;
        rustleRoutineRef = null;
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
