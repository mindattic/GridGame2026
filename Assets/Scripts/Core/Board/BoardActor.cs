using UnityEngine;
using Scripts.Models;

namespace Scripts.Core.Board
{
    /// <summary>
    /// BOARDACTOR - Immutable snapshot of one actor's board-relevant facts.
    /// <para>PURPOSE: A pure, MonoBehaviour-free value type that captures only what
    /// board algorithms (pincer detection, supporter lookup, line-of-sight) need:
    /// a stable id, the actor's team, and its grid location. By depending on this
    /// instead of <c>ActorInstance</c>, the algorithms in this folder are pure
    /// functions that can be reasoned about and unit-tested without a live scene.</para>
    /// <para>The <see cref="Id"/> is assigned by the caller that builds the snapshot
    /// (typically the index into its source list) and is the token used to map a
    /// detection result back to the live <c>ActorInstance</c>.</para>
    /// <para>RELATED FILES: PincerDetector.cs, PincerCandidate.cs, PincerAttackManager.cs</para>
    /// </summary>
    public readonly struct BoardActor
    {
        public readonly int Id;
        public readonly Team Team;
        public readonly Vector2Int Location;

        public BoardActor(int id, Team team, Vector2Int location)
        {
            Id = id;
            Team = team;
            Location = location;
        }
    }
}
