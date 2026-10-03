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

    // Equipment management — bar slot holds a weapon (saved weapon-swap slot).
    EquipWeapon,
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

    // Source weapon for weapon-swap bar slots (null for normal abilities). Effect should be
    // AbilityEffect.EquipWeapon.
    public ItemDefinition SourceWeapon;

    /// <summary>True if this ability is backed by a consumable item.</summary>
    public bool IsItemAbility => SourceItem != null;

    /// <summary>True if this ability is backed by a weapon (bar slot used as a loadout swap).</summary>
    public bool IsWeaponAbility => SourceWeapon != null;

    public bool IsActive => category == AbilityCategory.Active;
    public bool IsPassive => category == AbilityCategory.Passive;
    public bool IsReactive => category == AbilityCategory.Reactive;

}

}
