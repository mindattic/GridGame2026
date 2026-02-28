using Assets.Scripts.Factories;
using Assets.Scripts.Models;
using Game.Instances;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Behaviors
{
    /// <summary>
    /// ATTACKLINEMANAGER - Manages attack connection lines during combat.
    /// 
    /// PURPOSE:
    /// Creates and manages visual lines connecting attackers to their targets
    /// during pincer attacks and other combat sequences.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// [Hero A] ════════════════════ [Hero B]
    ///     ↑                            ↑
    ///     └──────── attack line ───────┘
    ///           (connects pincer pair)
    /// ```
    /// 
    /// KEYING:
    /// Lines keyed by (startLocation, endLocation) tuple to prevent duplicates.
    /// Uses actor grid positions, not actor references.
    /// 
    /// LIFECYCLE:
    /// 1. PincerAttackManager detects valid pincer
    /// 2. Spawn(actorPair) creates line between attackers
    /// 3. Line animates during combat
    /// 4. DespawnAll() clears after sequence
    /// 
    /// RELATED FILES:
    /// - AttackLineFactory.cs: Creates line GameObjects
    /// - AttackLineInstance.cs: Line behavior component
    /// - PincerAttackSequence.cs: Uses attack lines
    /// - ActorPair.cs: Start/end actor data
    /// 
    /// ACCESS: g.AttackLineManager
    /// </summary>
    public class AttackLineManager : MonoBehaviour
    {
        /// <summary>Active attack lines keyed by (startLoc, endLoc).</summary>
        public Dictionary<(Vector2Int, Vector2Int), AttackLineInstance> attackLines = new Dictionary<(Vector2Int, Vector2Int), AttackLineInstance>();

        /// <summary>Checks if a line exists for the given actor pair.</summary>
        public bool Exists(ActorPair actorPair)
        {
            var key = GetKey(actorPair);
            return attackLines.ContainsKey(key);
        }

        /// <summary>Creates an attack line between the two actors in the pair.</summary>
        public void Spawn(ActorPair actorPair)
        {
            var key = GetKey(actorPair);

            if (Exists(actorPair))
                return;

            var go = AttackLineFactory.Create();
            go.transform.position = Vector2.zero;
            go.transform.rotation = Quaternion.identity;
            var instance = go.GetComponent<AttackLineInstance>();
            attackLines[key] = instance;
            instance.Spawn(actorPair);
        }

        /// <summary>Removes the attack line for the given pair.</summary>
        public void Despawn(ActorPair pair)
        {
            var key = GetKey(pair);
            if (attackLines.TryGetValue(key, out var instance))
            {
                instance.Despawn();
                attackLines.Remove(key);
            }
        }

        /// <summary>Removes all attack lines.</summary>
        public void DespawnAll()
        {
            foreach (var instance in attackLines.Values)
            {

                instance.Despawn();
            }
            attackLines.Clear();
        }

        /// <summary>Creates dictionary key from actor pair locations.</summary>
        private (Vector2Int, Vector2Int) GetKey(ActorPair actorPair)
        {
            return (actorPair.startActor.location, actorPair.endActor.location);
        }
    }
}
