using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor;

/// <summary>
/// VENDORNAVBARSCAFFOLD - Builds the shared hamburger-menu navigation into a vendor scene's Canvas.
/// <para>PURPOSE: Single editor-time helper so every vendor builder (VendorBuilder,
/// AlchemistBuilder, ...) can call VendorNavBarBuilder.Build(canvas) and get an identical
/// nav. When a new vendor scene ships, add an entry to <see cref="VendorNavBar.Entries"/>
/// and re-run the affected builders — the visual stays consistent.</para>
/// <para>LAYOUT: A floating hamburger button anchored to the upper-right of the canvas.
/// Clicking the hamburger opens a vertical dropdown listing every scene; clicking outside
/// the dropdown (the transparent Backdrop) closes it. The dropdown drops down-left so its
/// right edge stays aligned with the hamburger.</para>
/// <para>RELATED FILES: VendorNavBar.cs, VendorBuilder.cs, AlchemistBuilder.cs</para>
/// </summary>
public static class VendorNavBarBuilder
{
    /// <summary>Reserved vertical space the nav consumes in the layout. Zero since the
    /// hamburger floats — vendor builders keep the constant for back-compat in their
    /// Body offset math.</summary>
    public const float HeightPx = 0f;

    public const float HamburgerSize = 48f;
    public const float HamburgerInset = 16f;
    public const float DropdownWidth = 220f;
    public const float DropdownButtonHeight = 44f;
    public const float DropdownPadding = 6f;
    public const float DropdownSpacing = 4f;
    public const float DropdownGap = 8f;

    /// <summary>Builds the hamburger + dropdown under <paramref name="canvas"/>.
    /// <paramref name="topInset"/> is the per-scene header height — the hamburger floats just
    /// below it so it doesn't collide with the header's GoldLabel.
    /// <paramref name="anchorLeft"/> mirrors the layout to the upper-left (hamburger on the
    /// left, dropdown drops down-right). Default false preserves existing builders.
    /// Idempotent — clears and rebuilds if it already exists.</summary>
    public static void Build(RectTransform canvas, float topInset, bool anchorLeft = false)
    {
        if (canvas == null) return;

        var existing = canvas.Find(VendorNavBar.RootName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var navGO = new GameObject(VendorNavBar.RootName);
        navGO.layer = LayerMask.NameToLayer("UI");
        var navRT = navGO.AddComponent<RectTransform>();
        navRT.SetParent(canvas, false);
        navRT.anchorMin = Vector2.zero;
        navRT.anchorMax = Vector2.one;
        navRT.offsetMin = Vector2.zero;
        navRT.offsetMax = Vector2.zero;
        navGO.AddComponent<CanvasRenderer>();
        var navImg = navGO.AddComponent<Image>();
        navImg.color = new Color(0f, 0f, 0f, 0f);
        navImg.raycastTarget = false;

        navGO.AddComponent<VendorNavBar>();

        BuildBackdrop(navRT);
        BuildDropdown(navRT, topInset, anchorLeft);
        BuildHamburger(navRT, topInset, anchorLeft);

        Undo.RegisterCreatedObjectUndo(navGO, "Create VendorNavBar");
    }

    private static void BuildBackdrop(RectTransform parent)
    {
        var go = new GameObject(VendorNavBar.BackdropName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.01f);
        img.raycastTarget = true;
        go.AddComponent<Button>();
        go.SetActive(false);
    }

    private static void BuildDropdown(RectTransform parent, float topInset, bool anchorLeft)
    {
        float entries = VendorNavBar.Entries.Count;
        float height = (entries * DropdownButtonHeight) + ((entries - 1) * DropdownSpacing) + (DropdownPadding * 2f);

        var go = new GameObject(VendorNavBar.DropdownName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        float ax = anchorLeft ? 0f : 1f;
        rt.anchorMin = new Vector2(ax, 1f);
        rt.anchorMax = new Vector2(ax, 1f);
        rt.pivot = new Vector2(ax, 1f);
        rt.sizeDelta = new Vector2(DropdownWidth, height);
        float xPos = anchorLeft ? HamburgerInset : -HamburgerInset;
        rt.anchoredPosition = new Vector2(xPos, -(topInset + HamburgerInset + HamburgerSize + DropdownGap));
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HubTheme.PanelBg;
        img.raycastTarget = true;
        UiKit.Border(rt); // simple-box treatment on the dropdown panel

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)DropdownPadding, (int)DropdownPadding, (int)DropdownPadding, (int)DropdownPadding);
        vlg.spacing = DropdownSpacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        foreach (var entry in VendorNavBar.Entries)
            BuildDropdownButton(rt, entry.buttonName, entry.label);

        go.SetActive(false);
    }

    private static void BuildDropdownButton(RectTransform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HubTheme.NavIdle;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = HubTheme.ButtonColors;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = DropdownButtonHeight;
        le.preferredHeight = DropdownButtonHeight;
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;

        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Outfit);
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    private static void BuildHamburger(RectTransform parent, float topInset, bool anchorLeft)
    {
        var go = new GameObject(VendorNavBar.HamburgerButtonName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        float ax = anchorLeft ? 0f : 1f;
        rt.anchorMin = new Vector2(ax, 1f);
        rt.anchorMax = new Vector2(ax, 1f);
        rt.pivot = new Vector2(ax, 1f);
        rt.sizeDelta = new Vector2(HamburgerSize, HamburgerSize);
        float xPos = anchorLeft ? HamburgerInset : -HamburgerInset;
        rt.anchoredPosition = new Vector2(xPos, -(topInset + HamburgerInset));
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HubTheme.NavIdle;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = HubTheme.ButtonColors;

        BuildHamburgerStripe(rt, "Line1", 0.66f);
        BuildHamburgerStripe(rt, "Line2", 0.50f);
        BuildHamburgerStripe(rt, "Line3", 0.34f);
    }

    private static void BuildHamburgerStripe(RectTransform parent, string name, float yAnchor01)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, yAnchor01);
        rt.anchorMax = new Vector2(0.5f, yAnchor01);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(24f, 3f);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HubTheme.TextLight;
        img.raycastTarget = false;
    }
}
