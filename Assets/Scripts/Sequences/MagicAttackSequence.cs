using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// MAGICATTACKSEQUENCE - One reusable offensive-magic effect (Fire, Ice, Thunder, Fireball,
    /// Smite, ...).
    ///
    /// <para>PURPOSE: Replaces the per-spell stubs that did nothing (Ability.Activate → Debug.Log)
    /// with a single real effect: play an element-themed impact VFX at the target, compute magic
    /// damage via <see cref="Formulas.CalculateMagicDamage"/> (scales off the caster's Intelligence
    /// /Wisdom), apply it through the same <see cref="ActorInstance.Damage"/> pipeline pincers use,
    /// and push the target's timeline icon back when a hero casts during the hero window.</para>
    ///
    /// <para>USAGE: AbilityManager builds one of these from the ability's AbilityEffect, picking the
    /// element + impact VFX. A DeathSequence is queued by the caller after the cast so kills resolve
    /// exactly like pincer kills.</para>
    ///
    /// <para>RELATED FILES: AbilityManager.cs (dispatch), Formulas.cs (CalculateMagicDamage),
    /// AttackHelper.cs (the physical-attack analogue), DeathSequence.cs.</para>
    /// </summary>
    public class MagicAttackSequence : SequenceEvent
    {
        private readonly ActorInstance caster;
        private readonly ActorInstance target;
        private readonly ElementalDamageType element;
        private readonly string impactVfxKey;

        public MagicAttackSequence(ActorInstance caster, ActorInstance target, ElementalDamageType element, string impactVfxKey)
        {
            this.caster = caster;
            this.target = target;
            this.element = element;
            this.impactVfxKey = impactVfxKey;
        }

        /// <summary>Coroutine that plays the impact VFX, then applies magic damage.</summary>
        public override IEnumerator ProcessRoutine()
        {
            if (caster == null || target == null || !target.IsPlaying)
                yield break;

            // Caster bobs to show they emitted the projectile.
            if (caster.IsPlaying)
                yield return caster.Animation.BobRoutine();

            var vfx = VisualEffectLibrary.Get(impactVfxKey);
            if (vfx != null)
                yield return g.VisualEffectManager.PlayRoutine(vfx, target.Position);

            // Miss → dodge feedback, no damage. Otherwise apply through the shared damage path.
            var result = Formulas.CalculateMagicDamage(caster, target, element);
            if (result.HitType == HitOutcome.Miss)
            {
                yield return target.AttackMissRoutine();
                yield break;
            }

            target.Damage(result);

            // Mirror physical attacks: a hero striking an enemy during the hero window shoves
            // that enemy's timeline icon back toward spawn (delaying its turn).
            bool isHeroTurn = g.TurnManager == null || g.TurnManager.IsHeroTurn;
            if (target.IsEnemy && isHeroTurn && caster.IsHero)
            {
                int casterStrength = caster.Stats?.Strength.ToInt() ?? 10;
                g.TimelineBar?.PushbackOnAttack(target, casterStrength);
            }

            // Fire spells leave a lingering Burn (damage-over-time) on a surviving target.
            // Burn severity scales with the caster's magic stats; cured by Esuna.
            if (element == ElementalDamageType.Fire && target.IsPlaying && target.Statuses != null)
            {
                float burn = Mathf.Max(2f, (caster.Stats.Intelligence + caster.Stats.Wisdom) * 0.35f);
                target.Statuses.Apply(new StatusEffect
                {
                    Kind = StatusKind.Burn,
                    Magnitude = burn,
                    RemainingTurns = 3
                });
                g.CombatTextManager.Spawn("Burning!", target.Position, "Damage");
            }

            yield return Wait.None();
        }
    }
}
