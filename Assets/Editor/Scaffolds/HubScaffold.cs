using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.Hub.Sections;
using Scripts.Helpers;

/// <summary>
/// HUBSCAFFOLD - Editor tool that builds the Hub scene from code.
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera                  Camera + AudioListener
/// EventSystem                  EventSystem + StandaloneInputModule
/// HubManager                   HubManager script owner (empty GO)
/// Canvas                       ScreenSpaceOverlay + background
///   ├── Header                 GoldLabel (TMP)
///   ├── NavBar                 Hamburger MenuButton + Overworld/Battle exits
///   ├── ContentPanel
///   │   ├── PartyPanel         PartySection — RosterList / PartyList / DetailLabel
///   │   ├── ShopPanel          GeneralStoreSection — BuyTab / SellTab / ItemList / ConfirmButton / DetailLabel
///   │   ├── AlchemistPanel     AlchemistSection — ItemList / ConfirmButton / DetailLabel
///   │   ├── ResidencePanel     InnSection — RestButton / DetailLabel
///   │   ├── BlacksmithPanel    BlacksmithSection — ItemList / ConfirmButton / DetailLabel
///   │   ├── TrainingPanel      TrainingSection — HeroList / TrainingList / ConfirmButton / DetailLabel
///   │   ├── EquipPanel         EquipSection — HeroList / SlotList / ItemPicker / StatsLabel
///   │   ├── InventoryPanel     InventorySection — FilterAll/Equip/Cons/Mats / ItemList / DetailLabel
///   │   ├── EnchanterPanel     EnchantSection — ItemList / ConfirmButton / DetailLabel
///   │   ├── SalvagePanel       SalvageSection — ItemList / ConfirmButton / DetailLabel
///   │   ├── PlacesPanel        PlacesSection — 5 biome buttons / ConfirmButton / DetailLabel
///   │   └── BountyPanel        BountySection — ItemList / ConfirmButton / AbandonButton / DetailLabel
///   ├── MenuDropdown           Hidden by default — Backdrop + MenuPanel (12 section buttons stacked)
///   └── FadeOverlay
/// ```
///
/// Every panel owns a `GoldLabel` TMP child so HubManager's gold-sync loop can find it
/// in whichever panel is visible.
///
/// RELATED FILES: HubManager.cs, HubSection.cs, GameObjectHelper.Hub
/// </summary>
public static class HubScaffold
{
    private const string SceneName = "Hub";

    private static readonly (string buttonName, string panelName, string label, System.Type sectionType)[] Sections = new (string, string, string, System.Type)[]
    {
        (GameObjectHelper.Hub.PartyButton,      GameObjectHelper.Hub.PartyPanel,      "Party",      typeof(PartySection)),
        (GameObjectHelper.Hub.ShopButton,       GameObjectHelper.Hub.ShopPanel,       "Store",      typeof(GeneralStoreSection)),
        (GameObjectHelper.Hub.AlchemistButton,  GameObjectHelper.Hub.AlchemistPanel,  "Alchemist",  typeof(AlchemistSection)),
        (GameObjectHelper.Hub.ResidenceButton,  GameObjectHelper.Hub.ResidencePanel,  "Inn",        typeof(InnSection)),
        (GameObjectHelper.Hub.BlacksmithButton, GameObjectHelper.Hub.BlacksmithPanel, "Blacksmith", typeof(BlacksmithSection)),
        (GameObjectHelper.Hub.TrainingButton,   GameObjectHelper.Hub.TrainingPanel,   "Training",   typeof(TrainingSection)),
        (GameObjectHelper.Hub.EquipButton,      GameObjectHelper.Hub.EquipPanel,      "Equip",      typeof(EquipSection)),
        (GameObjectHelper.Hub.InventoryButton,  GameObjectHelper.Hub.InventoryPanel,  "Inventory",  typeof(InventorySection)),
        (GameObjectHelper.Hub.EnchanterButton,  GameObjectHelper.Hub.EnchanterPanel,  "Enchanter",  typeof(EnchantSection)),
        (GameObjectHelper.Hub.SalvageButton,    GameObjectHelper.Hub.SalvagePanel,    "Salvage",    typeof(SalvageSection)),
        (GameObjectHelper.Hub.PlacesButton,     GameObjectHelper.Hub.PlacesPanel,     "Places",     typeof(PlacesSection)),
        (GameObjectHelper.Hub.BountyButton,     GameObjectHelper.Hub.BountyPanel,     "Bounty",     typeof(BountySection)),
    };

    [MenuItem("Tools/Scenes/Hub/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Hub scene and recreate all GameObjects from the scaffold?\n\nAny unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }

    [MenuItem("Tools/Scenes/Hub/Clear Scene")]
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

        var mgrGO = SceneScaffoldHelper.EnsureEmptyGameObject("HubManager", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<HubManager>(mgrGO);

        var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneScaffoldHelper.LogResults(SceneName, created, found); return; }

        // Hub is a wide-UI-heavy scene (8 section buttons in a row). Bias the scaler to
        // match width so narrow / portrait aspects don't inflate text & nav vertically.
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.matchWidthOrHeight = 0f;

        BuildHeader(canvas, ref created, ref found);
        BuildNavBar(canvas, ref created, ref found);
        var content = BuildContentPanel(canvas, ref created, ref found);
        BuildPanels(content, ref created, ref found);
        BuildMenuDropdown(canvas, ref created, ref found);
        BuildToast(canvas, ref created, ref found);

        SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    // ---------- Header ----------

    private static void BuildHeader(RectTransform canvas, ref int created, ref int found)
    {
        var header = FindOrMake(canvas, "Header", ref created, ref found);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 80f);
        header.anchoredPosition = Vector2.zero;
        Paint(header.gameObject, HubTheme.HeaderBg);

        var gold = MakeLabel(header, GameObjectHelper.Hub.GoldLabel, "Gold: 0g");
        gold.anchorMin = new Vector2(1f, 0.5f); gold.anchorMax = new Vector2(1f, 0.5f);
        gold.pivot = new Vector2(1f, 0.5f);
        gold.sizeDelta = new Vector2(400f, 60f);
        gold.anchoredPosition = new Vector2(-40f, 0f);
        var goldTmp = gold.GetComponent<TextMeshProUGUI>();
        goldTmp.alignment = TextAlignmentOptions.MidlineRight;
        goldTmp.fontSize = 36;
        goldTmp.color = HubTheme.Accent;
    }

    // ---------- Nav Bar ----------

    private static void BuildNavBar(RectTransform canvas, ref int created, ref int found)
    {
        var nav = FindOrMake(canvas, "NavBar", ref created, ref found);
        nav.anchorMin = new Vector2(0f, 1f);
        nav.anchorMax = new Vector2(1f, 1f);
        nav.pivot = new Vector2(0.5f, 1f);
        nav.sizeDelta = new Vector2(0f, 72f);
        nav.anchoredPosition = new Vector2(0f, -80f);
        Paint(nav.gameObject, HubTheme.PanelBg);

        // Two-container flex layout:
        //   NavBar  [HorizontalLayoutGroup]
        //   ├── MenuGroup  [LayoutElement.preferredWidth=180, flexibleWidth=0]  — single hamburger button
        //   └── ExitGroup  [LayoutElement.preferredWidth=360, flexibleWidth=0]  — Overworld + Battle
        // The 12 section buttons live inside MenuDropdown (built separately) and are revealed
        // when the hamburger is clicked. Section buttons keep their original names so HubManager.WireNavBar still resolves them.
        var navLayout = nav.gameObject.GetComponent<HorizontalLayoutGroup>() ?? nav.gameObject.AddComponent<HorizontalLayoutGroup>();
        navLayout.padding = new RectOffset(12, 12, 8, 8);
        navLayout.spacing = 12f;
        navLayout.childAlignment = TextAnchor.MiddleLeft;
        navLayout.childControlWidth = true;
        navLayout.childControlHeight = true;
        navLayout.childForceExpandWidth = true;
        navLayout.childForceExpandHeight = true;

        var menuGroup = FindOrMake(nav, "MenuGroup", ref created, ref found);
        ConfigureNavGroup(menuGroup, childSpacing: 0f);
        var menuLE = menuGroup.gameObject.GetComponent<LayoutElement>() ?? menuGroup.gameObject.AddComponent<LayoutElement>();
        menuLE.minWidth = 140f;
        menuLE.preferredWidth = 180f;
        menuLE.flexibleWidth = 0f;
        menuLE.flexibleHeight = 1f;

        var menuBtn = MakeButton(menuGroup, GameObjectHelper.Hub.MenuButton, "☰ Menu");
        StretchInLayout(menuBtn, minWidth: 140f, preferredWidth: 180f);

        // Spacer eats remaining width so MenuGroup hugs left and ExitGroup hugs right.
        var spacer = FindOrMake(nav, "NavSpacer", ref created, ref found);
        var spacerImg = spacer.GetComponent<Image>();
        if (spacerImg != null) { spacerImg.color = new Color(0f, 0f, 0f, 0f); spacerImg.raycastTarget = false; }
        var spacerLE = spacer.gameObject.GetComponent<LayoutElement>() ?? spacer.gameObject.AddComponent<LayoutElement>();
        spacerLE.minWidth = 0f;
        spacerLE.flexibleWidth = 1f;

        var exits = FindOrMake(nav, "ExitGroup", ref created, ref found);
        ConfigureNavGroup(exits, childSpacing: 8f);
        var exitLE = exits.gameObject.GetComponent<LayoutElement>() ?? exits.gameObject.AddComponent<LayoutElement>();
        exitLE.minWidth = 240f;
        exitLE.preferredWidth = 360f;
        exitLE.flexibleWidth = 0f;
        exitLE.flexibleHeight = 1f;

        var over = MakeButton(exits, GameObjectHelper.Hub.OverworldButton, "Overworld");
        StretchInLayout(over, minWidth: 110f, preferredWidth: 170f);

        var battle = MakeButton(exits, GameObjectHelper.Hub.BattleButton, "Battle");
        StretchInLayout(battle, minWidth: 110f, preferredWidth: 170f);
        Paint(battle.gameObject, HubTheme.AccentDim);
    }

    // ---------- Menu Dropdown ----------

    private static void BuildMenuDropdown(RectTransform canvas, ref int created, ref int found)
    {
        // Root: full-canvas container, sibling AFTER NavBar so it draws over ContentPanel.
        // Hidden by default; HubManager toggles SetActive on hamburger click.
        var dropdown = FindOrMake(canvas, GameObjectHelper.Hub.MenuDropdown, ref created, ref found);
        dropdown.SetAsLastSibling();
        dropdown.anchorMin = Vector2.zero; dropdown.anchorMax = Vector2.one;
        dropdown.offsetMin = Vector2.zero; dropdown.offsetMax = Vector2.zero;
        var dropImg = dropdown.GetComponent<Image>();
        if (dropImg != null) { dropImg.color = new Color(0f, 0f, 0f, 0f); dropImg.raycastTarget = false; }
        dropdown.gameObject.SetActive(false);

        // Backdrop: invisible click-catcher, fills the canvas, closes the dropdown when clicked.
        var backdrop = FindOrMake(dropdown, GameObjectHelper.Hub.MenuBackdrop, ref created, ref found);
        backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = Vector2.zero; backdrop.offsetMax = Vector2.zero;
        var bdImg = backdrop.GetComponent<Image>();
        if (bdImg == null) bdImg = backdrop.gameObject.AddComponent<Image>();
        bdImg.color = new Color(0f, 0f, 0f, 0.45f);
        bdImg.raycastTarget = true;
        var bdBtn = backdrop.gameObject.GetComponent<Button>() ?? backdrop.gameObject.AddComponent<Button>();
        bdBtn.transition = Selectable.Transition.None;
        bdBtn.targetGraphic = bdImg;

        // Panel: anchored to top-left under the NavBar, vertical stack of section buttons.
        var panel = FindOrMake(dropdown, GameObjectHelper.Hub.MenuPanel, ref created, ref found);
        panel.anchorMin = new Vector2(0f, 1f); panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        // 12 buttons × 48 + 11 × 4 spacing + 16 padding ≈ 636
        panel.sizeDelta = new Vector2(260f, 640f);
        panel.anchoredPosition = new Vector2(12f, -152f); // below Header(80) + NavBar(72)
        Paint(panel.gameObject, HubTheme.PanelBg);

        var vlg = panel.gameObject.GetComponent<VerticalLayoutGroup>() ?? panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        foreach (var entry in Sections)
        {
            var btn = MakeButton(panel, entry.buttonName, entry.label);
            var le = btn.gameObject.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 48f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
        }
    }

    private static void ConfigureNavGroup(RectTransform rt, float childSpacing)
    {
        // Transparent wrapper (SectionGroup / ExitGroup) that lays its button children out horizontally.
        var img = rt.GetComponent<Image>();
        if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
        if (img != null) img.raycastTarget = false;
        var hlg = rt.gameObject.GetComponent<HorizontalLayoutGroup>() ?? rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = childSpacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
    }

    private static void StretchInLayout(RectTransform btn, float minWidth, float preferredWidth)
    {
        // Let the parent HorizontalLayoutGroup drive size — clear manual anchor positioning.
        btn.anchorMin = new Vector2(0f, 0f); btn.anchorMax = new Vector2(1f, 1f);
        btn.pivot = new Vector2(0.5f, 0.5f);
        btn.offsetMin = Vector2.zero; btn.offsetMax = Vector2.zero;
        btn.anchoredPosition = Vector2.zero;
        btn.sizeDelta = Vector2.zero;
        var le = btn.gameObject.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
        le.minWidth = minWidth;
        le.preferredWidth = preferredWidth;
        le.flexibleWidth = 1f;
        le.minHeight = 48f;
        le.preferredHeight = 56f;
        le.flexibleHeight = 0f;
    }

    // ---------- Toast ----------

    private static void BuildToast(RectTransform canvas, ref int created, ref int found)
    {
        var toast = FindOrMake(canvas, Scripts.Hub.HubToast.GameObjectName, ref created, ref found);
        toast.anchorMin = new Vector2(0.5f, 1f);
        toast.anchorMax = new Vector2(0.5f, 1f);
        toast.pivot = new Vector2(0.5f, 1f);
        toast.sizeDelta = new Vector2(640f, 56f);
        toast.anchoredPosition = new Vector2(0f, -170f); // below Header+NavBar
        var bgImg = toast.GetComponent<Image>();
        if (bgImg != null) { bgImg.color = new Color(0.08f, 0.08f, 0.10f, 0.9f); bgImg.raycastTarget = false; }

        var cg = toast.gameObject.GetComponent<CanvasGroup>();
        if (cg == null) cg = toast.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        SceneScaffoldHelper.EnsureScript<Scripts.Hub.HubToast>(toast.gameObject);

        // Label child
        var labelRT = MakeLabel(toast, "Label", "");
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(16f, 4f); labelRT.offsetMax = new Vector2(-16f, -4f);
        var tmp = labelRT.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 26;
            tmp.color = HubTheme.Accent;
            tmp.raycastTarget = false;
        }
    }

    // ---------- Content root ----------

    private static RectTransform BuildContentPanel(RectTransform canvas, ref int created, ref int found)
    {
        var content = FindOrMake(canvas, "ContentPanel", ref created, ref found);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.offsetMin = new Vector2(16f, 16f);
        content.offsetMax = new Vector2(-16f, -152f); // leave room for Header (80) + NavBar (72)
        Paint(content.gameObject, new Color(0f, 0f, 0f, 0f));
        return content;
    }

    // ---------- Section panels ----------

    private static void BuildPanels(RectTransform content, ref int created, ref int found)
    {
        foreach (var entry in Sections)
        {
            var panel = FindOrMake(content, entry.panelName, ref created, ref found);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            Paint(panel.gameObject, HubTheme.PanelBg);
            AddScriptByType(panel.gameObject, entry.sectionType);
            AddGoldLabel(panel);
        }

        // Specialised children per section:
        PopulateParty(content.Find(GameObjectHelper.Hub.PartyPanel) as RectTransform);
        PopulateShop(content.Find(GameObjectHelper.Hub.ShopPanel) as RectTransform);
        PopulateAlchemist(content.Find(GameObjectHelper.Hub.AlchemistPanel) as RectTransform);
        PopulateInn(content.Find(GameObjectHelper.Hub.ResidencePanel) as RectTransform);
        PopulateBlacksmith(content.Find(GameObjectHelper.Hub.BlacksmithPanel) as RectTransform);
        PopulateTraining(content.Find(GameObjectHelper.Hub.TrainingPanel) as RectTransform);
        PopulateEquip(content.Find(GameObjectHelper.Hub.EquipPanel) as RectTransform);
        PopulateInventory(content.Find(GameObjectHelper.Hub.InventoryPanel) as RectTransform);
        PopulateEnchanter(content.Find(GameObjectHelper.Hub.EnchanterPanel) as RectTransform);
        PopulateSalvage(content.Find(GameObjectHelper.Hub.SalvagePanel) as RectTransform);
        PopulatePlaces(content.Find(GameObjectHelper.Hub.PlacesPanel) as RectTransform);
        PopulateBounty(content.Find(GameObjectHelper.Hub.BountyPanel) as RectTransform);
    }

    private static void PopulateBounty(RectTransform panel)
    {
        if (panel == null) return;
        // Left: scrollable bounty list. Right: detail + confirm + (optional) abandon.
        MakeNamedScrollView(panel, "ItemList", new Vector2(0f, 0f), new Vector2(0.60f, 1f));
        MakeDetail(panel, new Vector2(0.60f, 0.24f), new Vector2(1f, 1f));
        MakeConfirm(panel, new Vector2(0.60f, 0.12f), new Vector2(1f, 0.24f));

        var abandon = MakeButton(panel, "AbandonButton", "Abandon");
        abandon.anchorMin = new Vector2(0.60f, 0f);
        abandon.anchorMax = new Vector2(1f, 0.12f);
        abandon.offsetMin = new Vector2(16f, 8f);
        abandon.offsetMax = new Vector2(-16f, -4f);
    }

    private static void PopulatePlaces(RectTransform panel)
    {
        if (panel == null) return;
        // Left column: 5 biome buttons stacked vertically (Field, Forest, Ruins, Cave, Boss).
        // Right column: detail + confirm.
        MakeBiomeButton(panel, "FieldButton",  "Field",  new Vector2(0.02f, 0.78f), new Vector2(0.58f, 0.96f));
        MakeBiomeButton(panel, "ForestButton", "Forest", new Vector2(0.02f, 0.60f), new Vector2(0.58f, 0.78f));
        MakeBiomeButton(panel, "RuinsButton",  "Ruins",  new Vector2(0.02f, 0.42f), new Vector2(0.58f, 0.60f));
        MakeBiomeButton(panel, "CaveButton",   "Cave",   new Vector2(0.02f, 0.24f), new Vector2(0.58f, 0.42f));
        MakeBiomeButton(panel, "BossButton",   "Boss",   new Vector2(0.02f, 0.06f), new Vector2(0.58f, 0.24f));

        MakeDetail(panel, new Vector2(0.60f, 0.16f), new Vector2(0.98f, 0.96f));
        MakeConfirm(panel, new Vector2(0.60f, 0.04f), new Vector2(0.98f, 0.16f));
    }

    private static RectTransform MakeBiomeButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = MakeButton(parent, name, label);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(6f, 6f); rt.offsetMax = new Vector2(-6f, -6f);
        return rt;
    }

    private static void PopulateParty(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, GameObjectHelper.Hub.RosterList,
            new Vector2(0f, 0f), new Vector2(0.33f, 1f));
        MakeNamedScrollView(panel, GameObjectHelper.Hub.PartyList,
            new Vector2(0.33f, 0f), new Vector2(0.66f, 1f));
        MakeDetail(panel, new Vector2(0.66f, 0f), new Vector2(1f, 1f));
    }

    private static void PopulateShop(RectTransform panel)
    {
        if (panel == null) return;
        // Top row — mode toggle (Buy / Sell)
        MakeTab(panel, GameObjectHelper.Hub.BuyTab,  "Buy",  new Vector2(0f,    0.88f), new Vector2(0.30f, 1f));
        MakeTab(panel, GameObjectHelper.Hub.SellTab, "Sell", new Vector2(0.30f, 0.88f), new Vector2(0.60f, 1f));
        // Second row — category filters (shared names with Inventory; scoped per-panel by the section)
        MakeTab(panel, GameObjectHelper.Hub.FilterAll,   "All",       new Vector2(0f,    0.78f), new Vector2(0.15f, 0.88f));
        MakeTab(panel, GameObjectHelper.Hub.FilterEquip, "Gear",      new Vector2(0.15f, 0.78f), new Vector2(0.30f, 0.88f));
        MakeTab(panel, GameObjectHelper.Hub.FilterCons,  "Potions",   new Vector2(0.30f, 0.78f), new Vector2(0.45f, 0.88f));
        MakeTab(panel, GameObjectHelper.Hub.FilterMats,  "Materials", new Vector2(0.45f, 0.78f), new Vector2(0.60f, 0.88f));
        MakeNamedScrollView(panel, "ItemList",
            new Vector2(0f, 0f), new Vector2(0.60f, 0.78f));
        MakeDetail(panel, new Vector2(0.60f, 0.12f), new Vector2(1f, 1f));
        MakeConfirm(panel, new Vector2(0.60f, 0f), new Vector2(1f, 0.12f));
    }

    private static void PopulateAlchemist(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, "ItemList", new Vector2(0f, 0f), new Vector2(0.60f, 1f));
        MakeDetail(panel, new Vector2(0.60f, 0.12f), new Vector2(1f, 1f));
        MakeConfirm(panel, new Vector2(0.60f, 0f), new Vector2(1f, 0.12f));
    }

    private static void PopulateInn(RectTransform panel)
    {
        if (panel == null) return;
        MakeDetail(panel, new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.90f));
        var rest = MakeButton(panel, "RestButton", "Rest");
        rest.anchorMin = new Vector2(0.35f, 0.10f);
        rest.anchorMax = new Vector2(0.65f, 0.20f);
        rest.offsetMin = rest.offsetMax = Vector2.zero;
    }

    private static void PopulateBlacksmith(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, "ItemList", new Vector2(0f, 0f), new Vector2(0.60f, 1f));
        MakeDetail(panel, new Vector2(0.60f, 0.12f), new Vector2(1f, 1f));
        MakeConfirm(panel, new Vector2(0.60f, 0f), new Vector2(1f, 0.12f));
    }

    private static void PopulateTraining(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, GameObjectHelper.Hub.HeroList,     new Vector2(0f, 0f),    new Vector2(0.30f, 1f));
        MakeNamedScrollView(panel, GameObjectHelper.Hub.TrainingList, new Vector2(0.30f, 0f), new Vector2(0.65f, 1f));
        MakeDetail(panel, new Vector2(0.65f, 0.12f), new Vector2(1f, 1f));
        MakeConfirm(panel, new Vector2(0.65f, 0f), new Vector2(1f, 0.12f));
    }

    private static void PopulateEquip(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, GameObjectHelper.Hub.HeroList,   new Vector2(0f, 0f),    new Vector2(0.25f, 1f));
        MakeNamedScrollView(panel, GameObjectHelper.Hub.SlotList,   new Vector2(0.25f, 0f), new Vector2(0.50f, 1f));
        MakeNamedScrollView(panel, GameObjectHelper.Hub.ItemPicker, new Vector2(0.50f, 0f), new Vector2(0.75f, 1f));
        var stats = MakeLabel(panel, GameObjectHelper.Hub.StatsLabel, "");
        stats.anchorMin = new Vector2(0.75f, 0f);
        stats.anchorMax = new Vector2(1f, 1f);
        stats.offsetMin = new Vector2(16f, 16f); stats.offsetMax = new Vector2(-16f, -16f);
    }

    private static void PopulateSalvage(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, "ItemList", new Vector2(0f, 0f), new Vector2(0.60f, 1f));
        MakeDetail(panel, new Vector2(0.60f, 0.12f), new Vector2(1f, 1f));
        MakeConfirm(panel, new Vector2(0.60f, 0f), new Vector2(1f, 0.12f));
    }

    private static void PopulateEnchanter(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, "ItemList", new Vector2(0f, 0f), new Vector2(0.50f, 1f));
        MakeDetail(panel, new Vector2(0.50f, 0.12f), new Vector2(1f, 1f));
        MakeConfirm(panel, new Vector2(0.50f, 0f), new Vector2(1f, 0.12f));
    }

    private static void PopulateInventory(RectTransform panel)
    {
        if (panel == null) return;
        MakeNamedScrollView(panel, "ItemList", new Vector2(0f, 0f), new Vector2(0.60f, 0.88f));
        MakeTab(panel, GameObjectHelper.Hub.FilterAll,   "All",   new Vector2(0f,    0.88f), new Vector2(0.15f, 1f));
        MakeTab(panel, GameObjectHelper.Hub.FilterEquip, "Gear",  new Vector2(0.15f, 0.88f), new Vector2(0.30f, 1f));
        MakeTab(panel, GameObjectHelper.Hub.FilterCons,  "Potions", new Vector2(0.30f, 0.88f), new Vector2(0.45f, 1f));
        MakeTab(panel, GameObjectHelper.Hub.FilterMats,  "Materials", new Vector2(0.45f, 0.88f), new Vector2(0.60f, 1f));
        MakeDetail(panel, new Vector2(0.60f, 0f), new Vector2(1f, 1f));
    }

    // ---------- Primitive helpers (local to this scaffold) ----------

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
        // Snappier feedback than the Unity default: visible highlight + clear pressed flash + quick fade.
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

    private static RectTransform MakeTab(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = MakeButton(parent, name, label);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(2f, 2f); rt.offsetMax = new Vector2(-2f, -2f);
        return rt;
    }

    private static RectTransform MakeDetail(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = MakeLabel(parent, GameObjectHelper.Hub.DetailLabel, "");
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(16f, 8f); rt.offsetMax = new Vector2(-16f, -8f);
        return rt;
    }

    private static RectTransform MakeConfirm(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = MakeButton(parent, "ConfirmButton", "Confirm");
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(16f, 8f); rt.offsetMax = new Vector2(-16f, -8f);
        var img = rt.GetComponent<Image>();
        if (img != null) img.color = HubTheme.Accent;
        return rt;
    }

    /// <summary>Creates a ScrollView whose root is named exactly `name` (not "ScrollView"),
    /// so HubSection.Find("{name}/Viewport/Content") resolves.</summary>
    private static void MakeNamedScrollView(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var existing = parent.Find(name);
        if (existing != null) return;

        var rootGO = new GameObject(name);
        rootGO.layer = LayerMask.NameToLayer("UI");
        var rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.SetParent(parent, false);
        rootRT.anchorMin = anchorMin; rootRT.anchorMax = anchorMax;
        rootRT.offsetMin = new Vector2(8f, 8f); rootRT.offsetMax = new Vector2(-8f, -8f);
        rootGO.AddComponent<CanvasRenderer>();
        var rootImg = rootGO.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.35f);
        rootImg.raycastTarget = true;

        // Viewport — Mask + ScrollRect host
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

        // Content — vertical layout + size fitter
        var contentGO = new GameObject("Content");
        contentGO.layer = LayerMask.NameToLayer("UI");
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.SetParent(vpRT, false);
        contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f);
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

        Undo.RegisterCreatedObjectUndo(rootGO, $"Create {name}");
    }

    private static void AddGoldLabel(RectTransform panel)
    {
        if (panel.Find(GameObjectHelper.Hub.GoldLabel) != null) return;
        // Every panel gets its own hidden GoldLabel that HubManager updates when visible.
        var gold = MakeLabel(panel, GameObjectHelper.Hub.GoldLabel, "Gold: 0g");
        gold.anchorMin = new Vector2(1f, 1f); gold.anchorMax = new Vector2(1f, 1f);
        gold.pivot = new Vector2(1f, 1f);
        gold.sizeDelta = new Vector2(300f, 40f);
        gold.anchoredPosition = new Vector2(-16f, -8f);
        var tmp = gold.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.TopRight;
        tmp.fontSize = 22;
        tmp.color = HubTheme.Accent;
    }

    private static void AddScriptByType(GameObject go, System.Type t)
    {
        if (go == null || t == null) return;
        if (go.GetComponent(t) == null) go.AddComponent(t);
    }
}
