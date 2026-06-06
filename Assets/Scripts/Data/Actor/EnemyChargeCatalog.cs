using UnityEngine;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Libraries;
using Scripts.Models;

namespace Scripts.Data.Actor
{
    /// <summary>
    /// ENEMYCHARGECATALOG - Picks the telegraphed "charge" spell a caster enemy casts (US-026).
    ///
    /// <para>PURPOSE: Enemies are otherwise melee-only. An enemy tagged <see cref="ActorTag.Magic"/>
    /// is a <b>Caster</b> (game_bible.md §14.2) — it can telegraph a spell in the Prepare Zone instead
    /// of meleeing. This static, pure-data helper answers "is this a caster?" and "what does it cast?",
    /// deriving the spell <b>element</b> from the enemy's affinity tags (FireAffinity → Fireball, etc.)
    /// and a fixed <b>charge cast time</b>. Kept pure (no scene access, only the static
    /// <see cref="ActorLibrary"/>) so <see cref="Scripts.Services.EnemyPlanner"/> can call it while
    /// staying testable.</para>
    ///
    /// <para>US-027 hook: <see cref="ColorFor"/> maps the charge to a mana color so interrupting the
    /// cast can mint an off-palette orb to the team bank.</para>
    ///
    /// RELATED FILES: EnemyPlanner.cs (PlanCast), EnemyChargeSequence.cs, ManaColorAffinity.cs.
    /// </summary>
    public static class EnemyChargeCatalog
    {
        /// <summary>How long (seconds, before WIS/INT scaling in CastingState) a charge takes to load.</summary>
        public const float DefaultChargeCastSeconds = 2.5f;

        /// <summary>True if this enemy is a spellcaster (carries the <see cref="ActorTag.Magic"/> flag).</summary>
        public static bool IsCaster(ActorInstance enemy)
        {
            if (enemy == null || enemy.team != Team.Enemy) return false;
            var data = ActorLibrary.Get(enemy.characterClass);
            return data != null && (data.Tags & ActorTag.Magic) == ActorTag.Magic;
        }

        /// <summary>Builds the charge <see cref="Ability"/> this caster enemy telegraphs, or
        /// <c>null</c> if it isn't a caster. The element comes from its affinity tags.</summary>
        public static Ability For(ActorInstance enemy)
        {
            if (!IsCaster(enemy)) return null;
            var data = ActorLibrary.Get(enemy.characterClass);
            var (effect, spellName) = ElementFor(data != null ? data.Tags : ActorTag.None);
            return new Ability
            {
                name = spellName,
                type = AbilityType.TargetOpponent,
                category = AbilityCategory.Active,
                Effect = effect,
                CastTimeSeconds = DefaultChargeCastSeconds,
                ManaCost = 0, // enemies pay no mana for a charge
                TargetingMode = AbilityTargetingMode.AnyActor,
                Description = $"A telegraphed {spellName} charge."
            };
        }

        /// <summary>US-027: the mana color this charge mints when interrupted (derived from the element).</summary>
        public static ManaType ColorFor(ActorInstance enemy)
        {
            var data = ActorLibrary.Get(enemy != null ? enemy.characterClass : default);
            var (effect, _) = ElementFor(data != null ? data.Tags : ActorTag.None);
            switch (effect)
            {
                case AbilityEffect.Fireball: return ManaType.Red;
                case AbilityEffect.Ice:      return ManaType.Blue;
                case AbilityEffect.Thunder:  return ManaType.Blue;
                case AbilityEffect.Smite:    return ManaType.White;
                default:                     return ManaType.Colorless;
            }
        }

        /// <summary>Maps the enemy's affinity tags to a magic effect + display name (defaults to Fireball
        /// for a generic caster). Priority is fixed so multi-affinity enemies resolve deterministically.</summary>
        private static (AbilityEffect effect, string name) ElementFor(ActorTag tags)
        {
            if ((tags & ActorTag.FireAffinity) != 0)     return (AbilityEffect.Fireball, "Fireball");
            if ((tags & ActorTag.IceAffinity) != 0)      return (AbilityEffect.Ice, "Ice");
            if ((tags & ActorTag.ElectricAffinity) != 0) return (AbilityEffect.Thunder, "Thunder");
            if ((tags & ActorTag.LightAffinity) != 0)    return (AbilityEffect.Smite, "Smite");
            return (AbilityEffect.Fireball, "Fireball");
        }
    }
}
