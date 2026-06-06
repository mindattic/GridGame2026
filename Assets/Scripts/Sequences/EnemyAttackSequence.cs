// --- File: Assets/Scripts/Sequences/EnemyAttackSequence.cs ---
using Scripts.Helpers;
using Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// ENEMYATTACKSEQUENCE - Executes a single enemy's attack against heroes.
    /// 
    /// PURPOSE:
    /// Handles the attack phase of an enemy's turn. Finds adjacent heroes
    /// and attacks the first one found.
    /// 
    /// ATTACK FLOW:
    /// 1. Wait for attack intermission (visual pacing)
    /// 2. Find all adjacent heroes (IsPlaying && adjacent)
    /// 3. Select first adjacent hero as target
    /// 4. Calculate attack result via Formulas.CalculateAttackResult()
    /// 5. Execute bump animation with damage via BumpRoutine()
    /// 6. Reset attacker's action bar
    /// 
    /// TARGET SELECTION:
    /// - Only attacks heroes in adjacent tiles (not diagonal)
    /// - Attacks first hero found (no priority system)
    /// - If no adjacent heroes, exits early
    /// 
    /// DAMAGE APPLICATION:
    /// Uses AttackHelper.SingleAttackRoutine() which:
    /// - Plays VFX at target position
    /// - Applies damage at VFX apex
    /// - Shows combat text
    /// 
    /// SEQUENCE CHAIN POSITION:
    /// Part of enemy turn chain:
    /// - EnemyMoveSequence
    /// - EnemyPreAttackSequence
    /// - EnemyAttackSequence ← Here
    /// - EnemyPostAttackSequence
    /// - DeathSequence
    /// - EndTurnSequence
    /// 
    /// RELATED FILES:
    /// - EnemyTakeTurnSequence.cs: Orchestrates full enemy turn
    /// - AttackHelper.cs: SingleAttackRoutine for damage
    /// - Formulas.cs: CalculateAttackResult for damage math
    /// - ActorAnimation.cs: BumpRoutine for attack animation
    /// </summary>
    public class EnemyAttackSequence : SequenceEvent
    {
        private readonly ActorInstance attacker;

        /// <summary>Creates attack sequence for the specified enemy.</summary>
        public EnemyAttackSequence(ActorInstance enemy)
        {
            attacker = enemy;
        }

        /// <summary>
        /// Finds adjacent heroes and attacks the first one.
        /// If the target hero is casting, their cast is interrupted.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            if (attacker == null || !attacker.IsPlaying)
                yield break;

            // Pre-attack pause for visual pacing
            yield return Wait.For(Intermission.Before.Enemy.Attack);

            // Announce enemy attack on the top-center action title.
            g.ActionTitle?.Show($"{attacker.characterClass} attacks!");

            // Find adjacent heroes
            var defendingHeroes = g.Actors.Heroes
                .Where(x => x.IsPlaying && Geometry.IsAdjacentTo(x.location, attacker.location))
                .ToList();

            if (defendingHeroes.Count == 0)
                yield break;

            // Attack only the first adjacent hero
            var opponent = defendingHeroes.First();

            if (opponent.IsPlaying && !opponent.IsDying && !opponent.IsDead)
            {
                var attackResult = Formulas.CalculateAttackResult(attacker, opponent);

                if (attackResult != null && attackResult.Opponent != null &&
                    !attackResult.Opponent.IsDying && !attackResult.Opponent.IsDead)
                {
                    // Interrupt the hero's in-flight cast only when the blow actually LANDS.
                    // The bible ties interruption to "taking damage", so a fully-dodged (Miss)
                    // attack must not break a cast or consume its MP.
                    if (attackResult.HitType != HitOutcome.Miss)
                        InterruptCastingHero(opponent, attacker);

                    var singleAttack = AttackHelper.SingleAttackRoutine(attackResult);
                    yield return attacker.Animation.BumpRoutine(opponent, singleAttack);
                }
            }

        }

        /// <summary>
        /// Interrupts a hero's active cast(s) when the hero takes damage (US-024 stagger model).
        /// Each landed hit is resolved per cast by <see cref="Scripts.Services.CastInterruptResolver"/>:
        /// a rare LCK <b>Clutch</b> snaps the cast to the trigger to resolve (US-025 ClutchSequence),
        /// a WIS poise <b>shrug</b> ignores the hit, otherwise the hit adds WIS/STR-scaled cast-time
        /// <b>delay</b> — and once the accumulated delay exceeds the cast time the cast is cancelled.
        /// </summary>
        private void InterruptCastingHero(ActorInstance hero, ActorInstance attacker)
        {
            if (hero == null || g.TimelineBar == null) return;
            g.TimelineBar.InterruptCastsByOwner(hero, attacker);
        }
    }
}
