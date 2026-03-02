using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Helpers;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
/// <summary>
/// CLOCK - Real-time clock display UI component.
/// 
/// PURPOSE:
/// Displays the current system time in the UI, automatically
/// updating and respecting safe area insets.
/// 
/// FEATURES:
/// - Auto-updates time display each second
/// - Respects horizontal safe area insets
/// - Configurable format, font size, color
/// - Joins pane for equal width layout
/// 
/// CONFIGURATION:
/// - timeFormat: Display format (default "h:mm tt")
/// - fontSize: Text size (default 24)
/// - fontColor: Text color (default white)
/// - padding: Position offset from anchor
/// 
/// CHANGE DETECTION:
/// Monitors safe area, screen size, and scale changes
/// to reposition automatically.
/// 
/// RELATED FILES:
/// - CanvasHelper.cs: Canvas access
/// - GameManager.cs: UI hierarchy
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class Clock : MonoBehaviour
{
    // Authoring
    private string timeFormat = "h:mm tt";
    private int fontSize = 24;
    private Color fontColor = Color.white;
    private Vector2 padding = new Vector2(100f, -32f);
    private TextAlignmentOptions alignment = TextAlignmentOptions.Center;
    private bool respectHorizontalSafeInset = true;

    // Components
    private TMP_Text clockText;
    private RectTransform rect;
    private UnityEngine.Canvas rootCanvas;
    private CanvasScaler canvasScaler;
    private LayoutElement layoutElement;

    // Tick
    private float nextClockTickTime;

    // Change detection
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private float lastEffectiveScale = -1f;
    private int lastSiblingCount = -1;

    private void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        clockText = GetComponent<TextMeshProUGUI>();
  

        rootCanvas = GetComponentInParent<UnityEngine.Canvas>();
        canvasScaler = rootCanvas != null ? rootCanvas.GetComponent<CanvasScaler>() : null;

        clockText.raycastTarget = false;
        clockText.textWrappingMode = TextWrappingModes.NoWrap;
        clockText.fontSize = fontSize;
        clockText.color = fontColor;
        clockText.alignment = alignment;

        EnsureInOverlay();
        JoinPaneForEqualWidth();
        AnchorAndPosition(true);
        UpdateClock(true);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureInOverlay();
            JoinPaneForEqualWidth();
            AnchorAndPosition(true);
            UpdateClock(true);
            return;
        }
#endif
        if (transform.parent == null || GetComponentInParent<CutoutOverlay>() == null)
        {
            EnsureInOverlay();
        }

        // Recompute equal share if sibling set changed
        var p = rect.parent;
        if (p != null)
        {
            int sibs = CountActiveChildren(p);
            if (sibs != lastSiblingCount)
            {
                JoinPaneForEqualWidth();
            }
        }

        float s = UnitConversionHelper.UiScale.Get();
        if (Screen.safeArea != lastSafeArea ||
            Screen.width != lastScreenSize.x ||
            Screen.height != lastScreenSize.y ||
            !Mathf.Approximately(s, lastEffectiveScale))
        {
            AnchorAndPosition(true);
        }

        if (Application.isPlaying && Time.unscaledTime >= nextClockTickTime)
        {
            UpdateClock(false);
            nextClockTickTime = Mathf.Floor(Time.unscaledTime) + 1f;
        }
    }

    // Overlay hookup
    private void EnsureInOverlay()
    {
        var overlay = FindFirstObjectByType<CutoutOverlay>(FindObjectsInactive.Include);
        if (overlay == null) return;

        var target = overlay.LeftPane;
        if (target == null) return;

        if (rect.parent != target)
        {
            rect.SetParent(target, false);
            lastSiblingCount = -1; // force JoinPaneForEqualWidth to recalc
        }
    }

    // Equal width logic
    private void JoinPaneForEqualWidth()
    {
        if (rect == null || rect.parent == null) return;

        var pane = rect.parent as RectTransform;
        var hlg = pane.GetComponent<HorizontalLayoutGroup>();
        int sibs = CountActiveChildren(pane);
        lastSiblingCount = sibs;

        if (hlg != null)
        {
            // LayoutGroup path: let the group expand children equally
            hlg.childForceExpandWidth = true;

            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();

            layoutElement.minWidth = -1;
            layoutElement.preferredWidth = -1;
            layoutElement.flexibleWidth = 1;

            // Stretch within the cell
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        // Fallback: divide pane by active children using anchors
        if (sibs <= 0) sibs = 1;
        int myIndex = ActiveSiblingIndex(rect);
        float step = 1f / sibs;
        float aMinX = myIndex * step;
        float aMaxX = aMinX + step;

        rect.anchorMin = new Vector2(aMinX, 0f);
        rect.anchorMax = new Vector2(aMaxX, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Layout
    private void AnchorAndPosition(bool force)
    {
        if (rect == null) return;

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastEffectiveScale = UnitConversionHelper.UiScale.Get();

        // Top alignment inside our cell
        rect.pivot = new Vector2(0f, 1f);

        float leftInsetUnits = 0f;
        if (respectHorizontalSafeInset)
        {
            float leftInsetPixels = Mathf.Max(0f, Screen.safeArea.xMin);
            leftInsetUnits = UnitConversionHelper.UiScale.PixelsToCanvasUnits(leftInsetPixels);
        }

        Vector2 refPaddingUnits = UnitConversionHelper.UiScale.ReferencePixelsToCanvasUnits(padding);

        // Only affect local offset inside our equal-width cell
        rect.anchoredPosition = new Vector2(leftInsetUnits + refPaddingUnits.x, refPaddingUnits.y);

        // Height for the label area; width comes from equal-share logic
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fontSize + 8f);
    }

    // Time text
    private void UpdateClock(bool force)
    {
        if (clockText == null) return;

        string fmt = string.IsNullOrEmpty(timeFormat) ? "h:mm tt" : timeFormat;
        string now = System.DateTime.Now.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);

        if (force || !string.Equals(clockText.text, now))
        {
            clockText.text = now;
        }
    }

    // Helpers
    private int CountActiveChildren(Transform t)
    {
        int c = 0;
        for (int i = 0; i < t.childCount; i++)
            if (t.GetChild(i).gameObject.activeInHierarchy) c++;
        return c;
    }

    private int ActiveSiblingIndex(Transform me)
    {
        var p = me.parent;
        int idx = 0;
        for (int i = 0; i < p.childCount; i++)
        {
            var ch = p.GetChild(i);
            if (!ch.gameObject.activeInHierarchy) continue;
            if (ch == me) return idx;
            idx++;
        }
        return 0;
    }


}

}
