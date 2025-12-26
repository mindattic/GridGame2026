// --- File: Assets/Scripts/Events/Sequences/EnemyAttackSequence.cs ---
using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections;
using System.Linq;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Executes a single enemy attack turn for one attacker.
    /// Flow:
    /// 1) Wait pre-attack intermission.
    /// 2) Find adjacent defenders.
    /// 3) For each defender, run bump animation with an impact routine.
    /// 4) The impact routine evaluates a parry window via InputManager.OnParry.
    /// 5) If parried, skip normal damage and play optional parry feedback.
    /// 6) Reset the attacker's action bar.
    /// </summary>
    public class EnemyAttackSequence : SequenceEvent
    {
        private readonly ActorInstance attacker;

        public EnemyAttackSequence(ActorInstance enemy)
        {
            attacker = enemy;
        }

        /// <summary>
        /// Orchestrates enemy attack against adjacent heroes.
        /// Uses a custom impact routine that checks for a parry at the strike moment.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            if (attacker == null || !attacker.IsPlaying)
                yield break;

            yield return Wait.For(Intermission.Before.Enemy.Attack);

            var defendingHeroes = g.Actors.Heroes
                .Where(x => x.IsPlaying && Geometry.IsAdjacentTo(x.location, attacker.location))
                .ToList();

            if (defendingHeroes.Count > 0)
            {
                foreach (var opponent in defendingHeroes)
                {
                    // If opponent is in a dying state, do a 360 spin instead of bump
                    if (opponent.IsDying)
                    {
                        IEnumerator respite = RespiteRoutine(opponent);
                        yield return attacker.Animation.Spin360AndWaitRoutine(respite);
                        continue;
                    }

                    var attackResult = Formulas.CalculateAttackResult(attacker, opponent);

                    if (attackResult == null
                        || attackResult.Opponent == null
                        || attackResult.Opponent.IsDying
                        || attackResult.Opponent.IsDead)
                        continue;

                    var singleAttack = AttackHelper.SingleAttackRoutine(attackResult);
                    yield return attacker.Animation.BumpRoutine(opponent, singleAttack);
                }
            }

            attacker.ActionBar.Reset();
        }

        private IEnumerator RespiteRoutine(ActorInstance opponent)
        {
            if (opponent != null)
                g.CombatTextManager.Spawn("Respite", opponent.Position, "Heal");
            yield return null;
        }

        /// <summary>
        /// Runs at the exact bump impact.
        /// Evaluates the EnemyTurn parry window by:
        /// - Subscribing to InputManager.OnParry.
        /// - Triggering OnEnemyAttackOccurred to judge timing.
        /// - Branching on parry flag to either skip damage or run normal damage routine.
        /// </summary>
        private IEnumerator ImpactRoutineWithParry(AttackResult result, ActorInstance opponent)
        {
            var input = GameManager.instance.inputManager;

            // Track which defensive timing, if any, triggers
            var timing = DefenseTiming.None;

            if (input != null && input.InputMode == InputMode.EnemyTurn)
            {
                // Handlers that mark the outcome
                void MarkParry() { timing = DefenseTiming.Parry; }
                void MarkDodge() { timing = DefenseTiming.Dodge; }

                input.OnParry += MarkParry;
                input.OnDodge += MarkDodge;

                // Ask input to evaluate timing right now
                input.OnEnemyAttackOccurred();

                input.OnParry -= MarkParry;
                input.OnDodge -= MarkDodge;
            }

            if (timing == DefenseTiming.Parry)
            {
                // Perfect timing
                g.CombatTextManager.Spawn("Parry", opponent.Position, "Damage");
                yield break;
            }
            else if (timing == DefenseTiming.Dodge)
            {
                // Good timing
                g.CombatTextManager.Spawn("Dodge", opponent.Position, "Damage");
                yield break;
            }

            // No defense timing; run normal damage
            yield return AttackHelper.SingleAttackRoutine(result);
        }





        /// <summary>
        /// Plays simple feedback when a parry occurs.
        /// Replace or extend with project-specific VFX, SFX, or counters.
        /// </summary>
        private IEnumerator PlayParryFeedback(ActorInstance source, ActorInstance target)
        {
            g.CombatTextManager.Spawn("Parry", target.Position, "Damage");
            // Hook for parry VFX/SFX or counter logic.
            // Example placeholders only; keep lightweight to preserve pacing.
            // yield return Wait.Ticks(1);
            yield return null;
        }




    }
}
