using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Instances.Actor;
using Scripts.Models;

namespace Scripts.Managers
{
    /// <summary>
    /// SNAKEBOSSMANAGER - The segmented snake boss chain (US-140 / GG-A5; LttP Lanmola-style).
    ///
    /// <para>PURPOSE: A snake boss is a HEAD (the only chain member with a timeline icon and
    /// turns) plus N body SEGMENTS, each a real 1×1 enemy on its own tile. The chain moves as a
    /// unit — head steps via EnemyPlanner, each segment slides into its predecessor's vacated
    /// tile. Damage gates TAIL-FIRST: only the chain's last living member is vulnerable
    /// (pincer detection still sees every member — the armor zeroes the damage with an
    /// "Armored!" callout), so the player must pincer the pieces off from the back before the
    /// head can be hurt. Vulnerability is computed LAZILY from liveness — no death hooks.</para>
    ///
    /// <para>CANON RULE: every spawned <see cref="CharacterClass.Naga00"/> is a chain head
    /// (StageManager.LoadWave calls <see cref="CreateChain"/>). Chain members are immovable
    /// walls to hero drags (ActorMovement wall check), and segments never get timeline icons
    /// (TimelineBarInstance filter).</para>
    ///
    /// <para>RELATED FILES: StageManager.cs (spawn hook), EnemyMoveSequence.cs (follow),
    /// ActorInstance.cs (armor gate in DamageRoutine), TimelineBarInstance.cs (icon filter),
    /// ActorMovement.cs (wall check), StageLibrary.cs (Test-Snake fixture).</para>
    /// </summary>
    public static class SnakeBossManager
    {
        public const int DefaultSegmentCount = 3;

        // Each chain: index 0 = head, then segments toward the tail.
        private static readonly List<List<ActorInstance>> chains = new List<List<ActorInstance>>();

        // Head location snapshot taken before the head's move, consumed by the follow step.
        private static readonly Dictionary<ActorInstance, Vector2Int> preMoveHeadLocation
            = new Dictionary<ActorInstance, Vector2Int>();

        /// <summary>Registers a chain. The spawner delegate creates each segment actor (so this
        /// class stays free of StageManager internals); segments are placed on free tiles
        /// adjacent to the previous member, best-effort (fewer segments on a crowded board).</summary>
        public static void CreateChain(ActorInstance head, int segmentCount,
            System.Func<Vector2Int, ActorInstance> spawnSegmentAt)
        {
            if (head == null || spawnSegmentAt == null) return;
            var chain = new List<ActorInstance> { head };

            var tail = head;
            for (int i = 0; i < segmentCount; i++)
            {
                var freeTile = g.TileMap?.FindFirstFreeAdjacent(tail.location);
                if (freeTile == null) break;
                var segment = spawnSegmentAt(freeTile.location);
                if (segment == null) break;
                chain.Add(segment);
                tail = segment;
            }

            chains.Add(chain);
        }

        /// <summary>True when the actor is any member of any chain (head or segment).</summary>
        public static bool IsChainMember(ActorInstance actor)
            => actor != null && chains.Any(c => c.Contains(actor));

        /// <summary>True for body segments only (never get timeline icons or turns).</summary>
        public static bool IsSegment(ActorInstance actor)
            => actor != null && chains.Any(c => c.IndexOf(actor) > 0);

        /// <summary>Tail-first gating: a member is armored while ANY member behind it (toward
        /// the tail) is still alive. The last living member is always vulnerable.</summary>
        public static bool IsArmored(ActorInstance actor)
        {
            if (actor == null) return false;
            foreach (var chain in chains)
            {
                int index = chain.IndexOf(actor);
                if (index < 0) continue;
                for (int i = index + 1; i < chain.Count; i++)
                    if (chain[i] != null && chain[i].IsPlaying) return true;
                return false;
            }
            return false;
        }

        /// <summary>Snapshot the head's tile before its move (EnemyMoveSequence, pre-move).</summary>
        public static void RecordPreMove(ActorInstance head)
        {
            if (head != null && chains.Any(c => c.Count > 0 && c[0] == head))
                preMoveHeadLocation[head] = head.location;
        }

        /// <summary>After the head moved: shift every living segment into its predecessor's
        /// vacated tile, front to back (reuses the displacement slide so it animates).</summary>
        public static IEnumerator FollowRoutine(ActorInstance head)
        {
            var chain = chains.FirstOrDefault(c => c.Count > 0 && c[0] == head);
            if (chain == null || !preMoveHeadLocation.TryGetValue(head, out var vacated))
                yield break;
            preMoveHeadLocation.Remove(head);
            if (head.location == vacated) yield break; // head didn't actually move

            for (int i = 1; i < chain.Count; i++)
            {
                var segment = chain[i];
                if (segment == null || !segment.IsPlaying) break; // chain broken at the gap
                var next = segment.location;
                if (segment.location != vacated)
                    segment.Move.HandleOverlap(vacated); // vacated tile is guaranteed free
                vacated = next;
                yield return null; // one frame apart — the chain visibly ripples
            }
        }

        /// <summary>Wipe all chains (new battle / restart).</summary>
        public static void Clear()
        {
            chains.Clear();
            preMoveHeadLocation.Clear();
        }
    }
}
