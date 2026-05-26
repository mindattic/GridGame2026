using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Models;
using Scripts.Utilities;

namespace Scripts.Core.Board
{
    /// <summary>
    /// PINCERDETECTOR - Pure detection of pincer formations on a board snapshot.
    /// <para>PURPOSE: This is the single source of truth for THE core combat rule -
    /// two allies in the same row/column with a contiguous, gap-free line of opponents
    /// between them. It operates only on <see cref="BoardActor"/> value types and the
    /// pure spatial helpers in <see cref="Geometry"/>; it touches no MonoBehaviour, no
    /// GameHelper, and no scene state. That purity is the point: the same function serves
    /// the player's voluntary drop and the timer's forced drop identically, and it can be
    /// exercised in isolation without a live game.</para>
    /// <para>The snapshot passed to <see cref="Find"/> must contain exactly the actors that
    /// are in play (the caller applies the "is playing" filter when building it).</para>
    /// <para>RELATED FILES: BoardActor.cs, PincerCandidate.cs, PincerAttackManager.cs, Geometry.cs</para>
    /// </summary>
    public static class PincerDetector
    {
        /// <summary>
        /// Scans the snapshot for every valid pincer formed by a pair of <paramref name="team"/> actors.
        /// Returned in board-scan order; chain/turn ordering is the caller's concern.
        /// </summary>
        public static List<PincerCandidate> Find(IReadOnlyList<BoardActor> actors, Team team)
        {
            var result = new List<PincerCandidate>();

            var teamActors = actors.Where(a => a.Team == team).ToList();

            for (int i = 0; i < teamActors.Count; i++)
            {
                var attacker1 = teamActors[i];

                for (int j = i + 1; j < teamActors.Count; j++)
                {
                    var attacker2 = teamActors[j];

                    if (!Geometry.IsSameRow(attacker1.Location, attacker2.Location) &&
                        !Geometry.IsSameColumn(attacker1.Location, attacker2.Location))
                        continue;

                    var betweenLocs = Geometry.GetLocationsBetween(attacker1.Location, attacker2.Location);

                    var betweenActors = actors
                        .Where(a => betweenLocs.Contains(a.Location))
                        .ToList();

                    bool hasOpponent = betweenActors.Any(a => a.Team != team);
                    bool allOpponents = betweenActors.All(a => a.Team != team);
                    bool noGap = betweenLocs.Count == betweenActors.Count;

                    if (hasOpponent && allOpponents && noGap)
                    {
                        result.Add(new PincerCandidate
                        {
                            Attacker1Id = attacker1.Id,
                            Attacker2Id = attacker2.Id,
                            OpponentIds = betweenActors.Where(a => a.Team != team).Select(a => a.Id).ToList(),
                            Supporter1Ids = FindSupporters(actors, attacker1),
                            Supporter2Ids = FindSupporters(actors, attacker2),
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the ids of allies that support <paramref name="attacker"/>: same team, on the
        /// same row or column, with an unbroken line of sight (no actor of any team between them).
        /// </summary>
        public static List<int> FindSupporters(IReadOnlyList<BoardActor> actors, BoardActor attacker)
        {
            var result = new List<int>();

            foreach (var candidate in actors)
            {
                if (candidate.Id == attacker.Id) continue;
                if (candidate.Team != attacker.Team) continue;

                if (!Geometry.IsSameRow(candidate.Location, attacker.Location) &&
                    !Geometry.IsSameColumn(candidate.Location, attacker.Location))
                    continue;

                if (!IsLineBlocked(actors, attacker.Location, candidate.Location))
                    result.Add(candidate.Id);
            }

            return result;
        }

        /// <summary>
        /// True if the straight line strictly between <paramref name="a"/> and <paramref name="b"/>
        /// is broken - either the two points are not co-linear, or some actor occupies a tile between them.
        /// </summary>
        private static bool IsLineBlocked(IReadOnlyList<BoardActor> actors, Vector2Int a, Vector2Int b)
        {
            if (!Geometry.IsSameRow(a, b) && !Geometry.IsSameColumn(a, b))
                return true;

            // GetLocationsBetween already excludes the two endpoints.
            var between = Geometry.GetLocationsBetween(a, b);
            return actors.Any(x => between.Contains(x.Location));
        }
    }
}
