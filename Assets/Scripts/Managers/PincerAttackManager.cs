// --- File: Assets/Scripts/Managers/PincerAttackManager.cs ---
using Scripts.Models;
using Scripts.Sequences;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// PINCERATTACKMANAGER - Core combat mechanic handler.
/// 
/// PURPOSE:
/// Detects and resolves Pincer Attacks - the primary combat mechanic where
/// two allied actors trap enemies between them in a straight line.
/// 
/// PINCER ATTACK RULES:
/// 1. Two heroes must be in the SAME ROW or SAME COLUMN
/// 2. One or more enemies must be BETWEEN them
/// 3. All tiles between the two heroes must be occupied by enemies (no gaps)
/// 4. Triggers automatically when a hero is dropped into valid position
/// 
/// VISUAL EXAMPLE:
/// ```
/// Horizontal Pincer:
///   [Hero A] - [Enemy] - [Enemy] - [Hero B]
///        ↑                             ↑
///   attacker1      opponents     attacker2
/// 
/// Vertical Pincer:
///   [Hero A]
///      │
///   [Enemy]  ← opponent
///      │
///   [Hero B]
/// ```
/// 
/// SUPPORTERS:
/// Adjacent allies to pincer attackers add bonus damage.
/// FindSupporters(attacker) returns cardinally adjacent teammates.
/// 
/// FLOW:
/// 1. Check(team) or Check(team, selectedHero) called after hero drop
/// 2. GetParticipants() scans board for valid pincer configurations
/// 3. If found, EnqueueRoutine() creates PincerAttackSequence
/// 4. Sequence executes combat with VFX and damage
/// 5. OnResolved fires when complete
/// 
/// CHAIN ATTACKS:
/// Multiple pincers can chain when one attack creates new valid formations.
/// OrderPairsByChainsThenNearest() orders pincers to maximize chaining.
/// 
/// LLM CONTEXT:
/// This is THE core combat mechanic. Call g.PincerAttackManager.Check()
/// after any hero position change. Returns true if pincers were found.
/// PincerAttackPair contains attacker1, attacker2, opponents, supporters1/2.
/// </summary>
public class PincerAttackManager : MonoBehaviour
{
    /// <summary>Fired when pincer attack resolution completes (after all sequences).</summary>
    public event System.Action OnResolved;

    /// <summary>
    /// Entry point for resolving pincer attacks for a team.
    /// Returns true if any pincer work was enqueued, false if none found.
    /// Does not advance the turn - caller decides what to do when false.
    /// </summary>
    public bool Check(Team team)
    {
        var participants = GetParticipants(team, null);
        if (!participants.pair.Any())
            return false;

        StartCoroutine(EnqueueRoutine(participants));
        return true;
    }

    /// <summary>
    /// Preferred entry point when a hero was just dropped.
    /// Orders pincer chains to start from selectedHero if possible.
    /// Returns true if any pincer work was enqueued, false if none found.
    /// </summary>
    public bool Check(Team team, ActorInstance selectedHero)
    {
        var participants = GetParticipants(team, selectedHero);
        if (!participants.pair.Any())
            return false;

        StartCoroutine(EnqueueRoutine(participants));
        return true;
    }

    /// <summary>
    /// Scans the board for all valid pincer pairs for the given team.
    /// Aligns chains to begin from selectedHero when provided.
    /// </summary>
    /// <param name="team">Team to check for (Team.Hero or Team.Enemy)</param>
    /// <param name="selectedHero">Optional: Hero that was just dropped (for chain ordering)</param>
    /// <returns>PincerAttackParticipants containing all valid pincer pairs</returns>
    public PincerAttackParticipants GetParticipants(Team team, ActorInstance selectedHero)
    {
        var participants = new PincerAttackParticipants();

        var teamActors = g.Actors.All
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

                var betweenActors = g.Actors.All
                    .Where(x => x.IsPlaying && betweenLocs.Contains(x.location))
                    .ToList();

                bool hasEnemy = betweenActors.Any(x => x.team != team);
                bool allOpponents = betweenActors.All(x => x.IsPlaying && x.team != team);
                bool noGap = betweenLocs.Count == betweenActors.Count;

                if (hasEnemy && allOpponents && noGap)
                {
                    var opponents = betweenActors.Where(x => x.team != team).ToList();

                    participants.pair.Add(new PincerAttackPair
                    {
                        attacker1 = actor1,
                        attacker2 = actor2,
                        opponents = opponents,
                        supporters1 = FindSupporters(actor1),
                        supporters2 = FindSupporters(actor2)
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
    private List<PincerAttackPair> OrderPairsByChainsThenNearest(List<PincerAttackPair> pairs, ActorInstance preferredStartHero)
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

    private List<PincerAttackPair> SortPairsForAttacker1(ActorInstance attacker, List<PincerAttackPair> list)
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
    /// Main enqueue routine. Spawns visuals, builds sequences, resolves deaths once,
    /// leaves turn advancement decision to the caller (SelectionManager) based on timeline.
    /// </summary>
    private IEnumerator EnqueueRoutine(PincerAttackParticipants participants)
    {
        g.SortingManager.OnPincerAttack(participants);

        yield return g.BoardOverlay.FadeInRoutine();

        foreach (var p in participants.pair)
        {
            foreach (var supporter in p.supporters1)
            {
                g.SynergyLineManager.Spawn(supporter, p.attacker1);
                g.SequenceManager.Add(new PincerAttackSupportSequence(p.attacker1, supporter));
            }

            foreach (var supporter in p.supporters2)
            {
                g.SynergyLineManager.Spawn(supporter, p.attacker2);
                g.SequenceManager.Add(new PincerAttackSupportSequence(p.attacker2, supporter));
            }
        }

        foreach (var p in participants.pair)
        {
            p.attackResults1.Clear();
            p.attackResults2.Clear();

            bool vertical = p.attacker1.location.x == p.attacker2.location.x;
            bool horizontal = p.attacker1.location.y == p.attacker2.location.y;

            if (vertical)
            {
                bool attacker1Above = p.attacker1.location.y < p.attacker2.location.y;

                var asc = p.opponents.OrderBy(o => o.location.y).ToList();
                var desc = asc.AsEnumerable().Reverse().ToList();

                var attacker1Order = attacker1Above ? asc : desc;
                var attacker2Order = attacker1Above ? desc : asc;

                p.attackResults1.AddRange(attacker1Order.Select(opp => CreateAttackResult(p.attacker1, opp)));
                p.attackResults2.AddRange(attacker2Order.Select(opp => CreateAttackResult(p.attacker2, opp)));
            }
            else if (horizontal)
            {
                bool attacker1Left = p.attacker1.location.x < p.attacker2.location.x;

                var asc = p.opponents.OrderBy(o => o.location.x).ToList();
                var desc = asc.AsEnumerable().Reverse().ToList();

                // Fix: order from closest to furthest relative to each attacker
                var attacker1Order = attacker1Left ? asc : desc;
                var attacker2Order = attacker1Left ? desc : asc;

                p.attackResults1.AddRange(attacker1Order.Select(opp => CreateAttackResult(p.attacker1, opp)));
                p.attackResults2.AddRange(attacker2Order.Select(opp => CreateAttackResult(p.attacker2, opp)));
            }

            g.SequenceManager.Add(new PincerAttackSequence(p));
        }

        g.SequenceManager.Add(new DeathSequence());

        yield return g.SequenceManager.ExecuteRoutine();

        yield return g.BoardOverlay.FadeOutRoutine();
        g.SynergyLineManager.Clear();
        participants.Clear();

        // Signal completion to listeners (e.g., SelectionManager) so they can decide next step.
        OnResolved?.Invoke();
    }

    private AttackResult CreateAttackResult(ActorInstance attacker, ActorInstance opponent)
    {
        return Formulas.CalculateAttackResult(attacker, opponent);
    }

    public List<ActorInstance> FindSupporters(ActorInstance attacker)
    {
        var candidates = g.Actors.All
            .Where(x => x.IsPlaying && x.team == attacker.team && x != attacker)
            .Where(x => Geometry.IsSameRow(x.location, attacker.location) || Geometry.IsSameColumn(x.location, attacker.location))
            .ToList();

        var result = new List<ActorInstance>();
        foreach (var c in candidates)
            if (!IsActorBlocked(attacker, c))
                result.Add(c);

        return result;
    }

    private bool IsActorBlocked(ActorInstance a, ActorInstance b)
    {
        if (!Geometry.IsSameRow(a.location, b.location) && !Geometry.IsSameColumn(a.location, b.location))
            return true;

        var between = Geometry
            .GetLocationsBetween(a.location, b.location)
            .Where(loc => !loc.Equals(a.location) && !loc.Equals(b.location));

        return g.Actors.All.Any(x => x.IsPlaying && between.Contains(x.location));
    }
}

}
