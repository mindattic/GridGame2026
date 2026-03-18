using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;

/// <summary>
/// HUBSCAFFOLD - Editor tool to scaffold the Hub scene with a JRPG-style layout.
///
/// PURPOSE:
/// Builds every GameObject, component, and layout property needed for the Hub
/// scene — inspired by Final Fantasy XIV / Tactics menus: dark navy panels,
/// gold accent text, scrollable list views, and clearly separated header/footer
/// strips. All 9 sections are created (Party, Shop, Medical, Residence, Blacksmith,
/// Training, Equip, Inventory, BattlePrep) plus the full navigation bar.
///
/// LAYOUT:
/// ```
/// ┌──────────────────────────────────────────────────────────────────────────────┐
/// │  NavBar (70px)  Party│Shop│Medical│Residence│Blacksmith│…   Overworld│Battle │
/// ├──────────────────────────────────────────────────────────────────────────────┤
/// │  ContentPanel (fills remaining height)                                       │
/// │  ┌────────────────────────────────────────────────────────────────────────┐  │
/// │  │  [Section Panel — fills ContentPanel]                                  │  │
/// │  │  HeaderBg strip (58px)  ←─ VendorFactory paints title here             │  │
/// │  │  ──────────────────────────────────────────────────────────────────    │  │
/// │  │  [Tab row 46px, only for Shop/Blacksmith/Inventory]                    │  │
/// │  │  ┌───────────────────────────┬───────────────────────────────────────┐ │  │
/// │  │  │  ScrollView               │  ScrollView (two-column panels)       │ │  │
/// │  │  │  (full-width or left col) │  right column                         │ │  │
/// │  │  └───────────────────────────┴───────────────────────────────────────┘ │  │
/// │  │  FooterBg strip (82px)  DetailLabel                    GoldLabel       │  │
/// │  └────────────────────────────────────────────────────────────────────────┘  │
/// └──────────────────────────────────────────────────────────────────────────────┘
/// ```
///
/// SCROLL PATH CONVENTION:
/// List containers are nested inside scroll views. Controllers must use paths:
///   rt.Find("RosterScrollView/Viewport/RosterList")
///   rt.Find("ItemScrollView/Viewport/ItemList")  etc.
/// GoldLabel, DetailLabel, and tab buttons remain direct children of the panel.
///
/// SCENE FLOW: Overworld → Hub → (sections) → Overworld / Game
///
/// RELATED FILES:
/// - *SectionController.cs: Section logic (updated to use scroll paths)
/// - HubManager.cs: Hub scene controller
/// - SceneScaffoldHelper.cs: Shared scaffold utilities
/// - GameObjectHelper.Hub: Name constants used by controllers
/// </summary>
public static class HubScaffold
{
    private const string SceneName = "Hub";

    // ── Layout constants ──────────────────────────────────────────────────────
    const float NavH   = 70f;   // navigation bar height (top of screen)
    const float HeadH  = 58f;   // section header strip height
    const float FootH  = 82f;   // detail footer strip height
    const float TabH   = 46f;   // mode-tab row height (Shop, Blacksmith, Inventory)
    const float Pad    = 14f;   // inner edge padding
    const float SbW    = 18f;   // scrollbar width
    const float LeftR  = 0.34f; // left column width ratio for two-column panels
    const float GapH   = 6f;    // horizontal gap between two columns

    // ── Color palette (deep JRPG navy, Final Fantasy XIV inspired) ────────────
    static readonly Color NavBg      = new Color(0.04f, 0.04f, 0.08f, 1.00f);
    static readonly Color NavBtn     = new Color(0.12f, 0.16f, 0.27f, 0.90f);
    static readonly Color HeadBg     = new Color(0.06f, 0.08f, 0.14f, 0.95f);
    static readonly Color FootBg     = new Color(0.05f, 0.06f, 0.11f, 0.95f);
    static readonly Color ScrollBg   = new Color(0.06f, 0.07f, 0.12f, 0.85f);
    static readonly Color ScrollbarC = new Color(0.08f, 0.10f, 0.18f, 0.70f);
    static readonly Color HandleCol  = new Color(0.22f, 0.32f, 0.52f, 0.85f);
    static readonly Color TabBtnCol  = new Color(0.12f, 0.16f, 0.26f, 0.90f);
    static readonly Color SepCol     = new Color(0.20f, 0.26f, 0.40f, 0.80f);
    static readonly Color GoldCol    = new Color(1.00f, 0.84f, 0.20f, 1.00f);
    static readonly Color TextCol    = new Color(0.88f, 0.90f, 0.95f, 1.00f);
    static readonly Color SubTxtCol  = new Color(0.55f, 0.60f, 0.68f, 1.00f);
    static readonly Color BattleCol  = new Color(0.62f, 0.12f, 0.10f, 1.00f);

    // ── Navigation button definitions ─────────────────────────────────────────
    static readonly (string name, string label)[] SectionBtns =
    {
        ("PartyButton",      "Party"),
        ("ShopButton",       "Shop"),
        ("MedicalButton",    "Medical"),
        ("ResidenceButton",  "Residence"),
        ("BlacksmithButton", "Blacksmith"),
        ("TrainingButton",   "Training"),
        ("EquipButton",      "Equip"),
        ("InventoryButton",  "Inventory"),
        ("BattlePrepButton", "Battle Prep"),
    };

    static readonly (string name, string label)[] ExitBtns =
    {
        ("OverworldButton", "Overworld"),
        ("BattleButton",    "Battle"),
    };

    // ── Entry points ──────────────────────────────────────────────────────────

    [MenuItem("Tools/Scenes/Hub/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Hub scene and recreate all GameObjects from the scaffold?\n\n" +
            "Any unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneScaffoldHelper.EnsureEmptyGameObject("HubManager", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<HubManager>(mgrGO);

        var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            // ContentPanel first — vendor backgrounds render behind everything else
            var content = EnsureContentPanel(canvas, ref created, ref found);
            if (content != null)
                BuildAllPanels(content, ref created, ref found);

            // NavBar after ContentPanel — renders on top so buttons are never occluded
            EnsureNavBar(canvas, ref created, ref found);

            // Output — debug label, off-screen
            var output = SceneScaffoldHelper.EnsureLabel(canvas, "Output", "", ref created, ref found);
            if (output != null)
            {
                output.anchorMin = output.anchorMax = new Vector2(0.5f, 0.5f);
                output.sizeDelta = new Vector2(1000f, 200f);
                output.anchoredPosition = new Vector2(0f, 2000f);
            }

            // FadeOverlay always last — must render above everything for scene transitions
            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    // ── NavBar ────────────────────────────────────────────────────────────────

    static void EnsureNavBar(RectTransform canvas, ref int created, ref int found)
    {
        if (canvas.Find("NavBar") != null) { found++; return; }

        var go = new GameObject("NavBar");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = Vector2.one;
        rt.pivot    = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, NavH);
        rt.anchoredPosition = Vector2.zero;

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = NavBg;
        img.raycastTarget = true;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 3f;
        hlg.padding = new RectOffset(8, 8, 8, 8);

        foreach (var (name, label) in SectionBtns)
            AddNavButton(rt, name, label);

        // Flexible spacer pushes exit buttons to the right
        var spacerGO = new GameObject("NavSpacer");
        spacerGO.layer = LayerMask.NameToLayer("UI");
        var spacerRT = spacerGO.AddComponent<RectTransform>();
        spacerRT.SetParent(rt, false);
        spacerRT.sizeDelta = new Vector2(10f, 10f);
        var le = spacerGO.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minWidth = 10f;

        foreach (var (name, label) in ExitBtns)
            AddNavButton(rt, name, label);

        Undo.RegisterCreatedObjectUndo(go, "Create NavBar");
        created++;
    }

    static void AddNavButton(RectTransform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(145f, 54f);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = NavBtn;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = MakeButtonColors(1.20f, 0.78f);

        var labelGO = new GameObject("Text");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 17f;
        tmp.color = TextCol;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
    }

    // ── ContentPanel ──────────────────────────────────────────────────────────

    static RectTransform EnsureContentPanel(RectTransform canvas, ref int created, ref int found)
    {
        var existing = canvas.Find("ContentPanel");
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject("ContentPanel");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, -NavH);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, "Create ContentPanel");
        created++;
        return rt;
    }

    // ── Panel builders ────────────────────────────────────────────────────────

    static void BuildAllPanels(RectTransform content, ref int created, ref int found)
    {
        BuildPartyPanel(content, ref created, ref found);
        BuildShopPanel(content, ref created, ref found);
        BuildMedicalPanel(content, ref created, ref found);
        BuildResidencePanel(content, ref created, ref found);
        BuildBlacksmithPanel(content, ref created, ref found);
        BuildTrainingPanel(content, ref created, ref found);
        BuildEquipPanel(content, ref created, ref found);
        BuildInventoryPanel(content, ref created, ref found);
        BuildBattlePrepPanel(content, ref created, ref found);
    }

    static void BuildPartyPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "PartyPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, 0f);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        // Left: roster heroes not yet in party
        AddScrollView(panel, "RosterScrollView", "RosterList",
            new Vector2(0f, 0f), new Vector2(LeftR, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-GapH * 0.5f, -(HeadH + Pad)));
        // Right: active party members (max 4)
        AddScrollView(panel, "PartyScrollView", "PartyList",
            new Vector2(LeftR, 0f), new Vector2(1f, 1f),
            new Vector2(GapH * 0.5f, FootH + Pad), new Vector2(-Pad, -(HeadH + Pad)));
    }

    static void BuildShopPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "ShopPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, TabH);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        AddTabButton(panel, "BuyTab",  "Buy",  0f,   0.5f, HeadH);
        AddTabButton(panel, "SellTab", "Sell", 0.5f, 1f,   HeadH);
        AddScrollView(panel, "ItemScrollView", "ItemList",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-Pad, -(HeadH + TabH + Pad)));
    }

    static void BuildMedicalPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "MedicalPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, 0f);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        AddScrollView(panel, "HeroScrollView", "HeroList",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-Pad, -(HeadH + Pad)));
    }

    static void BuildResidencePanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "ResidencePanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, 0f);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        // Left: rest actions + current party list
        AddScrollView(panel, "ActionScrollView", "ActionList",
            new Vector2(0f, 0f), new Vector2(LeftR, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-GapH * 0.5f, -(HeadH + Pad)));
        // Right: recruitable heroes
        AddScrollView(panel, "RecruitScrollView", "RecruitList",
            new Vector2(LeftR, 0f), new Vector2(1f, 1f),
            new Vector2(GapH * 0.5f, FootH + Pad), new Vector2(-Pad, -(HeadH + Pad)));
    }

    static void BuildBlacksmithPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "BlacksmithPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, TabH);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        float tw = 1f / 5f;
        AddTabButton(panel, "BuyTab",     "Buy",     0f,     tw,     HeadH);
        AddTabButton(panel, "SellTab",    "Sell",    tw,     tw*2f,  HeadH);
        AddTabButton(panel, "CraftTab",   "Craft",   tw*2f,  tw*3f,  HeadH);
        AddTabButton(panel, "RepairTab",  "Repair",  tw*3f,  tw*4f,  HeadH);
        AddTabButton(panel, "SalvageTab", "Salvage", tw*4f,  1f,     HeadH);
        AddScrollView(panel, "ItemScrollView", "ItemList",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-Pad, -(HeadH + TabH + Pad)));
    }

    static void BuildTrainingPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "TrainingPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, 0f);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        // Left: party hero selector
        AddScrollView(panel, "HeroScrollView", "HeroList",
            new Vector2(0f, 0f), new Vector2(LeftR, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-GapH * 0.5f, -(HeadH + Pad)));
        // Right: available trainings for selected hero
        AddScrollView(panel, "TrainingScrollView", "TrainingList",
            new Vector2(LeftR, 0f), new Vector2(1f, 1f),
            new Vector2(GapH * 0.5f, FootH + Pad), new Vector2(-Pad, -(HeadH + Pad)));
    }

    static void BuildEquipPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "EquipPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, 0f);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        AddStatsLabel(panel);
        // Left column: hero selector
        AddScrollView(panel, "HeroScrollView", "HeroList",
            new Vector2(0f, 0f), new Vector2(LeftR, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-GapH * 0.5f, -(HeadH + Pad)));
        // Right-top 58%: equipment slot rows
        AddScrollView(panel, "SlotScrollView", "SlotList",
            new Vector2(LeftR, 0.42f), new Vector2(1f, 1f),
            new Vector2(GapH * 0.5f, 6f), new Vector2(-Pad, -(HeadH + Pad)));
        // Right-bottom 38%: item picker for selected slot
        AddScrollView(panel, "ItemPickerScrollView", "ItemPicker",
            new Vector2(LeftR, 0f), new Vector2(1f, 0.38f),
            new Vector2(GapH * 0.5f, FootH + Pad), new Vector2(-Pad, -6f));
    }

    static void BuildInventoryPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "InventoryPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, TabH);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        float fw = 1f / 5f;
        AddTabButton(panel, "FilterAll",   "All",   0f,     fw,     HeadH);
        AddTabButton(panel, "FilterEquip", "Equip", fw,     fw*2f,  HeadH);
        AddTabButton(panel, "FilterCons",  "Cons",  fw*2f,  fw*3f,  HeadH);
        AddTabButton(panel, "FilterMats",  "Mats",  fw*3f,  fw*4f,  HeadH);
        AddTabButton(panel, "FilterQuest", "Quest", fw*4f,  1f,     HeadH);
        AddScrollView(panel, "ItemScrollView", "ItemList",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(Pad, FootH + Pad), new Vector2(-Pad, -(HeadH + TabH + Pad)));
    }

    static void BuildBattlePrepPanel(RectTransform content, ref int created, ref int found)
    {
        var panel = EnsurePanel(content, "BattlePrepPanel", ref created, ref found);
        if (panel == null) return;
        AddHeaderBg(panel);
        AddHeaderSep(panel, 0f);
        AddFooterBg(panel);
        AddGoldLabel(panel);
        AddDetailLabel(panel);
        AddBattleButton(panel);
        // Top 52%: full party lineup with stats
        AddScrollView(panel, "PartyScrollView", "PartyList",
            new Vector2(0f, 0.44f), new Vector2(1f, 1f),
            new Vector2(Pad, 6f), new Vector2(-Pad, -(HeadH + Pad)));
        // Bottom 40%: consumable items to bring
        AddScrollView(panel, "ItemScrollView", "ItemList",
            new Vector2(0f, 0f), new Vector2(1f, 0.44f),
            new Vector2(Pad, FootH + 84f + Pad), new Vector2(-Pad, -6f));
    }

    // ── Primitive element builders ────────────────────────────────────────────

    /// <summary>Creates or finds a section panel that fills its parent.</summary>
    static RectTransform EnsurePanel(RectTransform parent, string name, ref int created, ref int found)
    {
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.04f, 0.05f, 0.09f, 0.97f);
        img.raycastTarget = true;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }

    /// <summary>Adds a dark header strip behind the VendorFactory nameplate area.</summary>
    static void AddHeaderBg(RectTransform panel)
    {
        var go = new GameObject("HeaderBg");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = Vector2.one;
        rt.pivot    = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, HeadH);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HeadBg;
        img.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, "Create HeaderBg");
    }

    /// <summary>Adds a 2px separator line below the header (and optional tab row).</summary>
    static void AddHeaderSep(RectTransform panel, float extraOffset)
    {
        var go = new GameObject("HeaderSep");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = Vector2.one;
        rt.pivot    = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 2f);
        rt.anchoredPosition = new Vector2(0f, -(HeadH + extraOffset));
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = SepCol;
        img.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, "Create HeaderSep");
    }

    /// <summary>Adds a dark footer strip behind the DetailLabel area.</summary>
    static void AddFooterBg(RectTransform panel)
    {
        var go = new GameObject("FooterBg");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot    = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, FootH);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = FootBg;
        img.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, "Create FooterBg");
    }

    /// <summary>
    /// Adds GoldLabel as a DIRECT child of the panel, anchored in the header top-right.
    /// Controllers find this with: rt.Find("GoldLabel").
    /// </summary>
    static void AddGoldLabel(RectTransform panel)
    {
        var go = new GameObject("GoldLabel");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0.55f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot    = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, -HeadH);
        rt.offsetMax = new Vector2(-Pad, 0f);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "Gold: 0";
        tmp.fontSize = 20f;
        tmp.color = GoldCol;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, "Create GoldLabel");
    }

    /// <summary>
    /// Adds DetailLabel as a DIRECT child of the panel, anchored in the footer.
    /// Controllers find this with: rt.Find("DetailLabel").
    /// </summary>
    static void AddDetailLabel(RectTransform panel)
    {
        var go = new GameObject("DetailLabel");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot    = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(-(Pad * 2f), FootH - 8f);
        rt.anchoredPosition = new Vector2(0f, 4f);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 16f;
        tmp.color = SubTxtCol;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, "Create DetailLabel");
    }

    /// <summary>Adds StatsLabel for EquipPanel — DIRECT child, anchored in the right-middle.</summary>
    static void AddStatsLabel(RectTransform panel)
    {
        var go = new GameObject("StatsLabel");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(LeftR, 0.36f);
        rt.anchorMax = new Vector2(1f, 0.44f);
        rt.pivot    = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(GapH * 0.5f + 4f, 0f);
        rt.offsetMax = new Vector2(-Pad, 0f);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 14f;
        tmp.color = SubTxtCol;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, "Create StatsLabel");
    }

    /// <summary>
    /// Adds a tab/filter button as a DIRECT child of the panel.
    /// Controllers find these with: rt.Find("BuyTab"), rt.Find("FilterAll"), etc.
    /// anchorX0..anchorX1 set horizontal extent; topOffset is pixels below the panel top.
    /// </summary>
    static void AddTabButton(RectTransform panel, string name, string label,
        float anchorX0, float anchorX1, float topOffset)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(anchorX0, 1f);
        rt.anchorMax = new Vector2(anchorX1, 1f);
        rt.pivot    = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-3f, TabH);
        rt.anchoredPosition = new Vector2(0f, -topOffset);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = TabBtnCol;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = MakeButtonColors(1.25f, 0.72f);

        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16f;
        tmp.color = TextCol;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
    }

    /// <summary>
    /// Creates a ScrollRect with Viewport + Mask + named Content (VerticalLayoutGroup +
    /// ContentSizeFitter) + themed Scrollbar. Controllers resolve the content via:
    ///   rt.Find("{scrollViewName}/Viewport/{contentName}")
    /// </summary>
    static void AddScrollView(RectTransform panel, string scrollViewName, string contentName,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        // ScrollView container
        var svGO = new GameObject(scrollViewName);
        svGO.layer = LayerMask.NameToLayer("UI");
        var svRT = svGO.AddComponent<RectTransform>();
        svRT.SetParent(panel, false);
        svRT.anchorMin = anchorMin;
        svRT.anchorMax = anchorMax;
        svRT.offsetMin = offsetMin;
        svRT.offsetMax = offsetMax;
        svGO.AddComponent<CanvasRenderer>();
        var svImg = svGO.AddComponent<Image>();
        svImg.color = ScrollBg;
        svImg.raycastTarget = true;
        var sr = svGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical   = true;
        sr.scrollSensitivity = 20f;
        sr.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var vpGO = new GameObject("Viewport");
        vpGO.layer = LayerMask.NameToLayer("UI");
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.SetParent(svRT, false);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-SbW, 0f);
        vpGO.AddComponent<CanvasRenderer>();
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = false;
        var mask = vpGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content — the named container controllers add rows into
        var contentGO = new GameObject(contentName);
        contentGO.layer = LayerMask.NameToLayer("UI");
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.SetParent(vpRT, false);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot    = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        contentRT.anchoredPosition = Vector2.zero;
        contentGO.AddComponent<CanvasRenderer>();
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Scrollbar
        var sbGO = new GameObject("Scrollbar");
        sbGO.layer = LayerMask.NameToLayer("UI");
        var sbRT = sbGO.AddComponent<RectTransform>();
        sbRT.SetParent(svRT, false);
        sbRT.anchorMin = new Vector2(1f, 0f);
        sbRT.anchorMax = Vector2.one;
        sbRT.pivot    = new Vector2(1f, 0.5f);
        sbRT.sizeDelta = new Vector2(SbW, 0f);
        sbRT.anchoredPosition = Vector2.zero;
        sbGO.AddComponent<CanvasRenderer>();
        var sbImg = sbGO.AddComponent<Image>();
        sbImg.color = ScrollbarC;
        sbImg.raycastTarget = true;
        var scrollbar = sbGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var areaGO = new GameObject("Sliding Area");
        areaGO.layer = LayerMask.NameToLayer("UI");
        var areaRT = areaGO.AddComponent<RectTransform>();
        areaRT.SetParent(sbRT, false);
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(2f, 2f);
        areaRT.offsetMax = new Vector2(-2f, -2f);

        var handleGO = new GameObject("Handle");
        handleGO.layer = LayerMask.NameToLayer("UI");
        var handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.SetParent(areaRT, false);
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.sizeDelta = Vector2.zero;
        handleGO.AddComponent<CanvasRenderer>();
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = HandleCol;
        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;

        // Wire ScrollRect
        sr.viewport = vpRT;
        sr.content  = contentRT;
        sr.verticalScrollbar = scrollbar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        Undo.RegisterCreatedObjectUndo(svGO, $"Create {scrollViewName}");
    }

    /// <summary>Adds the large "ENTER BATTLE" button for BattlePrepPanel.</summary>
    static void AddBattleButton(RectTransform panel)
    {
        var go = new GameObject("BattleButton");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0.25f, 0f);
        rt.anchorMax = new Vector2(0.75f, 0f);
        rt.pivot    = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, 72f);
        rt.anchoredPosition = new Vector2(0f, FootH + 8f);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = BattleCol;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = MakeButtonColors(1.15f, 0.70f);

        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "ENTER BATTLE";
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, "Create BattleButton");
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    static ColorBlock MakeButtonColors(float highlightMul, float pressMul) => new ColorBlock
    {
        normalColor     = Color.white,
        highlightedColor = new Color(highlightMul, highlightMul, highlightMul, 1f),
        pressedColor    = new Color(pressMul, pressMul, pressMul, 1f),
        selectedColor   = new Color(highlightMul, highlightMul, highlightMul, 1f),
        disabledColor   = new Color(0.5f, 0.5f, 0.5f, 0.6f),
        colorMultiplier = 1f,
        fadeDuration    = 0.1f,
    };
}
