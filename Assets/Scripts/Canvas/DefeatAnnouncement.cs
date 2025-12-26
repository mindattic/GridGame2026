using System.Collections;
using UnityEngine;
using TMPro;
using Assets.Helper;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// Displays a centered "Defeat" banner that starts higher, floats down, and fades in.
/// Uses the same Canvas child structure as WaveAnnouncement (Image, Back, Front).
/// </summary>
public class DefeatAnnouncement : MonoBehaviour
{
    // Units per second for downward floating movement.
    public float floatSpeed = 900f;

    // Vertical offset to start above the center.
    public float startOffsetY = 250f;

    // Track the currently running animation to prevent overlapping.
    private Coroutine animationRoutine;

    // Ensure this plays at most once per session.
    private bool hasPlayed = false;

    // Cached references from GameObjectHelper
    private GameObject root;
    private Image image;
    private TextMeshProUGUI back;
    private TextMeshProUGUI front;

    private RectTransform rect;
    private Vector2 startPos;
    private Vector2 centerPos = Vector2.zero;

    private void Awake()
    {
        // Resolve UI references via GameObjectHelper.
        root = GameObjectHelper.Game.DefeatAnnouncement.Root;
        image = GameObjectHelper.Game.DefeatAnnouncement.Image;
        back = GameObjectHelper.Game.DefeatAnnouncement.Back;
        front = GameObjectHelper.Game.DefeatAnnouncement.Front;
        rect = root != null ? root.GetComponent<RectTransform>() : GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Set initial position above center and make fully transparent.
        if (rect != null)
        {
            startPos = centerPos + new Vector2(0f, Mathf.Abs(startOffsetY));
            rect.anchoredPosition = startPos;
        }
        SetLabelAlpha(0f);
    }

    /// <summary>
    /// Shows the Defeat banner (floats down from above while fading in). Only plays once.
    /// </summary>
    public void Show()
    {
        if (hasPlayed) return;
        hasPlayed = true;

        g.AudioManager.Play("Defeat");
        SetText("Defeat");
        RestartAnimation();
    }

    private void RestartAnimation()
    {
        // Do not restart if already animating
        if (animationRoutine != null)
            return;

        animationRoutine = StartCoroutine(AnimateFloatInRoutine());
    }

    private IEnumerator AnimateFloatInRoutine()
    {
        if (rect != null)
            rect.anchoredPosition = startPos;

        // Prepare fade in
        SetLabelAlpha(0f);

        float totalDist = rect != null ? Vector2.Distance(startPos, centerPos) : 1f;
        if (totalDist <= 0.01f) totalDist = 1f;

        while (rect != null && (rect.anchoredPosition - centerPos).sqrMagnitude > 0.25f)
        {
            // Move down towards center
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, centerPos, floatSpeed * Time.deltaTime);

            // Fade in proportionally to progress
            float remaining = Vector2.Distance(rect.anchoredPosition, centerPos);
            float progress = Mathf.Clamp01(1f - (remaining / totalDist));
            float a = progress;
            SetLabelAlpha(a);

            yield return Wait.None();
        }

        if (rect != null)
            rect.anchoredPosition = centerPos;
        SetLabelAlpha(1f);

        animationRoutine = null;
    }

    // ------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------

    private void SetText(string value)
    {
        if (back != null) back.text = value;
        if (front != null) front.text = value;
    }

    private void SetLabelAlpha(float a)
    {
        if (image != null)
        {
            var c = image.color;
            c.a = a;
            image.color = c;
        }

        if (back != null)
        {
            var c = back.color;
            c.a = a;
            back.color = c;
        }

        if (front != null)
        {
            var c = front.color;
            c.a = a;
            front.color = c;
        }
    }
}
