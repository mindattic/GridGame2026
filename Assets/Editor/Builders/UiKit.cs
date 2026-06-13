using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;

/// <summary>
/// UIKIT - The shared UI component factory every scene builder uses (editor-side).
///
/// PURPOSE:
/// One visual language for the whole game (FFBE-inspired mobile JRPG): simple boxes with thin
/// steel borders, navy panels, gold accents, Attic for display text, Outfit for body text.
/// Builders compose screens from these components instead of hand-rolling per-scene helpers —
/// that hand-rolling is exactly how the scenes drifted apart (three back-button styles, hot-pink
/// scrollbars, hand-typed colors, default fonts on runtime rows).
///
/// COMPONENTS:
/// - Header(canvas, title)          96px HeaderBg bar + 3px gold rule + Attic 48 bold gold title
/// - HeaderRightLabel(header, ...)  right-aligned gold label in the header (gold count, page no.)
/// - Panel(parent, name)            flat PanelBg box + 2px PanelBorder edge frame
/// - Border(rt)                     adds the 2px steel edge frame to any rect
/// - Button(parent, name, label, UiButtonStyle)   themed button + standard ColorBlock + Outfit
/// - BackButton(canvas, label)      the ONE back-button convention: bottom-left 220x64 "← Label"
/// - ScrollList(parent, name)       bordered list well + ScrollRect + visible themed scrollbar;
///                                  hierarchy is exactly {name}/Viewport/Content so existing
///                                  manager Find() paths keep resolving
/// - Label / DisplayLabel           Outfit body text / Attic display text
///
/// All methods are idempotent (find-by-name first) and Undo-registered, matching the
/// SceneBuilderHelper contract. Colors come from HubTheme; fonts from UiFonts.
///
/// RELATED FILES: SceneBuilderHelper.cs, HubTheme.cs (palette), UiFonts.cs (typography),
/// HubItemRowFactory.cs (the runtime row counterpart)
/// </summary>
public static class UiKit
{
    public enum UiButtonStyle { Primary, Secondary, Tab, Danger }

    public const float HeaderHeight = 96f;
    public const float BorderPx = 2f;

    /// <summary>Extra pixels reserved at the top for the phone notch / status bar.</summary>
    public const float SafeAreaTop = 88f;
    /// <summary>Extra pixels reserved at the bottom for the home indicator.</summary>
    public const float SafeAreaBottom = 48f;

    // ===================== Header =====================

    /// <summary>The standard scene header: 96px HeaderBg bar across the top, 3px gold rule on
    /// its bottom edge, Attic 48 bold gold title at x=40. Returns the header RectTransform.</summary>
    public static RectTransform Header(RectTransform canvas, string title)
    {
        var header = FindOrMake(canvas, "Header", out bool created);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderHeight + SafeAreaTop);
        header.anchoredPosition = Vector2.zero;
        SetImage(header.gameObject, HubTheme.HeaderBg, raycast: true);

        // Gold rule along the bottom edge — the FFBE-style header underline.
        var rule = FindOrMake(header, "Rule", out _);
        rule.anchorMin = new Vector2(0f, 0f);
        rule.anchorMax = new Vector2(1f, 0f);
        rule.pivot = new Vector2(0.5f, 0f);
        rule.sizeDelta = new Vector2(0f, 3f);
        rule.anchoredPosition = Vector2.zero;
        SetImage(rule.gameObject, HubTheme.Accent, raycast: false);

        var titleRT = FindOrMake(header, "Title", out _);
        titleRT.anchorMin = new Vector2(0f, 0.5f);
        titleRT.anchorMax = new Vector2(0f, 0.5f);
        titleRT.pivot = new Vector2(0f, 0.5f);
        titleRT.sizeDelta = new Vector2(700f, 72f);
        titleRT.anchoredPosition = new Vector2(40f, -(SafeAreaTop / 2f));
        var tmp = SetText(titleRT.gameObject, title, UiFonts.Display, 48, HubTheme.Accent);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        if (created) Undo.RegisterCreatedObjectUndo(header.gameObject, "Create Header");
        return header;
    }

    /// <summary>Right-aligned gold label inside a Header (gold counter, page indicator, …).</summary>
    public static RectTransform HeaderRightLabel(RectTransform header, string name, string text)
    {
        var rt = FindOrMake(header, name, out _);
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 60f);
        rt.anchoredPosition = new Vector2(-40f, -(SafeAreaTop / 2f));
        var tmp = SetText(rt.gameObject, text, UiFonts.Display, 36, HubTheme.Accent);
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        return rt;
    }

    // ===================== Boxes =====================

    /// <summary>A "simple box": flat PanelBg fill + the 2px steel border frame.</summary>
    public static RectTransform Panel(RectTransform parent, string name)
    {
        var rt = FindOrMake(parent, name, out _);
        SetImage(rt.gameObject, HubTheme.PanelBg, raycast: true);
        Border(rt);
        return rt;
    }

    /// <summary>Adds the 2px PanelBorder edge frame (4 thin Images) to any rect.</summary>
    public static void Border(RectTransform rt)
    {
        MakeEdge(rt, "BorderTop",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, BorderPx));
        MakeEdge(rt, "BorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, BorderPx));
        MakeEdge(rt, "BorderLeft",   new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(BorderPx, 0f));
        MakeEdge(rt, "BorderRight",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(BorderPx, 0f));
    }

    private static void MakeEdge(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size)
    {
        var rt = FindOrMake(parent, name, out _);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        SetImage(rt.gameObject, HubTheme.PanelBorder, raycast: false);
        // Borders are decoration — never let a parent LayoutGroup treat them as rows/cells.
        var le = rt.gameObject.GetComponent<LayoutElement>();
        if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    // ===================== Buttons =====================

    /// <summary>Themed button: Outfit label, standard ColorBlock, style-driven fill.
    /// Primary = gold (the one confirm/commit action per screen); Secondary = navy;
    /// Tab = navy (manager tints NavActive at runtime); Danger = red.</summary>
    public static RectTransform Button(RectTransform parent, string name, string label,
        UiButtonStyle style = UiButtonStyle.Secondary, float fontSize = 26f)
    {
        var rt = FindOrMake(parent, name, out bool created);
        Color fill = style switch
        {
            UiButtonStyle.Primary => HubTheme.Accent,
            UiButtonStyle.Danger  => HubTheme.Danger,
            _                     => HubTheme.NavIdle,
        };
        Color textColor = style == UiButtonStyle.Primary ? Color.black : HubTheme.TextLight;

        var img = SetImage(rt.gameObject, fill, raycast: true);
        var btn = rt.gameObject.GetComponent<Button>();
        if (btn == null) btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = HubTheme.ButtonColors;

        var labelRT = FindOrMake(rt, "Label", out _);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var tmp = SetText(labelRT.gameObject, label, UiFonts.Body, fontSize, textColor);
        tmp.alignment = TextAlignmentOptions.Center;
        if (style == UiButtonStyle.Primary) tmp.fontStyle = FontStyles.Bold;

        if (created) Undo.RegisterCreatedObjectUndo(rt.gameObject, $"Create {name}");
        return rt;
    }

    /// <summary>The ONE back-button convention: bottom-left (24,24), 220×64, navy,
    /// "← {label}" in Outfit. Named "BackButton" so existing manager Find() paths resolve.</summary>
    public static RectTransform BackButton(RectTransform canvas, string label)
    {
        var rt = Button(canvas, "BackButton", $"← {label}", UiButtonStyle.Secondary, 26f);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(220f, 64f);
        rt.anchoredPosition = new Vector2(24f, SafeAreaBottom);
        return rt;
    }

    // ===================== Scroll list =====================

    /// <summary>The standard scrollable list: ListBg well + border + masked viewport + Content
    /// (VerticalLayoutGroup + ContentSizeFitter) + a VISIBLE 12px themed vertical scrollbar.
    /// Hierarchy is exactly {name}/Viewport/Content — manager paths like
    /// "Body/ItemList/Viewport/Content" keep resolving.</summary>
    public static RectTransform ScrollList(RectTransform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing as RectTransform;

        var rootRT = FindOrMake(parent, name, out _);
        SetImage(rootRT.gameObject, HubTheme.ListBg, raycast: true);
        Border(rootRT);
        // ScrollRect lives on the root — standard Unity pattern (root scrolls, viewport masks).
        var scroll = rootRT.gameObject.GetComponent<ScrollRect>();
        if (scroll == null) scroll = rootRT.gameObject.AddComponent<ScrollRect>();

        var vpRT = FindOrMake(rootRT, "Viewport", out _);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(BorderPx, BorderPx);
        vpRT.offsetMax = new Vector2(-(14f + BorderPx), -BorderPx); // room for the scrollbar
        vpRT.pivot = new Vector2(0f, 1f);
        // RectMask2D is Unity's recommended clipping for scroll views — no stencil dependency.
        // The old stencil Mask + showMaskGraphic=false + alpha=0.02 combination prevented the
        // stencil write in Unity 6, making all children invisible.
        if (vpRT.gameObject.GetComponent<RectMask2D>() == null)
            vpRT.gameObject.AddComponent<RectMask2D>();

        var contentRT = FindOrMake(vpRT, "Content", out _);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        contentRT.anchoredPosition = Vector2.zero;
        var vlg = contentRT.gameObject.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = contentRT.gameObject.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = contentRT.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Visible themed vertical scrollbar, inside the border on the right edge.
        var barRT = FindOrMake(rootRT, "Scrollbar Vertical", out _);
        barRT.anchorMin = new Vector2(1f, 0f);
        barRT.anchorMax = Vector2.one;
        barRT.pivot = Vector2.one;
        barRT.offsetMin = new Vector2(-(12f + BorderPx), BorderPx);
        barRT.offsetMax = new Vector2(-BorderPx, -BorderPx);
        SetImage(barRT.gameObject, HubTheme.ScrollTrack, raycast: true);
        var bar = barRT.gameObject.GetComponent<Scrollbar>();
        if (bar == null) bar = barRT.gameObject.AddComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;

        var slideRT = FindOrMake(barRT, "Sliding Area", out _);
        slideRT.anchorMin = Vector2.zero;
        slideRT.anchorMax = Vector2.one;
        slideRT.offsetMin = slideRT.offsetMax = Vector2.zero;

        var handleRT = FindOrMake(slideRT, "Handle", out _);
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = handleRT.offsetMax = Vector2.zero;
        var handleImg = SetImage(handleRT.gameObject, HubTheme.ScrollHandle, raycast: true);
        bar.handleRect = handleRT;
        bar.targetGraphic = handleImg;

        scroll.viewport = vpRT;
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        Undo.RegisterCreatedObjectUndo(rootRT.gameObject, $"Create {name}");
        return rootRT;
    }

    // ===================== Text =====================

    /// <summary>Body text — Outfit, TextLight, top-left, wrapping.</summary>
    public static RectTransform Label(RectTransform parent, string name, string text, float fontSize = 24f)
    {
        var rt = FindOrMake(parent, name, out _);
        var tmp = SetText(rt.gameObject, text, UiFonts.Body, fontSize, HubTheme.TextLight);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        return rt;
    }

    /// <summary>Display text — Attic. For titles and announcements outside the header.</summary>
    public static RectTransform DisplayLabel(RectTransform parent, string name, string text, float fontSize = 48f)
    {
        var rt = FindOrMake(parent, name, out _);
        var tmp = SetText(rt.gameObject, text, UiFonts.Display, fontSize, HubTheme.TextLight);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        return rt;
    }

    // ===================== Internals =====================

    private static RectTransform FindOrMake(RectTransform parent, string name, out bool created)
    {
        var existing = parent.Find(name);
        if (existing != null) { created = false; return existing as RectTransform; }
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        created = true;
        return rt;
    }

    private static Image SetImage(GameObject go, Color color, bool raycast)
    {
        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    private static TextMeshProUGUI SetText(GameObject go, string text, TMP_FontAsset font, float size, Color color)
    {
        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }
}
