using Scripts.Helpers;
using Scripts.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Scripts.Utilities.Intermission.Before;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
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

namespace Scripts.Instances
{
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

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>Initializes initialize.</summary>
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

    /// <summary>Updates the interactable based on mana availability and item stock.</summary>
    public void UpdateInteractable(float currentMana)
    {
        if (ability == null || button == null) return;

        bool canAfford = currentMana >= ability.ManaCost;

        // For item-backed abilities, also check inventory stock
        if (ability.IsItemAbility && ability.SourceItem != null)
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Inventory?.Items != null)
            {
                var entry = save.Inventory.Items.Find(e => e.ItemId == ability.SourceItem.Id);
                canAfford = canAfford && entry != null && entry.Count > 0;
            }
            else
            {
                canAfford = false;
            }
        }

        button.interactable = canAfford;
    }

    /// <summary>World position.</summary>
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
    // Item usage
    UseItem,
    // Offensive magic
    Fire,
    Ice,
    Thunder,
    Fireball,
    Fira,
    // Support magic
    GroupHeal,
    Esuna,
    Protect,
    Regen,
    // Passive effects - Attack
    DoubleAttack,
    TripleAttack,
    // Passive effects - Movement
    DoubleMove,
    TripleMove,
    // Passive effects - Stat upgrades
    ArmorUp,
    CritUp,
    Focus,
    EvasionUp,
    HPUp,
    // Reactive effects
    CounterAttack,
    Cover,
    // Utility
    Strike,
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

    // Casting time in seconds (0 = instant). Visible as a bar on the timeline.
    public float CastTimeSeconds = 0f;

    // Source item for consumable-backed abilities (null for normal abilities)
    public ItemDefinition SourceItem;

    /// <summary>True if this ability is backed by a consumable item.</summary>
    public bool IsItemAbility => SourceItem != null;

    public bool IsActive => category == AbilityCategory.Active;
    public bool IsPassive => category == AbilityCategory.Passive;
    public bool IsReactive => category == AbilityCategory.Reactive;

    public bool requiresTarget =>
        type == AbilityType.TargetAlly || type == AbilityType.TargetOpponent || type == AbilityType.TargetAny;

    /// <summary>Activate.</summary>
    public void Activate(ActorInstance user, ActorInstance target)
    {
        Debug.Log($"{user.name} used {name} on {(target ? target.name : "no target")}");
    }
}

}
