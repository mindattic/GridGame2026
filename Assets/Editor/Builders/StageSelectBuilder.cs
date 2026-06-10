using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Managers;
using Scripts.Vendor;

/// <summary>
/// STAGESELECTSCAFFOLD - Editor tool that builds the StageSelect scene from code.
///
/// SCENE HIERARCHY: Top-level campaign gateway — left list, right detail panel.
/// ```
/// Main Camera / EventSystem / StageSelectManagerGO / Canvas
///   ├── Header                "Select Stage" title + HubButton (top-right)
///   ├── VendorNavBar          Persistent strip linking to vendor scenes
///   ├── Body
///   │   ├── StageList         ScrollView (left 48%)
///   │   │   └── Viewport / Content (rows added at runtime)
///   │   └── DetailPanel       Selected stage preview (right 50%)
///   │       ├── DetailLabel   Wave / enemy composition (top region)
///   │       ├── ConfirmButton Gold accent (bottom right)
///   │       └── CancelButton  Bottom left
///   └── FadeOverlay
/// ```
///
/// SCENE FLOW: any vendor / PostBattleScreen → StageSelect → Game
/// RELATED FILES: StageSelectManager.cs, CampaignStages.cs, VendorNavBarBuilder.cs
/// </summary>
public static class StageSelectBuilder
{
    private const string SceneName = "StageSelect";
    private const float HeaderH = 96f;

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("StageSelectManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<StageSelectManager>(mgrGO);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        var canvasBg = canvas.GetComponent<Image>();
        if (canvasBg != null) canvasBg.color = HubTheme.PanelBg;

        BuildHeader(canvas, ref created, ref found);
        VendorNavBarBuilder.Build(canvas, topInset: HeaderH, anchorLeft: true);
        BuildBody(canvas, ref created, ref found);

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        var header = UiKit.Header(canvas, "Select Stage");
        created++;

        // US-112: Hub button — top-right of header → Hub vendor launcher.
        var hubBtn = UiKit.Button(header, "HubButton", "Shop", UiKit.UiButtonStyle.Primary, 26f);
        hubBtn.anchorMin = new Vector2(1f, 0.5f);
        hubBtn.anchorMax = new Vector2(1f, 0.5f);
        hubBtn.pivot = new Vector2(1f, 0.5f);
        hubBtn.sizeDelta = new Vector2(200f, 56f);
        hubBtn.anchoredPosition = new Vector2(-20f, 0f);
    }

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, 24f);
        body.offsetMax = new Vector2(-24f, -(HeaderH + VendorNavBarBuilder.HeightPx + 8f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) { bodyImg.color = new Color(0f, 0f, 0f, 0f); bodyImg.raycastTarget = false; }

        BuildStageList(body, ref created, ref found);
        BuildDetailPanel(body, ref created, ref found);
    }

    private static void BuildStageList(RectTransform body, ref int created, ref int found)
    {
        var stageList = UiKit.ScrollList(body, "StageList");
        stageList.anchorMin = new Vector2(0f, 0f);
        stageList.anchorMax = new Vector2(0.48f, 1f);
        stageList.offsetMin = Vector2.zero;
        stageList.offsetMax = new Vector2(-12f, 0f);
        created++;
    }

    private static void BuildDetailPanel(RectTransform body, ref int created, ref int found)
    {
        // UiKit.Panel gives PanelBg fill + border frame.
        var panelRT = UiKit.Panel(body, "DetailPanel");
        panelRT.anchorMin = new Vector2(0.50f, 0f);
        panelRT.anchorMax = new Vector2(1f, 1f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        created++;

        var detail = UiKit.Label(panelRT, "DetailLabel", "");
        detail.anchorMin = new Vector2(0f, 0.18f);
        detail.anchorMax = new Vector2(1f, 1f);
        detail.offsetMin = new Vector2(20f, 8f);
        detail.offsetMax = new Vector2(-20f, -16f);
        var dt = detail.GetComponent<TextMeshProUGUI>();
        if (dt != null) { dt.fontSize = 24; dt.alignment = TextAlignmentOptions.TopLeft; dt.enableWordWrapping = true; dt.richText = true; }

        var cancel = UiKit.Button(panelRT, "CancelButton", "Cancel", UiKit.UiButtonStyle.Secondary, 28f);
        cancel.anchorMin = new Vector2(0.05f, 0.04f);
        cancel.anchorMax = new Vector2(0.45f, 0.16f);
        cancel.offsetMin = Vector2.zero; cancel.offsetMax = Vector2.zero;

        var confirm = UiKit.Button(panelRT, "ConfirmButton", "Confirm", UiKit.UiButtonStyle.Primary, 28f);
        confirm.anchorMin = new Vector2(0.55f, 0.04f);
        confirm.anchorMax = new Vector2(0.95f, 0.16f);
        confirm.offsetMin = Vector2.zero; confirm.offsetMax = Vector2.zero;
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
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }
}
