using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;
using c = Assets.Helpers.CanvasHelper;
using g = Assets.Helpers.GameHelper;
using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Factories;

/// <summary>
/// TOOLTIPINSTANCE - Floating tooltip UI component.
/// 
/// PURPOSE:
/// Displays contextual tooltip text near UI elements or world objects.
/// Supports typewriter effect, fading, and auto-positioning.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌─────────────────┐
/// │ Tooltip message │
/// │ with details    │
/// └─────────────────┘
///         ↑
///    [Target Element]
/// ```
/// 
/// FEATURES:
/// - followPointer: Tooltip follows mouse/touch
/// - useFade: Fade in/out animation
/// - useTypewriter: Text appears character by character
/// - autoDestroy: Self-destructs after delay
/// 
/// PLACEMENT:
/// TooltipPlacement enum determines anchor position relative to target:
/// - Above, Below, Left, Right
/// 
/// TEXT ALIGNMENT:
/// TooltipTextAlignment controls text positioning within tooltip.
/// 
/// TYPEWRITER MODES:
/// - CharacterByCharacter: One char at a time
/// - WordByWord: One word at a time
/// 
/// RELATED FILES:
/// - TooltipFactory.cs: Creates tooltip GameObjects
/// - TooltipManager.cs: Manages tooltip lifecycle
/// - CanvasHelper.cs: Canvas positioning utilities
/// </summary>
public class TooltipInstance : MonoBehaviour
{
    #region UI References

    private RectTransform background;
    private TextMeshProUGUI label;
    private CanvasGroup canvasGroup;

    #endregion

    #region Settings

    private Vector2 screenOffset;
    private bool followingPointer = false;
    private float fadeTime = 0.2f;

    public bool followPointer = false;
    public bool useFade = false;
    public bool useTypewriter = false;
    public bool autoDestroy = false;
    public float autoDestroyDelay = 2.5f;
    public float horizontalPadding = 12f;
    public float verticalPadding = 12f;
    public float lineBuffer = 4f;
    public float horizontalMargin = 24f;
    public float typewriterFocus = 0.02f;
    public TypewriterMode typewriterMode = TypewriterMode.CharacterByCharacter;
    public TooltipTextAlignment textAlignment = TooltipTextAlignment.TopLeft;

    #endregion

    #region Initialization

    /// <summary>
    /// Configures tooltip with message and positioning.
    /// </summary>
    public void Assign(string message, RectTransform uiTarget, Transform worldTarget, TooltipPlacement placement)
    {
        canvasGroup = GetComponent<CanvasGroup>();
        background = transform.Find("Background").GetComponent<RectTransform>();
        label = background.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        if (!background || !label)
        {
            Debug.LogWarning("TooltipInstance is missing Background or Label references.");
            return;
        }

        label.enableAutoSizing = false;
        label.lineSpacing = label.fontSize * 1.5f;

        float canvasMaxWidth = c.CanvasRect.rect.width - (horizontalMargin * 2f);
        string wrappedMessage = WrapMessage(message, canvasMaxWidth);

        LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);

        float fontSize = label.fontSize;
        float lineHeight = fontSize + lineBuffer;
        string[] lines = wrappedMessage.Split('\n');
        float maxLineWidth = 0f;

    #endregion
        foreach (var line in lines)
        {
            var size = label.GetPreferredValues(line, Mathf.Infinity, lineHeight);
            maxLineWidth = Mathf.Max(maxLineWidth, size.x);
        }

        float width = maxLineWidth + (horizontalPadding * 2f);
        float height = (lines.Length * lineHeight) + (verticalPadding * 2f);
        background.sizeDelta = new Vector2(width, height);

        switch (textAlignment)
        {
            case TooltipTextAlignment.TopLeft:
                label.alignment = TextAlignmentOptions.TopLeft;
                label.rectTransform.pivot = new Vector2(0, 1);
                label.rectTransform.anchorMin = new Vector2(0, 1);
                label.rectTransform.anchorMax = new Vector2(0, 1);
                label.rectTransform.sizeDelta = new Vector2(width - (horizontalPadding * 2f), height - (verticalPadding * 2f));
                label.rectTransform.anchoredPosition = new Vector2(horizontalPadding, -verticalPadding);
                break;

            case TooltipTextAlignment.Center:
                label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchoredPosition = Vector2.zero;
                break;
        }

        if (useTypewriter)
        {
            if (typewriterMode == TypewriterMode.LineByLine)
                StartCoroutine(TypewriterLineRoutine(wrappedMessage));
            else
                StartCoroutine(TypewriterRoutine(wrappedMessage));
        }
        else
        {
            label.text = wrappedMessage;
            if (autoDestroy) StartCoroutine(AutoDestroyRoutine());
        }

        Vector2 tooltipSize = background.sizeDelta;
        Vector2 screenPos = uiTarget ? UnitConversionHelper.Canvas.TransformToScreen(uiTarget)
            : worldTarget ? UnitConversionHelper.World.ToScreen(worldTarget.position)
            : UnitConversionHelper.Viewport.ToScreen(new Vector2(0.5f, 0.5f));

        Vector2 localPos = UnitConversionHelper.Screen.ToCanvas(c.CanvasRect, screenPos);

        Vector2 finalPos = CalculatePosition(localPos, tooltipSize, uiTarget ? uiTarget.sizeDelta : Vector2.zero, placement);

        switch (placement)
        {
            case TooltipPlacement.Top:
                background.pivot = new Vector2(0.5f, 0);
                break;
            case TooltipPlacement.Bottom:
                background.pivot = new Vector2(0.5f, 1);
                break;
            default:
                background.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        background.anchoredPosition = ClampToScreen(finalPos, tooltipSize);
        StartCoroutine(AnimateGrowthRoutine(background.anchoredPosition, placement));

        if (followPointer)
        {
            followingPointer = true;
            screenOffset = finalPos - (Vector2)Input.mousePosition;
        }

        if (useFade)
        {
            canvasGroup.alpha = 0;
            StartCoroutine(FadeRoutine(0f, 1f, fadeTime));
        }
    }

    private string WrapMessage(string input, float maxWidth)
    {
        string[] words = input.Split(' ');
        float lineHeight = label.fontSize + lineBuffer;
        StringBuilder builder = new StringBuilder();
        string currentLine = "";

        foreach (var word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            Vector2 size = label.GetPreferredValues(testLine, Mathf.Infinity, lineHeight);
            if (size.x > maxWidth)
            {
                if (!string.IsNullOrEmpty(currentLine))
                    builder.AppendLine(currentLine.TrimEnd());
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        builder.Append(currentLine.TrimEnd());
        return builder.ToString();
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        label.text = "";
        int i = 0;
        StringBuilder visibleText = new StringBuilder();
        bool insideTag = false;

        while (i < fullText.Length)
        {
            char c = fullText[i];
            if (c == '<') insideTag = true;
            if (c == '>') insideTag = false;
            visibleText.Append(c);
            i++;

            if (!insideTag)
            {
                label.text = visibleText.ToString();
                LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);
                UpdateBackgroundSize();
                yield return new WaitForSeconds(typewriterFocus);
            }
        }

        label.text = fullText;
        if (autoDestroy) StartCoroutine(AutoDestroyRoutine());
    }

    private IEnumerator TypewriterLineRoutine(string fullText)
    {
        label.text = "";
        string[] lines = fullText.Split('\n');
        StringBuilder displayedText = new StringBuilder();

        foreach (string line in lines)
        {
            displayedText.AppendLine(line);
            label.text = displayedText.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);
            UpdateBackgroundSize();
            yield return new WaitForSeconds(typewriterFocus * 10f);
        }

        label.text = fullText;
        if (autoDestroy) StartCoroutine(AutoDestroyRoutine());
    }

    private IEnumerator AnimateGrowthRoutine(Vector2 anchoredTarget, TooltipPlacement placement)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.95f, 0.95f, 1f);
        Vector3 endScale = Vector3.one;
        float offset = 10f;
        Vector2 startPos = anchoredTarget;
        Vector2 endPos = anchoredTarget;

        if (placement == TooltipPlacement.Top)
            startPos.y -= offset;
        else if (placement == TooltipPlacement.Bottom)
            startPos.y += offset;

        background.localScale = startScale;
        background.anchoredPosition = startPos;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            background.localScale = Vector3.Lerp(startScale, endScale, eased);
            background.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            elapsed += Time.deltaTime;
            yield return Wait.None();
        }

        background.localScale = endScale;
        background.anchoredPosition = endPos;
    }

    private void UpdateBackgroundSize()
    {
        float fontSize = label.fontSize;
        float lineHeight = fontSize + lineBuffer;
        string[] lines = label.text.Split('\n');
        float maxLineWidth = 0f;
        foreach (var line in lines)
        {
            var size = label.GetPreferredValues(line, Mathf.Infinity, lineHeight);
            maxLineWidth = Mathf.Max(maxLineWidth, size.x);
        }
        float width = maxLineWidth + (horizontalPadding * 2f);
        float height = (lines.Length * lineHeight) + (verticalPadding * 2f);
        background.sizeDelta = new Vector2(width, height);
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            float eased = Mathf.SmoothStep(from, to, t / duration);
            canvasGroup.alpha = eased;
            t += Time.deltaTime;
            yield return Wait.None();
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator AutoDestroyRoutine()
    {
        yield return new WaitForSeconds(autoDestroyDelay);
        if (useFade)
            yield return StartCoroutine(FadeRoutine(1f, 0f, fadeTime));
        Destroy(gameObject);
    }

    private void Update()
    {
        if (followingPointer)
        {
            Vector2 mousePos = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(c.CanvasRect, mousePos + screenOffset, null, out Vector2 localPos);
            background.anchoredPosition = ClampToScreen(localPos, background.sizeDelta);
        }
    }

    private Vector2 CalculatePosition(Vector2 origin, Vector2 tooltipSize, Vector2 targetSize, TooltipPlacement placement)
    {
        float padding = 16f;
        switch (placement)
        {
            case TooltipPlacement.Top:
                return origin + new Vector2(0, targetSize.y / 2 + padding);
            case TooltipPlacement.Bottom:
                return origin - new Vector2(0, targetSize.y / 2 + padding);
            case TooltipPlacement.Left:
                return origin - new Vector2(targetSize.x / 2 + tooltipSize.x / 2 + padding, 0);
            case TooltipPlacement.Right:
                return origin + new Vector2(targetSize.x / 2 + tooltipSize.x / 2 + padding, 0);
            default:
                return origin;
        }
    }

    private Vector2 ClampToScreen(Vector2 position, Vector2 size)
    {
        float canvasWidth = c.CanvasRect.rect.width;
        float canvasHeight = c.CanvasRect.rect.height;
        float halfWidth = size.x / 2;
        float halfHeight = size.y / 2;
        position.x = Mathf.Clamp(position.x, -canvasWidth / 2 + halfWidth, canvasWidth / 2 - halfWidth);
        position.y = Mathf.Clamp(position.y, -canvasHeight / 2 + halfHeight, canvasHeight / 2 - halfHeight);
        return position;
    }
}

public class TooltipSettings
{
    public string message;
    public object target = null;
    public TooltipPlacement placement = TooltipPlacement.Top;
    public float autoDestroyDelay = 2.5f;
    public bool followPointer = false;
    public bool useFade = false;
    public bool useTypewriter = false;
    public bool autoDestroy = false;
    public TypewriterMode typewriterMode = TypewriterMode.CharacterByCharacter;
    public TooltipTextAlignment textAlignment = TooltipTextAlignment.TopLeft;
    public float typewriterFocus = 0.02f;
}

public static class Tooltip
{
    public static TooltipInstance Show(TooltipSettings settings)
    {
        // Use factory instead of Instantiate(prefab)
        GameObject go = TooltipFactory.Create(c.CanvasRect);
        go.name = $"Tooltip_{System.Guid.NewGuid():N}";

        var instance = go.GetComponent<TooltipInstance>();
        instance.useFade = settings.useFade;
        instance.useTypewriter = settings.useTypewriter;
        instance.autoDestroy = settings.autoDestroy;
        instance.followPointer = settings.followPointer;
        instance.autoDestroyDelay = settings.autoDestroyDelay;
        instance.typewriterMode = settings.typewriterMode;
        instance.textAlignment = settings.textAlignment;
        instance.typewriterFocus = settings.typewriterFocus;

        RectTransform uiTarget = null;
        Transform worldTarget = null;

        if (settings.target is RectTransform rectTransform)
            uiTarget = rectTransform;
        else if (settings.target is Transform transform)
            worldTarget = transform;

        instance.Assign(settings.message, uiTarget, worldTarget, settings.placement);
        return instance;
    }




}
