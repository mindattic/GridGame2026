using System;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models.Actor
{
/// <summary>
/// BASESTATS - Core stat definitions shared by all actors.
/// 
/// PURPOSE:
/// Defines the fundamental RPG stats that all characters
/// (heroes and enemies) possess. Extended by ActorStats.
/// 
/// STAT DEFINITIONS:
/// ```
/// PHYSICAL:
/// - Strength     → Physical damage output
/// - Vitality     → HP pool and physical defense
/// - Agility      → Dodge chance, accuracy
/// 
/// SPEED/ACTION:
/// - Speed        → Timeline movement rate (faster = act sooner)
/// - Stamina      → AP regeneration rate
/// 
/// MAGICAL:
/// - Intelligence → Magic damage output
/// - Wisdom       → Magic defense/resistance
/// 
/// MISC:
/// - Luck         → Critical hit chance, drop rates
/// ```
/// 
/// STAT SCALING:
/// Stats typically range from 1-100 for player characters,
/// with enemies scaling based on stage difficulty.
/// 
/// INHERITANCE:
/// ```
/// BaseStats (this class)
///     └── ActorStats (adds HP, AP, Level, XP)
/// ```
/// 
/// RELATED FILES:
/// - ActorStats.cs: Extends with runtime values
/// - DamageCalculator.cs: Uses stats for combat
/// - LevelHelper.cs: Stat growth on level up
/// </summary>
[Serializable]
public class BaseStats
{
    /// <summary>Physical damage multiplier.</summary>
    public float Strength;

    /// <summary>HP pool and physical defense.</summary>
    public float Vitality;

    /// <summary>Dodge chance and accuracy.</summary>
    public float Agility;

    /// <summary>Timeline movement speed (faster = act sooner).</summary>
    public float Speed;

    /// <summary>AP regeneration rate.</summary>
    public float Stamina;

    /// <summary>Magic damage output.</summary>
    public float Intelligence;

    /// <summary>Magic defense/resistance.</summary>
    public float Wisdom;

    /// <summary>Critical hit chance and drop rates.</summary>
    public float Luck;
}
}
