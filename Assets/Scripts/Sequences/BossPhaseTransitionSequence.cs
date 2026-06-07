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
    /// BOSSPHASETRANSITIONSEQUENCE - The one-time effect when a boss ENTERS an authored phase (US-083).
    ///
    /// <para>Reusable, data-built (see <c>BossScriptLibrary</c>): announces the phase, optionally
    /// self-heals a fraction of MaxHP, and optionally <b>Quickens</b> the boss's own timeline icon
    /// (US-028) so an "enrage" reads as "acts sooner". Composed from existing systems — no per-boss
    /// subclass. The Cyclops uses it for its sub-50%-HP enrage (banner + hasten).</para>
    /// </summary>
    public sealed class BossPhaseTransitionSequence : SequenceEvent
    {
        private readonly ActorInstance boss;
        private readonly string label;
        private readonly float hastenU;
        private readonly float healFraction;

        public BossPhaseTransitionSequence(ActorInstance boss, string label, float hastenU = 0f, float healFraction = 0f)
        {
            this.boss = boss;
            this.label = label;
            this.hastenU = hastenU;
            this.healFraction = healFraction;
        }

        /// <summary>Coroutine that announces + applies the phase-entry effect.</summary>
        public override IEnumerator ProcessRoutine()
        {
            if (boss == null || !boss.IsPlaying)
                yield break;

            // Announce the phase change (routes through the dedicated AnnouncementWindow once built).
            g.ActionTitle?.Show($"{boss.characterClass} is {label}");
            g.CombatTextManager?.Spawn(label, boss.Position, "Damage");

            if (healFraction > 0f && boss.Stats != null && boss.Stats.MaxHP > 0f)
            {
                int gain = Mathf.RoundToInt(boss.Stats.MaxHP * healFraction);
                boss.Stats.HP = Mathf.Clamp(boss.Stats.HP + gain, 0, boss.Stats.MaxHP);
                boss.HealthText.Refresh();
                g.CombatTextManager?.Spawn($"+{gain}", boss.Position, "Heal");
            }

            // Enrage = acts sooner: slide the boss's own timeline icon toward the trigger (US-028).
            if (hastenU > 0f)
                g.TimelineBar?.HastenIcon(boss, hastenU);

            yield return Wait.For(0.4f); // a beat so the announcement reads before the turn proceeds
        }
    }
}
