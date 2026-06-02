using System.Linq;
using UnityEngine;
using Scripts.Models;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Managers
{
    /// <summary>
    /// BUFFTICKMANAGER - Drives per-tick effects (Burning damage, Poisoned damage, Wet/Warm
    /// countdown) off the timeline clock.
    ///
    /// <para>Every <see cref="TickSeconds"/> while the timeline is advancing: for each playing
    /// actor, the manager walks their <see cref="BuffSystem"/>-tracked buffs and (a) deals any
    /// <see cref="Buff.DamagePerTick"/> they declare, then (b) decrements any tick-unit duration.
    /// Buffs with on-expire follow-ups (Fire→Warm, Frozen→Wet) are chained by BuffSystem.Expire.</para>
    ///
    /// <para>Game-scene-only — silently no-ops elsewhere. Auto-attached by <see cref="ManaPoolManager.Start"/>.</para>
    /// </summary>
    public sealed class BuffTickManager : MonoBehaviour
    {
        public const float TickSeconds = 1.0f;

        private float timer;

        private void Update()
        {
            // Only tick while the timeline is actually advancing (matches the legacy
            // ManaPoolManager.Update pattern).
            var tl = g.TimelineBar;
            if (tl == null || !tl.IsAdvancing) return;

            timer += Time.deltaTime;
            if (timer < TickSeconds) return;
            timer = 0f;

            // `g.Actors` is a nested static class, not a property — can't be null-checked.
            // The All list itself can be null pre-GameReady; guard on that.
            var all = g.Actors.All;
            if (all == null) return;

            for (int i = 0; i < all.Count; i++)
            {
                var actor = all[i];
                if (actor == null || !actor.IsPlaying || actor.Stats == null) continue;

                // Apply damage-per-tick BEFORE decrementing so a 1-remaining buff still ticks.
                // Snapshot to an array: OnDamaged below can break (RemoveAt) buffs flagged
                // BreaksOnDamage from the LIVE list GetAll returns, shifting indices mid-loop
                // and skipping a sibling DoT buff. Iterating a copy keeps every buff ticking.
                var buffs = BuffSystem.GetAll(actor).ToArray();
                if (buffs != null)
                {
                    for (int b = 0; b < buffs.Length; b++)
                    {
                        var bi = buffs[b];
                        if (bi.IsExpired) continue;
                        if (bi.Definition.DurationUnit != BuffDurationUnit.Ticks) continue;
                        if (bi.Definition.DamagePerTick > 0f)
                        {
                            int dmg = Mathf.Max(1, Mathf.RoundToInt(bi.Definition.DamagePerTick));
                            actor.Stats.HP = Mathf.Clamp(actor.Stats.HP - dmg, 0, actor.Stats.MaxHP);
                            BuffSystem.OnDamaged(actor);
                        }
                    }
                }

                // Decrement tick-unit durations (may trigger on-expire follow-ups).
                BuffSystem.TickClock(actor);
            }
        }
    }
}
