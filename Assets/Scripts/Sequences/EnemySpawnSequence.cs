using System.Collections;
using System.Linq;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    public class EnemySpawnSequence : SequenceEvent
    {
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
