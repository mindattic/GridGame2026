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
/// RELATED FILES: AbilitiesManager.cs, VendorNavBarScaffold.cs, HeroEquipmentSave
/// </summary>
public static class AbilitiesScaffold
{
    private const string SceneName = "Abilities";
    private const float HeaderH = 96f;

    [MenuItem("Tools/Scenes/Abilities/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Abilities scene and recreate all GameObjects from the scaffold?\n\nAny unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }

    [MenuItem("Tools/Scenes/Abilities/Clear Scene")]
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

        var mgrGO = SceneScaffoldHelper.EnsureEmptyGameObject("AbilitiesManagerGO", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<AbilitiesManager>(mgrGO);

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

        var title = MakeLabel(header, "Title", "Abilities");
        title.anchorMin = new Vector2(0f, 0.5f); title.anchorMax = new Vector2(1f, 0.5f);
        title.pivot = new Vector2(0.5f, 0.5f);
        title.sizeDelta = new Vector2(0f, 72f);
        title.offsetMin = new Vector2(40f, -36f); title.offsetMax = new Vector2(-40f, 36f);
        title.anchoredPosition = Vector2.zero;
        var tt = title.GetComponent<TextMeshProUGUI>();
        if (tt != null) { tt.fontSize = 44; tt.fontStyle = FontStyles.Bold; tt.color = HubTheme.Accent; tt.alignment = TextAlignmentOptions.MidlineLeft; }
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
        Paint(row.gameObject, new Color(0f, 0f, 0f, 0.15f));
        var rowImg = row.GetComponent<Image>();
        if (rowImg != null) rowImg.raycastTarget = false;

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
        var flash = MakeLabel(body, "FlashLabel", "");
        flash.anchorMin = new Vector2(0f, 0.55f);
        flash.anchorMax = new Vector2(1f, 0.65f);
        flash.offsetMin = new Vector2(12f, 4f);
        flash.offsetMax = new Vector2(-12f, -4f);
        var tmp = flash.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 22;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
        }
    }

    private static void BuildConsumablesList(RectTransform body, ref int created, ref int found)
    {
        var existing = body.Find("ConsumablesList");
        if (existing != null) { found++; return; }

        var rootGO = new GameObject("ConsumablesList");
        rootGO.layer = LayerMask.NameToLayer("UI");
        var rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.SetParent(body, false);
        rootRT.anchorMin = new Vector2(0f, 0f);
        rootRT.anchorMax = new Vector2(1f, 0.55f);
        rootRT.offsetMin = new Vector2(0f, 0f);
        rootRT.offsetMax = new Vector2(0f, -8f);
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

        Undo.RegisterCreatedObjectUndo(rootGO, "Create ConsumablesList");
        created++;
    }

    private static void BuildBackButton(RectTransform canvas, ref int created, ref int found)
    {
        var btn = MakeButton(canvas, "BackButton", "← Party");
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
