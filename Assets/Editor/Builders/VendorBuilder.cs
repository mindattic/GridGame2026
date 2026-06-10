using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using Scripts.Hub;

/// <summary>
/// VENDORBUILDER - Editor tool that builds the Vendor (Merchant) scene from code.
///
/// SCENE HIERARCHY (manager-facing paths preserved — VendorManager Find()s these):
/// ```
/// Main Camera / EventSystem / VendorManagerGO / Canvas
///   ├── Header                UiKit header — "Merchant" title + gold rule
///   ├── ModeBar               HorizontalLayoutGroup of mode tabs
///   │   ├── SellTabButton     Tab (manager tints NavActive at runtime)
///   │   └── BuyTabButton      Tab
///   ├── List                  UiKit.ScrollList → List/Viewport/Content + themed scrollbar
///   ├── FooterBar             HeaderBg strip at the bottom
///   │   ├── TotalLabel        "Pay: Ng | Gold: Ng"
///   │   └── ActionButton      Primary (gold) commit button — label flips Buy/Sell
///   ├── VendorNavBar          Floating hamburger nav
///   └── FadeOverlay
/// ```
///
/// RELATED FILES: VendorManager.cs, UiKit.cs, HubTheme.cs
/// </summary>
public static class VendorBuilder
{
    private const string SceneName = "Vendor";

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("VendorManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<Scripts.Vendor.VendorManager>(mgrGO);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        var header = UiKit.Header(canvas, "Merchant");

        // ModeBar — tab strip directly under the header.
        var modeBar = MakeRect(canvas, "ModeBar");
        modeBar.anchorMin = new Vector2(0f, 1f);
        modeBar.anchorMax = new Vector2(1f, 1f);
        modeBar.pivot = new Vector2(0.5f, 1f);
        modeBar.sizeDelta = new Vector2(-48f, 88f);
        modeBar.anchoredPosition = new Vector2(0f, -(UiKit.HeaderHeight + 8f));
        var hlg = modeBar.gameObject.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = modeBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        UiKit.Button(modeBar, "SellTabButton", "Sell", UiKit.UiButtonStyle.Tab, 30f);
        UiKit.Button(modeBar, "BuyTabButton", "Buy", UiKit.UiButtonStyle.Tab, 30f);

        // List — between the mode bar and the footer.
        var list = UiKit.ScrollList(canvas, "List");
        list.anchorMin = new Vector2(0f, 0.1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.offsetMin = new Vector2(24f, 8f);
        list.offsetMax = new Vector2(-24f, -(UiKit.HeaderHeight + 8f + 88f + 8f));

        // FooterBar — total + commit action.
        var footer = MakeRect(canvas, "FooterBar");
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0.1f);
        footer.offsetMin = footer.offsetMax = Vector2.zero;
        var footerImg = footer.gameObject.GetComponent<Image>();
        if (footerImg == null) footerImg = footer.gameObject.AddComponent<Image>();
        footerImg.color = HubTheme.HeaderBg;
        footerImg.raycastTarget = true;

        var total = UiKit.Label(footer, "TotalLabel", "Pay: 0g  |  Gold: 0g", 32f);
        total.anchorMin = new Vector2(0f, 0f);
        total.anchorMax = new Vector2(0.6f, 1f);
        total.offsetMin = new Vector2(24f, 0f);
        total.offsetMax = new Vector2(-12f, 0f);
        var totalTmp = total.GetComponent<TextMeshProUGUI>();
        if (totalTmp != null) { totalTmp.alignment = TextAlignmentOptions.MidlineLeft; totalTmp.enableWordWrapping = false; }

        var action = UiKit.Button(footer, "ActionButton", "Buy", UiKit.UiButtonStyle.Primary, 36f);
        action.anchorMin = new Vector2(0.6f, 0f);
        action.anchorMax = new Vector2(1f, 1f);
        action.offsetMin = new Vector2(8f, 12f);
        action.offsetMax = new Vector2(-24f, -12f);

        VendorNavBarBuilder.Build(canvas, topInset: 0f, anchorLeft: true);

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    private static RectTransform MakeRect(RectTransform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing as RectTransform;
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return rt;
    }
}
