using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// FORCEHERODROPSEQUENCE - Forces immediate hero placement.
    /// 
    /// PURPOSE:
    /// When a hero is mid-drag and an enemy timeline tag triggers,
    /// this sequence forces the hero to drop at their current
    /// position immediately.
    /// 
    /// TRIGGER CONDITIONS:
    /// - Timeline tag reaches trigger during hero drag
    /// - Banking action interrupts movement
    /// 
    /// CLEANUP ACTIONS:
    /// 1. Pause timeline movement
    /// 2. Reset tile highlighting
    /// 3. Refresh mana UI
    /// 4. Clear support lines
    /// 5. Finalize hero position
    /// 6. Reset selection state
    /// 7. Check for pincer attack
    /// 
    /// RELATED FILES:
    /// - TimelineTriggerSequence.cs: Creates this sequence
    /// - SelectionManager.cs: Selection state reset
    /// - PincerAttackManager.cs: Pincer check on drop
    /// </summary>
    public sealed class ForceHeroDropSequence : SequenceEvent
    {
        private readonly ActorInstance hero;

        public ForceHeroDropSequence(ActorInstance hero)
        {
            this.hero = hero;
        }

        public override IEnumerator ProcessRoutine()
        {
            if (hero == null || !hero.IsHero)
                yield break;

            g.TimelineBar?.OnHeroStopMove();
            g.TileManager?.Reset();
            g.ManaPoolManager?.RefreshUI();
            g.SupportLineManager?.Clear();

            hero.Move.ToLocation();
            hero.Flags.IsMoving = false;
            hero.transform.localRotation = Quaternion.Euler(Vector3.zero);

            g.SortingManager?.OnSelectedHeroDrop();
            g.Actors.MovingHero = null;
            g.SelectionManager.ResetState();

            // Small visual pause for the drop
            yield return Wait.For(0.05f);
        }
    }
}
