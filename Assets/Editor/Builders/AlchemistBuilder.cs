using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor.Alchemy;

/// <summary>
/// ALCHEMISTSCAFFOLD - Editor tool that builds the Alchemist scene from code.
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera
/// EventSystem
/// AlchemistManagerGO         AlchemistManager script owner
/// Canvas                     ScreenSpaceOverlay + dark background
///   ├── Header               "Alchemist" title (left) + GoldLabel (right)
///   ├── VendorNavBar         Shared nav strip (Vendor / Alchemist / Overworld)
///   ├── Body
///   │   ├── ItemList         ScrollView (left 60%)
///   │   ├── DetailLabel      Multi-line TMP (right 40%, top 64%)
///   │   ├── FlashLabel       Single-line success/fail line (right 40%, middle band)
///   │   ├── MixButton        Gold accent (right 40%, bottom-left)
///   │   └── HealButton       Green accent (right 40%, bottom-right) — gold-cost party
///   │                        full-heal (US-122, §29.3 #12 model A — the cut Inn's role)
///   ├── BackButton           Bottom-left, fades to Overworld
///   └── FadeOverlay
/// ```
///
/// RELATED FILES: AlchemistManager.cs, VendorNavBarBuilder.cs, RecipeLibrary.cs
/// </summary>
public static class AlchemistBuilder
{
    private const string SceneName = "Alchemist";
    private const float HeaderH = 96f;

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("AlchemistManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<AlchemistManager>(mgrGO);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        var canvasBg = canvas.GetComponent<Image>();
        if (canvasBg != null) canvasBg.color = HubTheme.PanelBg;

        BuildHeader(canvas, ref created, ref found);
        VendorNavBarBuilder.Build(canvas, topInset: HeaderH + UiKit.SafeAreaTop, anchorLeft: true);
        BuildBody(canvas, ref created, ref found);
        UiKit.BackButton(canvas, "Stage Select");
        created++;

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        var header = UiKit.Header(canvas, "Alchemist");
        created++;

        // GoldLabel — manager finds via "Header/" + GoldLabelName = "Header/GoldLabel".
        UiKit.HeaderRightLabel(header, AlchemistManager.GoldLabelName, "Gold: 0g");
    }

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, UiKit.SafeAreaBottom + 64f + 8f);
        body.offsetMax = new Vector2(-24f, -(HeaderH + UiKit.SafeAreaTop + VendorNavBarBuilder.HeightPx + 8f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) { bodyImg.color = new Color(0f, 0f, 0f, 0f); bodyImg.raycastTarget = false; }

        BuildItemList(body, ref created, ref found);
        BuildDetail(body, ref created, ref found);
        BuildFlash(body, ref created, ref found);
        BuildMixButton(body, ref created, ref found);
        BuildHealButton(body, ref created, ref found);
    }

    private static void BuildItemList(RectTransform body, ref int created, ref int found)
    {
        var itemList = UiKit.ScrollList(body, "ItemList");
        itemList.anchorMin = new Vector2(0f, 0f);
        itemList.anchorMax = new Vector2(0.6f, 1f);
        itemList.offsetMin = new Vector2(0f, 0f);
        itemList.offsetMax = new Vector2(-12f, 0f);
        created++;
    }

    private static void BuildDetail(RectTransform body, ref int created, ref int found)
    {
        var detail = UiKit.Label(body, "DetailLabel", "");
        detail.anchorMin = new Vector2(0.6f, 0.32f);
        detail.anchorMax = new Vector2(1f, 1f);
        detail.offsetMin = new Vector2(12f, 8f);
        detail.offsetMax = new Vector2(0f, -8f);
        var tmp = detail.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
        }
    }

    private static void BuildFlash(RectTransform body, ref int created, ref int found)
    {
        var flash = UiKit.Label(body, "FlashLabel", "");
        flash.anchorMin = new Vector2(0.6f, 0.18f);
        flash.anchorMax = new Vector2(1f, 0.32f);
        flash.offsetMin = new Vector2(12f, 4f);
        flash.offsetMax = new Vector2(0f, -4f);
        var tmp = flash.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
        }
    }

    private static void BuildMixButton(RectTransform body, ref int created, ref int found)
    {
        var btn = UiKit.Button(body, "MixButton", "Mix", UiKit.UiButtonStyle.Primary, 32f);
        btn.anchorMin = new Vector2(0.6f, 0f);
        btn.anchorMax = new Vector2(0.79f, 0.18f);
        btn.offsetMin = new Vector2(12f, 8f);
        btn.offsetMax = new Vector2(-4f, -8f);
        created++;
    }

    private static void BuildHealButton(RectTransform body, ref int created, ref int found)
    {
        // Secondary style (navy base), then override fill → HubTheme.Success and label → black.
        var btn = UiKit.Button(body, "HealButton", "Heal Party", UiKit.UiButtonStyle.Secondary, 24f);
        btn.anchorMin = new Vector2(0.79f, 0f);
        btn.anchorMax = new Vector2(1f, 0.18f);
        btn.offsetMin = new Vector2(4f, 8f);
        btn.offsetMax = new Vector2(0f, -8f);
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = HubTheme.Success;
        var labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (labelTmp != null) { labelTmp.color = Color.black; labelTmp.enableWordWrapping = true; }
        created++;
    }

    // ---------- Primitives (mirrors VendorBuilder helpers) ----------

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
