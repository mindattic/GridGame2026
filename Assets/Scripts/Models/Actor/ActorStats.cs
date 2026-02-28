using System;

/// <summary>
/// ACTORSTATS - Runtime stat block for an actor on the battlefield.
/// 
/// PURPOSE:
/// Holds all numeric stats for an ActorInstance during combat.
/// Extends BaseStats with HP/AP tracking and level/XP data.
/// 
/// STAT CATEGORIES:
/// 
/// RESOURCES (volatile during combat):
/// - HP / MaxHP: Health points (0 = death)
/// - AP / MaxAP: Action points (for abilities)
/// - PreviousHP/PreviousAP: For interpolating health bar animations
/// 
/// PROGRESSION:
/// - Level: Current character level
/// - CurrentXP: XP toward next level
/// - TotalXP: Lifetime accumulated XP
/// 
/// CORE STATS (inherited from BaseStats):
/// - Strength: Physical damage multiplier
/// - Vitality: HP and defense
/// - Speed: Turn order / timeline speed
/// - Stamina: Action economy
/// - Intelligence: Magical damage
/// - Wisdom: Magical defense / resistance
/// - Luck: Critical hit and dodge chance
/// 
/// STAT EFFECTS ON GAMEPLAY:
/// - Strength → Damage dealt in pincer attacks
/// - Vitality → MaxHP calculation, damage reduction
/// - Speed → Timeline tag movement speed (faster = act sooner)
/// - Stamina → AP regeneration rate
/// - Intelligence → Ability damage
/// - Wisdom → Ability resistance
/// - Luck → Crit chance, drop rates
/// 
/// DAMAGE FORMULA (simplified):
/// Damage = (Attacker.Strength * Weapon.Damage) - (Target.Vitality * DefenseMultiplier)
/// 
/// RELATED FILES:
/// - BaseStats.cs: Parent class with core stat definitions
/// - ActorInstance.cs: Stats field owner
/// - DamageCalculator.cs: Uses stats for combat math
/// - LevelHelper.cs: XP/level progression
/// </summary>
[Serializable]
public class ActorStats : BaseStats
{
    #region Progression

    /// <summary>Current character level (1-based).</summary>
    public int Level = 1; 

    /// <summary>XP accumulated toward next level.</summary>
    public int CurrentXP;

    /// <summary>Total lifetime XP earned.</summary>
    public int TotalXP;

    #endregion

    #region Health Points

    /// <summary>Previous HP value (for health bar animation).</summary>
    public float PreviousHP;

    /// <summary>Current health points. 0 = death.</summary>
    public float HP;

    /// <summary>Maximum health points.</summary>
    public float MaxHP;

    #endregion

    #region Action Points

    /// <summary>Previous AP value (for action bar animation).</summary>
    public float PreviousAP;

    /// <summary>Current action points for abilities.</summary>
    public float AP;

    /// <summary>Maximum action points.</summary>
    public float MaxAP;

    #endregion

    #region Constructors

    /// <summary>Default constructor.</summary>
    public ActorStats() { }

    /// <summary>Copy constructor for cloning stats.</summary>
    public ActorStats(ActorStats other)
    {
        if (other == null) return;

        Level = other.Level;
        CurrentXP = other.CurrentXP;
        TotalXP = other.TotalXP;

        PreviousHP = other.HP;
        HP = other.HP;
        MaxHP = other.MaxHP;

        PreviousAP = 0f;
        AP = 0f;
        MaxAP = 100f;

        Strength = other.Strength;
        Vitality = other.Vitality;
        Speed = other.Speed;
        Stamina = other.Stamina;
        Intelligence = other.Intelligence;
        Wisdom = other.Wisdom;
        Luck = other.Luck;
    }

    #endregion
}
