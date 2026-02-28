using Assets.Scripts.Models;
using System.Collections;
using System.Linq;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// HEROPINCERSEQUENCE - Resolves hero pincer attacks.
    /// 
    /// PURPOSE:
    /// Wraps the complete pincer attack flow including support
    /// sequences, attack sequences, and visual effects.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Notify sorting for pincer visuals
    /// 2. Fade in board overlay
    /// 3. Spawn synergy lines for supporters
    /// 4. Queue support sequences for each supporter
    /// 5. Build attack results for each attacker pair
    /// 6. Queue attack sequences
    /// 7. Process death sequences
    /// 8. Clean up visuals
    /// 
    /// RELATED FILES:
    /// - PincerAttackManager.cs: Detects pincer setups
    /// - PincerAttackSequence.cs: Individual attack execution
    /// - PincerAttackSupportSequence.cs: Support animations
    /// </summary>
    public sealed class HeroPincerSequence : SequenceEvent
    {
        private readonly PincerAttackParticipants participants;
        private readonly ActorInstance droppedHero;

        public HeroPincerSequence(PincerAttackParticipants participants, ActorInstance droppedHero)
        {
            this.participants = participants;
            this.droppedHero = droppedHero;
        }

        public override IEnumerator ProcessRoutine()
        {
            if (participants == null || !participants.pair.Any())
                yield break;

            g.SortingManager?.OnPincerAttack(participants);

            yield return g.BoardOverlay?.FadeInRoutine();

            foreach (var p in participants.pair)
            {
                foreach (var supporter in p.supporters1)
                {
                    g.SynergyLineManager?.Spawn(supporter, p.attacker1);
                    g.SequenceManager.Add(new PincerAttackSupportSequence(p.attacker1, supporter));
                }

                foreach (var supporter in p.supporters2)
                {
                    g.SynergyLineManager?.Spawn(supporter, p.attacker2);
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

                    p.attackResults1.AddRange(attacker1Order.Select(opp => Formulas.CalculateAttackResult(p.attacker1, opp)));
                    p.attackResults2.AddRange(attacker2Order.Select(opp => Formulas.CalculateAttackResult(p.attacker2, opp)));
                }
                else if (horizontal)
                {
                    bool attacker1Left = p.attacker1.location.x < p.attacker2.location.x;

                    var asc = p.opponents.OrderBy(o => o.location.x).ToList();
                    var desc = asc.AsEnumerable().Reverse().ToList();

                    var attacker1Order = attacker1Left ? asc : desc;
                    var attacker2Order = attacker1Left ? desc : asc;

                    p.attackResults1.AddRange(attacker1Order.Select(opp => Formulas.CalculateAttackResult(p.attacker1, opp)));
                    p.attackResults2.AddRange(attacker2Order.Select(opp => Formulas.CalculateAttackResult(p.attacker2, opp)));
                }

                g.SequenceManager.Add(new PincerAttackSequence(p));
            }

            // Resolve deaths from pincer attacks
            g.SequenceManager.Add(new DeathSequence());

            // Execute all pincer sequences
            yield return g.SequenceManager.ExecuteRoutine();

            // Fade out and cleanup
            yield return g.BoardOverlay?.FadeOutRoutine();
            g.SynergyLineManager?.Clear();
            participants.Clear();
        }
    }
}
