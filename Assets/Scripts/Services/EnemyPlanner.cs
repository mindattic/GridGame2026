using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Instances.Actor;
using Scripts.Models;

namespace Scripts.Services
{
    /// <summary>
    /// ENEMYPLANNER - Pure positional AI for an enemy's move step (no Unity scene access, no g.).
    ///
    /// <para>PURPOSE: Replaces the old 50/20/15/10/5 weighted-random "strategy" with real tactics:
    /// pick the most attractive hero to pressure (near + wounded), then step one tile toward it —
    /// but never step into a tile where two heroes would immediately pincer this enemy. Greedy,
    /// deterministic, and reasoned the way every clone of this genre does enemy movement.</para>
    ///
    /// <para>It is handed the actor list and the TileMap (both plain data) so it can be reasoned
    /// about and tested without a live battle. The caller applies the returned location.</para>
    /// </summary>
    public static class EnemyPlanner
    {
        /// <summary>
        /// Returns the tile this enemy should occupy after its move (one cardinal step, or its
        /// current tile if standing pat is best). Never returns an off-board or occupied tile.
        /// </summary>
        public static Vector2Int PlanStep(ActorInstance enemy, IReadOnlyList<ActorInstance> actors, TileMap tileMap)
        {
            if (enemy == null || actors == null || tileMap == null)
                return enemy != null ? enemy.location : Vector2Int.zero;

            // Fix #10: Frozen / Sleep stick the enemy in place. Until this hook landed the buff
            // was cosmetic; now an immobilised enemy never advances.
            if (Scripts.Managers.BuffSystem.IsImmobile(enemy))
                return enemy.location;

            var heroes = actors.Where(a => a != null && a.IsPlaying && a.team == Team.Hero).ToList();
            if (heroes.Count == 0)
                return enemy.location;

            // Choose a target: prefer heroes that are both NEAR and WOUNDED (kill pressure).
            // Lower score wins (distance, plus a big bonus for low HP fraction).
            // US-080: smarter enemies hold a grudge — subtract an INT-scaled threat term so a
            // high-INT enemy gravitates to whoever has dealt it the most damage. A dumb (low-INT)
            // enemy barely weights threat and keeps chasing the nearest/most-wounded hero.
            float maxThreat = Scripts.Managers.ThreatTracker.MaxThreat(heroes);
            float intFactor = (enemy.Stats != null ? enemy.Stats.Intelligence : 0f) * ThreatIntScale;
            ActorInstance target = heroes
                .OrderBy(h => Manhattan(enemy.location, h.location) + HpFraction(h) * 8f - ThreatTerm(h, maxThreat, intFactor))
                .First();

            // Candidate steps: stay put + the four cardinal neighbors that are on-board and free.
            var candidates = new List<Vector2Int> { enemy.location };
            foreach (var dir in Cardinals)
            {
                var c = enemy.location + dir;
                if (tileMap.GetTile(c) == null) continue;            // off board
                if (IsOccupied(c, actors, enemy)) continue;          // blocked by another actor
                candidates.Add(c);
            }

            Vector2Int best = enemy.location;
            float bestScore = float.NegativeInfinity;

            // Humanoid enemies seek out a pincer attack before they seek out a regular attack.
            // For each candidate, simulate the move and check whether this enemy + any other
            // Humanoid enemy would form a valid pincer pair around heroes at that position.
            bool seeksPincer = Scripts.Managers.PincerAttackManager.IsHumanoid(enemy);

            // US-081: a badly wounded enemy RETREATS — it flees the target (maximizes distance)
            // instead of advancing, and drops its offensive biases (adjacency, pincer-seek).
            // Flank-avoidance still applies so it never backs into a pincer.
            bool wounded = HpFraction(enemy) < RetreatHpThreshold;

            foreach (var c in candidates)
            {
                float dist = Manhattan(c, target.location);
                float score = wounded ? dist : -dist;               // flee (maximize) vs advance (minimize)
                if (!wounded && IsCardinalAdjacent(c, target.location)) score += 2f; // in range to strike next
                if (WouldBeFlanked(c, heroes)) score -= 100f;        // do not walk into a pincer
                if (c == enemy.location) score -= 0.5f;              // mild bias to keep moving

                if (!wounded && seeksPincer && WouldFormPincer(enemy, c, actors))
                    score += 50f;                                    // pincer-seek beats positional ties

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best;
        }

        /// <summary>True if moving <paramref name="enemy"/> to <paramref name="candidate"/> would
        /// form at least one Humanoid-vs-hero pincer. Mutates the enemy's location briefly to
        /// reuse the standard PincerDetector; always restores.</summary>
        private static bool WouldFormPincer(ActorInstance enemy, Vector2Int candidate, IReadOnlyList<ActorInstance> actors)
        {
            var prev = enemy.location;
            enemy.location = candidate;
            try
            {
                var participants = PincerDetector.Detect(actors, Team.Enemy, enemy);
                if (participants.pair == null) return false;
                foreach (var p in participants.pair)
                {
                    if (p?.attacker1 != null && p.attacker2 != null
                        && Scripts.Managers.PincerAttackManager.IsHumanoid(p.attacker1)
                        && Scripts.Managers.PincerAttackManager.IsHumanoid(p.attacker2))
                        return true;
                }
                return false;
            }
            finally { enemy.location = prev; }
        }

        private static readonly Vector2Int[] Cardinals =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static bool IsCardinalAdjacent(Vector2Int a, Vector2Int b) =>
            Manhattan(a, b) == 1;

        private static float HpFraction(ActorInstance h) =>
            h.Stats != null && h.Stats.MaxHP > 0f ? h.Stats.HP / h.Stats.MaxHP : 1f;

        /// <summary>US-081: at/under this HP fraction an enemy switches from advancing to retreating.</summary>
        private const float RetreatHpThreshold = 0.30f;

        /// <summary>US-080: per-INT weight on threat. Tuned so the term is comparable to the HP bonus
        /// (×8) around INT 10, dominates distance at high INT, and is negligible at low INT.</summary>
        private const float ThreatIntScale = 0.8f;

        /// <summary>Threat contribution to the (lower-wins) target score: the hero's share of the
        /// highest threat (0..1) × the enemy's INT-scaled weight. 0 when no one has dealt damage.</summary>
        private static float ThreatTerm(ActorInstance h, float maxThreat, float intFactor)
        {
            if (maxThreat <= 0f || intFactor <= 0f) return 0f;
            float norm = Scripts.Managers.ThreatTracker.GetThreat(h) / maxThreat; // 0..1
            return norm * intFactor;
        }

        private static bool IsOccupied(Vector2Int loc, IReadOnlyList<ActorInstance> actors, ActorInstance self) =>
            actors.Any(a => a != null && a != self && a.IsPlaying && a.location == loc);

        /// <summary>
        /// True if standing at <paramref name="loc"/> puts the enemy directly between two heroes
        /// on the same row or column (the tightest immediate pincer) — a tile to avoid.
        /// </summary>
        private static bool WouldBeFlanked(Vector2Int loc, List<ActorInstance> heroes)
        {
            bool HeroAt(Vector2Int p) => heroes.Any(h => h.location == p);

            bool horizontal = HeroAt(loc + new Vector2Int(1, 0)) && HeroAt(loc + new Vector2Int(-1, 0));
            bool vertical = HeroAt(loc + new Vector2Int(0, 1)) && HeroAt(loc + new Vector2Int(0, -1));
            return horizontal || vertical;
        }
    }
}
