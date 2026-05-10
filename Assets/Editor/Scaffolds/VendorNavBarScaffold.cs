using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor;

/// <summary>
/// VENDORNAVBARSCAFFOLD - Builds the shared VendorNavBar strip into a vendor scene's Canvas.
/// <para>PURPOSE: Single editor-time helper so every vendor scaffold (StoreScaffold,
/// AlchemistScaffold, ...) can call VendorNavBarScaffold.Build(canvas) and get an identical
/// nav bar. When a new vendor scene ships, add an entry to <see cref="VendorNavBar.Entries"/>
/// and re-run the affected scaffolds — the visual stays consistent.</para>
/// <para>LAYOUT: Strip anchored to top of canvas, just below the Header, height 56px.
/// Buttons stretch evenly via HorizontalLayoutGroup.</para>
/// <para>RELATED FILES: VendorNavBar.cs, StoreScaffold.cs, AlchemistScaffold.cs</para>
/// </summary>
public static class VendorNavBarScaffold
{
    /// <summary>The vertical space the nav bar consumes — vendor scaffolds use this to
    /// inset their Body offsets so the bar doesn't overlap content.</summary>
    public const float HeightPx = 56f;

    /// <summary>Builds the nav bar under <paramref name="canvas"/>, anchored to the top edge
    /// at <paramref name="topInset"/> px from the canvas top (typically the Header height).
    /// Idempotent — clears and rebuilds if it already exists, so re-scaffolding is safe.</summary>
    public static void Build(RectTransform canvas, float topInset)
    {
        if (canvas == null) return;

        var existing = canvas.Find(VendorNavBar.RootName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var navGO = new GameObject(VendorNavBar.RootName);
        navGO.layer = LayerMask.NameToLayer("UI");
        var navRT = navGO.AddComponent<RectTransform>();
        navRT.SetParent(canvas, false);
        navRT.anchorMin = new Vector2(0f, 1f);
        navRT.anchorMax = new Vector2(1f, 1f);
        navRT.pivot = new Vector2(0.5f, 1f);
        navRT.sizeDelta = new Vector2(0f, HeightPx);
        navRT.anchoredPosition = new Vector2(0f, -topInset);
        navGO.AddComponent<CanvasRenderer>();
        var navImg = navGO.AddComponent<Image>();
        navImg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);
        navImg.raycastTarget = true;

        var hlg = navGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        navGO.AddComponent<VendorNavBar>();

        foreach (var entry in VendorNavBar.Entries)
            BuildButton(navRT, entry.buttonName, entry.label);

        Undo.RegisterCreatedObjectUndo(navGO, "Create VendorNavBar");
    }

    private static void BuildButton(RectTransform parent, string name, string label)
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
        le.minHeight = 40f;
        le.preferredHeight = 44f;
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
}
