using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Instances.Actor;
using Scripts.Models;
using Scripts.Utilities;

namespace Scripts.Services
{
    /// <summary>
    /// PINCERDETECTOR - Pure pincer-detection rules (no Unity scene access, no g. switchboard).
    ///
    /// <para>PURPOSE: This is the BRAIN for pincer detection — the rules for "given a board of
    /// actors, which valid pincers exist and in what order do they resolve?" It takes the actor
    /// list as an argument instead of reaching through GameHelper, so the logic can be read
    /// top-to-bottom and reasoned about (and, once ActorInstance position/team are abstracted
    /// behind data, unit-tested) without spinning up a battle scene.</para>
    ///
    /// <para>WHAT MOVED HERE: the detection + chain-ordering + supporter logic formerly buried
    /// inside PincerAttackManager. The manager keeps only the BODY (VFX, sequences, animation
    /// orchestration) and calls Detect()/FindSupporters() for the rules.</para>
    ///
    /// <para>RULES: two same-team actors in the same row OR column, with a contiguous line of
    /// opposing actors between them (no gaps, no allies in the line, at least one opponent).
    /// Diagonals do not exist. All valid pincers present on the board are returned; chains
    /// (where one pair's second attacker is another pair's first attacker) are ordered to
    /// resolve consecutively, otherwise nearest-to-the-just-dropped-hero first.</para>
    ///
    /// <para>RELATED FILES: PincerAttackManager.cs (caller/body), PincerAttackPair.cs,
    /// PincerAttackParticipants.cs, Geometry.cs.</para>
    /// </summary>
    public static class PincerDetector
    {
        /// <summary>
        /// Scans <paramref name="actors"/> for every valid pincer for <paramref name="team"/>,
        /// ordered to begin from <paramref name="selectedHero"/> when provided.
        /// </summary>
        public static PincerAttackParticipants Detect(IReadOnlyList<ActorInstance> actors, Team team, ActorInstance selectedHero)
        {
            var participants = new PincerAttackParticipants();

            var teamActors = actors
                .Where(x => x.IsPlaying && x.team == team)
                .ToList();

            var indexed = teamActors.Select((actor, idx) => (actor, idx));

            foreach (var (actor1, i) in indexed)
            {
                foreach (var actor2 in teamActors.Skip(i + 1))
                {
                    if (!Geometry.IsSameRow(actor1.location, actor2.location) &&
                        !Geometry.IsSameColumn(actor1.location, actor2.location))
                        continue;

                    var betweenLocs = Geometry.GetLocationsBetween(actor1.location, actor2.location);
                    if (betweenLocs.Count == 0)
                        continue; // attackers adjacent — no enemy between them

                    // Footprint-aware pincer rule: EVERY tile strictly between the two attackers
                    // must be covered by some opposing actor, and none by an ally. A multi-tile
                    // enemy (2×2 boss) covering several line tiles counts as ONE distinct opponent
                    // (not a "gap"), so a boss flanked across its width is a valid single target.
                    var covering = new HashSet<ActorInstance>();
                    bool allCovered = true;
                    foreach (var loc in betweenLocs)
                    {
                        var occ = ActorCovering(actors, loc);
                        if (occ == null) { allCovered = false; break; } // a real empty gap
                        covering.Add(occ);
                    }
                    if (!allCovered) continue;

                    bool allOpponents = covering.All(x => x.team != team);
                    if (covering.Count > 0 && allOpponents)
                    {
                        var opponents = covering.ToList();

                        participants.pair.Add(new PincerAttackPair
                        {
                            attacker1 = actor1,
                            attacker2 = actor2,
                            opponents = opponents,
                            supporters1 = FindSupporters(actors, actor1),
                            supporters2 = FindSupporters(actors, actor2)
                        });
                    }
                }
            }

            participants.pair = OrderPairsByChainsThenNearest(participants.pair, selectedHero);
            return participants;
        }

        /// <summary>
        /// Orders pincer pairs to maximize chain attacks, starting from preferredStartHero.
        /// </summary>
        private static List<PincerAttackPair> OrderPairsByChainsThenNearest(List<PincerAttackPair> pairs, ActorInstance preferredStartHero)
        {
            var ordered = new List<PincerAttackPair>();
            var remaining = new HashSet<PincerAttackPair>(pairs);

            System.Func<PincerAttackPair, (int y, int x)> posKey = p => (p.attacker1.location.y, p.attacker1.location.x);

            var byAttacker1 = pairs
                .GroupBy(p => p.attacker1)
                .ToDictionary(gp => gp.Key, gp => SortPairsForAttacker1(gp.Key, gp.ToList()));

            int Dist(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

            PincerAttackPair PickInitialStart()
            {
                if (preferredStartHero != null)
                {
                    var prefer = remaining.FirstOrDefault(p => p.attacker1 == preferredStartHero);
                    if (prefer != null) return prefer;
                }

                return remaining.OrderBy(posKey).First();
            }

            PincerAttackPair PickNearestStartTo(Vector2Int from)
            {
                return remaining
                    .OrderBy(p => Dist(p.attacker1.location, from))
                    .ThenBy(posKey)
                    .First();
            }

            while (remaining.Any())
            {
                var start = ordered.Any()
                    ? PickNearestStartTo(ordered.Last().attacker2.location)
                    : PickInitialStart();

                var current = start;

                while (current != null)
                {
                    ordered.Add(current);
                    remaining.Remove(current);

                    if (byAttacker1.TryGetValue(current.attacker1, out var consumedList))
                        consumedList.Remove(current);

                    PincerAttackPair next = null;
                    if (byAttacker1.TryGetValue(current.attacker2, out var nextList))
                        next = nextList.FirstOrDefault(remaining.Contains);

                    current = next;
                }
            }

            return ordered;
        }

        /// <summary>Sort pairs for attacker1.</summary>
        private static List<PincerAttackPair> SortPairsForAttacker1(ActorInstance attacker, List<PincerAttackPair> list)
        {
            IEnumerable<(PincerAttackPair pair, int orientPri, int primaryDist, int tieX, int tieY)> keyed =
                list.Select(p =>
                {
                    var a = attacker.location;
                    var b = (p.attacker1 == attacker ? p.attacker2.location : p.attacker1.location);

                    bool vertical = a.x == b.x;
                    bool horizontal = a.y == b.y;

                    int dy = Mathf.Abs(a.y - b.y);
                    int dx = Mathf.Abs(a.x - b.x);

                    int orientPri = dy == dx ? 0 : (dy > dx ? -1 : 1);

                    int primaryDist;
                    if (vertical)
                    {
                        bool attackerAbove = a.y < b.y;
                        primaryDist = attackerAbove ? b.y : -b.y;
                    }
                    else
                    {
                        bool attackerLeft = a.x < b.x;
                        primaryDist = attackerLeft ? -b.x : b.x;
                    }

                    return (p, orientPri, primaryDist, b.x, b.y);
                });

            return keyed
                .OrderBy(k => k.orientPri)
                .ThenBy(k => k.primaryDist)
                .ThenBy(k => k.tieY)
                .ThenBy(k => k.tieX)
                .Select(k => k.pair)
                .ToList();
        }

        /// <summary>
        /// Allies <b>cardinally adjacent</b> to <paramref name="attacker"/> (a pincer endpoint),
        /// not blocked by an intervening actor — each adds bonus pincer damage. Adjacency is
        /// required per game_bible.md §1: a supporter sits directly next to the endpoint, not
        /// merely somewhere along the same row/column.
        /// </summary>
        public static List<ActorInstance> FindSupporters(IReadOnlyList<ActorInstance> actors, ActorInstance attacker)
        {
            var candidates = actors
                .Where(x => x.IsPlaying && x.team == attacker.team && x != attacker)
                .Where(x => Geometry.AreAdjacent(x, attacker)) // footprint-aware (1×1 fast path unchanged)
                .ToList();

            var result = new List<ActorInstance>();
            foreach (var c in candidates)
                if (!IsActorBlocked(actors, attacker, c))
                    result.Add(c);

            return result;
        }

        /// <summary>Returns whether an intervening actor blocks the line between a and b.</summary>
        private static bool IsActorBlocked(IReadOnlyList<ActorInstance> actors, ActorInstance a, ActorInstance b)
        {
            if (!Geometry.IsSameRow(a.location, b.location) && !Geometry.IsSameColumn(a.location, b.location))
                return true;

            var between = Geometry
                .GetLocationsBetween(a.location, b.location)
                .Where(loc => !loc.Equals(a.location) && !loc.Equals(b.location))
                .ToList();

            // Footprint-aware: a multi-tile enemy blocks the supporter line if ANY of its tiles
            // sits between a and b (not just its anchor).
            return actors.Any(x => x.IsPlaying && x != a && x != b && between.Any(loc => x.Occupies(loc)));
        }

        /// <summary>Pure footprint-aware "actor covering tile" — the playing actor whose footprint
        /// includes <paramref name="loc"/>, or null. Kept local (takes the actor list) so this
        /// service stays free of the g. switchboard and remains testable.</summary>
        private static ActorInstance ActorCovering(IReadOnlyList<ActorInstance> actors, Vector2Int loc)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i];
                if (a != null && a.IsPlaying && a.Occupies(loc)) return a;
            }
            return null;
        }
    }
}
