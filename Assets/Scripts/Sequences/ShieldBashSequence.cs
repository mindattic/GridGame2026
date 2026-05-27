using Scripts.Helpers;
using System.Collections;
using UnityEngine;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// Paladin Shield Bash sequence: slide to the tile adjacent to the target (toward Paladin)
    /// and perform a bump with impact feedback. Preconditions (alignment, clear path, play states)
    /// are validated by the caller before this sequence is queued.
    /// </summary>
    public class ShieldBashSequence : SequenceEvent
    {
        private readonly ActorInstance paladin;
        private readonly ActorInstance target;

        public ShieldBashSequence(ActorInstance paladin, ActorInstance target)
        {
            this.paladin = paladin;
            this.target = target;
        }

        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            // Safety checks in case state changed after queuing
            if (paladin == null || target == null || !paladin.IsPlaying || !target.IsPlaying)
                yield break;

            // Determine direction from target to paladin (axis-aligned is guaranteed by caller)
            Direction dirFromTargetToPaladin;
            if (paladin.location.x == target.location.x)
                dirFromTargetToPaladin = (paladin.location.y > target.location.y) ? Direction.North : Direction.South;
            else
                dirFromTargetToPaladin = (paladin.location.x > target.location.x) ? Direction.East : Direction.West;

            // Destination: adjacent to target along the axis toward paladin
            var destLoc = Geometry.GetAdjacentLocationInDirection(target.location, dirFromTargetToPaladin);
            var destTile = g.TileMap.GetTile(destLoc);
            if (destTile == null || destTile.IsOccupied)
                yield break; // cannot move; abort cleanly

            // Slide via actor movement routine
            paladin.location = destLoc;
            yield return paladin.Move.TowardDestinationRoutine();

            // Bump -> damage/feedback
            yield return paladin.Animation.BumpRoutine(target, ShieldBashDamageRoutine());
        }

        /// <summary>Coroutine that executes the shield bash damage + stun at impact.</summary>
        private IEnumerator ShieldBashDamageRoutine()
        {
            if (target == null || !target.IsPlaying)
                yield break;

            // Real physical damage from the bash.
            var result = Formulas.CalculateAttackResult(paladin, target);
            target.Damage(result);

            // Stun for one round: skip the target's next move (reuses the working root-skip in
            // EnemyMoveSequence) and freeze its timeline icon in place (Pushback with zero push,
            // which drops the icon straight into Stunned mode for the duration).
            target.Statuses.Apply(new StatusEffect { Kind = StatusKind.Stun, Magnitude = 0f, RemainingTurns = 1 });
            target.Flags.RootedTurnsRemaining = System.Math.Max(target.Flags.RootedTurnsRemaining, 1);

            var icon = g.TimelineBar?.GetIconFor(target);
            icon?.Pushback(0f, 0f, 1f, target.Stats.Agility.ToInt(), 2f);

            g.CombatTextManager.Spawn("Stunned!", target.Position, "Damage");
            yield return null;
        }
    }
}
