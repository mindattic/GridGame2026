using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Forces a hero to drop at their current location when interrupted mid-movement
    /// (e.g., by a timeline trigger or banking). Handles all cleanup without InputMode guards.
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

            // Pause timeline movement
            g.TimelineBar?.OnHeroStopMove();

            // Reset tile colors
            g.TileManager?.Reset();

            // Refresh mana UI
            g.ManaPoolManager?.RefreshUI();

            // Clear support lines
            g.SupportLineManager?.Clear();

            // Finalize hero position at current tile
            hero.Move.ToLocation();
            hero.Flags.IsMoving = false;
            hero.transform.localRotation = Quaternion.Euler(Vector3.zero);

            // Notify sorting manager
            g.SortingManager?.OnSelectedHeroDrop();

            // Clear moving hero reference
            g.Actors.MovingHero = null;

            // Reset selection state
            g.SelectionManager.ResetState();

            // Small visual pause for the drop
            yield return Wait.For(0.05f);
        }
    }
}
