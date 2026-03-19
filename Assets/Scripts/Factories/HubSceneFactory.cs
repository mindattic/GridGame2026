using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Factories
{
/// <summary>
/// HUBSCENEFACTORY - Programmatically builds the entire Hub scene hierarchy.
///
/// PURPOSE:
/// Creates all GameObjects, components, and layout needed by HubManager
/// and the nine section controllers so the Hub scene can be generated
/// entirely from code — no manual Inspector setup required.
///
/// CREATED HIERARCHY:
/// ```
/// Canvas (CanvasScaler 1920x1080, GraphicRaycaster)
/// ├── NavBar (HorizontalLayoutGroup, top strip)
/// │   ├── PartyButton
/// │   ├── ShopButton
/// │   ├── BlacksmithButton
/// │   ├── TrainingButton
/// │   ├── MedicalButton
/// │   ├── ResidenceButton
/// │   ├── EquipButton
/// │   ├── InventoryButton
/// │   ├── BattlePrepButton
/// │   ├── NavSpacer (flexible space)
/// │   ├── OverworldButton
/// │   └── BattleButton
/// ├── ContentPanel (fills below NavBar)
/// │   ├── PartyPanel
/// │   ├── ShopPanel
/// │   ├── BlacksmithPanel
/// │   ├── TrainingPanel
/// │   ├── MedicalPanel
/// │   ├── ResidencePanel
/// │   ├── EquipPanel
/// │   ├── InventoryPanel
/// │   └── BattlePrepPanel
/// ├── FadeOverlay (full-screen, FadeOverlayInstance)
/// └── Output (debug TMP, bottom-left)
/// EventSystem (StandaloneInputModule)
/// HubManager (MonoBehaviour)
/// ```
///
/// USAGE:
/// Called from a lightweight bootstrap MonoBehaviour on scene load:
/// ```csharp
/// void Awake() { HubSceneFactory.Build(); }
/// ```
///
/// RELATED FILES:
/// - HubManager.cs: Resolves created objects via GameObject.Find()
/// - GameObjectHelper.cs: Name constants that must match factory output
/// - All section controllers: Resolve children via transform.Find()
/// - HubVendorFactory.cs: Adds vendor overlays at runtime
/// - HubItemRowFactory.cs: Adds list rows at runtime
/// </summary>
public static class HubSceneFactory
{
    // ===================== Layout Constants =====================

    private const float NavBarHeight = 52f;
    private const float NavButtonMinWidth = 90f;
    private const float NavButtonSpacing = 2f;
    private const int ReferenceWidth = 1920;
    private const int ReferenceHeight = 1080;

    // ===================== Colors =====================

    private static readonly Color CanvasBgColor = new Color(0.06f, 0.06f, 0.12f, 1f);
    private static readonly Color NavBarBgColor = new Color(0.08f, 0.08f, 0.18f, 0.96f);
    private static readonly Color NavButtonColor = new Color(0.10f, 0.12f, 0.25f, 0.90f);
    private static readonly Color NavButtonTextColor = new Color(0.78f, 0.82f, 0.92f, 1f);
    private static readonly Color PanelBgColor = new Color(0.10f, 0.10f, 0.23f, 0.94f); // FF6 PR deep blue
    private static readonly Color HeaderBgColor = new Color(0.08f, 0.08f, 0.20f, 0.92f);
    private static readonly Color HeaderSepColor = new Color(0.30f, 0.38f, 0.60f, 0.70f); // brighter blue border
    private static readonly Color FooterBgColor = new Color(0.07f, 0.07f, 0.16f, 0.92f);
    private static readonly Color ScrollbarColor = new Color(0.20f, 0.22f, 0.38f, 0.70f);
    private static readonly Color ScrollHandleColor = new Color(0.35f, 0.40f, 0.60f, 0.85f);
    private static readonly Color TabButtonColor = new Color(0.12f, 0.14f, 0.30f, 0.92f);
    private static readonly Color TabButtonTextColor = new Color(0.72f, 0.75f, 0.88f, 1f);
    private static readonly Color GoldLabelColor = new Color(1f, 0.85f, 0.35f, 1f);
    private static readonly Color DetailLabelColor = new Color(0.78f, 0.82f, 0.92f, 1f);
    private static readonly Color PanelBorderColor = new Color(0.30f, 0.40f, 0.70f, 0.80f); // FF6 signature blue border

    // ===================== Entry Point =====================

    /// <summary>
    /// Builds the entire Hub scene hierarchy from scratch.
    /// Safe to call once at scene load.
    /// </summary>
    public static void Build()
    {
        var canvas = CreateCanvas();
        var canvasRT = canvas.GetComponent<RectTransform>();

        // NavBar
        CreateNavBar(canvasRT);

        // Content panel (fills space below nav)
        var content = CreateContentPanel(canvasRT);

        // Section panels
        BuildPartyPanel(content);
        BuildShopPanel(content);
        BuildBlacksmithPanel(content);
        BuildTrainingPanel(content);
        BuildMedicalPanel(content);
        BuildResidencePanel(content);
        BuildEquipPanel(content);
        BuildInventoryPanel(content);
        BuildBattlePrepPanel(content);

        // Fade overlay (must be last sibling to render on top)
        CreateFadeOverlay(canvasRT);

        // Debug output label
        CreateOutputLabel(canvasRT);

        // EventSystem (only if one doesn't already exist)
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            CreateEventSystem();
    }

    // ===================== Canvas =====================

    private static GameObject CreateCanvas()
    {
        var go = new GameObject("Canvas");
        go.layer = LayerMask.NameToLayer("UI");

        var canvas = go.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // Full-screen background image
        var bgImg = go.AddComponent<Image>();
        bgImg.color = CanvasBgColor;
        bgImg.raycastTarget = true;

        return go;
    }

    // ===================== EventSystem =====================

    private static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    // ===================== NavBar =====================

    private static void CreateNavBar(RectTransform canvasRT)
    {
        var go = new GameObject("NavBar");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvasRT, false);
        // Top strip
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -NavBarHeight);
        rt.offsetMax = Vector2.zero;

        go.AddComponent<CanvasRenderer>();
        var bgImg = go.AddComponent<Image>();
        bgImg.color = NavBarBgColor;
        bgImg.raycastTarget = true;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = NavButtonSpacing;
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Section buttons (order matches typical RPG flow)
        CreateNavButton(rt, GameObjectHelper.Hub.PartyButton, "Party");
        CreateNavButton(rt, GameObjectHelper.Hub.ShopButton, "Shop");
        CreateNavButton(rt, GameObjectHelper.Hub.BlacksmithButton, "Smith");
        CreateNavButton(rt, GameObjectHelper.Hub.TrainingButton, "Train");
        CreateNavButton(rt, GameObjectHelper.Hub.MedicalButton, "Medical");
        CreateNavButton(rt, GameObjectHelper.Hub.ResidenceButton, "Inn");
        CreateNavButton(rt, GameObjectHelper.Hub.EquipButton, "Equip");
        CreateNavButton(rt, GameObjectHelper.Hub.InventoryButton, "Items");
        CreateNavButton(rt, GameObjectHelper.Hub.BattlePrepButton, "Prepare");

        // Flexible spacer pushes action buttons to the right
        CreateNavSpacer(rt);

        // Action buttons
        CreateNavButton(rt, GameObjectHelper.Hub.OverworldButton, "Leave");
        CreateNavButton(rt, GameObjectHelper.Hub.BattleButton, "Battle!");
    }

    private static void CreateNavButton(RectTransform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = NavButtonColor;
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.90f, 0.90f, 1f, 1f),
            pressedColor = new Color(0.70f, 0.70f, 0.80f, 1f),
            selectedColor = new Color(0.85f, 0.85f, 0.95f, 1f),
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        var layout = go.AddComponent<LayoutElement>();
        layout.minWidth = NavButtonMinWidth;
        layout.preferredWidth = NavButtonMinWidth;
        layout.flexibleWidth = 0f;

        // Text child
        var textGo = new GameObject("Text");
        textGo.layer = LayerMask.NameToLayer("UI");
        var textRT = textGo.AddComponent<RectTransform>();
        textRT.SetParent(rt, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(4f, 2f);
        textRT.offsetMax = new Vector2(-4f, -2f);

        textGo.AddComponent<CanvasRenderer>();
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = NavButtonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    private static void CreateNavSpacer(RectTransform parent)
    {
        var go = new GameObject("NavSpacer");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        var layout = go.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
    }

    // ===================== Content Panel =====================

    private static RectTransform CreateContentPanel(RectTransform canvasRT)
    {
        var go = new GameObject("ContentPanel");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvasRT, false);
        // Fill below NavBar
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, -NavBarHeight);
        return rt;
    }

    // ===================== Section Panel Builders =====================

    // --- PARTY PANEL ---
    // Three-column: Roster (left), Party + Detail (center), Ability Bar (right)
    // Plus sub-tabs: Status | Equip | Abilities
    // Layout:
    // ┌──────────┬──────────────────┬────────────────────┐
    // │ ROSTER   │ ACTIVE PARTY     │ [Status][Equip][Ab]│ ← sub-tabs
    // │          │ ★ Paladin  Lv5   │ Right column shows │
    // │ • Monk   │ ★ Knight   Lv3   │ sub-tab content:   │
    // │ • Cleric │                  │ Status / Equip /   │
    // │          ├──────────────────│ Abilities          │
    // │          │ HERO DETAIL      │                    │
    // │          │ Stats, equip...  │                    │
    // └──────────┴──────────────────┴────────────────────┘
    private static void BuildPartyPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.PartyPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // Left column: Roster (0..0.25)
        CreateScrollView(panel, "RosterScrollView", GameObjectHelper.Hub.RosterList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.25f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-4f, -52f));

        // Center-top: Party list (0.25..0.60, top half)
        CreateScrollView(panel, "PartyScrollView", GameObjectHelper.Hub.PartyList,
            anchorMin: new Vector2(0.25f, 0.50f), anchorMax: new Vector2(0.60f, 1f),
            offsetMin: new Vector2(4f, 4f), offsetMax: new Vector2(-4f, -52f));

        // Center-bottom: Detail (text) — this is the DetailLabel area for stats
        // (DetailLabel is already created in the header area; the controller re-purposes it)

        // Right column: Sub-tab buttons (0.60..1, top strip)
        float subTabY = -52f;
        float subTabH = 32f;
        float subTabW = 1f / 3f; // 3 sub-tabs spanning the right column
        // Map sub-tab anchors to the right third of the panel (0.60..1.0)
        CreateTabButton(panel, GameObjectHelper.Hub.PartyStatusTab, "Status",
            new Vector2(0.60f, 1f), new Vector2(0.60f + 0.40f * subTabW, 1f), subTabY, subTabH);
        CreateTabButton(panel, GameObjectHelper.Hub.PartyEquipTab, "Equip",
            new Vector2(0.60f + 0.40f * subTabW, 1f), new Vector2(0.60f + 0.40f * 2f * subTabW, 1f), subTabY, subTabH);
        CreateTabButton(panel, GameObjectHelper.Hub.PartyAbilityTab, "Abilities",
            new Vector2(0.60f + 0.40f * 2f * subTabW, 1f), new Vector2(1f, 1f), subTabY, subTabH);

        // Right-top: Ability bar slots (0.60..1, upper portion below sub-tabs)
        CreateScrollView(panel, "AbilityBarScrollView", GameObjectHelper.Hub.AbilityBarList,
            anchorMin: new Vector2(0.60f, 0.50f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(4f, 4f), offsetMax: new Vector2(-8f, -(52f + subTabH + 4f)));

        // Right-bottom: Ability picker (0.60..1, lower portion)
        CreateScrollView(panel, "AbilityPickerScrollView", GameObjectHelper.Hub.AbilityPicker,
            anchorMin: new Vector2(0.60f, 0f), anchorMax: new Vector2(1f, 0.50f),
            offsetMin: new Vector2(4f, 48f), offsetMax: new Vector2(-8f, -4f));

        CreateFooterBar(panel);
    }

    // --- SHOP PANEL ---
    // Single ItemScrollView, GoldLabel, DetailLabel
    private static void BuildShopPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.ShopPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // Item list (center, full width)
        CreateScrollView(panel, "ItemScrollView", GameObjectHelper.Hub.ItemList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-8f, -52f));

        CreateFooterBar(panel);
    }

    // --- BLACKSMITH PANEL ---
    // ItemScrollView + 5 mode tabs (BuyTab, SellTab, CraftTab, RepairTab, SalvageTab)
    private static void BuildBlacksmithPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.BlacksmithPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // Tab row (just below header)
        float tabY = -52f;
        float tabH = 36f;
        float tabW = 0.2f; // 5 tabs = 20% each
        CreateTabButton(panel, GameObjectHelper.Hub.BuyTab, "Buy",
            new Vector2(0f, 1f), new Vector2(tabW, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.SellTab, "Sell",
            new Vector2(tabW, 1f), new Vector2(tabW * 2, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.CraftTab, "Craft",
            new Vector2(tabW * 2, 1f), new Vector2(tabW * 3, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.RepairTab, "Repair",
            new Vector2(tabW * 3, 1f), new Vector2(tabW * 4, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.SalvageTab, "Salvage",
            new Vector2(tabW * 4, 1f), new Vector2(1f, 1f), tabY, tabH);

        // Item list (below tabs)
        CreateScrollView(panel, "ItemScrollView", GameObjectHelper.Hub.ItemList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-8f, -(52f + tabH + 4f)));

        CreateFooterBar(panel);
    }

    // --- TRAINING PANEL ---
    // Two-column: HeroScrollView (left), TrainingScrollView (right)
    // Plus VendorPortrait, GoldLabel, DetailLabel
    private static void BuildTrainingPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.TrainingPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // VendorPortrait (left, above hero list)
        var portraitGo = new GameObject(GameObjectHelper.Hub.VendorPortrait);
        portraitGo.layer = LayerMask.NameToLayer("UI");
        var porRT = portraitGo.AddComponent<RectTransform>();
        porRT.SetParent(panel, false);
        porRT.anchorMin = new Vector2(0f, 1f);
        porRT.anchorMax = new Vector2(0.12f, 1f);
        porRT.pivot = new Vector2(0f, 1f);
        porRT.offsetMin = new Vector2(8f, -140f);
        porRT.offsetMax = new Vector2(0f, -56f);
        portraitGo.AddComponent<CanvasRenderer>();
        var porImg = portraitGo.AddComponent<Image>();
        porImg.raycastTarget = false;
        porImg.preserveAspect = true;
        porImg.color = new Color(1f, 1f, 1f, 0.8f);

        // Left column: Heroes (0..0.35)
        CreateScrollView(panel, "HeroScrollView", GameObjectHelper.Hub.HeroList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.35f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-4f, -52f));

        // Right column: Trainings (0.35..1)
        CreateScrollView(panel, "TrainingScrollView", GameObjectHelper.Hub.TrainingList,
            anchorMin: new Vector2(0.35f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(4f, 48f), offsetMax: new Vector2(-8f, -52f));

        CreateFooterBar(panel);
    }

    // --- MEDICAL PANEL ---
    // Single HeroScrollView, GoldLabel, DetailLabel
    private static void BuildMedicalPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.MedicalPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        CreateScrollView(panel, "HeroScrollView", GameObjectHelper.Hub.HeroList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-8f, -52f));

        CreateFooterBar(panel);
    }

    // --- RESIDENCE PANEL ---
    // Two-column: ActionScrollView (left), RecruitScrollView (right)
    // Plus GoldLabel, DetailLabel
    private static void BuildResidencePanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.ResidencePanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // Left column: Actions (0..0.45)
        CreateScrollView(panel, "ActionScrollView", GameObjectHelper.Hub.ActionList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.45f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-4f, -52f));

        // Right column: Recruits (0.45..1)
        CreateScrollView(panel, "RecruitScrollView", GameObjectHelper.Hub.RecruitList,
            anchorMin: new Vector2(0.45f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(4f, 48f), offsetMax: new Vector2(-8f, -52f));

        CreateFooterBar(panel);
    }

    // --- EQUIP PANEL ---
    // Three-area: HeroScrollView (left), SlotScrollView (right-top), ItemPickerScrollView (right-bottom)
    // Plus GoldLabel, StatsLabel, DetailLabel
    private static void BuildEquipPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.EquipPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // Left column: Heroes (0..0.28)
        CreateScrollView(panel, "HeroScrollView", GameObjectHelper.Hub.HeroList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.28f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-4f, -52f));

        // Right-top: Slots (0.28..0.65, top half)
        CreateScrollView(panel, "SlotScrollView", GameObjectHelper.Hub.SlotList,
            anchorMin: new Vector2(0.28f, 0.5f), anchorMax: new Vector2(0.65f, 1f),
            offsetMin: new Vector2(4f, 4f), offsetMax: new Vector2(-4f, -52f));

        // Right-bottom: Item picker (0.28..0.65, bottom half)
        CreateScrollView(panel, "ItemPickerScrollView", GameObjectHelper.Hub.ItemPicker,
            anchorMin: new Vector2(0.28f, 0f), anchorMax: new Vector2(0.65f, 0.5f),
            offsetMin: new Vector2(4f, 48f), offsetMax: new Vector2(-4f, -4f));

        // Stats label (0.65..1, right column)
        var statsGo = new GameObject(GameObjectHelper.Hub.StatsLabel);
        statsGo.layer = LayerMask.NameToLayer("UI");
        var statsRT = statsGo.AddComponent<RectTransform>();
        statsRT.SetParent(panel, false);
        statsRT.anchorMin = new Vector2(0.65f, 0f);
        statsRT.anchorMax = new Vector2(1f, 1f);
        statsRT.offsetMin = new Vector2(8f, 48f);
        statsRT.offsetMax = new Vector2(-8f, -52f);
        statsGo.AddComponent<CanvasRenderer>();
        var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
        statsTmp.text = "";
        statsTmp.fontSize = 20;
        statsTmp.color = DetailLabelColor;
        statsTmp.alignment = TextAlignmentOptions.TopLeft;
        statsTmp.enableWordWrapping = true;
        statsTmp.raycastTarget = false;

        CreateFooterBar(panel);
    }

    // --- INVENTORY PANEL ---
    // ItemScrollView + 5 filter buttons + GoldLabel, DetailLabel
    private static void BuildInventoryPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.InventoryPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // Filter tab row (just below header)
        float tabY = -52f;
        float tabH = 36f;
        float tabW = 0.2f;
        CreateTabButton(panel, GameObjectHelper.Hub.FilterAll, "All",
            new Vector2(0f, 1f), new Vector2(tabW, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.FilterEquip, "Equip",
            new Vector2(tabW, 1f), new Vector2(tabW * 2, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.FilterCons, "Consumable",
            new Vector2(tabW * 2, 1f), new Vector2(tabW * 3, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.FilterMats, "Materials",
            new Vector2(tabW * 3, 1f), new Vector2(tabW * 4, 1f), tabY, tabH);
        CreateTabButton(panel, GameObjectHelper.Hub.FilterQuest, "Quest",
            new Vector2(tabW * 4, 1f), new Vector2(1f, 1f), tabY, tabH);

        // Item list (below filter tabs)
        CreateScrollView(panel, "ItemScrollView", GameObjectHelper.Hub.ItemList,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.55f, 1f),
            offsetMin: new Vector2(8f, 48f), offsetMax: new Vector2(-4f, -(52f + tabH + 4f)));

        CreateFooterBar(panel);
    }

    // --- BATTLEPREP PANEL ---
    // PartyScrollView (top), ItemScrollView (middle), BattleButton, GoldLabel, DetailLabel
    private static void BuildBattlePrepPanel(RectTransform content)
    {
        var panel = CreateSectionPanel(content, GameObjectHelper.Hub.BattlePrepPanel);

        CreateHeaderBar(panel);
        CreateGoldLabel(panel);
        CreateDetailLabel(panel);
        CreateHeaderSep(panel);

        // Top: Party lineup (0..1, top 55%)
        CreateScrollView(panel, "PartyScrollView", GameObjectHelper.Hub.PartyList,
            anchorMin: new Vector2(0f, 0.45f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(8f, 4f), offsetMax: new Vector2(-8f, -52f));

        // Middle: Consumable items (0..1, middle 30%)
        CreateScrollView(panel, "ItemScrollView", GameObjectHelper.Hub.ItemList,
            anchorMin: new Vector2(0f, 0.15f), anchorMax: new Vector2(1f, 0.45f),
            offsetMin: new Vector2(8f, 4f), offsetMax: new Vector2(-8f, -4f));

        // Battle button (bottom-center)
        var battleGo = new GameObject(GameObjectHelper.Hub.BattlePrepBattleButton);
        battleGo.layer = LayerMask.NameToLayer("UI");
        var battleRT = battleGo.AddComponent<RectTransform>();
        battleRT.SetParent(panel, false);
        battleRT.anchorMin = new Vector2(0.3f, 0f);
        battleRT.anchorMax = new Vector2(0.7f, 0.15f);
        battleRT.offsetMin = new Vector2(0f, 12f);
        battleRT.offsetMax = new Vector2(0f, -4f);

        battleGo.AddComponent<CanvasRenderer>();
        var battleImg = battleGo.AddComponent<Image>();
        battleImg.color = new Color(0.55f, 0.20f, 0.15f, 1f);
        battleImg.raycastTarget = true;

        var battleBtn = battleGo.AddComponent<Button>();
        battleBtn.targetGraphic = battleImg;
        battleBtn.transition = Selectable.Transition.ColorTint;
        battleBtn.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1f, 0.90f, 0.85f, 1f),
            pressedColor = new Color(0.80f, 0.60f, 0.55f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        battleBtn.navigation = new Navigation { mode = Navigation.Mode.None };

        // Battle button label
        var bTextGo = new GameObject("Text");
        bTextGo.layer = LayerMask.NameToLayer("UI");
        var bTextRT = bTextGo.AddComponent<RectTransform>();
        bTextRT.SetParent(battleRT, false);
        bTextRT.anchorMin = Vector2.zero;
        bTextRT.anchorMax = Vector2.one;
        bTextRT.offsetMin = Vector2.zero;
        bTextRT.offsetMax = Vector2.zero;
        bTextGo.AddComponent<CanvasRenderer>();
        var bTmp = bTextGo.AddComponent<TextMeshProUGUI>();
        bTmp.text = "Enter Battle";
        bTmp.fontSize = 28;
        bTmp.fontStyle = FontStyles.Bold;
        bTmp.color = new Color(1f, 0.90f, 0.80f, 1f);
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.raycastTarget = false;

        CreateFooterBar(panel);
    }

    // ===================== Shared Section Helpers =====================

    /// <summary>Creates a section panel that fills the content area. Starts inactive.</summary>
    private static RectTransform CreateSectionPanel(RectTransform parent, string name)
    {
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
        img.color = PanelBgColor;
        img.raycastTarget = true;

        // FF6 PR-style 2px bright blue border around each panel
        var outline = go.AddComponent<Outline>();
        outline.effectColor = PanelBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        // Panels start active so HubManager.ResolveSceneObjects() can find
        // them with GameObject.Find() (which only finds active objects).
        // HubManager.GoToPartySection() at the end of Awake() will disable
        // all panels except the default one.

        return rt;
    }

    /// <summary>Creates the dark header background strip at the top of a panel.</summary>
    private static void CreateHeaderBar(RectTransform panel)
    {
        var go = new GameObject("HeaderBg");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -48f);
        rt.offsetMax = Vector2.zero;

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HeaderBgColor;
        img.raycastTarget = false;
    }

    /// <summary>Creates a thin separator line below the header.</summary>
    private static void CreateHeaderSep(RectTransform panel)
    {
        var go = new GameObject("HeaderSep");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(8f, -50f);
        rt.offsetMax = new Vector2(-8f, -48f);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HeaderSepColor;
        img.raycastTarget = false;
    }

    /// <summary>Creates the footer background strip at the bottom of a panel.</summary>
    private static void CreateFooterBar(RectTransform panel)
    {
        var go = new GameObject("FooterBg");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, 44f);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = FooterBgColor;
        img.raycastTarget = false;
    }

    /// <summary>Creates the GoldLabel TMP in the header area (top-right).</summary>
    private static void CreateGoldLabel(RectTransform panel)
    {
        var go = new GameObject(GameObjectHelper.Hub.GoldLabel);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0.7f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, -44f);
        rt.offsetMax = new Vector2(-12f, -4f);

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "0 G";
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = GoldLabelColor;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    /// <summary>Creates the DetailLabel TMP in the header area (top-left).</summary>
    private static void CreateDetailLabel(RectTransform panel)
    {
        var go = new GameObject(GameObjectHelper.Hub.DetailLabel);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0.7f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(12f, -44f);
        rt.offsetMax = new Vector2(0f, -4f);

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 24;
        tmp.color = DetailLabelColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    // ===================== ScrollView Builder =====================

    /// <summary>
    /// Creates a complete ScrollView + Viewport + Content + Scrollbar hierarchy.
    /// The content container gets a VerticalLayoutGroup + ContentSizeFitter.
    /// </summary>
    private static RectTransform CreateScrollView(
        RectTransform panel,
        string scrollViewName,
        string contentName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        // --- ScrollView root ---
        var svGo = new GameObject(scrollViewName);
        svGo.layer = LayerMask.NameToLayer("UI");
        var svRT = svGo.AddComponent<RectTransform>();
        svRT.SetParent(panel, false);
        svRT.anchorMin = anchorMin;
        svRT.anchorMax = anchorMax;
        svRT.offsetMin = offsetMin;
        svRT.offsetMax = offsetMax;

        svGo.AddComponent<CanvasRenderer>();
        var svImg = svGo.AddComponent<Image>();
        svImg.color = new Color(0f, 0f, 0f, 0.15f);
        svImg.raycastTarget = true;

        var scrollRect = svGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 25f;

        // --- Viewport ---
        var vpGo = new GameObject("Viewport");
        vpGo.layer = LayerMask.NameToLayer("UI");
        var vpRT = vpGo.AddComponent<RectTransform>();
        vpRT.SetParent(svRT, false);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-12f, 0f); // leave room for scrollbar
        vpRT.pivot = new Vector2(0f, 1f);

        vpGo.AddComponent<CanvasRenderer>();
        var vpImg = vpGo.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;

        vpGo.AddComponent<RectMask2D>();

        scrollRect.viewport = vpRT;

        // --- Content ---
        var contentGo = new GameObject(contentName);
        contentGo.layer = LayerMask.NameToLayer("UI");
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.SetParent(vpRT, false);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = new Vector2(0f, 0f);
        contentRT.offsetMax = new Vector2(0f, 0f);
        contentRT.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRT;

        // --- Scrollbar (vertical, right edge) ---
        var sbGo = new GameObject("Scrollbar");
        sbGo.layer = LayerMask.NameToLayer("UI");
        var sbRT = sbGo.AddComponent<RectTransform>();
        sbRT.SetParent(svRT, false);
        sbRT.anchorMin = new Vector2(1f, 0f);
        sbRT.anchorMax = new Vector2(1f, 1f);
        sbRT.pivot = new Vector2(1f, 0.5f);
        sbRT.offsetMin = new Vector2(-10f, 0f);
        sbRT.offsetMax = Vector2.zero;
        sbRT.sizeDelta = new Vector2(10f, 0f);

        sbGo.AddComponent<CanvasRenderer>();
        var sbImg = sbGo.AddComponent<Image>();
        sbImg.color = ScrollbarColor;
        sbImg.raycastTarget = true;

        // Sliding area
        var slideGo = new GameObject("Sliding Area");
        slideGo.layer = LayerMask.NameToLayer("UI");
        var slideRT = slideGo.AddComponent<RectTransform>();
        slideRT.SetParent(sbRT, false);
        slideRT.anchorMin = Vector2.zero;
        slideRT.anchorMax = Vector2.one;
        slideRT.offsetMin = Vector2.zero;
        slideRT.offsetMax = Vector2.zero;

        // Handle
        var handleGo = new GameObject("Handle");
        handleGo.layer = LayerMask.NameToLayer("UI");
        var handleRT = handleGo.AddComponent<RectTransform>();
        handleRT.SetParent(slideRT, false);
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;

        handleGo.AddComponent<CanvasRenderer>();
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = ScrollHandleColor;
        handleImg.raycastTarget = true;

        var scrollbar = sbGo.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.transition = Selectable.Transition.ColorTint;
        scrollbar.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.90f, 0.90f, 1f, 1f),
            pressedColor = new Color(0.70f, 0.70f, 0.80f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -2f;

        return contentRT;
    }

    // ===================== Tab Button =====================

    /// <summary>Creates a small tab button anchored relative to the panel top.</summary>
    private static void CreateTabButton(
        RectTransform panel,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float topOffset,
        float height)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(2f, 0f);
        rt.offsetMax = new Vector2(-2f, topOffset);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = TabButtonColor;
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.90f, 0.90f, 1f, 1f),
            pressedColor = new Color(0.70f, 0.70f, 0.80f, 1f),
            selectedColor = new Color(0.85f, 0.85f, 0.95f, 1f),
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        // Label
        var textGo = new GameObject("Label");
        textGo.layer = LayerMask.NameToLayer("UI");
        var textRT = textGo.AddComponent<RectTransform>();
        textRT.SetParent(rt, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(4f, 2f);
        textRT.offsetMax = new Vector2(-4f, -2f);

        textGo.AddComponent<CanvasRenderer>();
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = TabButtonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    // ===================== Fade Overlay =====================

    /// <summary>Creates the full-screen FadeOverlay used for scene transitions.</summary>
    private static void CreateFadeOverlay(RectTransform canvasRT)
    {
        var go = new GameObject("FadeOverlay");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvasRT, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false; // must not block input after fade completes

        go.AddComponent<FadeOverlayInstance>();
    }

    // ===================== Debug Output =====================

    /// <summary>Creates a debug output TMP label (bottom-left, used by TiltParallax).</summary>
    private static void CreateOutputLabel(RectTransform canvasRT)
    {
        var go = new GameObject("Output");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvasRT, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0.3f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.offsetMin = new Vector2(8f, 4f);
        rt.offsetMax = new Vector2(0f, 28f);

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 14;
        tmp.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }
}

}
