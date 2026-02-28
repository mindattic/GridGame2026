using Assets.Helper;
using Assets.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Intermission.Before;

/// <summary>
/// ABILITYBUTTON - UI button for casting abilities.
/// 
/// PURPOSE:
/// Represents a single ability button in the ability bar.
/// Handles display, interactability based on mana cost,
/// and click callbacks.
/// 
/// USAGE:
/// Created by AbilityButtonFactory for each ability.
/// Disabled when insufficient mana.
/// 
/// RELATED FILES:
/// - AbilityButtonFactory.cs: Creates buttons
/// - AbilityButtonManager.cs: Manages button list
/// - Ability.cs: Ability data
/// </summary>
public class AbilityButton : MonoBehaviour
{
    #region Components

    public Button button;
    public TMP_Text label;
    private Ability ability;

    #endregion

    #region Initialization

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
    }

    public void Initialize(Ability ability, System.Action onClick)
    {
        if (label != null)
            label.text = ability.name;
        else
            Debug.LogError("AbilityButton.label is null");

        this.ability = ability;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
        else
        {
            Debug.LogError("AbilityButton.button is null");
        }
    }

    #endregion

    #region State

    public void UpdateInteractable(float currentMana)
    {
        if (ability == null || button == null) return;
        button.interactable = currentMana >= ability.ManaCost;
    }

    public Vector3 WorldPosition()
    {
        return UnitConversionHelper.Canvas.ToWorld(button.transform, button.transform.position.z);
    }

    #endregion
}

/// <summary>Ability effect types.</summary>
public enum AbilityEffect
{
    None,
    Heal,
    ShieldRush,
    Trap,
    Smite,
    // Passive effects - Attack
    DoubleAttack,
    TripleAttack,
    // Passive effects - Movement
    DoubleMove,
    TripleMove,
    // Reactive effects
    CounterAttack
}

public enum AbilityTargetingMode
{
    AnyActor,
    Linear
}

public class Ability
{
    public string name;
    public AbilityType type;
    public AbilityCategory category = AbilityCategory.Active; // Default to active
    public Sprite button;

    // New: how many distinct targets this ability can select (default 1)
    public int TotalNumberOfTargets = 1;

    // New: semantic effect and targeting mode
    public AbilityEffect Effect = AbilityEffect.None;
    public AbilityTargetingMode TargetingMode = AbilityTargetingMode.AnyActor;

    // New: mana cost to cast this ability once
    public int ManaCost = 0;

    // New: description to display on the card when selected
    public string Description;

    // For passive abilities: number of extra attacks (DoubleAttack = 1, TripleAttack = 2)
    public int ExtraAttacks = 0;
    
    // For passive abilities: number of extra moves (DoubleMove = 1, TripleMove = 2)
    public int ExtraMoves = 0;

    public bool IsActive => category == AbilityCategory.Active;
    public bool IsPassive => category == AbilityCategory.Passive;
    public bool IsReactive => category == AbilityCategory.Reactive;

    public bool requiresTarget =>
        type == AbilityType.TargetAlly || type == AbilityType.TargetOpponent || type == AbilityType.TargetAny;

    public void Activate(ActorInstance user, ActorInstance target)
    {
        Debug.Log($"{user.name} used {name} on {(target ? target.name : "no target")}");
    }
}
