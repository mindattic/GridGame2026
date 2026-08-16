using System.Collections;
using System.Linq;
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
    /// PLACETRAPSEQUENCE - A trap-layer enemy spends its turn arming a tile (US-139 / GG-A5).
    ///
    /// <para>PURPOSE: The enemy picks an unoccupied, untrapped tile CARDINALLY ADJACENT to
    /// itself (the snare is laid at its feet, not teleported across the board), registers the
    /// trap with <see cref="TrapManager"/>, and announces it. Any hero who slides — or is
    /// displaced — onto the tile springs it (ActorMovement trigger hooks).</para>
    ///
    /// <para>Queued by <see cref="EnemyTakeTurnSequence"/> in place of the move/attack chain,
    /// with the same rhythm as the charge branch.</para>
    ///
    /// RELATED FILES: TrapCatalog.cs, TrapManager.cs, EnemyTakeTurnSequence.cs, ActorMovement.cs.
    /// </summary>
    public sealed class PlaceTrapSequence : SequenceEvent
    {
        private readonly ActorInstance enemy;

        public PlaceTrapSequence(ActorInstance enemy)
        {
            this.enemy = enemy;
        }

        /// <summary>Coroutine that arms the trap with a readable beat.</summary>
        public override IEnumerator ProcessRoutine()
        {
            if (enemy == null || !enemy.IsPlaying)
                yield break;

            var trap = TrapCatalog.For(enemy);
            if (trap == null) yield break;

            // Adjacent, on-board, unoccupied, untrapped candidates.
            var candidates = g.TileMap?
                .GetAdjacentNeighbors(enemy.location, includeOccupied: false)
                .Where(t => t != null && !TrapManager.HasTrapAt(t.location))
                .ToList();
            if (candidates == null || candidates.Count == 0) yield break;

            var tile = RNG.Pick(candidates);
            if (tile == null) yield break;

            TrapManager.Place(tile.location, trap);
            g.AudioManager?.Play("Debuff");
            AnnouncementWindow.Announce($"{enemy.characterClass} lays a {trap.DisplayName}!");

            // A readable beat so the placement doesn't feel like a skipped turn.
            yield return Wait.For(0.4f);
        }
    }
}
