// --- File: Assets/Scripts/Sequences/EnemyTakeTurnSequence.cs ---
using System.Collections;
using System.Linq;
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
    /// ENEMYTAKETURNSEQUENCE - Orchestrates a single enemy's turn.
    /// 
    /// Executes the full turn sequence for one enemy actor:
    /// 1. EnemyMoveSequence: Enemy moves toward target
    /// 2. EnemyPreAttackSequence: Attack windup
    /// 3. EnemyAttackSequence: Damage dealing
    /// 4. EnemyPostAttackSequence: Recovery
    /// 5. DeathSequence: Handle deaths
    /// 6. EndTurnSequence: Advance turn
    /// 
    /// Called by TurnManager when enemy timeline tag reaches trigger.
    /// </summary>
    public sealed class EnemyTakeTurnSequence : SequenceEvent
    {
        private readonly ActorInstance enemy;

        public EnemyTakeTurnSequence(ActorInstance enemy)
        {
            this.enemy = enemy;
        }

        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            // If this enemy died/despawned before acting, just end turn.
            if (enemy == null || !enemy.IsPlaying)
            {
                g.SequenceManager.Add(new EndTurnSequence());
                g.SequenceManager.Execute();
                yield break;
            }

            // Small pacing
            yield return Wait.None();

            // US-083: fire any boss-phase transitions whose HP threshold was crossed since last turn
            // (e.g. the Cyclops enrage) BEFORE the turn proceeds. Queued ahead of the action below.
            foreach (var transition in Scripts.Services.BossPhaseRunner.AdvancePhasesAndCollectTransitions(enemy))
                g.SequenceManager.Add(transition);

            // US-026 (Legion Option A): a Caster may TELEGRAPH a charge instead of moving/meleeing.
            // PlanCast is pure and side-effect-free; we only branch on its result. Skip planning if a
            // charge is already in flight for this enemy (don't stack) — it then takes a normal turn.
            // US-083 knob: in a "prefers charge" phase a caster boss charges even at melee range.
            bool alreadyCasting = g.TimelineBar != null && g.TimelineBar.GetSpellIconFor(enemy) != null;
            bool prefersCharge = Scripts.Services.BossPhaseRunner.Current(enemy)?.PrefersCharge ?? false;
            var charge = alreadyCasting ? null
                : Scripts.Services.EnemyPlanner.PlanCast(enemy, g.Actors.All, ignoreMeleeRange: prefersCharge);

            // US-139: a trap-layer with no adjacent hero may spend its turn arming a snare
            // (RNG-rolled; deterministic under RNG.Seed). Cornered layers fight normally.
            if (Scripts.Data.Actor.TrapCatalog.IsTrapLayer(enemy) && charge == null)
            {
                bool heroAdjacent = g.Actors.Heroes.Any(h =>
                    h != null && h.IsPlaying && Scripts.Utilities.Geometry.AreAdjacent(h, enemy));
                if (!heroAdjacent && RNG.Percent < Scripts.Data.Actor.TrapCatalog.LayChancePerTurn)
                {
                    g.SequenceManager.Add(new PlaceTrapSequence(enemy));
                    g.SequenceManager.Add(new EndTurnSequence());
                    g.SequenceManager.Execute();
                    yield break;
                }
            }

            if (charge != null)
            {
                // Charge path: spawn the cast-icon and end the turn. The cast resolves later on the
                // shared clock (its own onComplete), so no move/attack chain this turn.
                g.SequenceManager.Add(new EnemyChargeSequence(enemy, charge.Target, charge.Ability));
                g.SequenceManager.Add(new EndTurnSequence());
                g.SequenceManager.Execute();
                yield break;
            }

            // Queue sequences: move once, attack once
            g.SequenceManager.Add(new EnemyMoveSequence(enemy));
            g.SequenceManager.Add(new EnemyPreAttackSequence(enemy));
            g.SequenceManager.Add(new EnemyAttackSequence(enemy));
            g.SequenceManager.Add(new EnemyPostAttackSequence(enemy));
            g.SequenceManager.Add(new DeathSequence());
            g.SequenceManager.Add(new EndTurnSequence());
            g.SequenceManager.Execute();
        }
    }
}
