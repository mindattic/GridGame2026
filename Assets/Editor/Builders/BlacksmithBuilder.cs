using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Vendor.Blacksmith;

/// <summary>
/// BLACKSMITHSCAFFOLD - Editor tool that builds the Blacksmith scene from code.
///
/// SCENE HIERARCHY: Forge / Salvage / Repair tabs over the left list (slice 9 + US-121).
/// ```
/// Main Camera / EventSystem / BlacksmithManagerGO / Canvas
///   ├── Header                Title + GoldLabel
///   ├── VendorNavBar
///   ├── Body
///   │   ├── ForgeTab          Mode tab (top-left)
///   │   ├── SalvageTab        Mode tab (top-left, right of Forge)
///   │   ├── RepairTab         Mode tab (top-left, right of Salvage)
///   │   ├── ItemList          Recipes (Forge) / Equipment (Salvage) / Worn gear (Repair), left 60%
///   │   ├── DetailLabel       Selected row preview (right, top)
///   │   ├── FlashLabel        Action result line (right, mid)
///   │   └── ForgeButton       Gold accent action button — label flips Forge / Salvage / Repair at runtime
///   ├── BackButton            ← Overworld
///   └── FadeOverlay
/// ```
///
/// RELATED FILES: BlacksmithManager.cs, AlchemistBuilder.cs (parallel)
/// </summary>
public static class BlacksmithBuilder
{
    private const string SceneName = "Blacksmith";
    private const float HeaderH = 96f;

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("BlacksmithManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<BlacksmithManager>(mgrGO);

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
        var header = UiKit.Header(canvas, "Blacksmith");
        created++;

        // GoldLabel — manager finds via "Header/" + GoldLabelName = "Header/GoldLabel".
        UiKit.HeaderRightLabel(header, BlacksmithManager.GoldLabelName, "Gold: 0g");
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

        BuildModeTabs(body, ref created, ref found);
        BuildItemList(body, ref created, ref found);
        BuildDetail(body, ref created, ref found);
        BuildFlash(body, ref created, ref found);
        BuildForgeButton(body, ref created, ref found);
    }

    private static void BuildModeTabs(RectTransform body, ref int created, ref int found)
    {
        var forge = UiKit.Button(body, "ForgeTab", "Forge", UiKit.UiButtonStyle.Tab, 24f);
        forge.anchorMin = new Vector2(0f,    0.92f);
        forge.anchorMax = new Vector2(0.30f, 1f);
        forge.offsetMin = Vector2.zero; forge.offsetMax = new Vector2(-4f, 0f);
        var fLbl = forge.GetComponentInChildren<TextMeshProUGUI>();
        if (fLbl != null) fLbl.fontStyle = FontStyles.Bold;
        created++;

        var salvage = UiKit.Button(body, "SalvageTab", "Salvage", UiKit.UiButtonStyle.Tab, 24f);
        salvage.anchorMin = new Vector2(0.30f, 0.92f);
        salvage.anchorMax = new Vector2(0.60f, 1f);
        salvage.offsetMin = new Vector2(4f, 0f); salvage.offsetMax = new Vector2(-4f, 0f);
        var sLbl = salvage.GetComponentInChildren<TextMeshProUGUI>();
        if (sLbl != null) sLbl.fontStyle = FontStyles.Bold;
        created++;

        var repair = UiKit.Button(body, "RepairTab", "Repair", UiKit.UiButtonStyle.Tab, 24f);
        repair.anchorMin = new Vector2(0.60f, 0.92f);
        repair.anchorMax = new Vector2(0.90f, 1f);
        repair.offsetMin = new Vector2(4f, 0f); repair.offsetMax = new Vector2(-12f, 0f);
        var rLbl = repair.GetComponentInChildren<TextMeshProUGUI>();
        if (rLbl != null) rLbl.fontStyle = FontStyles.Bold;
        created++;
    }

    private static void BuildItemList(RectTransform body, ref int created, ref int found)
    {
        var itemList = UiKit.ScrollList(body, "ItemList");
        itemList.anchorMin = new Vector2(0f, 0f);
        itemList.anchorMax = new Vector2(0.6f, 0.92f);
        itemList.offsetMin = new Vector2(0f, 0f);
        itemList.offsetMax = new Vector2(-12f, -4f);
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
        if (tmp != null) { tmp.fontSize = 22; tmp.alignment = TextAlignmentOptions.TopLeft; tmp.enableWordWrapping = true; tmp.richText = true; }
    }

    private static void BuildFlash(RectTransform body, ref int created, ref int found)
    {
        var flash = UiKit.Label(body, "FlashLabel", "");
        flash.anchorMin = new Vector2(0.6f, 0.18f);
        flash.anchorMax = new Vector2(1f, 0.32f);
        flash.offsetMin = new Vector2(12f, 4f);
        flash.offsetMax = new Vector2(0f, -4f);
        var tmp = flash.GetComponent<TextMeshProUGUI>();
        if (tmp != null) { tmp.fontSize = 26; tmp.alignment = TextAlignmentOptions.Center; tmp.enableWordWrapping = false; tmp.richText = true; }
    }

    private static void BuildForgeButton(RectTransform body, ref int created, ref int found)
    {
        var btn = UiKit.Button(body, "ForgeButton", "Forge", UiKit.UiButtonStyle.Primary, 32f);
        btn.anchorMin = new Vector2(0.6f, 0f);
        btn.anchorMax = new Vector2(1f, 0.18f);
        btn.offsetMin = new Vector2(12f, 8f);
        btn.offsetMax = new Vector2(0f, -8f);
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
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }
}
