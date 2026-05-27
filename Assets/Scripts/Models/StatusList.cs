using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts.Models
{
    /// <summary>
    /// STATUSTICKRESULT - What one turn-tick of a StatusList produced: the net HP change to apply
    /// (negative = damage, positive = healing) and which kinds expired (for VFX cleanup).
    /// </summary>
    public sealed class StatusTickResult
    {
        public int HpDelta;
        public readonly List<StatusKind> Expired = new List<StatusKind>();
        public bool DealtDamage;
        public bool Healed;
    }

    /// <summary>
    /// STATUSLIST - The buff/debuff container that lives on every actor.
    ///
    /// <para>PURPOSE: This is the BRAIN for status effects — one place that owns an actor's active
    /// buffs/debuffs, ticks them per turn, and answers aggregate questions (how much damage is
    /// mitigated? is the actor silenced/stunned? how many debuffs to cleanse?). It holds NO Unity
    /// scene references and never reaches g.; the actor applies the HP delta and shows feedback.
    /// </para>
    ///
    /// <para>EXTENSION: a new effect = one StatusKind value + (if it ticks) one case in
    /// AdvanceOneTurn + (if it changes a formula) one aggregator property. Curing = ClearDebuffs.
    /// </para>
    /// </summary>
    public sealed class StatusList
    {
        private readonly List<StatusEffect> active = new List<StatusEffect>();
        public IReadOnlyList<StatusEffect> Active => active;

        /// <summary>Fraction of incoming damage prevented by Protect buffs (summed, capped 80%).</summary>
        public float ProtectPercent
        {
            get
            {
                float sum = 0f;
                foreach (var s in active)
                    if (s.Kind == StatusKind.Protect) sum += s.Magnitude;
                return Mathf.Clamp(sum, 0f, 0.8f);
            }
        }

        public bool IsSilenced => active.Any(s => s.Kind == StatusKind.Silence);
        public bool IsStunned => active.Any(s => s.Kind == StatusKind.Stun);
        public bool Has(StatusKind kind) => active.Any(s => s.Kind == kind);
        public int DebuffCount => active.Count(s => IsDebuff(s.Kind));

        /// <summary>Adds an effect. Stacking is "instances coexist"; aggregators combine them.</summary>
        public void Apply(StatusEffect e)
        {
            if (e == null) return;
            active.Add(e);
        }

        /// <summary>
        /// Advances every effect by one turn: accrues DoT damage / Regen healing into a net
        /// HpDelta, decrements durations, and drops expired effects. Pure — mutates only this
        /// list and returns what the owning actor should apply.
        /// </summary>
        public StatusTickResult AdvanceOneTurn()
        {
            var result = new StatusTickResult();

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var s = active[i];
                switch (s.Kind)
                {
                    case StatusKind.Regen:
                        result.HpDelta += Mathf.RoundToInt(s.Magnitude);
                        result.Healed = true;
                        break;
                    case StatusKind.Burn:
                    case StatusKind.Poison:
                    case StatusKind.Bleed:
                        result.HpDelta -= Mathf.RoundToInt(s.Magnitude);
                        result.DealtDamage = true;
                        break;
                }

                s.RemainingTurns--;
                if (s.RemainingTurns <= 0)
                {
                    result.Expired.Add(s.Kind);
                    active.RemoveAt(i);
                }
            }

            return result;
        }

        /// <summary>Removes all debuffs (the Esuna/cleanse path). Returns how many fell off.</summary>
        public int ClearDebuffs()
        {
            int removed = 0;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (IsDebuff(active[i].Kind))
                {
                    active.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>Removes everything (e.g., on death / battle reset).</summary>
        public void Clear() => active.Clear();

        private static bool IsDebuff(StatusKind k) =>
            k == StatusKind.Burn ||
            k == StatusKind.Poison ||
            k == StatusKind.Bleed ||
            k == StatusKind.Slow ||
            k == StatusKind.Silence ||
            k == StatusKind.Stun;
    }
}
