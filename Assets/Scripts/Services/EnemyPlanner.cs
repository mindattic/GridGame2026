using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Models;

namespace Scripts.Services
{
    /// <summary>US-026: a planned enemy charge-cast — which spell, aimed at which hero. Returned by
    /// <see cref="EnemyPlanner.PlanCast"/> (null = no charge this turn).</summary>
    public sealed class EnemyChargePlan
    {
        public ActorInstance Target;
        public Ability Ability;
    }

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
                if (enemy.IsMultiTile)
                {
                    // A multi-tile mover steps its whole footprint to anchor c. Legal if the whole
                    // rectangle is on-board and not blocked by ANOTHER ENEMY (heroes in the way are
                    // shovable — StepFootprint cascades them); off-board / enemy overlap is illegal.
                    if (!FootprintStepLegal(enemy, c, actors, tileMap)) continue;
                }
                else
                {
                    if (tileMap.GetTile(c) == null) continue;        // off board
                    if (IsOccupied(c, actors, enemy)) continue;      // blocked by another actor
                }
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

                // US-082: failing its own pincer, an enemy still values buffing an ALLY's pincer
                // by standing where it becomes a supporter (worth less than forming a pincer itself).
                if (!wounded && WouldSupportAllyPincer(enemy, c, actors))
                    score += 25f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best;
        }

        /// <summary>
        /// US-026: decide whether this enemy should TELEGRAPH a charge spell this turn instead of
        /// moving/meleeing. Pure + side-effect-free (the Legion-ratified Option A shape) — it does NOT
        /// alter <see cref="PlanStep"/> or the melee chain; the caller checks this first and, on a
        /// non-null result, queues an <c>EnemyChargeSequence</c> in place of the move/attack chain.
        ///
        /// <para>Rule: a caster (<see cref="ActorTag.Magic"/>) that is NOT cardinally adjacent to any
        /// hero charges its affinity spell at the nearest hero — a ranged attacker telegraphing from
        /// afar. If it can melee (adjacent), it falls through to the normal chain. Immobilised or
        /// targetless enemies never charge. Returns null = "no charge, run the normal turn".</para>
        /// </summary>
        public static EnemyChargePlan PlanCast(ActorInstance enemy, IReadOnlyList<ActorInstance> actors, bool ignoreMeleeRange = false)
        {
            if (enemy == null || actors == null) return null;
            if (Scripts.Managers.BuffSystem.IsImmobile(enemy)) return null;

            var ability = Scripts.Data.Actor.EnemyChargeCatalog.For(enemy);
            if (ability == null) return null; // not a caster

            var heroes = actors.Where(a => a != null && a.IsPlaying && a.team == Team.Hero).ToList();
            if (heroes.Count == 0) return null;

            // If it can melee, let it melee — only telegraph from range. US-083: a boss phase that
            // prefers charging (ignoreMeleeRange) telegraphs even point-blank.
            if (!ignoreMeleeRange && heroes.Any(h => IsCardinalAdjacent(enemy.location, h.location))) return null;

            var target = heroes.OrderBy(h => Manhattan(enemy.location, h.location)).First();
            return new EnemyChargePlan { Target = target, Ability = ability };
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

        /// <summary>US-082: true if moving <paramref name="enemy"/> to <paramref name="candidate"/>
        /// makes it a §1.2.3 SUPPORTER of some OTHER ally's Humanoid pincer (adjacent to an endpoint
        /// with line of sight), reusing <see cref="PincerDetector.FindSupporters"/>. Pincers where this
        /// enemy is itself an endpoint are excluded — those are <see cref="WouldFormPincer"/>'s job.
        /// Mutates the enemy's location briefly; always restores.</summary>
        private static bool WouldSupportAllyPincer(ActorInstance enemy, Vector2Int candidate, IReadOnlyList<ActorInstance> actors)
        {
            var prev = enemy.location;
            enemy.location = candidate;
            try
            {
                foreach (var ally in actors)
                {
                    if (ally == null || ally == enemy || !ally.IsPlaying || ally.team != Team.Enemy) continue;
                    if (!Scripts.Managers.PincerAttackManager.IsHumanoid(ally)) continue;

                    var participants = PincerDetector.Detect(actors, Team.Enemy, ally);
                    if (participants.pair == null) continue;

                    foreach (var p in participants.pair)
                    {
                        if (p?.attacker1 == null || p.attacker2 == null) continue;
                        if (p.attacker1 == enemy || p.attacker2 == enemy) continue; // own pincer → WouldFormPincer
                        if (!Scripts.Managers.PincerAttackManager.IsHumanoid(p.attacker1)
                            || !Scripts.Managers.PincerAttackManager.IsHumanoid(p.attacker2)) continue;

                        if (PincerDetector.FindSupporters(actors, p.attacker1).Contains(enemy)
                            || PincerDetector.FindSupporters(actors, p.attacker2).Contains(enemy))
                            return true;
                    }
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
            actors.Any(a => a != null && a != self && a.IsPlaying && a.Occupies(loc));

        /// <summary>Phase 4: legality of stepping a multi-tile <paramref name="enemy"/>'s footprint to
        /// anchor <paramref name="newAnchor"/>. Every destination tile must be on-board, and none may
        /// be covered by ANOTHER ENEMY (heroes are shovable, handled by StepFootprint's cascade). The
        /// enemy's own current tiles don't block (it's vacating them).</summary>
        private static bool FootprintStepLegal(ActorInstance enemy, Vector2Int newAnchor, IReadOnlyList<ActorInstance> actors, TileMap tileMap)
        {
            for (int dy = 0; dy < enemy.Footprint.y; dy++)
                for (int dx = 0; dx < enemy.Footprint.x; dx++)
                {
                    var t = new Vector2Int(newAnchor.x + dx, newAnchor.y + dy);
                    if (tileMap.GetTile(t) == null) return false; // off board
                    for (int i = 0; i < actors.Count; i++)
                    {
                        var a = actors[i];
                        if (a == null || a == enemy || !a.IsPlaying) continue;
                        if (a.team == Team.Enemy && a.Occupies(t)) return false; // can't shove another enemy
                    }
                }
            return true;
        }

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
