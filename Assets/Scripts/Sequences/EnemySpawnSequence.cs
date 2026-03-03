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
    /// ENEMYSPAWNSEQUENCE - Spawns queued enemies onto the battlefield.
    /// 
    /// PURPOSE:
    /// Finds all enemies flagged as spawnable and places them
    /// on random unoccupied tiles.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Find all spawnable enemies
    /// 2. For each, find random unoccupied tile
    /// 3. Spawn enemy at location
    /// 4. Wait for spawn visuals
    /// 5. Add timeline tags for new enemies
    /// 6. If enemy turn, begin first spawned enemy's turn
    /// 
    /// RELATED FILES:
    /// - StageManager.cs: Creates spawnable enemies
    /// - EnemyManager.cs: Enemy pool management
    /// - TimelineBarInstance.cs: Timeline tag creation
    /// </summary>
    public class EnemySpawnSequence : SequenceEvent
    {
        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            // Show any enemies flagged as spawnable
            var spawnableEnemies = g.Actors.Enemies.Where(x => x.IsSpawnable).ToList();
            foreach (var enemy in spawnableEnemies)
            {
                var unoccupiedLocation = RNG.UnoccupiedLocation;
                if (unoccupiedLocation != null)
                    enemy.Spawn(unoccupiedLocation);
            }

            // Allow spawn visuals to apply
            yield return Wait.None();

            // Ensure new enemies are represented on the timeline
            g.TimelineBar?.EnsureTagsForAllEnemies(false);

            // If currently in enemy turn and no active actor set, pick first spawned enemy
            if (g.TurnManager.IsEnemyTurn && g.TurnManager.ActiveActor == null)
            {
                var first = spawnableEnemies.FirstOrDefault(e => e != null && e.IsPlaying);
                if (first != null)
                    g.TurnManager.BeginEnemyTurn(first);
            }
        }
    }
}
