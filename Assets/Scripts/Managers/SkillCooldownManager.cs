using System.Collections.Generic;
using Scripts.Instances.Actor;
using Scripts.Models;

namespace Scripts.Managers
{
    /// <summary>
    /// SKILLCOOLDOWNMANAGER - Per-hero cooldown tracking for Skill-kind <see cref="ManaAbility"/>s.
    ///
    /// <para>PURPOSE: A Skill is free but, once used, locks for <see cref="ManaAbility.CooldownTurns"/>
    /// turn-cycles. The remaining countdown lives HERE, keyed per hero, rather than on the ability —
    /// Skill ManaAbility instances (e.g. <c>ManaAbilities.Steal</c>) are shared statics referenced by
    /// several class loadouts, so a counter on the instance would bleed across every hero that owns
    /// the skill. Mirrors the static-dictionary-per-actor shape of <see cref="BuffSystem"/>.</para>
    ///
    /// <para>FLOW: <see cref="Begin"/> on use sets remaining = CooldownTurns. <see cref="TickAll"/> runs
    /// at the start of each hero window (<c>TurnManager.BeginHeroWindow</c>) and decrements every
    /// hero's cooldowns; an entry that reaches 0 is removed and the skill is usable again. The
    /// AbilityBar fades the slot and shows the remaining count while <see cref="IsOnCooldown"/>.</para>
    ///
    /// <para>RELATED FILES: ManaRecipe.cs (ManaAbility.CooldownTurns), AbilityBar.cs (gate + UI),
    /// TurnManager.cs (TickAll / Clear hooks), BuffSystem.cs (same pattern).</para>
    /// </summary>
    public static class SkillCooldownManager
    {
        // hero -> (skill ability -> turns remaining). Keyed by the shared ability INSTANCE; safe
        // because the hero key disambiguates two heroes that both own the same skill.
        private static readonly Dictionary<ActorInstance, Dictionary<ManaAbility, int>> remaining =
            new Dictionary<ActorInstance, Dictionary<ManaAbility, int>>();

        /// <summary>Put <paramref name="skill"/> on cooldown for its full CooldownTurns. No-op for a
        /// null hero/skill or a skill with no cooldown.</summary>
        public static void Begin(ActorInstance hero, ManaAbility skill)
        {
            if (hero == null || skill == null || skill.CooldownTurns <= 0) return;
            if (!remaining.TryGetValue(hero, out var map))
            {
                map = new Dictionary<ManaAbility, int>();
                remaining[hero] = map;
            }
            map[skill] = skill.CooldownTurns;
        }

        /// <summary>Turns left before <paramref name="skill"/> is usable again (0 = ready).</summary>
        public static int GetRemaining(ActorInstance hero, ManaAbility skill)
        {
            if (hero == null || skill == null) return 0;
            if (remaining.TryGetValue(hero, out var map) && map.TryGetValue(skill, out var turns))
                return turns > 0 ? turns : 0;
            return 0;
        }

        /// <summary>Whether <paramref name="skill"/> is currently locked for <paramref name="hero"/>.</summary>
        public static bool IsOnCooldown(ActorInstance hero, ManaAbility skill) => GetRemaining(hero, skill) > 0;

        /// <summary>Decrement every hero's cooldowns by one — call at the start of each hero window.
        /// Skills that reach 0, and entries for dead/destroyed heroes, are dropped.</summary>
        public static void TickAll()
        {
            if (remaining.Count == 0) return;

            var staleHeroes = new List<ActorInstance>();
            foreach (var kv in remaining)
            {
                var hero = kv.Key;
                if (hero == null || !hero.IsPlaying) { staleHeroes.Add(hero); continue; }

                var map = kv.Value;
                var cleared = new List<ManaAbility>();
                foreach (var skill in new List<ManaAbility>(map.Keys))
                {
                    int next = map[skill] - 1;
                    if (next <= 0) cleared.Add(skill);
                    else map[skill] = next;
                }
                foreach (var s in cleared) map.Remove(s);
                if (map.Count == 0) staleHeroes.Add(hero);
            }
            foreach (var h in staleHeroes) remaining.Remove(h);
        }

        /// <summary>Wipe all cooldowns (new battle).</summary>
        public static void Clear() => remaining.Clear();
    }
}
