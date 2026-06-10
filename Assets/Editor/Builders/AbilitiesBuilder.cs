using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor.Abilities;

/// <summary>
/// ABILITIESSCAFFOLD - Editor tool that builds the Abilities scene from code.
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera
/// EventSystem
/// AbilitiesManagerGO          AbilitiesManager (Scripts.Vendor.Abilities) script owner
/// Canvas                      ScreenSpaceOverlay + dark background
///   ├── Header                Title "Abilities — {Hero}"
///   ├── VendorNavBar          Shared nav strip
///   ├── Body
///   │   ├── SlotsRow          Horizontal strip of 5 slot buttons (top 35%)
///   │   ├── FlashLabel        Single-line success/clear message (mid)
///   │   └── ConsumablesList   ScrollView of consumables in inventory (bottom)
///   ├── BackButton            Bottom-left, fades to Party
///   └── FadeOverlay
/// ```
///
/// RELATED FILES: AbilitiesManager.cs, VendorNavBarBuilder.cs, HeroEquipmentSave
/// </summary>
public static class AbilitiesBuilder
{
    private const string SceneName = "Abilities";
    private const float HeaderH = 96f;

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("AbilitiesManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<AbilitiesManager>(mgrGO);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        var canvasBg = canvas.GetComponent<Image>();
        if (canvasBg != null) canvasBg.color = HubTheme.PanelBg;

        BuildHeader(canvas, ref created, ref found);
        VendorNavBarBuilder.Build(canvas, topInset: HeaderH, anchorLeft: true);
        BuildBody(canvas, ref created, ref found);
        UiKit.BackButton(canvas, "Party");
        created++;

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        // UiKit.Header uses Attic 48pt bold — fixes the 44pt drift from the old bespoke header.
        UiKit.Header(canvas, "Abilities");
        created++;
    }

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, 96f);
        body.offsetMax = new Vector2(-24f, -(HeaderH + VendorNavBarBuilder.HeightPx + 8f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) { bodyImg.color = new Color(0f, 0f, 0f, 0f); bodyImg.raycastTarget = false; }

        BuildSlotsRow(body, ref created, ref found);
        BuildFlashLabel(body, ref created, ref found);
        BuildConsumablesList(body, ref created, ref found);
    }

    private static void BuildSlotsRow(RectTransform body, ref int created, ref int found)
    {
        var row = FindOrMake(body, "SlotsRow", ref created, ref found);
        row.anchorMin = new Vector2(0f, 0.65f);
        row.anchorMax = new Vector2(1f, 1f);
        row.offsetMin = new Vector2(0f, 0f);
        row.offsetMax = new Vector2(0f, 0f);
        var rowImg = row.GetComponent<Image>();
        if (rowImg != null) { rowImg.color = new Color(0f, 0f, 0f, 0.15f); rowImg.raycastTarget = false; }

        var hlg = row.gameObject.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 12, 12);
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
    }

    private static void BuildFlashLabel(RectTransform body, ref int created, ref int found)
    {
        var flash = UiKit.Label(body, "FlashLabel", "");
        flash.anchorMin = new Vector2(0f, 0.55f);
        flash.anchorMax = new Vector2(1f, 0.65f);
        flash.offsetMin = new Vector2(12f, 4f);
        flash.offsetMax = new Vector2(-12f, -4f);
        var tmp = flash.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
        }
    }

    private static void BuildConsumablesList(RectTransform body, ref int created, ref int found)
    {
        var consumList = UiKit.ScrollList(body, "ConsumablesList");
        consumList.anchorMin = new Vector2(0f, 0f);
        consumList.anchorMax = new Vector2(1f, 0.55f);
        consumList.offsetMin = new Vector2(0f, 0f);
        consumList.offsetMax = new Vector2(0f, -8f);
        created++;
    }

    // ---------- Primitives ----------

    private static RectTransform FindOrMake(RectTransform parent, string name, ref int created, ref int found)
    {
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing as RectTransform; }
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        go.AddComponent<Image>().raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }
}
