using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor.Summon;

/// <summary>
/// SUMMONSCAFFOLD - Editor tool that builds the Summon Circle scene from code (US-132 / GG-A5).
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera / EventSystem / SummonManagerGO / Canvas
///   ├── Header                "Summon Circle" title
///   ├── VendorNavBar          Persistent strip linking to vendor scenes
///   ├── Body
///   │   ├── SummonList        ScrollView (full width) — class rows added at runtime
///   │   ├── GoldLabel         Live gold + next-summon cost (bottom strip)
///   │   └── FlashLabel        Action feedback line
///   └── FadeOverlay
/// ```
///
/// SCENE FLOW: any vendor / StageSelect → Summon → (recruit) → Party shows the new hero
/// RELATED FILES: SummonManager.cs, SummonService.cs, VendorNavBarBuilder.cs
/// </summary>
public static class SummonBuilder
{
    private const string SceneName = "Summon";
    private const float HeaderH = 96f;
    private const float FooterH = 120f;

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("SummonManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<SummonManager>(mgrGO);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        var canvasBg = canvas.GetComponent<Image>();
        if (canvasBg != null) canvasBg.color = HubTheme.PanelBg;

        UiKit.Header(canvas, "Summon Circle");
        created++;
        VendorNavBarBuilder.Build(canvas, topInset: HeaderH + UiKit.SafeAreaTop, anchorLeft: true);
        BuildBody(canvas, ref created, ref found);

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, UiKit.SafeAreaBottom + 8f);
        body.offsetMax = new Vector2(-24f, -(HeaderH + UiKit.SafeAreaTop + VendorNavBarBuilder.HeightPx + 8f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) { bodyImg.color = new Color(0f, 0f, 0f, 0f); bodyImg.raycastTarget = false; }

        var list = UiKit.ScrollList(body, "SummonList");
        list.anchorMin = new Vector2(0f, 0f);
        list.anchorMax = new Vector2(1f, 1f);
        list.offsetMin = new Vector2(0f, FooterH);
        list.offsetMax = Vector2.zero;
        created++;

        var gold = UiKit.Label(body, "GoldLabel", "", 26f);
        gold.anchorMin = new Vector2(0f, 0f);
        gold.anchorMax = new Vector2(1f, 0f);
        gold.pivot = new Vector2(0.5f, 0f);
        gold.offsetMin = new Vector2(8f, 58f);
        gold.offsetMax = new Vector2(-8f, 58f + 44f);

        var flash = UiKit.Label(body, "FlashLabel", "", 24f);
        flash.anchorMin = new Vector2(0f, 0f);
        flash.anchorMax = new Vector2(1f, 0f);
        flash.pivot = new Vector2(0.5f, 0f);
        flash.offsetMin = new Vector2(8f, 8f);
        flash.offsetMax = new Vector2(-8f, 8f + 40f);
    }

    // ---------- Primitives (same shape as StageSelectBuilder's) ----------

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
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }
}
