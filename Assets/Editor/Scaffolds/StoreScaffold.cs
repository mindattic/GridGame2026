using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor.Store;

/// <summary>
/// STORESCAFFOLD - Editor tool that builds the Store scene from code.
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera
/// EventSystem
/// StoreManagerGO              StoreManager script owner
/// Canvas                      ScreenSpaceOverlay + dark background
///   ├── Header                "Store" title (left) + GoldLabel (right)
///   ├── Body
///   │   ├── ItemList          ScrollView with Viewport/Content (left 60%)
///   │   ├── DetailLabel       TMP label (right 40%, top)
///   │   └── BuyButton         Gold accent (right 40%, bottom)
///   ├── BackButton            Bottom-left, fades to Overworld
///   └── FadeOverlay
/// ```
///
/// RELATED FILES: StoreManager.cs, HubTheme.cs, SceneScaffoldHelper.cs
/// </summary>
public static class StoreScaffold
{
    private const string SceneName = "Store";

    [MenuItem("Tools/Scenes/Store/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Store scene and recreate all GameObjects from the scaffold?\n\nAny unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }

    [MenuItem("Tools/Scenes/Store/Clear Scene")]
    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneScaffoldHelper.EnsureEmptyGameObject("StoreManagerGO", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<StoreManager>(mgrGO);

        var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneScaffoldHelper.LogResults(SceneName, created, found); return; }

        var canvasBg = canvas.GetComponent<Image>();
        if (canvasBg != null) canvasBg.color = HubTheme.PanelBg;

        BuildHeader(canvas, ref created, ref found);
        VendorNavBarScaffold.Build(canvas, topInset: HeaderH);
        BuildBody(canvas, ref created, ref found);
        BuildBackButton(canvas, ref created, ref found);

        SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    private const float HeaderH = 96f;

    // ---------- Header ----------

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        var header = FindOrMake(canvas, "Header", ref created, ref found);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 96f);
        header.anchoredPosition = Vector2.zero;
        Paint(header.gameObject, HubTheme.HeaderBg);

        var title = MakeLabel(header, "Title", "Store");
        title.anchorMin = new Vector2(0f, 0.5f); title.anchorMax = new Vector2(0f, 0.5f);
        title.pivot = new Vector2(0f, 0.5f);
        title.sizeDelta = new Vector2(400f, 72f);
        title.anchoredPosition = new Vector2(40f, 0f);
        var titleTmp = title.GetComponent<TextMeshProUGUI>();
        if (titleTmp != null)
        {
            titleTmp.fontSize = 48;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = HubTheme.Accent;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        var gold = MakeLabel(header, StoreManager.GoldLabelName, "Gold: 0g");
        gold.anchorMin = new Vector2(1f, 0.5f); gold.anchorMax = new Vector2(1f, 0.5f);
        gold.pivot = new Vector2(1f, 0.5f);
        gold.sizeDelta = new Vector2(400f, 60f);
        gold.anchoredPosition = new Vector2(-40f, 0f);
        var goldTmp = gold.GetComponent<TextMeshProUGUI>();
        if (goldTmp != null)
        {
            goldTmp.fontSize = 36;
            goldTmp.color = HubTheme.Accent;
            goldTmp.alignment = TextAlignmentOptions.MidlineRight;
        }
    }

    // ---------- Body (item list + detail + buy) ----------

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, 96f);   // leave room for BackButton at bottom
        // Header (96) + nav bar (56) + 8px gutter at top.
        body.offsetMax = new Vector2(-24f, -(HeaderH + VendorNavBarScaffold.HeightPx + 8f));
        Paint(body.gameObject, new Color(0f, 0f, 0f, 0f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) bodyImg.raycastTarget = false;

        BuildItemList(body, ref created, ref found);
        BuildDetail(body, ref created, ref found);
        BuildBuyButton(body, ref created, ref found);
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

        // Viewport (Mask + ScrollRect)
        var vpGO = new GameObject("Viewport");
        vpGO.layer = LayerMask.NameToLayer("UI");
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.SetParent(rootRT, false);
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vpRT.pivot = new Vector2(0f, 1f);
        vpGO.AddComponent<CanvasRenderer>();
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.sprite = SceneScaffoldHelper.LoadBuiltinSprite("UIMask");
        vpImg.type = Image.Type.Sliced;
        vpImg.color = new Color(1f, 1f, 1f, 0.02f);
        vpImg.raycastTarget = true;
        var mask = vpGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var scroll = vpGO.AddComponent<ScrollRect>();

        // Content (vertical layout)
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
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
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
        detail.anchorMin = new Vector2(0.6f, 0.18f);
        detail.anchorMax = new Vector2(1f, 1f);
        detail.offsetMin = new Vector2(12f, 8f);
        detail.offsetMax = new Vector2(0f, -8f);
        var tmp = detail.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 24;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
        }
    }

    private static void BuildBuyButton(RectTransform body, ref int created, ref int found)
    {
        var btn = MakeButton(body, "BuyButton", "Buy");
        btn.anchorMin = new Vector2(0.6f, 0f);
        btn.anchorMax = new Vector2(1f, 0.18f);
        btn.offsetMin = new Vector2(12f, 8f);
        btn.offsetMax = new Vector2(0f, -8f);
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = HubTheme.Accent;
        var labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (labelTmp != null)
        {
            labelTmp.fontSize = 32;
            labelTmp.color = Color.black;
        }
    }

    // ---------- Back button ----------

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
        tmp.font = SceneScaffoldHelper.LoadFont(SceneScaffoldHelper.FontPaths.Attic);
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
        tmp.font = SceneScaffoldHelper.LoadFont(SceneScaffoldHelper.FontPaths.Attic);
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
