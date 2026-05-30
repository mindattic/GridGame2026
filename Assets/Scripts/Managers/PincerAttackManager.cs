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
        FilterToHumanoidAttackers(participants);
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
        // Per design: only Humanoid-tagged actors can SCAN for a pincer. A beast or mechanical
        // selectedHero just won't be considered a valid attacker even if the geometry lines up.
        if (selectedHero != null && !IsHumanoid(selectedHero)) return false;

        var participants = GetParticipants(team, selectedHero);
        FilterToHumanoidAttackers(participants);
        if (!participants.pair.Any())
            return false;

        StartCoroutine(EnqueueRoutine(participants));
        return true;
    }

    /// <summary>
    /// Scans the board for all valid pincer pairs for the given team.
    /// Delegates the detection RULES to the pure <see cref="Scripts.Services.PincerDetector"/>;
    /// this manager only supplies the live actor list and owns the animation/sequence BODY.
    /// </summary>
    /// <param name="team">Team to check for (Team.Hero or Team.Enemy)</param>
    /// <param name="selectedHero">Optional: Hero that was just dropped (for chain ordering)</param>
    /// <returns>PincerAttackParticipants containing all valid pincer pairs</returns>
    public PincerAttackParticipants GetParticipants(Team team, ActorInstance selectedHero)
    {
        return Scripts.Services.PincerDetector.Detect(g.Actors.All, team, selectedHero);
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

        // PHASE B: hero-side pincer completion DROPS Blue mana orbs (bouncing pickups) toward the
        // orb line — heroes are the primary mana source. V1 is always Blue; per-hero color
        // affinity arrives with WUBRG. Each orb commits on landing (ManaOrbInstance.Update).
        foreach (var p in participants.pair)
        {
            if (p == null || p.attacker1 == null || p.attacker1.team != Team.Hero) continue;
            DropOrbAt(p.attacker1, ManaType.Blue);
            if (p.attacker2 != null) DropOrbAt(p.attacker2, ManaType.Blue);
            if (p.supporters1 != null) foreach (var s in p.supporters1) DropOrbAt(s, ManaType.Blue);
            if (p.supporters2 != null) foreach (var s in p.supporters2) DropOrbAt(s, ManaType.Blue);
        }

        yield return g.BoardOverlay.FadeOutRoutine();
        g.SynergyLineManager.Clear();
        participants.Clear();

        // Signal completion to listeners (e.g., SelectionManager) so they can decide next step.
        OnResolved?.Invoke();
    }

    /// <summary>Creates the attack result.</summary>
    private AttackResult CreateAttackResult(ActorInstance attacker, ActorInstance opponent)
    {
        return Formulas.CalculateAttackResult(attacker, opponent);
    }

    /// <summary>True if the actor's ActorData carries the <see cref="ActorTag.Humanoid"/> flag.
    /// Heroes fall back to true even if the data file forgot the tag (sensible default — most
    /// heroes are humanoid). Enemies must be explicitly tagged.</summary>
    public static bool IsHumanoid(ActorInstance actor)
    {
        if (actor == null) return false;
        var data = Scripts.Libraries.ActorLibrary.Get(actor.characterClass);
        if (data != null && (data.Tags & ActorTag.Humanoid) == ActorTag.Humanoid) return true;
        return actor.team == Team.Hero; // hero fallback so existing data without the tag still pincers
    }

    /// <summary>Drop pincer pairs whose attackers are not Humanoid — per design only Humanoid
    /// actors can perform a pincer attack regardless of which team is scanning.</summary>
    private static void FilterToHumanoidAttackers(PincerAttackParticipants participants)
    {
        if (participants == null || participants.pair == null) return;
        participants.pair.RemoveAll(p =>
            p == null ||
            (p.attacker1 != null && !IsHumanoid(p.attacker1)) ||
            (p.attacker2 != null && !IsHumanoid(p.attacker2)));
    }

    /// <summary>Drops a bouncing mana orb from <paramref name="source"/>'s world position toward the orb line.</summary>
    private void DropOrbAt(ActorInstance source, ManaType color)
    {
        if (source == null || source.transform == null) return;
        Scripts.Factories.ManaOrbFactory.Drop(source.transform.position, color);
    }

    /// <summary>Finds the supporters for an attacker. Delegates to the pure detector service.</summary>
    public List<ActorInstance> FindSupporters(ActorInstance attacker)
    {
        return Scripts.Services.PincerDetector.FindSupporters(g.Actors.All, attacker);
    }
}

}
