using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor;

/// <summary>
/// VENDORNAVBARSCAFFOLD - Builds the shared hamburger-menu navigation into a vendor scene's Canvas.
/// <para>PURPOSE: Single editor-time helper so every vendor scaffold (VendorScaffold,
/// AlchemistScaffold, ...) can call VendorNavBarScaffold.Build(canvas) and get an identical
/// nav. When a new vendor scene ships, add an entry to <see cref="VendorNavBar.Entries"/>
/// and re-run the affected scaffolds — the visual stays consistent.</para>
/// <para>LAYOUT: A floating hamburger button anchored to the upper-right of the canvas.
/// Clicking the hamburger opens a vertical dropdown listing every scene; clicking outside
/// the dropdown (the transparent Backdrop) closes it. The dropdown drops down-left so its
/// right edge stays aligned with the hamburger.</para>
/// <para>RELATED FILES: VendorNavBar.cs, VendorScaffold.cs, AlchemistScaffold.cs</para>
/// </summary>
public static class VendorNavBarScaffold
{
    /// <summary>Reserved vertical space the nav consumes in the layout. Zero since the
    /// hamburger floats — vendor scaffolds keep the constant for back-compat in their
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
    /// Idempotent — clears and rebuilds if it already exists.</summary>
    public static void Build(RectTransform canvas, float topInset)
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
        BuildDropdown(navRT, topInset);
        BuildHamburger(navRT, topInset);

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

    private static void BuildDropdown(RectTransform parent, float topInset)
    {
        float entries = VendorNavBar.Entries.Count;
        float height = (entries * DropdownButtonHeight) + ((entries - 1) * DropdownSpacing) + (DropdownPadding * 2f);

        var go = new GameObject(VendorNavBar.DropdownName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(DropdownWidth, height);
        rt.anchoredPosition = new Vector2(-HamburgerInset, -(topInset + HamburgerInset + HamburgerSize + DropdownGap));
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
        img.raycastTarget = true;

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
        img.color = new Color(0.14f, 0.18f, 0.28f, 1f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1.15f, 1.15f, 1.20f, 1f),
            pressedColor = new Color(0.65f, 0.65f, 0.80f, 1f),
            selectedColor = new Color(1.00f, 1.00f, 1.10f, 1f),
            disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.85f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

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
        tmp.font = SceneScaffoldHelper.LoadFont(SceneScaffoldHelper.FontPaths.Attic);
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    private static void BuildHamburger(RectTransform parent, float topInset)
    {
        var go = new GameObject(VendorNavBar.HamburgerButtonName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(HamburgerSize, HamburgerSize);
        rt.anchoredPosition = new Vector2(-HamburgerInset, -(topInset + HamburgerInset));
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.14f, 0.18f, 0.28f, 0.96f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1.15f, 1.15f, 1.20f, 1f),
            pressedColor = new Color(0.65f, 0.65f, 0.80f, 1f),
            selectedColor = new Color(1.00f, 1.00f, 1.10f, 1f),
            disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.85f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

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
