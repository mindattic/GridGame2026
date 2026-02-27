using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;

public class AbilityButtonManager : MonoBehaviour
{
    private Transform abilityButtonContainer;
    private HorizontalLayoutGroup layoutGroup;

    private readonly Dictionary<CharacterClass, List<AbilityButton>> buttonsByHero = new();
    private readonly List<AbilityButton> allButtons = new();

    public void Awake()
    {
        abilityButtonContainer = GameObjectHelper.Game.Card.AbilityButtonContainer;

        // Configure HorizontalLayoutGroup for left-aligned buttons (like books on a shelf)
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

    private void Start()
    {
        BuildAllHeroButtons();
        HideAll();
    }

    private static bool IsInteractionLocked()
    {
        return g.InputManager == null || g.SequenceManager == null ||
               g.InputManager.InputMode == InputMode.EnemyTurn || g.SequenceManager.IsExecuting || g.InputManager.InputMode == InputMode.None;
    }

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

    private List<Ability> GetAbilitiesFor(CharacterClass characterClass)
    {
        var list = new List<Ability>();
        if (characterClass == CharacterClass.Cleric)
        {
            list.Add(AbilityLibrary.Heal());
            list.Add(AbilityLibrary.Smite());
        }
        else if (characterClass == CharacterClass.Paladin)
        {
            list.Add(AbilityLibrary.ShieldRush());
        }
        else if (characterClass == CharacterClass.Barbarian)
        {
            list.Add(AbilityLibrary.Trap());
        }
        return list;
    }

    private void CreateButtonsForHero(CharacterClass characterClass, List<Ability> abilities)
    {
        var list = new List<AbilityButton>();
        foreach (var ability in abilities)
        {
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
        }
        buttonsByHero[characterClass] = list;
    }

    public void Show(ActorInstance actor)
    {
        HideAll();
        if (actor == null || !actor.IsPlaying || actor.IsEnemy) return;

        var name = actor.characterClass;
        if (!buttonsByHero.TryGetValue(name, out var list))
        {
            // If a new hero enters mid-stage, build on demand
            var abilities = GetAbilitiesFor(name);
            CreateButtonsForHero(name, abilities);
            buttonsByHero.TryGetValue(name, out list);
        }

        if (list == null) return;
        foreach (var btn in list) if (btn != null) { btn.gameObject.SetActive(true); btn.UpdateInteractable(g.ManaPoolManager != null ? g.ManaPoolManager.heroMana : 0f); }
    }

    public void Hide()
    {
        HideAll();
    }

    private void HideAll()
    {
        foreach (var btn in allButtons) if (btn != null) btn.gameObject.SetActive(false);
    }

    // Update interactable state for all known buttons based on current hero mana
    public void UpdateAllInteractables(float currentMana)
    {
        foreach (var btn in allButtons) if (btn != null) btn.UpdateInteractable(currentMana);
    }

    private void OnAbilityButtonClicked(ActorInstance actor, Ability ability)
    {
        if (IsInteractionLocked()) return;
        if (actor == null || !actor.IsPlaying || !actor.IsHero) return;

        var title = GameObjectHelper.Game.Card.Title.GetComponent<TextMeshProUGUI>();
        var desc = GameObjectHelper.Game.Card.Details.GetComponent<TextMeshProUGUI>();
        if (title != null) title.text = ability.name;
        if (desc != null) desc.text = ability.Description ?? string.Empty;

        // Do not set title yet for abilities that require a target; AbilityManager will set title when a target is selected

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
            // Ensure enough mana before activating
            if (g.ManaPoolManager == null || g.ManaPoolManager.Spend(Team.Hero, ability.ManaCost))
            {
                ability.Activate(actor, null);
            }
            else
            {
                // Optionally: show feedback for insufficient mana
            }
        }
    }
}
