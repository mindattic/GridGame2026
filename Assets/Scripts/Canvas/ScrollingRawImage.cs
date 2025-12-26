using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Utilities;
using g = Assets.Helpers.GameHelper;

public class ScrollingRawImage : MonoBehaviour
{
    public Vector2 scrollFocus = new Vector2(0f, 0f);
    private Vector2 scrollFocusMin = new Vector2(-0.02f, -0.02f);
    private Vector2 scrollFocusMax = new Vector2(0.02f, 0.02f);

    private float minSecondsBetweenChanges = 10f;
    private float maxSecondsBetweenChanges = 30f;
    private float focusLerpSpeed = 3f;
    private bool useLerpTransition = true;

    private RawImage rawImage;
    private Rect uvRect;
    private Vector2 targetScrollFocus;
    private float nextChangeAt;

    private void Awake()
    {
        GameReady.Begin(this);
    }

    /// <summary>
    /// Initializes references, seeds the UV position, and schedules the first direction change.
    /// </summary>
    void Start()
    {
        rawImage = GetComponent<RawImage>();
        if (rawImage == null)
        {
            Debug.LogError("ScrollingUITexture: No RawImage component found!");
            return;
        }

        uvRect = rawImage.uvRect;
        uvRect.position = new Vector2(RNG.Float(0, 1), RNG.Float(0, 1));
        rawImage.uvRect = uvRect;

        targetScrollFocus = RandomFocusInRange();
        if (!useLerpTransition)
        {
            scrollFocus = targetScrollFocus;
        }

        ScheduleNextChange();
    }

    /// <summary>
    /// Drives timed direction changes and advances the UV rect based on the current focus.
    /// Lerp can be toggled off to snap instantly to each new direction.
    /// </summary>
    void Update()
    {
        if (rawImage == null || !gameObject.activeInHierarchy)
            return;

        if (g.PauseMenu == null || !g.PauseMenu.IsPaused)
            return;

        if (Time.unscaledTime >= nextChangeAt)
        {
            targetScrollFocus = RandomFocusInRange();

            if (!useLerpTransition)
                scrollFocus = targetScrollFocus;

            ScheduleNextChange();
        }

        float dt = Time.unscaledDeltaTime;

        if (useLerpTransition)
        {
            float t = 1f - Mathf.Exp(-focusLerpSpeed * dt);
            scrollFocus = Vector2.Lerp(scrollFocus, targetScrollFocus, t);
        }

        uvRect.position += scrollFocus * dt;
        rawImage.uvRect = uvRect;
    }

    /// <summary>
    /// Picks a new scroll focus within the configured min and max range.
    /// </summary>
    private Vector2 RandomFocusInRange()
    {
        float x = RNG.Range(scrollFocusMin.x, scrollFocusMax.x);
        float y = RNG.Range(scrollFocusMin.y, scrollFocusMax.y);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Sets the timestamp for the next direction change using a random interval.
    /// </summary>
    private void ScheduleNextChange()
    {
        float wait = RNG.Range(minSecondsBetweenChanges, maxSecondsBetweenChanges);
        nextChangeAt = Time.unscaledTime + Mathf.Max(0f, wait);
    }

}
