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
        VendorNavBarBuilder.Build(canvas, topInset: HeaderH, anchorLeft: true);
        BuildBody(canvas, ref created, ref found);
        BuildBackButton(canvas, ref created, ref found);

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

        var title = MakeLabel(header, "Title", "Alchemist");
        title.anchorMin = new Vector2(0f, 0.5f); title.anchorMax = new Vector2(0f, 0.5f);
        title.pivot = new Vector2(0f, 0.5f);
        title.sizeDelta = new Vector2(500f, 72f);
        title.anchoredPosition = new Vector2(40f, 0f);
        var tt = title.GetComponent<TextMeshProUGUI>();
        if (tt != null) { tt.fontSize = 48; tt.fontStyle = FontStyles.Bold; tt.color = HubTheme.Accent; tt.alignment = TextAlignmentOptions.MidlineLeft; }

        var gold = MakeLabel(header, AlchemistManager.GoldLabelName, "Gold: 0g");
        gold.anchorMin = new Vector2(1f, 0.5f); gold.anchorMax = new Vector2(1f, 0.5f);
        gold.pivot = new Vector2(1f, 0.5f);
        gold.sizeDelta = new Vector2(400f, 60f);
        gold.anchoredPosition = new Vector2(-40f, 0f);
        var gt = gold.GetComponent<TextMeshProUGUI>();
        if (gt != null) { gt.fontSize = 36; gt.color = HubTheme.Accent; gt.alignment = TextAlignmentOptions.MidlineRight; }
    }

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, 96f);
        // Header (96) + nav bar (56) = 152 of top inset.
        body.offsetMax = new Vector2(-24f, -(HeaderH + VendorNavBarBuilder.HeightPx + 8f));
        Paint(body.gameObject, new Color(0f, 0f, 0f, 0f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) bodyImg.raycastTarget = false;

        BuildItemList(body, ref created, ref found);
        BuildDetail(body, ref created, ref found);
        BuildFlash(body, ref created, ref found);
        BuildMixButton(body, ref created, ref found);
        BuildHealButton(body, ref created, ref found);
    }

    private static void BuildItemList(RectTransform body, ref int created, ref int found)
    {
        var existing = body.Find("ItemList");
        if (existing != null) { found++; return; }

        var rootGO = new GameObject("ItemList");
        rootGO.layer = LayerMask.NameToLayer("UI");
        var rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.SetParent(body, false);
        rootRT.anchorMin = new Vector2(0f, 0f);
        rootRT.anchorMax = new Vector2(0.6f, 1f);
        rootRT.offsetMin = new Vector2(0f, 0f);
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
        vlg.spacing = 4f; vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRT;
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;

        Undo.RegisterCreatedObjectUndo(rootGO, "Create ItemList");
        created++;
    }

    private static void BuildDetail(RectTransform body, ref int created, ref int found)
    {
        var detail = MakeLabel(body, "DetailLabel", "");
        detail.anchorMin = new Vector2(0.6f, 0.32f);
        detail.anchorMax = new Vector2(1f, 1f);
        detail.offsetMin = new Vector2(12f, 8f);
        detail.offsetMax = new Vector2(0f, -8f);
        var tmp = detail.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 22;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
        }
    }

    private static void BuildFlash(RectTransform body, ref int created, ref int found)
    {
        var flash = MakeLabel(body, "FlashLabel", "");
        flash.anchorMin = new Vector2(0.6f, 0.18f);
        flash.anchorMax = new Vector2(1f, 0.32f);
        flash.offsetMin = new Vector2(12f, 4f);
        flash.offsetMax = new Vector2(0f, -4f);
        var tmp = flash.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 26;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
        }
    }

    private static void BuildMixButton(RectTransform body, ref int created, ref int found)
    {
        var btn = MakeButton(body, "MixButton", "Mix");
        btn.anchorMin = new Vector2(0.6f, 0f);
        btn.anchorMax = new Vector2(0.79f, 0.18f);
        btn.offsetMin = new Vector2(12f, 8f);
        btn.offsetMax = new Vector2(-4f, -8f);
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = HubTheme.Accent;
        var labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (labelTmp != null) { labelTmp.fontSize = 32; labelTmp.color = Color.black; }
    }

    private static void BuildHealButton(RectTransform body, ref int created, ref int found)
    {
        var btn = MakeButton(body, "HealButton", "Heal Party");
        btn.anchorMin = new Vector2(0.79f, 0f);
        btn.anchorMax = new Vector2(1f, 0.18f);
        btn.offsetMin = new Vector2(4f, 8f);
        btn.offsetMax = new Vector2(0f, -8f);
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = new Color(0.40f, 0.80f, 0.53f, 1f); // heal green
        var labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (labelTmp != null) { labelTmp.fontSize = 24; labelTmp.color = Color.black; labelTmp.enableWordWrapping = true; }
    }

    private static void BuildBackButton(RectTransform canvas, ref int created, ref int found)
    {
        var btn = MakeButton(canvas, "BackButton", "← Overworld");
        btn.anchorMin = new Vector2(0f, 0f);
        btn.anchorMax = new Vector2(0f, 0f);
        btn.pivot = new Vector2(0f, 0f);
        btn.sizeDelta = new Vector2(220f, 64f);
        btn.anchoredPosition = new Vector2(24f, 24f);
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = HubTheme.NavIdle;
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
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
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
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1.15f, 1.15f, 1.20f, 1f),
            pressedColor = new Color(0.65f, 0.65f, 0.80f, 1f),
            selectedColor = new Color(1.00f, 1.00f, 1.10f, 1f),
            disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.60f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

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
        tmp.fontSize = 26;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return rt;
    }
}
