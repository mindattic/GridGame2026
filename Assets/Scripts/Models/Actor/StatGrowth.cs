using System;

/// <summary>
/// Represents growth values for all Stats, inheriting from BaseStats.
/// Supports construction from values, copy construction, and addition.
/// </summary>
[Serializable]
public class StatGrowth : BaseStats
{
    // ------------------------------------------------------------
    // Constructors
    // ------------------------------------------------------------

    public StatGrowth() { }

    public StatGrowth(
        float strength,
        float vitality,
        float agility,
        float speed,
        float stamina,
        float intelligence,
        float wisdom,
        float luck)
    {
        Strength = strength;
        Vitality = vitality;
        Agility = agility;
        Speed = speed;
        Stamina = stamina;
        Intelligence = intelligence;
        Wisdom = wisdom;
        Luck = luck;
    }

    public StatGrowth(StatGrowth other)
    {
        if (other == null) return;

        Strength = other.Strength;
        Vitality = other.Vitality;
        Agility = other.Agility;
        Speed = other.Speed;
        Stamina = other.Stamina;
        Intelligence = other.Intelligence;
        Wisdom = other.Wisdom;
        Luck = other.Luck;
    }

    // ------------------------------------------------------------
    // Operators
    // ------------------------------------------------------------

    /// <summary>
    /// Add two StatGrowth objects together, returning a new instance.
    /// Handles null values by treating them as zero.
    /// </summary>
    public static StatGrowth operator +(StatGrowth a, StatGrowth b)
    {
        if (a == null && b == null) return new StatGrowth();
        if (a == null) return new StatGrowth(b);
        if (b == null) return new StatGrowth(a);

        return new StatGrowth
        {
            Strength = a.Strength + b.Strength,
            Vitality = a.Vitality + b.Vitality,
            Agility = a.Agility + b.Agility,
            Speed = a.Speed + b.Speed,
            Stamina = a.Stamina + b.Stamina,
            Intelligence = a.Intelligence + b.Intelligence,
            Wisdom = a.Wisdom + b.Wisdom,
            Luck = a.Luck + b.Luck
        };
    }
}
