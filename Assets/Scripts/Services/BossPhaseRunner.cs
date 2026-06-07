using System.Collections.Generic;
using UnityEngine;
using Scripts.Data.Actor;
using Scripts.Helpers;
using Scripts.Instances.Actor;
using Scripts.Sequences;

namespace Scripts.Services
{
    /// <summary>
    /// BOSSPHASERUNNER - The engine for authored boss phases (US-083). Pure (no g. switchboard, no
    /// scene access) — reads only the boss's HP + script and its <c>Flags.BossPhaseIndex</c>, so it
    /// stays reasoned-about/testable like the other Services. The caller (EnemyTakeTurnSequence)
    /// queues whatever transitions this returns onto the SequenceManager.
    /// </summary>
    public static class BossPhaseRunner
    {
        /// <summary>The phase index the boss SHOULD be in given its current HP — the deepest phase
        /// whose threshold its HP fraction has reached (phases are threshold-descending).</summary>
        public static int CurrentPhaseIndex(ActorInstance boss)
        {
            var phases = BossScriptLibrary.For(boss != null ? boss.characterClass : default);
            if (phases == null || phases.Count == 0) return 0;
            float frac = (boss.Stats != null && boss.Stats.MaxHP > 0f) ? boss.Stats.HP / boss.Stats.MaxHP : 1f;
            int idx = 0;
            for (int i = 0; i < phases.Count; i++)
                if (frac <= phases[i].HpThreshold) idx = i;
            return idx;
        }

        /// <summary>The phase the boss has currently ENTERED (clamped to its recorded index).</summary>
        public static BossPhase Current(ActorInstance boss)
        {
            var phases = BossScriptLibrary.For(boss != null ? boss.characterClass : default);
            if (phases == null || phases.Count == 0) return null;
            int i = Mathf.Clamp(boss.Flags != null ? boss.Flags.BossPhaseIndex : 0, 0, phases.Count - 1);
            return phases[i];
        }

        /// <summary>If HP has crossed into one or more deeper phases since the last check, advance
        /// <c>Flags.BossPhaseIndex</c> and return each newly-entered phase's transition sequence (in
        /// order) for the caller to queue. Returns an empty list if no new phase was entered.</summary>
        public static List<SequenceEvent> AdvancePhasesAndCollectTransitions(ActorInstance boss)
        {
            var result = new List<SequenceEvent>();
            var phases = BossScriptLibrary.For(boss != null ? boss.characterClass : default);
            if (phases == null || boss.Flags == null) return result;

            int target = CurrentPhaseIndex(boss);
            int from = boss.Flags.BossPhaseIndex;
            if (target <= from) return result;

            for (int i = from + 1; i <= target; i++)
            {
                var seq = phases[i].Transition?.Invoke(boss);
                if (seq != null) result.Add(seq);
            }
            boss.Flags.BossPhaseIndex = target;
            return result;
        }
    }
}
