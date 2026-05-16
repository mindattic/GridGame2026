using Scripts.Models;
using System.Collections;
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
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
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

        /// <summary>Coroutine that executes the process sequence.</summary>
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

                // FE-style weapon attrition: each pincer counts as one swing per attacker. The
                // helper writes the new durability straight to the save data so a broken weapon
                // is reflected when the player gets back to a vendor scene.
                var shatter1 = WeaponDurabilityHelper.OnHeroAttacked(p.attacker1);
                var shatter2 = WeaponDurabilityHelper.OnHeroAttacked(p.attacker2);

                // Shatter: the swing that drops durability to 0 hits harder AND damages the
                // wielder. Boost the first target's damage by the shatter bonus and queue
                // self-damage to the wielder.
                ApplyShatterEffects(p.attacker1, p.attackResults1, shatter1);
                ApplyShatterEffects(p.attacker2, p.attackResults2, shatter2);

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

        /// <summary>If the swing shattered the wielder's weapon, boost the first target's damage
        /// by the shatter multiplier and deal self-damage to the wielder. Announces the shatter
        /// on the ActionTitle banner so the player understands the dramatic moment.</summary>
        private void ApplyShatterEffects(Scripts.Instances.Actor.ActorInstance wielder,
                                         System.Collections.Generic.List<AttackResult> results,
                                         Scripts.Helpers.ShatterResult shatter)
        {
            if (!shatter.Shattered || wielder == null) return;

            // Boost the first target's damage by the shatter bonus (acts as the "final swing").
            if (results != null && results.Count > 0 && shatter.TargetBonusMultiplier > 1f)
            {
                int boosted = Mathf.RoundToInt(results[0].Damage * shatter.TargetBonusMultiplier);
                results[0].Damage = boosted;
            }

            // Wielder takes self-damage from the shatter — bypasses defense (it's not an attack).
            wielder.Stats.HP = Mathf.Max(0f, wielder.Stats.HP - shatter.WielderSelfDamage);

            // Surface the moment on the top-center banner.
            if (shatter.ShatteredWeapon != null)
                g.ActionTitle?.Show($"{wielder.characterClass}'s {shatter.ShatteredWeapon.DisplayName} shattered!");
        }
    }
}
