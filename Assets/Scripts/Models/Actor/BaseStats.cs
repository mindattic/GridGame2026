using System;

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