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
///   ├── Header            "Shop District" title
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
        UiKit.BackButton(canvas, "Stages");
        created++;

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    // ─── Header ──────────────────────────────────────────────────────────────

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        UiKit.Header(canvas, "Shop District");
        created++;
    }

    // ─── 2×3 button grid ─────────────────────────────────────────────────────

    // Button names only — navigation is wired at runtime by HubManager.WireButtons
    // (persistent onClick listeners can't serialize lambdas or plain delegates).
    private static readonly string[] Vendors =
    {
        "Vendor", "Blacksmith", "Alchemist", "Equip", "Party", "Abilities",
    };

    private static void BuildGrid(RectTransform canvas, ref int created, ref int found)
    {
        var grid = FindOrMake(canvas, "ButtonGrid", ref created, ref found);
        grid.anchorMin = new Vector2(0f, 0f);
        grid.anchorMax = new Vector2(1f, 1f);
        grid.offsetMin = new Vector2(GridPad, BackH + GridPad);
        grid.offsetMax = new Vector2(-GridPad, -(HeaderH + GridPad));
        var gridImg = grid.GetComponent<Image>();
        if (gridImg != null) { gridImg.color = new Color(0f, 0f, 0f, 0f); gridImg.raycastTarget = false; }

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

        foreach (var label in Vendors)
            EnsureVendorButton(grid, label, ref created, ref found);
    }

    private static void EnsureVendorButton(RectTransform parent, string label,
        ref int created, ref int found)
    {
        var existing = parent.Find(label);
        if (existing != null) { found++; return; }

        // Use UiKit.Button with Secondary style (navy/white), then re-apply the 42pt
        // Attic Bold display font and bold style the Hub grid uses.
        var rt = UiKit.Button(parent, label, label, UiKit.UiButtonStyle.Secondary, 42f);
        var lbl = rt.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null) lbl.fontStyle = FontStyles.Bold;

        // onClick is wired at runtime by HubManager (lambdas can't persist in scene YAML).
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
}
