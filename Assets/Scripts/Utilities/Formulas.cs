using Scripts.Models;
using Scripts.Managers;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Utilities
{
/// <summary>Elemental damage types for abilities and attacks.</summary>
public enum ElementalDamageType
{
    Physical,
    Fire,
    Ice,
    Lightning,
    Acid,
    Dark,
    Light,
    Earth,
    Water,
    Wind,
    Psychic,
    Poison,
    Arcane
}

/// <summary>Attack hit outcome types.</summary>
public enum HitOutcome
{
    Normal,
    Critical,
    Miss
}

/// <summary>
/// FORMULAS - Combat calculation formulas.
/// 
/// PURPOSE:
/// Pure utility class containing all combat math including
/// damage calculation, hit/crit chance, and stat formulas.
/// 
/// KEY FORMULAS:
/// - CalculateAttackResult: Full attack resolution
/// - Offense/Defense: Damage dealing/mitigation
/// - HitChance/CritChance: Probability calculations
/// 
/// DESIGN:
/// FF-inspired formulas using Speed, Luck, Attack, Defense.
/// Light level influence, capped ranges, small variance.
/// All methods are pure - no game state mutation.
/// 
/// RELATED FILES:
/// - AttackHelper.cs: Uses formulas for attacks
/// - ActorStats.cs: Stat definitions
/// - PincerAttackSequence.cs: Combat execution
/// </summary>
public static class Formulas
{
    // Level influence constants
    private const float HitShiftPerLevel = 3f;
    private const float DamageScalePerLevel = 1.07f;

    /// <summary>Level advantage.</summary>
    private static int LevelAdvantage(ActorStats atk, ActorStats def)
        => Mathf.RoundToInt(atk.Level - def.Level);

    // Outcome multipliers
    private const float CritMultiplier = 1.60f;

    // Defense tuning
    private const float DefenseMitigation = 0.35f;
    private const float PenetrationFromAttack = 0.25f;
    private const float OffenseFloorFraction = 0.20f;

    // ---------------
    // Logging helpers
    // ---------------
    /// <summary>Log.</summary>
    private static void Log(string message)
    {
        CombatLogHelper.Write(message);
    }

    /// <summary>Name of.</summary>
    private static string NameOf(ActorInstance a)
    {
        if (a == null) return "<null>";
        if (a.characterClass != CharacterClass.None) return a.characterClass.ToString();
        return a.name;
    }

    // ----------------------------
    // Minimal stat accessors (6x)
    // ----------------------------
    /// <summary>Atk.</summary>
    private static float Atk(ActorStats s) => s.Strength;  // Attack
    /// <summary>Def.</summary>
    private static float Def(ActorStats s) => s.Vitality;  // Defense
    /// <summary>Spd.</summary>
    private static float Spd(ActorStats s) => s.Speed;     // Speed
    /// <summary>Lck.</summary>
    private static float Lck(ActorStats s) => s.Luck;      // Luck

    // Hit%: raise the base a bit to reduce early whiffs
    /// <summary>Hit percent.</summary>
    private static float HitPercent(ActorStats s)
        => Mathf.Clamp(80f + Spd(s) * 0.5f + Lck(s) * 0.2f, 5f, 95f); // base was 70f

    // Crit%: small bump to base crit so early hits feel better
    /// <summary>Crit percent.</summary>
    private static float CritPercent(ActorStats s)
        => Mathf.Clamp(8f + Lck(s) * 0.4f, 0f, 50f); // base was 5f

    /// <summary>
    /// Tiny multiplicative tilt from Luck to bias variance slightly.
    /// </summary>
    private static float LuckTilt(ActorStats stats)
    {
        float t = Mathf.Clamp01(Lck(stats) / 100f);
        return Mathf.Lerp(0.99f, 1.01f, t);
    }

    /// <summary>
    /// Samples a multiplicative variance in [1 - range, 1 + range], slightly biased by Luck.
    /// </summary>
    private static float SampleVarianceWithLuck(ActorStats stats, float rangeFraction)
    {
        float minV = 1f - rangeFraction;
        float maxV = 1f + rangeFraction;

        float roll = RNG.Float(minV, maxV);
        float tilt = LuckTilt(stats);
        float adjusted = roll * tilt;
        float result = Mathf.Clamp(adjusted, minV, maxV);

        Log($"Variance: ±{rangeFraction * 100f:F0}% roll={roll:F3} tilt(Luck={Lck(stats):F0})={tilt:F3} -> factor={result:F3}");
        return result;
    }

    // -----------------------
    // Simple core percentages
    // -----------------------
    /// <summary>Accuracy.</summary>
    public static float Accuracy(ActorStats stats) => HitPercent(stats);
    /// <summary>Evasion.</summary>
    public static float Evasion(ActorStats stats) => 0f;

    /// <summary>
    /// Rolls outcome with a single uniform roll: Crit -> Normal -> Miss.
    /// </summary>
    public static HitOutcome CalculateHitType(ActorInstance attacker, ActorInstance opponent)
    {
        int adv = LevelAdvantage(attacker.Stats, opponent.Stats);

        float hitChance = Mathf.Clamp(Accuracy(attacker.Stats) + adv * HitShiftPerLevel, 5f, 95f);
        float critChance = CritPercent(attacker.Stats);

        float roll = RNG.Float(0f, 100f);
        Log($"HitOutcome roll: {NameOf(attacker)} vs {NameOf(opponent)} | Hit%={hitChance:F1}, Crit%={critChance:F1}, Adv={adv}, Roll={roll:F2}");

        if (roll <= critChance) return HitOutcome.Critical;
        if (roll <= hitChance) return HitOutcome.Normal;
        return HitOutcome.Miss;
    }

    /// <summary>
    /// Derived max health: keep very simple.
    /// </summary>
    public static float Health(ActorStats stats)
    {
        // Lower baseline HP so early fights are punchy. Tweak per class as needed.
        const float baseHP = 12f;   // was 50f
        const float perVit = 5f;    // was 10f
        const float perLvl = 1f;    // was 2f
        return Mathf.Floor(baseHP + Def(stats) * perVit + stats.Level * perLvl);
    }

    /// <summary>Offense.</summary>
    public static float Offense(ActorStats stats, float weaponPower = 0f)
    {
        return Atk(stats) + weaponPower;
    }

    /// <summary>Defense.</summary>
    public static float Defense(ActorStats stats, float armorRating = 0f)
    {
        return Def(stats) + armorRating;
    }

    /// <summary>Magic offense.</summary>
    public static float MagicOffense(ActorStats stats)
    {
        return Atk(stats);
    }

    /// <summary>Magic resistance.</summary>
    public static float MagicResistance(ActorStats stats)
    {
        return Def(stats);
    }

    /// <summary>Applies the resistance.</summary>
    public static float ApplyResistance(float baseValue, float resistance)
    {
        if (resistance >= 0f)
            return baseValue * (100f / (100f + resistance));

        return baseValue * (1f + Mathf.Abs(resistance) / 100f);
    }

    /// <summary>
    /// Computes a physical AttackResult using the adjusted, more-forgiving low-level model.
    /// </summary>
    public static AttackResult CalculateAttackResult(
        ActorInstance attacker, ActorInstance opponent,
        float weaponPower = 0f, float armorRating = 0f,
        ElementalDamageType element = ElementalDamageType.Physical, float resistance = 0f)
    {
        float off = Offense(attacker.Stats, weaponPower);
        float def = Defense(opponent.Stats, armorRating);

        // New mitigation shape:
        // 1) Attack-based penetration lowers effective Defense.
        // 2) Remaining Defense mitigates with a lighter coefficient.
        // 3) A floor ensures at least a fraction of Offense gets through.
        float pen = off * PenetrationFromAttack;
        float effDef = Mathf.Max(0f, def - pen);
        float reduced = off - effDef * DefenseMitigation;
        float floorBase = off * OffenseFloorFraction;
        float raw = Mathf.Max(1f, Mathf.Max(reduced, floorBase));

        float resisted = ApplyResistance(raw, resistance);
        float varied = resisted * SampleVarianceWithLuck(attacker.Stats, 0.10f); // ±10%

        HitOutcome type = CalculateHitType(attacker, opponent);
        float typeMult = type == HitOutcome.Critical ? CritMultiplier : 1f;

        int adv = LevelAdvantage(attacker.Stats, opponent.Stats);
        float levelMult = Mathf.Pow(DamageScalePerLevel, adv);

        int finalDamage = (type == HitOutcome.Miss)
            ? 0
            : Mathf.Max(1, Mathf.FloorToInt(varied * typeMult * levelMult));

        var s =
            $"[PHYS] {NameOf(attacker)} -> {NameOf(opponent)} ({element}, Resist {resistance:+0;-0;0}%)\n" +
            $"  Offense = {off:F1}, Defense = {def:F1}, Penetration({PenetrationFromAttack:P0}) = {pen:F1} -> EffDef {effDef:F1}\n" +
            $"  Reduced = Off - EffDef*{DefenseMitigation:F2} = {reduced:F2}, Floor({OffenseFloorFraction:P0} of Off) = {floorBase:F2}\n" +
            $"  Raw = max(1, max(Reduced, Floor)) = {raw:F2}\n" +
            $"  After Resistance: {resisted:F2}\n" +
            $"  Variance applied -> {varied:F2}\n" +
            $"  HitOutcome: {type} x{(type == HitOutcome.Critical ? CritMultiplier : 1f):F2}\n" +
            $"  LevelAdvantage: {adv} -> Scale {levelMult:F3}\n" +
            $"  Final Damage = {(type == HitOutcome.Miss ? 0 : Mathf.FloorToInt(varied * typeMult * levelMult))} {(type == HitOutcome.Miss ? "(MISS)" : string.Empty)}";
        Log(s);

        return new AttackResult(attacker, opponent, finalDamage, type);
    }

    /// <summary>
    /// Computes magical AttackResult using the same adjusted mitigation.
    /// </summary>
    public static AttackResult CalculateMagicDamage(
        ActorInstance caster, ActorInstance target,
        ElementalDamageType element = ElementalDamageType.Arcane, float resistance = 0f)
    {
        float off = MagicOffense(caster.Stats);
        float res = MagicResistance(target.Stats);

        float pen = off * PenetrationFromAttack;
        float effRes = Mathf.Max(0f, res - pen);
        float reduced = off - effRes * DefenseMitigation;
        float floorBase = off * OffenseFloorFraction;
        float raw = Mathf.Max(1f, Mathf.Max(reduced, floorBase));

        float resisted = ApplyResistance(raw, resistance);
        float varied = resisted * SampleVarianceWithLuck(caster.Stats, 0.10f);

        HitOutcome type = CalculateHitType(caster, target);
        float typeMult = type == HitOutcome.Critical ? CritMultiplier : 1f;

        int adv = LevelAdvantage(caster.Stats, target.Stats);
        float levelMult = Mathf.Pow(DamageScalePerLevel, adv);

        int finalDamage = (type == HitOutcome.Miss)
            ? 0
            : Mathf.Max(1, Mathf.FloorToInt(varied * typeMult * levelMult));

        var s =
            $"[MAG] {NameOf(caster)} -> {NameOf(target)} ({element}, Resist {resistance:+0;-0;0}%)\n" +
            $"  M.Offense = {off:F1}, M.Resist = {res:F1}, Penetration({PenetrationFromAttack:P0}) = {pen:F1} -> EffRes {effRes:F1}\n" +
            $"  Reduced = Off - EffRes*{DefenseMitigation:F2} = {reduced:F2}, Floor({OffenseFloorFraction:P0} of Off) = {floorBase:F2}\n" +
            $"  Raw = max(1, max(Reduced, Floor)) = {raw:F2}\n" +
            $"  After Resistance: {resisted:F2}\n" +
            $"  Variance applied -> {varied:F2}\n" +
            $"  HitOutcome: {type} x{(type == HitOutcome.Critical ? CritMultiplier : 1f):F2}\n" +
            $"  LevelAdvantage: {adv} -> Scale {levelMult:F3}\n" +
            $"  Final Damage = {(type == HitOutcome.Miss ? 0 : Mathf.FloorToInt(varied * typeMult * levelMult))} {(type == HitOutcome.Miss ? "(MISS)" : string.Empty)}";
        Log(s);

        return new AttackResult(caster, target, finalDamage, type);
    }

    /// <summary>Clamp alive.</summary>
    public static float ClampAlive(float hp)
    {
        return hp < 1f ? 0f : hp;
    }

    /// <summary>Ap regen.</summary>
    public static float APRegen(ActorStats stats)
    {
        return 3f + Spd(stats) * 0.6f + stats.Level * 0.2f;
    }

    /// <summary>Critical hit percent.</summary>
    public static float CriticalHitPercent(ActorInstance attacker, ActorInstance opponent)
    {
        return CritPercent(attacker.Stats);
    }
}
}
