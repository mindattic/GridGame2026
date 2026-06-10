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
///   ├── Header                "Select Stage" title
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
        var header = FindOrMake(canvas, "Header", ref created, ref found);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderH);
        header.anchoredPosition = Vector2.zero;
        Paint(header.gameObject, HubTheme.HeaderBg);

        var title = MakeLabel(header, "Title", "Select Stage");
        title.anchorMin = new Vector2(0f, 0.5f); title.anchorMax = new Vector2(0f, 0.5f);
        title.pivot = new Vector2(0f, 0.5f);
        title.sizeDelta = new Vector2(800f, 72f);
        title.anchoredPosition = new Vector2(40f, 0f);
        var tt = title.GetComponent<TextMeshProUGUI>();
        if (tt != null) { tt.fontSize = 48; tt.fontStyle = FontStyles.Bold; tt.color = HubTheme.Accent; tt.alignment = TextAlignmentOptions.MidlineLeft; }

        // US-112: Hub button — top-right of header → Hub vendor launcher.
        var hubBtn = header.Find("HubButton");
        if (hubBtn == null)
        {
            var go = new GameObject("HubButton");
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(header, false);
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 56f);
            rt.anchoredPosition = new Vector2(-20f, 0f);
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = HubTheme.NavActive;
            img.sprite = SceneBuilderHelper.LoadBuiltinSprite("UISprite");
            img.type = Image.Type.Sliced;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var lGO = new GameObject("Label");
            lGO.layer = LayerMask.NameToLayer("UI");
            var lRT = lGO.AddComponent<RectTransform>();
            lRT.SetParent(rt, false);
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = Vector2.zero; lRT.offsetMax = Vector2.zero;
            lGO.AddComponent<CanvasRenderer>();
            var tmp = lGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Shop";
            tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
            tmp.fontSize = 32; tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create HubButton");
            created++;
        }
        else found++;
    }

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, 24f);
        body.offsetMax = new Vector2(-24f, -(HeaderH + VendorNavBarBuilder.HeightPx + 8f));
        Paint(body.gameObject, new Color(0f, 0f, 0f, 0f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) bodyImg.raycastTarget = false;

        BuildStageList(body, ref created, ref found);
        BuildDetailPanel(body, ref created, ref found);
    }

    private static void BuildStageList(RectTransform body, ref int created, ref int found)
    {
        var existing = body.Find("StageList");
        if (existing != null) { found++; return; }

        var rootGO = new GameObject("StageList");
        rootGO.layer = LayerMask.NameToLayer("UI");
        var rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.SetParent(body, false);
        rootRT.anchorMin = new Vector2(0f, 0f);
        rootRT.anchorMax = new Vector2(0.48f, 1f);
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = new Vector2(-12f, 0f);
        rootGO.AddComponent<CanvasRenderer>();
        var rootImg = rootGO.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.35f);
        rootImg.raycastTarget = true;

        var vpGO = new GameObject("Viewport");
        vpGO.layer = LayerMask.NameToLayer("UI");
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.SetParent(rootRT, false);
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vpRT.pivot = new Vector2(0f, 1f);
        vpGO.AddComponent<CanvasRenderer>();
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.sprite = SceneBuilderHelper.LoadBuiltinSprite("UIMask");
        vpImg.type = Image.Type.Sliced;
        vpImg.color = new Color(1f, 1f, 1f, 0.02f);
        vpImg.raycastTarget = true;
        var mask = vpGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var scroll = vpGO.AddComponent<ScrollRect>();

        var contentGO = new GameObject("Content");
        contentGO.layer = LayerMask.NameToLayer("UI");
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.SetParent(vpRT, false);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        contentRT.anchoredPosition = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.spacing = 6f; vlg.padding = new RectOffset(6, 6, 6, 6);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRT;
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;

        UnityEditor.Undo.RegisterCreatedObjectUndo(rootGO, "Create StageList");
        created++;
    }

    private static void BuildDetailPanel(RectTransform body, ref int created, ref int found)
    {
        var existing = body.Find("DetailPanel");
        if (existing != null) { found++; return; }

        var panelGO = new GameObject("DetailPanel");
        panelGO.layer = LayerMask.NameToLayer("UI");
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.SetParent(body, false);
        panelRT.anchorMin = new Vector2(0.50f, 0f);
        panelRT.anchorMax = new Vector2(1f, 1f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        panelGO.AddComponent<CanvasRenderer>();
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.35f);
        panelImg.raycastTarget = false;

        var detail = MakeLabel(panelRT, "DetailLabel", "");
        detail.anchorMin = new Vector2(0f, 0.18f);
        detail.anchorMax = new Vector2(1f, 1f);
        detail.offsetMin = new Vector2(20f, 8f);
        detail.offsetMax = new Vector2(-20f, -16f);
        var dt = detail.GetComponent<TextMeshProUGUI>();
        if (dt != null) { dt.fontSize = 24; dt.color = HubTheme.TextLight; dt.alignment = TextAlignmentOptions.TopLeft; dt.enableWordWrapping = true; dt.richText = true; }

        var cancel = MakeButton(panelRT, "CancelButton", "Cancel");
        cancel.anchorMin = new Vector2(0.05f, 0.04f);
        cancel.anchorMax = new Vector2(0.45f, 0.16f);
        cancel.offsetMin = Vector2.zero; cancel.offsetMax = Vector2.zero;
        var cancelImg = cancel.GetComponent<Image>();
        if (cancelImg != null) cancelImg.color = HubTheme.NavIdle;

        var confirm = MakeButton(panelRT, "ConfirmButton", "Confirm");
        confirm.anchorMin = new Vector2(0.55f, 0.04f);
        confirm.anchorMax = new Vector2(0.95f, 0.16f);
        confirm.offsetMin = Vector2.zero; confirm.offsetMax = Vector2.zero;
        var confirmImg = confirm.GetComponent<Image>();
        if (confirmImg != null) confirmImg.color = HubTheme.Accent;
        var confirmLbl = confirm.GetComponentInChildren<TextMeshProUGUI>();
        if (confirmLbl != null) confirmLbl.color = Color.black;

        UnityEditor.Undo.RegisterCreatedObjectUndo(panelGO, "Create DetailPanel");
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
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }

    private static void Paint(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
    }

    private static RectTransform MakeLabel(RectTransform parent, string name, string text)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing as RectTransform;
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        tmp.raycastTarget = false;
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return rt;
    }

    private static RectTransform MakeButton(RectTransform parent, string name, string label)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing as RectTransform;
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

        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return rt;
    }
}
