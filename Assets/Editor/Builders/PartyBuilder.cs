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
/// RELATED FILES: PartyManager.cs, VendorNavBarBuilder.cs, ProfileHelper.cs
/// </summary>
public static class PartyBuilder
{
    private const string SceneName = "Party";
    private const float HeaderH = 96f;

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("PartyManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<PartyManager>(mgrGO);

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
        var header = UiKit.Header(canvas, "Party");
        created++;

        // PartyCountLabel — manager finds via PartyCountLabelName = "Header/PartyCountLabel".
        var countRT = UiKit.HeaderRightLabel(header, PartyManager.PartyCountLabelName.Replace("Header/", ""), "Party: 0/4");
        var ct = countRT.GetComponent<TextMeshProUGUI>();
        if (ct != null) ct.fontSize = 32f;
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

        BuildRosterList(body, ref created, ref found);
        BuildDetail(body, ref created, ref found);
        BuildActionButton(body, ref created, ref found);
        BuildEquipButton(body, ref created, ref found);
        BuildAbilitiesButton(body, ref created, ref found);
    }

    private static void BuildRosterList(RectTransform body, ref int created, ref int found)
    {
        var rosterList = UiKit.ScrollList(body, "RosterList");
        rosterList.anchorMin = new Vector2(0f, 0f);
        rosterList.anchorMax = new Vector2(0.55f, 1f);
        rosterList.offsetMin = new Vector2(0f, 0f);
        rosterList.offsetMax = new Vector2(-12f, 0f);
        created++;
    }

    private static void BuildDetail(RectTransform body, ref int created, ref int found)
    {
        var detail = UiKit.Label(body, "DetailLabel", "");
        detail.anchorMin = new Vector2(0.55f, 0.40f);
        detail.anchorMax = new Vector2(1f, 1f);
        detail.offsetMin = new Vector2(12f, 8f);
        detail.offsetMax = new Vector2(0f, -8f);
        var tmp = detail.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
        }
    }

    private static void BuildActionButton(RectTransform body, ref int created, ref int found)
    {
        var btn = UiKit.Button(body, "ActionButton", "Add to Party", UiKit.UiButtonStyle.Primary, 28f);
        btn.anchorMin = new Vector2(0.55f, 0.27f);
        btn.anchorMax = new Vector2(1f, 0.39f);
        btn.offsetMin = new Vector2(12f, 4f);
        btn.offsetMax = new Vector2(0f, -4f);
        created++;
    }

    private static void BuildEquipButton(RectTransform body, ref int created, ref int found)
    {
        var btn = UiKit.Button(body, "EquipButton", "Equip", UiKit.UiButtonStyle.Secondary, 26f);
        btn.anchorMin = new Vector2(0.55f, 0.14f);
        btn.anchorMax = new Vector2(1f, 0.26f);
        btn.offsetMin = new Vector2(12f, 4f);
        btn.offsetMax = new Vector2(0f, -4f);
        created++;
    }

    private static void BuildAbilitiesButton(RectTransform body, ref int created, ref int found)
    {
        var btn = UiKit.Button(body, "AbilitiesButton", "Abilities", UiKit.UiButtonStyle.Secondary, 26f);
        btn.anchorMin = new Vector2(0.55f, 0f);
        btn.anchorMax = new Vector2(1f, 0.13f);
        btn.offsetMin = new Vector2(12f, 4f);
        btn.offsetMax = new Vector2(0f, -4f);
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
