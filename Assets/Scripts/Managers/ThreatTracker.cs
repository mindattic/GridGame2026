using System.Collections.Generic;
using Scripts.Instances.Actor;

namespace Scripts.Managers
{
    /// <summary>
    /// THREATTRACKER - Per-battle tally of how much damage each hero has dealt to enemies (US-080).
    ///
    /// <para>PURPOSE: feeds enemy target selection so that <b>smarter (higher-INT) enemies prefer to
    /// strike whoever has been hurting them most</b>, while dumb enemies keep targeting the
    /// nearest/most-wounded hero. Accrued in <see cref="ActorInstance.DamageRoutine"/> on every
    /// hero→enemy hit; read by <see cref="Scripts.Services.EnemyPlanner"/>; cleared at battle start
    /// (<c>TurnManager.Initialize</c>). Static-dictionary-per-actor shape mirrors <see cref="BuffSystem"/>
    /// and <see cref="SkillCooldownManager"/> — pure data, no scene access (so EnemyPlanner stays testable).</para>
    /// </summary>
    public static class ThreatTracker
    {
        private static readonly Dictionary<ActorInstance, float> threat = new Dictionary<ActorInstance, float>();

        /// <summary>Add <paramref name="amount"/> damage-dealt to <paramref name="hero"/>'s threat.</summary>
        public static void AddThreat(ActorInstance hero, float amount)
        {
            if (hero == null || amount <= 0f) return;
            threat.TryGetValue(hero, out var cur);
            threat[hero] = cur + amount;
        }

        /// <summary>Total damage <paramref name="hero"/> has dealt this battle (0 if none).</summary>
        public static float GetThreat(ActorInstance hero)
        {
            if (hero == null) return 0f;
            return threat.TryGetValue(hero, out var v) ? v : 0f;
        }

        /// <summary>Highest threat among the given heroes (0 if none) — used to normalize.</summary>
        public static float MaxThreat(IEnumerable<ActorInstance> heroes)
        {
            float max = 0f;
            if (heroes == null) return 0f;
            foreach (var h in heroes)
            {
                var t = GetThreat(h);
                if (t > max) max = t;
            }
            return max;
        }

        /// <summary>Wipe all threat (new battle).</summary>
        public static void Clear() => threat.Clear();
    }
}
