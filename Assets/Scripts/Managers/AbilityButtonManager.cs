using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Libraries;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// ABILITYBUTTONMANAGER - Manages ability button UI for heroes.
/// 
/// PURPOSE:
/// Creates and manages the ability buttons shown when a hero is selected.
/// All buttons for every hero are pre-spawned at Start() and simply
/// shown/hidden when the selected hero changes. No dynamic spawning,
/// despawning, or re-binding at runtime.
/// 
/// BUTTON LIFECYCLE:
/// 1. Start() → BuildAllHeroButtons() creates buttons for ALL heroes, hidden
/// 2. Show(hero) → activates that hero's buttons in save-file order
/// 3. Hide() → deactivates all buttons
/// 
/// ORDERING:
/// Button order matches the AbilityBarSlots saved in HeroEquipmentSave
/// (assigned by the player in the Hub scene). When no save data exists,
/// falls back to class defaults (e.g. Cleric gets Heal + Smite).
/// 
/// INPUT LOCKING:
/// When a button is clicked the InputMode changes to AnyTarget or
/// LinearTarget. While in those modes the ability buttons become
/// non-interactable so no other ability can be activated until
/// targeting completes or is cancelled.
/// 
/// RELATED FILES:
/// - AbilityButtonFactory.cs: Creates button GameObjects
/// - AbilityButton.cs: Individual button component
/// - AbilityManager.cs: Handles targeting/casting
/// - ActorCard.cs: Contains AbilityButtonContainer
/// - Ability.cs: Ability data definition
/// - HeroEquipmentSave.AbilityBarSlots: Persisted bar layout
/// 
/// ACCESS: g.AbilityButtonManager
/// </summary>
public class AbilityButtonManager : MonoBehaviour
{
    #region Fields

    /// <summary>Maximum ability buttons (slots) per hero. Includes skills and equipped consumable items.</summary>
    public const int MaxAbilitySlots = 5;

    private Transform abilityButtonContainer;
    private HorizontalLayoutGroup layoutGroup;

    /// <summary>Buttons organized by hero's CharacterClass.</summary>
    private readonly Dictionary<CharacterClass, List<AbilityButton>> buttonsByHero = new();

    /// <summary>All ability buttons for quick iteration.</summary>
    private readonly List<AbilityButton> allButtons = new();

    /// <summary>The CharacterClass whose buttons are currently visible (None if hidden).</summary>
    private CharacterClass visibleHero = CharacterClass.None;

    #endregion

    #region Initialization

    /// <summary>Initializes component references and state.</summary>
    public void Awake()
    {
        abilityButtonContainer = GameObjectHelper.Game.Card.AbilityButtonContainer;

        // Configure HorizontalLayoutGroup for left-aligned buttons
        if (abilityButtonContainer != null)
        {
            layoutGroup = abilityButtonContainer.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = abilityButtonContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;
        }
    }

    /// <summary>Defers initialization until GameReady so all heroes are spawned.</summary>
    private void Start()
    {
        GameReady.WhenReady(this, Initialize);
    }

    /// <summary>Builds all hero buttons and subscribes to events.
    /// Called via GameReady.WhenReady so g.Actors.Heroes is fully populated.</summary>
    private void Initialize()
    {
        BuildAllHeroButtons();
        HideAll();

        // Subscribe to input mode changes to lock/unlock buttons during targeting
        if (g.InputManager != null)
            g.InputManager.OnInputModeChanged += OnInputModeChanged;

        // If a hero is already selected (e.g. auto-selected at turn start), show their buttons
        var selected = g.Actors.SelectedActor;
        if (selected != null && selected.IsHero && selected.IsPlaying)
            Show(selected);
    }

    private void OnDestroy()
    {
        if (g.InputManager != null)
            g.InputManager.OnInputModeChanged -= OnInputModeChanged;
    }

    #endregion

    #region Interaction Guards

    /// <summary>Returns true if ability button clicks should be blocked.
    /// Only allows interaction during PlayerTurn with no sequences executing.</summary>
    private static bool IsInteractionLocked()
    {
        if (g.InputManager == null || g.SequenceManager == null) return true;
        if (g.SequenceManager.IsExecuting) return true;
        return g.InputManager.InputMode != InputMode.PlayerTurn;
    }

    /// <summary>Handles input mode changes. Only PlayerTurn enables ability buttons;
    /// all other modes (targeting, enemy turn, sequences, etc.) disable them.</summary>
    private void OnInputModeChanged(InputMode newMode)
    {
        if (newMode == InputMode.PlayerTurn)
        {
            // Verify the visible hero is still valid (may have died during enemy turn)
            if (visibleHero != CharacterClass.None)
            {
                var selected = g.Actors.SelectedActor;
                if (selected == null || !selected.IsPlaying || !selected.IsHero || selected.characterClass != visibleHero)
                {
                    HideAll();
                    return;
                }
            }

            // Re-enable buttons for visible hero
            RefreshVisibleInteractables();
        }
        else
        {
            // Any non-PlayerTurn mode → disable all ability buttons
            SetAllButtonsInteractable(false);
        }
    }

    /// <summary>Sets all ability buttons interactable or non-interactable.</summary>
    private void SetAllButtonsInteractable(bool interactable)
    {
        foreach (var btn in allButtons)
            if (btn != null && btn.button != null)
                btn.button.interactable = interactable;
    }

    /// <summary>Re-evaluates interactable state for the visible hero's buttons based on current mana.</summary>
    private void RefreshVisibleInteractables()
    {
        if (visibleHero == CharacterClass.None) return;
        if (!buttonsByHero.TryGetValue(visibleHero, out var list)) return;
        float mana = g.ManaPoolManager != null ? g.ManaPoolManager.heroMana : 0f;
        foreach (var btn in list)
            if (btn != null && btn.gameObject.activeSelf)
                btn.UpdateInteractable(mana);
    }

    #endregion

    #region Button Building

    /// <summary>Creates ability buttons for all heroes at startup.</summary>
    private void BuildAllHeroButtons()
    {
        buttonsByHero.Clear();
        allButtons.Clear();

        var heroes = g.Actors.Heroes.Where(h => h != null).ToList();
        foreach (var hero in heroes)
        {
            var characterClass = hero.characterClass;
            if (characterClass == CharacterClass.None) continue;
            if (buttonsByHero.ContainsKey(characterClass)) continue; // already built (e.g., duplicates)

            var abilities = GetAbilitiesFor(characterClass);
            CreateButtonsForHero(characterClass, abilities);
        }
    }

    /// <summary>Gets the abilities for a hero. Priority order:
    /// 1. Save-file loadout (AbilityBarSlots assigned in Hub)
    /// 2. ActorData.Abilities (data-driven defaults)
    /// 3. Hardcoded class fallbacks (last resort)
    /// </summary>
    private List<Ability> GetAbilitiesFor(CharacterClass characterClass)
    {
        // 1. Save-file loadout (player-assigned in Hub scene)
        var loadout = GetLoadoutFor(characterClass);
        if (loadout != null && loadout.EquippedAbilities.Count > 0)
            return loadout.GetActiveAbilities();

        // 2. ActorData.Abilities (data-driven defaults defined in Data/Actor/*.cs)
        var actorData = ActorLibrary.Get(characterClass);
        if (actorData?.Abilities != null && actorData.Abilities.Count > 0)
        {
            var dataAbilities = new List<Ability>();
            foreach (var a in actorData.Abilities)
                if (a != null && a.IsActive) dataAbilities.Add(a);
            if (dataAbilities.Count > 0) return dataAbilities;
        }

        // 3. Hardcoded class fallbacks (last resort, used until Hub assigns abilities)
        return GetClassDefaults(characterClass);
    }

    /// <summary>Hardcoded default abilities per class. Used only when no save data
    /// and no ActorData.Abilities exist. Add new hero defaults here.</summary>
    private static List<Ability> GetClassDefaults(CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Cleric:
                return new List<Ability> { AbilityLibrary.Heal(), AbilityLibrary.Smite() };
            case CharacterClass.Paladin:
                return new List<Ability> { AbilityLibrary.ShieldRush() };
            case CharacterClass.Barbarian:
                return new List<Ability> { AbilityLibrary.Trap() };
            default:
                return new List<Ability>();
        }
    }

    /// <summary>Gets the hero loadout from the hub or profile.</summary>
    private HeroLoadout GetLoadoutFor(CharacterClass characterClass)
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save?.Equipment == null) return null;
        var heroSave = save.Equipment.Heroes?.Find(h => h.CharacterClass == characterClass);
        if (heroSave == null) return null;
        var loadout = new HeroLoadout { CharacterClass = characterClass };
        loadout.LoadFromSave(heroSave);
        return loadout;
    }

    /// <summary>Creates the buttons for hero, capped at MaxAbilitySlots.</summary>
    private void CreateButtonsForHero(CharacterClass characterClass, List<Ability> abilities)
    {
        var list = new List<AbilityButton>();
        int slotCount = 0;
        foreach (var ability in abilities)
        {
            if (slotCount >= MaxAbilitySlots) break;

            // Use factory instead of Instantiate(prefab)
            var go = AbilityButtonFactory.Create(abilityButtonContainer);
            var layout = go.GetComponent<LayoutElement>();
            if (layout == null) layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 96f;
            layout.preferredHeight = 96f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            var instance = go.GetComponent<AbilityButton>();
            if (instance == null) instance = go.AddComponent<AbilityButton>();
            instance.name = $"AbilityButton_{characterClass}_{ability.name.Replace(" ", "_")}";
            var image = instance.GetComponent<Image>();
            if (image != null) image.sprite = ability.button;
            var label = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = string.Empty;
            instance.Initialize(ability, () => OnAbilityButtonClicked(g.Actors.SelectedActor, ability));
            instance.gameObject.SetActive(false);
            list.Add(instance);
            allButtons.Add(instance);
            slotCount++;
        }
        buttonsByHero[characterClass] = list;
    }

    #endregion

    #region Show/Hide

    /// <summary>Shows the ability buttons for the given hero. Buttons were pre-spawned at Start.</summary>
    public void Show(ActorInstance actor)
    {
        HideAll();
        if (actor == null || !actor.IsPlaying || actor.IsEnemy) return;

        visibleHero = actor.characterClass;
        if (!buttonsByHero.TryGetValue(visibleHero, out var list) || list == null) return;

        // Activate button GameObjects
        foreach (var btn in list)
            if (btn != null) btn.gameObject.SetActive(true);

        // Set interactable state based on current input mode
        if (g.InputManager != null && g.InputManager.InputMode == InputMode.PlayerTurn)
            RefreshVisibleInteractables();
        else
            SetAllButtonsInteractable(false);
    }

    /// <summary>Hides all ability buttons.</summary>
    public void Hide()
    {
        HideAll();
    }

    private void HideAll()
    {
        visibleHero = CharacterClass.None;
        foreach (var btn in allButtons)
            if (btn != null) btn.gameObject.SetActive(false);
    }

    /// <summary>Updates interactable state for all visible buttons based on current hero mana.
    /// No-op if not in PlayerTurn mode (buttons stay locked during targeting/enemy turns).</summary>
    public void UpdateAllInteractables(float currentMana)
    {
        if (g.InputManager == null || g.InputManager.InputMode != InputMode.PlayerTurn) return;
        foreach (var btn in allButtons)
            if (btn != null && btn.gameObject.activeSelf)
                btn.UpdateInteractable(currentMana);
    }

    #endregion

    #region Button Click

    /// <summary>Handles an ability button click. Locks input and begins targeting.</summary>
    private void OnAbilityButtonClicked(ActorInstance actor, Ability ability)
    {
        if (IsInteractionLocked()) return;
        if (actor == null || !actor.IsPlaying || !actor.IsHero) return;

        var title = GameObjectHelper.Game.Card.Title.GetComponent<TextMeshProUGUI>();
        var desc = GameObjectHelper.Game.Card.Details.GetComponent<TextMeshProUGUI>();
        if (title != null) title.text = ability.name;
        if (desc != null) desc.text = ability.Description ?? string.Empty;

        if (ability.TargetingMode == AbilityTargetingMode.Linear)
        {
            g.AbilityManager.BeginTargeting(actor, ability);
            g.TileManager.HighlightLinearPaths(actor.location);
            g.InputManager.BeginAbilityTargeting(actor);
            g.InputManager.RequireTouchRelease();
            return;
        }

        if (ability.requiresTarget)
        {
            g.AbilityManager.BeginTargeting(actor, ability);
        }
        else
        {
            // Instant-cast: ensure enough mana before activating
            if (g.ManaPoolManager == null || g.ManaPoolManager.Spend(Team.Hero, ability.ManaCost))
            {
                ability.Activate(actor, null);
            }
        }
    }

    #endregion
}

}
