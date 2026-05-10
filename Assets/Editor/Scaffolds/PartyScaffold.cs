using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor.Party;

/// <summary>
/// PARTYSCAFFOLD - Editor tool that builds the Party scene from code.
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera
/// EventSystem
/// PartyManagerGO              PartyManager (Scripts.Vendor.Party) script owner
/// Canvas                      ScreenSpaceOverlay + dark background
///   ├── Header                "Party" title (left) + PartyCountLabel (right)
///   ├── VendorNavBar          Shared nav strip
///   ├── Body
///   │   ├── RosterList        ScrollView of every roster member (left 55%)
///   │   ├── DetailLabel       Selected hero's stats (right 45%, top 60%)
///   │   ├── ActionButton      Add/Remove from party (right, mid)
///   │   ├── EquipButton       Routes to Equip scene (right, lower mid) — disabled until slice 5
///   │   └── AbilitiesButton   Routes to Abilities scene (right, lower) — disabled until slice 4
///   ├── BackButton            Bottom-left, fades to Overworld
///   └── FadeOverlay
/// ```
///
/// RELATED FILES: PartyManager.cs, VendorNavBarScaffold.cs, ProfileHelper.cs
/// </summary>
public static class PartyScaffold
{
    private const string SceneName = "Party";
    private const float HeaderH = 96f;

    [MenuItem("Tools/Scenes/Party/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Party scene and recreate all GameObjects from the scaffold?\n\nAny unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }

    [MenuItem("Tools/Scenes/Party/Clear Scene")]
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

        var mgrGO = SceneScaffoldHelper.EnsureEmptyGameObject("PartyManagerGO", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<PartyManager>(mgrGO);

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

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        var header = FindOrMake(canvas, "Header", ref created, ref found);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderH);
        header.anchoredPosition = Vector2.zero;
        Paint(header.gameObject, HubTheme.HeaderBg);

        var title = MakeLabel(header, "Title", "Party");
        title.anchorMin = new Vector2(0f, 0.5f); title.anchorMax = new Vector2(0f, 0.5f);
        title.pivot = new Vector2(0f, 0.5f);
        title.sizeDelta = new Vector2(500f, 72f);
        title.anchoredPosition = new Vector2(40f, 0f);
        var tt = title.GetComponent<TextMeshProUGUI>();
        if (tt != null) { tt.fontSize = 48; tt.fontStyle = FontStyles.Bold; tt.color = HubTheme.Accent; tt.alignment = TextAlignmentOptions.MidlineLeft; }

        var count = MakeLabel(header, PartyManager.PartyCountLabelName.Replace("Header/", ""), "Party: 0/4");
        count.anchorMin = new Vector2(1f, 0.5f); count.anchorMax = new Vector2(1f, 0.5f);
        count.pivot = new Vector2(1f, 0.5f);
        count.sizeDelta = new Vector2(400f, 60f);
        count.anchoredPosition = new Vector2(-40f, 0f);
        var ct = count.GetComponent<TextMeshProUGUI>();
        if (ct != null) { ct.fontSize = 32; ct.color = HubTheme.Accent; ct.alignment = TextAlignmentOptions.MidlineRight; }
    }

    private static void BuildBody(RectTransform canvas, ref int created, ref int found)
    {
        var body = FindOrMake(canvas, "Body", ref created, ref found);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(24f, 96f);
        body.offsetMax = new Vector2(-24f, -(HeaderH + VendorNavBarScaffold.HeightPx + 8f));
        Paint(body.gameObject, new Color(0f, 0f, 0f, 0f));
        var bodyImg = body.GetComponent<Image>();
        if (bodyImg != null) bodyImg.raycastTarget = false;

        BuildRosterList(body, ref created, ref found);
        BuildDetail(body, ref created, ref found);
        BuildActionButton(body, ref created, ref found);
        BuildEquipButton(body, ref created, ref found);
        BuildAbilitiesButton(body, ref created, ref found);
    }

    private static void BuildRosterList(RectTransform body, ref int created, ref int found)
    {
        var existing = body.Find("RosterList");
        if (existing != null) { found++; return; }

        var rootGO = new GameObject("RosterList");
        rootGO.layer = LayerMask.NameToLayer("UI");
        var rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.SetParent(body, false);
        rootRT.anchorMin = new Vector2(0f, 0f);
        rootRT.anchorMax = new Vector2(0.55f, 1f);
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
        vpImg.sprite = SceneScaffoldHelper.LoadBuiltinSprite("UIMask");
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

        Undo.RegisterCreatedObjectUndo(rootGO, "Create RosterList");
        created++;
    }

    private static void BuildDetail(RectTransform body, ref int created, ref int found)
    {
        var detail = MakeLabel(body, "DetailLabel", "");
        detail.anchorMin = new Vector2(0.55f, 0.40f);
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

    private static void BuildActionButton(RectTransform body, ref int created, ref int found)
    {
        var btn = MakeButton(body, "ActionButton", "Add to Party");
        btn.anchorMin = new Vector2(0.55f, 0.27f);
        btn.anchorMax = new Vector2(1f, 0.39f);
        btn.offsetMin = new Vector2(12f, 4f);
        btn.offsetMax = new Vector2(0f, -4f);
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = HubTheme.Accent;
        var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null) { lbl.fontSize = 28; lbl.color = Color.black; }
    }

    private static void BuildEquipButton(RectTransform body, ref int created, ref int found)
    {
        var btn = MakeButton(body, "EquipButton", "Equip");
        btn.anchorMin = new Vector2(0.55f, 0.14f);
        btn.anchorMax = new Vector2(1f, 0.26f);
        btn.offsetMin = new Vector2(12f, 4f);
        btn.offsetMax = new Vector2(0f, -4f);
    }

    private static void BuildAbilitiesButton(RectTransform body, ref int created, ref int found)
    {
        var btn = MakeButton(body, "AbilitiesButton", "Abilities");
        btn.anchorMin = new Vector2(0.55f, 0f);
        btn.anchorMax = new Vector2(1f, 0.13f);
        btn.offsetMin = new Vector2(12f, 4f);
        btn.offsetMax = new Vector2(0f, -4f);
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
