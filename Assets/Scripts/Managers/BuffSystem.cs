using System.Collections.Generic;
using UnityEngine;
using Scripts.Data;
using Scripts.Instances.Actor;
using Scripts.Models;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Managers
{
    /// <summary>
    /// BUFFSYSTEM - Central registry of which actors carry which buffs.
    ///
    /// <para>Stateless from Unity's perspective — pure dictionary keyed by ActorInstance, so any
    /// caller can <see cref="Apply"/> a buff to an actor, query <see cref="Has"/>, fetch the
    /// effective incoming-damage multiplier via <see cref="GetIncomingDamageMultiplier"/>, or
    /// <see cref="TickTurn"/>/<see cref="TickClock"/> the durations.</para>
    ///
    /// <para>The damage-formula and action-gating sites read from this; they don't need to know
    /// what "Protection" or "Frozen" means individually — they read the knobs off the buff.</para>
    /// </summary>
    public static class BuffSystem
    {
        private static readonly Dictionary<ActorInstance, List<BuffInstance>> active =
            new Dictionary<ActorInstance, List<BuffInstance>>();

        /// <summary>
        /// Drops every tracked buff. Must run whenever the battle's actors are torn down
        /// (battle start, stage restart) — the dictionary is static, so destroyed
        /// ActorInstance keys otherwise survive into the next run as ghost buffs.
        /// </summary>
        public static void Clear() => active.Clear();

        /// <summary>Apply a buff with its default duration. Stacks if already present (latest wins on duration).</summary>
        public static void Apply(ActorInstance target, Buff buff)
        {
            if (target == null || buff == null) return;
            Apply(target, buff, buff.DefaultDuration);
        }

        /// <summary>Apply with a custom duration.</summary>
        public static void Apply(ActorInstance target, Buff buff, int duration)
        {
            if (target == null || buff == null) return;
            if (!active.TryGetValue(target, out var list))
                active[target] = list = new List<BuffInstance>();

            // If already present, refresh to max(remaining, new) so applying twice doesn't shorten.
            var existing = list.Find(bi => bi.Definition.Id == buff.Id);
            if (existing != null)
                existing.RemainingDuration = Mathf.Max(existing.RemainingDuration, duration);
            else
                list.Add(new BuffInstance(buff, duration));
        }

        /// <summary>Apply a buff to every hero (Protection from the Shield button uses this).</summary>
        public static void ApplyToAllHeroes(Buff buff)
        {
            var heroes = g.Actors.Heroes;
            if (heroes == null) return;
            foreach (var h in heroes) Apply(h, buff);
        }

        /// <summary>Strip every debuff (Kind == Debuff) off the target — used by Antidote / Cleanse spells.</summary>
        public static int RemoveAllDebuffs(ActorInstance target)
        {
            if (target == null || !active.TryGetValue(target, out var list)) return 0;
            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].Definition.Kind == BuffKind.Debuff) { list.RemoveAt(i); removed++; }
            return removed;
        }

        /// <summary>Remove all instances of a specific buff by id — used by element-interaction code (Fire×Wet → strip Wet).</summary>
        public static int RemoveAllDebuffsMatching(ActorInstance target, string buffId)
        {
            if (target == null || string.IsNullOrEmpty(buffId) || !active.TryGetValue(target, out var list)) return 0;
            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].Definition.Id == buffId) { list.RemoveAt(i); removed++; }
            return removed;
        }

        public static bool Has(ActorInstance target, string buffId)
        {
            if (target == null || !active.TryGetValue(target, out var list)) return false;
            return list.Exists(bi => bi.Definition.Id == buffId && !bi.IsExpired);
        }

        public static IReadOnlyList<BuffInstance> GetAll(ActorInstance target)
        {
            return active.TryGetValue(target, out var list) ? (IReadOnlyList<BuffInstance>)list : System.Array.Empty<BuffInstance>();
        }

        /// <summary>
        /// Multiplier to apply to incoming damage on <paramref name="target"/>. Returns 1f for a
        /// clean target; 0.85 if Protection is active; multiplies in any other DR buffs too.
        /// </summary>
        public static float GetIncomingDamageMultiplier(ActorInstance target)
        {
            if (target == null || !active.TryGetValue(target, out var list)) return 1f;
            float m = 1f;
            foreach (var bi in list)
            {
                if (bi.IsExpired) continue;
                if (bi.Definition.IncomingDamageReductionPercent > 0f)
                    m *= (1f - bi.Definition.IncomingDamageReductionPercent);
            }
            return m;
        }

        /// <summary>True if any active buff on <paramref name="target"/> says they're immobile (Frozen, Sleep).</summary>
        public static bool IsImmobile(ActorInstance target)
        {
            if (target == null || !active.TryGetValue(target, out var list)) return false;
            foreach (var bi in list)
                if (!bi.IsExpired && bi.Definition.Immobile) return true;
            return false;
        }

        /// <summary>Notify that <paramref name="target"/> took damage — breaks buffs flagged BreaksOnDamage (Sleep).</summary>
        public static void OnDamaged(ActorInstance target)
        {
            BreakWhere(target, bi => bi.Definition.BreaksOnDamage);
        }

        /// <summary>Notify that <paramref name="target"/> was moved — breaks buffs flagged BreaksOnMove (Sleep on slide-through).</summary>
        public static void OnMoved(ActorInstance target)
        {
            BreakWhere(target, bi => bi.Definition.BreaksOnMove);
        }

        /// <summary>Tick all turn-unit buffs on <paramref name="target"/> by 1 — call at the bearer's turn boundary.</summary>
        public static void TickTurn(ActorInstance target) => Decrement(target, BuffDurationUnit.Turns);

        /// <summary>Tick all tick-unit buffs on <paramref name="target"/> by 1 — call on each timeline clock tick.</summary>
        public static void TickClock(ActorInstance target) => Decrement(target, BuffDurationUnit.Ticks);

        // ── internals ──

        private static void Decrement(ActorInstance target, BuffDurationUnit unit)
        {
            if (target == null || !active.TryGetValue(target, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var bi = list[i];
                if (bi.Definition.DurationUnit != unit) continue;
                bi.RemainingDuration--;
                if (bi.IsExpired) Expire(target, list, i);
            }
        }

        private static void BreakWhere(ActorInstance target, System.Func<BuffInstance, bool> predicate)
        {
            if (target == null || !active.TryGetValue(target, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i])) Expire(target, list, i);
            }
        }

        private static void Expire(ActorInstance target, List<BuffInstance> list, int index)
        {
            var expiring = list[index];
            list.RemoveAt(index);
            // Follow-up (Fire→Warm, Frozen→Wet).
            if (!string.IsNullOrEmpty(expiring.Definition.OnExpireApplyId) &&
                Buffs.ById.TryGetValue(expiring.Definition.OnExpireApplyId, out var followup))
            {
                Apply(target, followup);
            }
        }
    }
}
