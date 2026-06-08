using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Helpers;
using Scripts.Managers;

/// <summary>
/// HUBBUILDER - Builds Hub.unity: a 2×3 grid launcher that routes to all 6 vendor scenes.
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera / EventSystem / HubManagerGO / Canvas
///   ├── Header            "Shop District" title + gold count
///   ├── ButtonGrid        GridLayoutGroup — 6 vendor buttons
///   │   ├── Vendor        → VendorScene
///   │   ├── Blacksmith    → BlacksmithScene
///   │   ├── Alchemist     → AlchemistScene
///   │   ├── Equip         → EquipScene
///   │   ├── Party         → PartyScene
///   │   └── Abilities     → AbilitiesScene
///   ├── BackButton        → StageSelect
///   └── FadeOverlay
/// ```
///
/// SCENE FLOW: StageSelect (Hub button) → Hub → any vendor scene → Hub (back) → StageSelect
/// RELATED FILES: HubManager.cs, SceneHelper.cs, HubTheme.cs
/// US-112
/// </summary>
public static class HubBuilder
{
    private const string SceneName = "Hub";
    private const float HeaderH = 96f;
    private const float BackH   = 64f;
    private const float GridPad = 32f;

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("HubManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<HubManager>(mgrGO);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        var bg = canvas.GetComponent<Image>();
        if (bg != null) bg.color = HubTheme.PanelBg;

        BuildHeader(canvas, ref created, ref found);
        BuildGrid(canvas, ref created, ref found);
        BuildBackButton(canvas, ref created, ref found);

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    // ─── Header ──────────────────────────────────────────────────────────────

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        var header = FindOrMake(canvas, "Header", ref created, ref found);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderH);
        header.anchoredPosition = Vector2.zero;
        Paint(header.gameObject, HubTheme.HeaderBg);

        var title = MakeLabel(header, "Title", "Shop District");
        title.anchorMin = new Vector2(0f, 0.5f);
        title.anchorMax = new Vector2(1f, 0.5f);
        title.pivot = new Vector2(0.5f, 0.5f);
        title.sizeDelta = new Vector2(0f, 72f);
        title.anchoredPosition = Vector2.zero;
        var tt = title.GetComponent<TextMeshProUGUI>();
        if (tt != null)
        {
            tt.fontSize = 52;
            tt.fontStyle = FontStyles.Bold;
            tt.color = HubTheme.Accent;
            tt.alignment = TextAlignmentOptions.Center;
        }
    }

    // ─── 2×3 button grid ─────────────────────────────────────────────────────

    private static readonly (string label, System.Action navigate)[] Vendors =
    {
        ("Vendor",      () => SceneHelper.Fade.ToVendor()),
        ("Blacksmith",  () => SceneHelper.Fade.ToBlacksmith()),
        ("Alchemist",   () => SceneHelper.Fade.ToAlchemist()),
        ("Equip",       () => SceneHelper.Fade.ToEquip()),
        ("Party",       () => SceneHelper.Fade.ToParty()),
        ("Abilities",   () => SceneHelper.Fade.ToAbilities()),
    };

    private static void BuildGrid(RectTransform canvas, ref int created, ref int found)
    {
        var grid = FindOrMake(canvas, "ButtonGrid", ref created, ref found);
        grid.anchorMin = new Vector2(0f, 0f);
        grid.anchorMax = new Vector2(1f, 1f);
        grid.offsetMin = new Vector2(GridPad, BackH + GridPad);
        grid.offsetMax = new Vector2(-GridPad, -(HeaderH + GridPad));
        Paint(grid.gameObject, new Color(0f, 0f, 0f, 0f));

        var glg = grid.gameObject.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = grid.gameObject.AddComponent<GridLayoutGroup>();
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.spacing = new Vector2(20f, 20f);
        glg.padding = new RectOffset(0, 0, 0, 0);
        glg.childAlignment = TextAnchor.MiddleCenter;
        // Cell size is driven by the stretch fill — use a ContentSizeFitter approach:
        // each cell fills a proportional share; since GridLayoutGroup needs fixed cell size,
        // we set cells to (520×280) which fills 1170px-wide portrait with 2 cols + spacing.
        glg.cellSize = new Vector2(520f, 280f);

        var csf = grid.gameObject.GetComponent<ContentSizeFitter>();
        if (csf != null) Object.DestroyImmediate(csf);

        foreach (var (label, navigate) in Vendors)
            EnsureVendorButton(grid, label, navigate, ref created, ref found);
    }

    private static void EnsureVendorButton(RectTransform parent, string label,
        System.Action navigate, ref int created, ref int found)
    {
        var existing = parent.Find(label);
        if (existing != null) { found++; return; }

        var go = new GameObject(label);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HubTheme.NavIdle;
        img.sprite = SceneBuilderHelper.LoadBuiltinSprite("UISprite");
        img.type   = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = new ColorBlock
        {
            normalColor      = HubTheme.NavIdle,
            highlightedColor = HubTheme.NavHover,
            pressedColor     = HubTheme.NavActive,
            selectedColor    = HubTheme.NavIdle,
            disabledColor    = new Color(0.4f, 0.4f, 0.4f, 0.5f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f,
        };
        btn.colors = cb;

        // Wired at build time so it survives scene rebuild without a manager hook.
        SceneBuilderHelper.WireOnClick(btn, navigate.Invoke);

        // Label child.
        var lGO = new GameObject("Label");
        lGO.layer = LayerMask.NameToLayer("UI");
        var lRT = lGO.AddComponent<RectTransform>();
        lRT.SetParent(rt, false);
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(12f, 12f);
        lRT.offsetMax = new Vector2(-12f, -12f);
        lGO.AddComponent<CanvasRenderer>();
        var tmp = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.fontSize = 42;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        created++;
    }

    // ─── Back button ─────────────────────────────────────────────────────────

    private static void BuildBackButton(RectTransform canvas, ref int created, ref int found)
    {
        var existing = canvas.Find("BackButton");
        if (existing != null) { found++; return; }

        var go = new GameObject("BackButton");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(220f, BackH - 8f);
        rt.anchoredPosition = new Vector2(GridPad, GridPad);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HubTheme.NavIdle;
        img.sprite = SceneBuilderHelper.LoadBuiltinSprite("UISprite");
        img.type   = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        SceneBuilderHelper.WireOnClick(btn, () => SceneHelper.Fade.ToStageSelect());

        var lGO = new GameObject("Label");
        lGO.layer = LayerMask.NameToLayer("UI");
        var lRT = lGO.AddComponent<RectTransform>();
        lRT.SetParent(rt, false);
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;
        lGO.AddComponent<CanvasRenderer>();
        var tmp = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "← Stages";
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.fontSize = 32;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        created++;
    }

    // ─── Shared helpers (mirrors StageSelectBuilder pattern) ─────────────────

    private static RectTransform FindOrMake(RectTransform parent, string name,
        ref int created, ref int found)
    {
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        go.AddComponent<Image>();
        created++;
        return rt;
    }

    private static void Paint(GameObject go, Color c)
    {
        var img = go.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private static RectTransform MakeLabel(RectTransform parent, string name, string text)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<RectTransform>();

        var go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.fontSize = 32;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return rt;
    }
}
