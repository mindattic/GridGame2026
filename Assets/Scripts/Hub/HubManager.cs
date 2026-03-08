using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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

namespace Scripts.Hub
{
/// <summary>
/// HUBMANAGER - Central hub/town scene controller.
/// 
/// PURPOSE:
/// Manages the hub scene where players can access various
/// services between battles (party, shop, medical, etc.).
/// Owns a shared PlayerInventory loaded from the current save,
/// and writes it back before scene transitions.
/// 
/// SECTIONS:
/// - Party: Manage party composition
/// - Shop: Buy items/equipment/materials
/// - Medical: Heal party members
/// - Residence: Recruit new characters
/// - Blacksmith: Craft and repair equipment
/// - Training: Learn new abilities
/// 
/// NAVIGATION:
/// Button bar at top switches between sections.
/// Only one section visible at a time.
/// 
/// RELATED FILES:
/// - PartySectionController.cs: Party management
/// - ShopSectionController.cs: Shop functionality
/// - BlacksmithSectionController.cs: Crafting/repair
/// - TrainingSectionController.cs: Ability training
/// - ProfileHelper.cs: Save data access
/// - SceneHelper.cs: Scene transitions
/// </summary>
public class HubManager : MonoBehaviour
{
    // ===================== Shared State =====================

    /// <summary>Shared player inventory used by all sections.</summary>
    public PlayerInventory SharedInventory { get; private set; } = new PlayerInventory();

    /// <summary>Shared party loadout used by all sections.</summary>
    public PartyLoadout SharedLoadout { get; private set; } = new PartyLoadout();

    // ===================== Navigation Buttons =====================

    private Button partyButton;
    private Button shopButton;
    private Button medicalButton;
    private Button residenceButton;
    private Button blacksmithButton;
    private Button trainingButton;
    private Button equipButton;
    private Button inventoryButton;
    private Button battlePrepButton;
    private Button overworldButton;
    private Button battleButton;

    // ===================== Section Panels =====================

    private RectTransform partyPanel;
    private RectTransform shopPanel;
    private RectTransform medicalPanel;
    private RectTransform residencePanel;
    private RectTransform blacksmithPanel;
    private RectTransform trainingPanel;
    private RectTransform equipPanel;
    private RectTransform inventoryPanel;
    private RectTransform battlePrepPanel;

    // ===================== Controllers =====================

    private PartySectionController partyController;
    private ShopSectionController shopController;
    private MedicalSectionController medicalController;
    private ResidenceSectionController residenceController;
    private BlacksmithSectionController blacksmithController;
    private TrainingSectionController trainingController;
    private EquipSectionController equipController;
    private InventorySectionController inventoryController;
    private BattlePrepSectionController battlePrepController;

    // Track active section.
    private RectTransform activePanel;

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        LoadFromSave();
        ResolveSceneObjects();
        AttachTiltParallax();
        InitializeSections();
        WireButtonListeners();

        // Ensure a clean start state: disable all panels then show Party.
        GoToPartySection();
    }

    /// <summary>Cleans up resources when the object is destroyed.</summary>
    private void OnDestroy()
    {
        if (partyButton != null) partyButton.onClick.RemoveListener(GoToPartySection);
        if (shopButton != null) shopButton.onClick.RemoveListener(GoToShopSection);
        if (medicalButton != null) medicalButton.onClick.RemoveListener(GoToMedicalSection);
        if (residenceButton != null) residenceButton.onClick.RemoveListener(GoToResidenceSection);
        if (blacksmithButton != null) blacksmithButton.onClick.RemoveListener(GoToBlacksmithSection);
        if (trainingButton != null) trainingButton.onClick.RemoveListener(GoToTrainingSection);
        if (equipButton != null) equipButton.onClick.RemoveListener(GoToEquipSection);
        if (inventoryButton != null) inventoryButton.onClick.RemoveListener(GoToInventorySection);
        if (battlePrepButton != null) battlePrepButton.onClick.RemoveListener(GoToBattlePrepSection);
        if (overworldButton != null) overworldButton.onClick.RemoveListener(GoToOverworld);
        if (battleButton != null) battleButton.onClick.RemoveListener(GoToBattle);
    }

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start()
    {
        scene.FadeIn();
    }

    // ===================== Save / Load =====================

    /// <summary>Loads inventory, equipment, and training from the current save.</summary>
    private void LoadFromSave()
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save == null) return;

        // Inventory
        if (save.Inventory != null)
            SharedInventory.LoadFromSaveData(save.Inventory);

        // Equipment
        if (save.Equipment != null)
            SharedLoadout.LoadFromSave(save.Equipment);
    }

    /// <summary>Writes inventory, equipment, and training back to the current save.</summary>
    public void WriteToSave()
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save == null) return;

        save.Inventory = SharedInventory.ToSaveData();
        save.Equipment = SharedLoadout.ToSave();
    }

    // ===================== Resolve Scene Objects =====================

    /// <summary>Resolve scene objects.</summary>
    private void ResolveSceneObjects()
    {
        // Buttons
        partyButton = GameObject.Find(GameObjectHelper.Hub.PartyButton)?.GetComponent<Button>();
        shopButton = GameObject.Find(GameObjectHelper.Hub.ShopButton)?.GetComponent<Button>();
        medicalButton = GameObject.Find(GameObjectHelper.Hub.MedicalButton)?.GetComponent<Button>();
        residenceButton = GameObject.Find(GameObjectHelper.Hub.ResidenceButton)?.GetComponent<Button>();
        blacksmithButton = GameObject.Find(GameObjectHelper.Hub.BlacksmithButton)?.GetComponent<Button>();
        trainingButton = GameObject.Find(GameObjectHelper.Hub.TrainingButton)?.GetComponent<Button>();
        equipButton = GameObject.Find(GameObjectHelper.Hub.EquipButton)?.GetComponent<Button>();
        inventoryButton = GameObject.Find(GameObjectHelper.Hub.InventoryButton)?.GetComponent<Button>();
        battlePrepButton = GameObject.Find(GameObjectHelper.Hub.BattlePrepButton)?.GetComponent<Button>();
        overworldButton = GameObject.Find(GameObjectHelper.Hub.OverworldButton)?.GetComponent<Button>();
        battleButton = GameObject.Find(GameObjectHelper.Hub.BattleButton)?.GetComponent<Button>();

        // Panels
        partyPanel = GameObject.Find(GameObjectHelper.Hub.PartyPanel)?.GetComponent<RectTransform>();
        shopPanel = GameObject.Find(GameObjectHelper.Hub.ShopPanel)?.GetComponent<RectTransform>();
        medicalPanel = GameObject.Find(GameObjectHelper.Hub.MedicalPanel)?.GetComponent<RectTransform>();
        residencePanel = GameObject.Find(GameObjectHelper.Hub.ResidencePanel)?.GetComponent<RectTransform>();
        blacksmithPanel = GameObject.Find(GameObjectHelper.Hub.BlacksmithPanel)?.GetComponent<RectTransform>();
        trainingPanel = GameObject.Find(GameObjectHelper.Hub.TrainingPanel)?.GetComponent<RectTransform>();
        equipPanel = GameObject.Find(GameObjectHelper.Hub.EquipPanel)?.GetComponent<RectTransform>();
        inventoryPanel = GameObject.Find(GameObjectHelper.Hub.InventoryPanel)?.GetComponent<RectTransform>();
        battlePrepPanel = GameObject.Find(GameObjectHelper.Hub.BattlePrepPanel)?.GetComponent<RectTransform>();
    }

    /// <summary>Attach tilt parallax.</summary>
    private void AttachTiltParallax()
    {
        void Ensure(RectTransform rt)
        {
            if (rt == null) return;
            var t = rt.GetComponent<TiltParallax>();
            if (t == null) t = rt.gameObject.AddComponent<TiltParallax>();
            t.amplitude = 12f;
            t.smoothing = 6f;
            t.deadzone = 0.015f;
            t.writeToOutput = false;
        }
        Ensure(partyPanel);
        Ensure(shopPanel);
        Ensure(medicalPanel);
        Ensure(residencePanel);
        Ensure(blacksmithPanel);
        Ensure(trainingPanel);
        Ensure(equipPanel);
        Ensure(inventoryPanel);
        Ensure(battlePrepPanel);
    }

    /// <summary>Collect all section panels (non-null) for iteration.</summary>
    private IEnumerable<RectTransform> AllPanels()
    {
        if (partyPanel != null) yield return partyPanel;
        if (shopPanel != null) yield return shopPanel;
        if (medicalPanel != null) yield return medicalPanel;
        if (residencePanel != null) yield return residencePanel;
        if (blacksmithPanel != null) yield return blacksmithPanel;
        if (trainingPanel != null) yield return trainingPanel;
        if (equipPanel != null) yield return equipPanel;
        if (inventoryPanel != null) yield return inventoryPanel;
        if (battlePrepPanel != null) yield return battlePrepPanel;
    }

    // ===================== Day/Night Tint =====================

    /// <summary>
    /// Creates a frozen DayNightCycle overlay on the Hub canvas.
    /// Reads the cycle position from save data or the static snapshot,
    /// applies the matching color, and pauses so the tint stays fixed
    /// for the entire Hub visit.
    /// </summary>
    private void ApplyDayNightTint()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        // Create overlay GameObject — start inactive so Awake/OnEnable
        // don't fire until configuration is complete.
        var go = new GameObject("DayNightCycle");
        go.SetActive(false);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        // Stretch to fill screen
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Image is the tint overlay (raycast disabled so it doesn't block input)
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;

        // Configure cycle: overlay-only mode, frozen (not playing).
        // Because the GO is inactive, AddComponent won't trigger Awake/OnEnable
        // so these values are in place before the first lifecycle callbacks.
        var dnc = go.AddComponent<DayNightCycle>();
        dnc.overlayImage = img;
        dnc.applyMode = DayNightCycle.ApplyMode.OverlayImage;
        dnc.playOnEnable = false;

        // Determine the T01 to use: prefer static snapshot, fall back to save data
        float t01 = -1f;
        if (DayNightState.HasSnapshot)
        {
            t01 = DayNightState.T01;
        }
        else
        {
            var ow = ProfileHelper.CurrentProfile?.CurrentSave?.Overworld;
            if (ow != null) t01 = ow.DayNightT01;
        }

        // Activate — Awake and OnEnable now run with correct configuration
        go.SetActive(true);

        // Default to midday (minimal tint) when no saved time exists
        if (t01 < 0f)
            t01 = dnc.GetPhaseMidpointT01(DayNightCycle.DayPhase.Day);

        // Jump to the saved position (frozen because playOnEnable is false)
        dnc.CycleTime01 = t01;
    }

    /// <summary>Wire button listeners.</summary>
    private void WireButtonListeners()
    {
        if (partyButton != null) { partyButton.onClick.RemoveListener(GoToPartySection); partyButton.onClick.AddListener(GoToPartySection); }
        if (shopButton != null) { shopButton.onClick.RemoveListener(GoToShopSection); shopButton.onClick.AddListener(GoToShopSection); }
        if (medicalButton != null) { medicalButton.onClick.RemoveListener(GoToMedicalSection); medicalButton.onClick.AddListener(GoToMedicalSection); }
        if (residenceButton != null) { residenceButton.onClick.RemoveListener(GoToResidenceSection); residenceButton.onClick.AddListener(GoToResidenceSection); }
        if (blacksmithButton != null) { blacksmithButton.onClick.RemoveListener(GoToBlacksmithSection); blacksmithButton.onClick.AddListener(GoToBlacksmithSection); }
        if (trainingButton != null) { trainingButton.onClick.RemoveListener(GoToTrainingSection); trainingButton.onClick.AddListener(GoToTrainingSection); }
        if (equipButton != null) { equipButton.onClick.RemoveListener(GoToEquipSection); equipButton.onClick.AddListener(GoToEquipSection); }
        if (inventoryButton != null) { inventoryButton.onClick.RemoveListener(GoToInventorySection); inventoryButton.onClick.AddListener(GoToInventorySection); }
        if (battlePrepButton != null) { battlePrepButton.onClick.RemoveListener(GoToBattlePrepSection); battlePrepButton.onClick.AddListener(GoToBattlePrepSection); }
        if (overworldButton != null) { overworldButton.onClick.RemoveListener(GoToOverworld); overworldButton.onClick.AddListener(GoToOverworld); }
        if (battleButton != null) { battleButton.onClick.RemoveListener(GoToBattle); battleButton.onClick.AddListener(GoToBattle); }
    }

    /// <summary>Ensures each section controller is present and initialized.</summary>
    private void InitializeSections()
    {
        if (partyPanel != null) partyController = EnsureController<PartySectionController>(partyPanel);
        if (shopPanel != null) shopController = EnsureController<ShopSectionController>(shopPanel);
        if (medicalPanel != null) medicalController = EnsureController<MedicalSectionController>(medicalPanel);
        if (residencePanel != null) residenceController = EnsureController<ResidenceSectionController>(residencePanel);
        if (blacksmithPanel != null) blacksmithController = EnsureController<BlacksmithSectionController>(blacksmithPanel);
        if (trainingPanel != null) trainingController = EnsureController<TrainingSectionController>(trainingPanel);
        if (equipPanel != null) equipController = EnsureController<EquipSectionController>(equipPanel);
        if (inventoryPanel != null) inventoryController = EnsureController<InventorySectionController>(inventoryPanel);
        if (battlePrepPanel != null) battlePrepController = EnsureController<BattlePrepSectionController>(battlePrepPanel);

        partyController?.Initialize(this);
        shopController?.Initialize(this);
        medicalController?.Initialize(this);
        residenceController?.Initialize(this);
        blacksmithController?.Initialize(this);
        trainingController?.Initialize(this);
        equipController?.Initialize(this);
        inventoryController?.Initialize(this);
        battlePrepController?.Initialize(this);
    }

    /// <summary>Generic helper that guarantees a controller component exists on a panel.</summary>
    private T EnsureController<T>(RectTransform panel) where T : Component
    {
        var existing = panel.GetComponent<T>();
        if (existing != null) return existing;
        return panel.gameObject.AddComponent<T>();
    }

    /// <summary>Core section activation logic.</summary>
    private void Activate(RectTransform panel)
    {
        if (panel == null) return;

        // Auto-save state when switching sections
        WriteToSave();
        ProfileHelper.Save(false);

        foreach (var p in AllPanels())
        {
            if (p == null) continue;
            bool enable = p == panel;
            if (p.gameObject.activeSelf != enable)
                p.gameObject.SetActive(enable);
        }
        activePanel = panel;
    }

    // ===================== Section Navigation =====================

    /// <summary>Switches to Party section (default landing view).</summary>
    public void GoToPartySection()
    {
        Activate(partyPanel);
        partyController?.OnActivated();
    }

    /// <summary>Switches to Shop section.</summary>
    public void GoToShopSection()
    {
        Activate(shopPanel);
        shopController?.OnActivated();
    }

    /// <summary>Switches to Medical section.</summary>
    public void GoToMedicalSection()
    {
        Activate(medicalPanel);
        medicalController?.OnActivated();
    }

    /// <summary>Switches to Residence section.</summary>
    public void GoToResidenceSection()
    {
        Activate(residencePanel);
        residenceController?.OnActivated();
    }

    /// <summary>Switches to Blacksmith section.</summary>
    public void GoToBlacksmithSection()
    {
        Activate(blacksmithPanel);
        blacksmithController?.OnActivated();
    }

    /// <summary>Switches to Training section.</summary>
    public void GoToTrainingSection()
    {
        Activate(trainingPanel);
        trainingController?.OnActivated();
    }

    /// <summary>Switches to Equip section.</summary>
    public void GoToEquipSection()
    {
        Activate(equipPanel);
        equipController?.OnActivated();
    }

    /// <summary>Switches to Inventory section.</summary>
    public void GoToInventorySection()
    {
        Activate(inventoryPanel);
        inventoryController?.OnActivated();
    }

    /// <summary>Switches to Battle Prep section.</summary>
    public void GoToBattlePrepSection()
    {
        Activate(battlePrepPanel);
        battlePrepController?.OnActivated();
    }

    // ===================== Scene Transitions =====================

    /// <summary>Saves and transitions to overworld.</summary>
    public void GoToOverworld()
    {
        WriteToSave();
        scene.Fade.ToOverworld();
    }

    /// <summary>Saves and transitions to battle.</summary>
    public void GoToBattle()
    {
        WriteToSave();
        scene.Fade.ToGame();
    }
}

}
