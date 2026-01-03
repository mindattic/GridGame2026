using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Handles the full flow when an enemy timeline tag reaches the trigger point:
    /// 1. Force drop any moving hero
    /// 2. Resolve hero pincer attacks (if any)
    /// 3. Begin the enemy turn
    /// </summary>
    public sealed class TimelineTriggerSequence : SequenceEvent
    {
        private readonly ActorInstance triggeringEnemy;

        public TimelineTriggerSequence(ActorInstance enemy)
        {
            triggeringEnemy = enemy;
        }

        public override IEnumerator ProcessRoutine()
        {
            if (triggeringEnemy == null || !triggeringEnemy.IsPlaying)
                yield break;

            // Step 1: Force drop any hero that's being moved
            var hero = g.Actors.SelectedActor;
            if (hero != null && hero.IsHero && g.SelectionManager.IsHeroBeingMoved)
            {
                g.SequenceManager.Add(new ForceHeroDropSequence(hero));
                yield return g.SequenceManager.ExecuteRoutine();
            }

            // Step 2: Check and resolve hero pincer attacks
            var participants = g.PincerAttackManager.GetParticipants(Team.Hero, hero);
            if (participants.pair.Count > 0)
            {
                g.SequenceManager.Add(new HeroPincerSequence(participants, hero));
                yield return g.SequenceManager.ExecuteRoutine();
            }

            // Step 3: Begin the enemy turn
            g.TurnManager.BeginEnemyTurn(triggeringEnemy);
        }
    }
}
