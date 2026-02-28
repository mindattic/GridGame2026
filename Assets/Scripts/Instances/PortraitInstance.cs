using Assets.Helper;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using c = Assets.Helpers.CanvasHelper;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// PORTRAITINSTANCE - Character portrait slide-in effect.
/// 
/// PURPOSE:
/// Controls character portrait images that slide in during combat
/// sequences like pincer attacks. Supports both UI (Image) and
/// world-space (SpriteRenderer) rendering modes.
/// 
/// VISUAL EFFECT:
/// ```
/// Pincer Attack with Portraits:
/// 
/// ←──[Hero A]    [Enemy]    [Hero B]──→
///     slides in  (target)   slides in
/// ```
/// 
/// DUAL MODE:
/// - UI Mode: Uses Image + RectTransform for canvas space
/// - World Mode: Uses SpriteRenderer for world space
/// Automatically detects which components are present.
/// 
/// ANIMATIONS:
/// - SlideIn: Portrait enters from screen edge
/// - SlideOut: Portrait exits off screen
/// - PopIn/PopOut: 3D rotation effects
/// 
/// LANE LOCKING:
/// fixedX/fixedY can lock portrait to specific screen positions
/// for consistent layout during multi-portrait sequences.
/// 
/// RELATED FILES:
/// - PortraitManager.cs: Orchestrates portrait display
/// - Portrait2DFactory.cs: Creates UI portraits
/// - Portrait3DFactory.cs: Creates world portraits
/// </summary>
public class PortraitInstance : MonoBehaviour
{
    #region Components

    public RectTransform rectTransform { get; private set; }
    public Image image { get; private set; }
    public SpriteRenderer spriteRenderer { get; private set; }

    #endregion

    #region State

    public Direction direction;
    protected AnimationCurve slideCurve;
    public ActorInstance actor;
    protected bool isBeingDestroyed = false;

    // UI-only: lane locking
    public float? fixedX = null;
    public float? fixedY = null;

    /// <summary>Animation speed multiplier.</summary>
    [Range(0.1f, 10f)]
    public float speedMultiplier = 1.75f;

    // 3D/world helpers
    [SerializeField] public float startTime;
    [SerializeField] public Vector2 startPosition;
    private float popInRotY = 0f;
    private Quaternion lastPopInRot = Quaternion.identity;
    private Vector3 popOutFrontRestorePos;

    #endregion

    #region Properties

    public Transform parent
    {
        get => transform.parent;
        set
        {
            if (rectTransform != null)
                rectTransform.SetParent(value, false);
            else
                transform.SetParent(value, true);
        }
    }

    public Vector3 position
    {
        get => transform.position;
        set => transform.position = value;
    }

    public Vector3 scale
    {
        get => (rectTransform != null ? (Vector3)rectTransform.localScale : transform.localScale);
        set
        {
            if (rectTransform != null) rectTransform.localScale = value;
            else transform.localScale = value;
        }
    }

    public Sprite sprite
    {
        get => image != null ? image.sprite : (spriteRenderer != null ? spriteRenderer.sprite : null);
        set
        {
            if (image != null) image.sprite = value;
            if (spriteRenderer != null) spriteRenderer.sprite = value;
        }
    }

    public SortingGroup sortingGroup => GetComponent<SortingGroup>();
    public void SetSorting(string sortingLayer, int sortingOrder = 0)
    {
        var sg = sortingGroup;
        if (sg == null) return;
        sg.sortingLayerID = SortingLayer.NameToID(sortingLayer);
        sg.sortingOrder = sortingOrder;
    }

    protected void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        // Shared slide curve (overshoot then settle)
        slideCurve = new AnimationCurve(
            new Keyframe(0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
            new Keyframe(0.8f, 0.05202637f, 0.0f, 0.0f, 0.33333334f, 0.70263505f),
            new Keyframe(1.2f, -0.05f, 0.0f, 0.0f, 0.33333334f, 0.33322528f),
            new Keyframe(1.993103f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f)
        )
        {
            preWrapMode = WrapMode.ClampForever,
            postWrapMode = WrapMode.ClampForever
        };
    }

    private void OnDestroy() => isBeingDestroyed = true;

    // =====================
    // UI (Canvas) behaviors
    // =====================

    /// <summary>
    /// Slides a UI portrait (Image on RectTransform) from offscreen to offscreen using the slide curve.
    /// Uses fixedX/fixedY when provided to create lanes.
    /// </summary>
    public IEnumerator SlideInRoutine()
    {
        if (image == null || rectTransform == null)
            yield break; // Not a UI portrait

        Rect canvas = c.CanvasRect.rect;
        float halfCanvasW = canvas.width * 0.5f;
        float halfCanvasH = canvas.height * 0.5f;
        float halfPortraitW = rectTransform.rect.width * rectTransform.localScale.x * 0.5f;
        float halfPortraitH = rectTransform.rect.height * rectTransform.localScale.y * 0.5f;
        const float padding = 2f;

        float offscreenRightX = halfCanvasW + halfPortraitW + padding;
        float offscreenLeftX = -offscreenRightX;
        float offscreenTopY = halfCanvasH + halfPortraitH + padding;
        float offscreenBottomY = -offscreenTopY;

        // Random lane offset (unless fixed lane provided)
        float crossSpan = (direction == Direction.East || direction == Direction.West) ? canvas.height : canvas.width;
        float offsetAmount = RNG.Float(0f, crossSpan * Increment.Percent10);
        float offset = RNG.Int(1, 2) == 1 ? offsetAmount : -offsetAmount;
        bool isVertical = direction == Direction.North || direction == Direction.South;

        float laneX = isVertical ? (fixedX ?? offset) : 0f;
        float laneY = !isVertical ? (fixedY ?? offset) : 0f;

        Vector2 destination;
        switch (direction)
        {
            case Direction.East:
                destination = new Vector2(offscreenRightX, laneY);
                break;
            case Direction.West:
                destination = new Vector2(offscreenLeftX, laneY);
                break;
            case Direction.North:
                destination = new Vector2(laneX, offscreenTopY);
                break;
            default: // South
                destination = new Vector2(laneX, offscreenBottomY);
                break;
        }

        float startV = slideCurve.Evaluate(0f);
        Vector2 startPos = (direction == Direction.East || direction == Direction.West)
            ? new Vector2(destination.x * startV, destination.y)
            : new Vector2(destination.x, destination.y * startV);
        rectTransform.anchoredPosition = startPos;

        float startTimeLocal = Time.time;
        float curveLength = slideCurve.keys[slideCurve.length - 1].time;
        float elapsedTime = 0f;

        while (elapsedTime < curveLength)
        {
            elapsedTime = (Time.time - startTimeLocal) * Mathf.Max(0.0001f, speedMultiplier);
            float v = slideCurve.Evaluate(elapsedTime);

            Vector2 pos = (direction == Direction.East || direction == Direction.West)
                ? new Vector2(destination.x * v, destination.y)
                : new Vector2(destination.x, destination.y * v);

            rectTransform.anchoredPosition = pos;
            yield return Wait.None();
        }

        Despawn();
    }

    // ==========================
    // World (SpriteRenderer) FX
    // ==========================

    public IEnumerator SlideIn()
    {
        if (spriteRenderer == null)
            yield break; // Not a world portrait

        spriteRenderer.color = Color.white;
        Vector3 destination = Vector3.zero;

        // Start/end positions (simple offscreen world coords)
        switch (direction)
        {
            case Direction.North:
                position = new Vector3(1, -10, 1);
                destination = new Vector3(1, 10, 1);
                break;
            case Direction.East:
                position = new Vector3(-10, 1, 1);
                destination = new Vector3(10, 1, 1);
                break;
            case Direction.South:
                position = new Vector3(-1, 10, 1);
                destination = new Vector3(-1, -10, 1);
                break;
            case Direction.West:
                position = new Vector3(10, -1, 1);
                destination = new Vector3(-10, -1, 1);
                break;
        }

        float curveLength = slideCurve.keys[slideCurve.length - 1].time;
        float t0 = Time.time - startTime; // allow external startTime to seed offset
        float elapsed = 0f;

        while (elapsed < curveLength)
        {
            elapsed = (Time.time - startTime) * Mathf.Max(0.0001f, speedMultiplier);
            float v = slideCurve.Evaluate(elapsed);

            switch (direction)
            {
                case Direction.North:
                case Direction.South:
                    position = new Vector3(destination.x, destination.y * v, destination.z);
                    break;
                case Direction.East:
                case Direction.West:
                    position = new Vector3(destination.x * v, destination.y, destination.z);
                    break;
            }

            yield return Wait.None();
        }

        Despawn();
    }

    public IEnumerator PopInOut(float fadeDuration = 0.25f, float holdDuration = 0.25f, float rotateDuration = 0.2f)
    {
        if (isBeingDestroyed || spriteRenderer == null)
            yield break;

        Color baseColor = spriteRenderer.color;
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        yield return PopIn(rotateDuration, fadeDuration);

        for (float elapsed = 0; elapsed < holdDuration; elapsed += Time.deltaTime)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            Vector3 frontAnchorPos = actor.Render.front.transform.position;
            AlignPortraitWithFront(frontAnchorPos);
            yield return Wait.None();
        }

        yield return PopOut(rotateDuration, fadeDuration);
    }

    public IEnumerator PopIn(float rotateDuration = 0.2f, float fadeDuration = 0.25f)
    {
        if (isBeingDestroyed || spriteRenderer == null)
            yield break;

        Transform front = actor.Render.front.transform;
        Vector3 originalFrontPos = front.position;
        float yOffset = -g.TileSize * 0.33f; // Lowered by 33%

        float y = RNG.Float(20f, 25f);
        popInRotY = RNG.Float() < 0.5f ? -y : y;
        Quaternion startRot = front.rotation;
        Quaternion targetRot = Quaternion.Euler(75, popInRotY, 0);
        lastPopInRot = targetRot;

        for (float elapsed = 0; elapsed < rotateDuration; elapsed += Time.deltaTime)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            float t = elapsed / rotateDuration;
            front.rotation = Quaternion.Slerp(startRot, targetRot, t);
            Vector3 loweredPos = originalFrontPos + new Vector3(0, yOffset, 0);
            front.position = Vector3.Lerp(originalFrontPos, loweredPos, t);
            AlignPortraitWithFront(front.position);
            yield return Wait.None();
        }
        front.rotation = targetRot;
        front.position = originalFrontPos + new Vector3(0, yOffset, 0);
        AlignPortraitWithFront(front.position);

        Color c = spriteRenderer.color;
        for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(0, 1, t);
            spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
            AlignPortraitWithFront(front.position);
            yield return Wait.None();
        }
        spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);
        AlignPortraitWithFront(front.position);

        popOutFrontRestorePos = originalFrontPos;
    }

    public IEnumerator PopOut(float rotateDuration = 0.2f, float fadeDuration = 0.25f)
    {
        if (isBeingDestroyed || spriteRenderer == null)
            yield break;

        Transform front = actor.Render.front.transform;
        Vector3 loweredPos = front.position;
        Vector3 originalPos = popOutFrontRestorePos;

        Color c = spriteRenderer.color;
        spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);

        for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(1, 0, t);
            spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
            AlignPortraitWithFront(front.position);
            yield return Wait.None();
        }
        spriteRenderer.color = new Color(c.r, c.g, c.b, 0f);
        AlignPortraitWithFront(front.position);

        Quaternion startRot = front.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, 0);
        for (float elapsed = 0; elapsed < rotateDuration; elapsed += Time.deltaTime)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            float t = elapsed / rotateDuration;
            front.rotation = Quaternion.Slerp(startRot, targetRot, t);
            front.position = Vector3.Lerp(loweredPos, originalPos, t);
            AlignPortraitWithFront(front.position);
            yield return Wait.None();
        }
        front.rotation = targetRot;
        front.position = originalPos;
        AlignPortraitWithFront(front.position);

        Despawn();
    }

    public IEnumerator DissolveRoutine(IEnumerator routine = null)
    {
        if (isBeingDestroyed || spriteRenderer == null)
            yield break;

        float alpha = 1f;
        spriteRenderer.color = new Color(1, 1, 1, alpha);
        Coroutine runningCoroutine = null;

        while (alpha > 0f)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            position = startPosition;
            position += new Vector3(RNG.Range(ShakeIntensity.Medium), RNG.Range(ShakeIntensity.Medium), 1);
            transform.localScale *= 0.99f;

            alpha = Mathf.Clamp01(alpha - Increment.Percent1);

            if (routine != null && runningCoroutine == null && alpha <= Opacity.Percent10)
                runningCoroutine = StartCoroutine(routine);

            spriteRenderer.color = new Color(1, 1, 1, alpha);
            yield return Wait.None();
        }

        Despawn();
    }

    private void AlignPortraitWithFront(Vector3 frontAnchorPos)
    {
        if (isBeingDestroyed || spriteRenderer == null)
            return;

        float halfPortraitHeight = spriteRenderer.bounds.size.y / 2f;
        transform.position = frontAnchorPos + Vector3.up * halfPortraitHeight;
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    protected void Despawn()
    {
        if (isBeingDestroyed) return;
        isBeingDestroyed = true;
        Destroy(gameObject);
    }

    #endregion
}
