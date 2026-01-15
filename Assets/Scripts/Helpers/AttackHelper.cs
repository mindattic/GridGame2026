using Assets.Helper;
using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using g = Assets.Helpers.GameHelper;

namespace Assets.Helpers
{
    public static class AttackHelper
    {
        /// <summary>
        /// Applies damage for a single attack result, then yields once.
        /// If the opponent is an enemy, plays BlueSlash1 and triggers damage at the slash apex.
        /// </summary>
        public static IEnumerator SingleAttackRoutine(AttackResult attackResult)
        {
            var opp = attackResult?.Opponent;
            if (opp == null || !opp.IsPlaying)           // guard: opponent might have died/deactivated earlier this frame
                yield break;

            // New: handle clean Miss with dodge feedback
            if (attackResult.HitType == HitOutcome.Miss)
            {
                yield return opp.AttackMissRoutine();
                // Preserve original yield
                yield return Wait.None();
                yield break;
            }

            // For attacks against enemies, play and apply damage at its apex
            if (opp.IsEnemy)
            {
                var vfx = VisualEffectLibrary.Get("BlueSlash4");
                if (vfx != null)
                {
                    var inst = g.VisualEffectManager.SpawnInstance(vfx, opp.Position, null);
                    if (inst != null)
                    {
                        // Wait until the slash reaches apex, then apply damage
                        yield return inst.WaitUntilTrigger(vfx);
                        opp.Damage(attackResult);
                        
                        // Only push the enemy's timeline tag back if:
                        // 1. The attacker is a Hero
                        // 2. It's currently the hero's turn (not during enemy turn/counter-attacks)
                        bool isHeroTurn = g.TurnManager == null || g.TurnManager.IsHeroTurn;
                        if (isHeroTurn && attackResult.Attacker != null && attackResult.Attacker.IsHero)
                        {
                            int attackerStrength = attackResult.Attacker.Stats?.Strength.ToInt() ?? 10;
                            g.TimelineBar?.PushbackOnAttack(opp, attackerStrength);
                        }
                        
                        // Optional: let VFX continue; do not block on full duration
                        yield return Wait.None();
                        yield break;
                    }
                }
            }

            // Fallback: apply damage immediately
            opp.Damage(attackResult);
            
            // Only push the enemy's timeline tag back if attacker is a Hero AND it's hero turn
            bool isHeroTurnFallback = g.TurnManager == null || g.TurnManager.IsHeroTurn;
            if (opp.IsEnemy && isHeroTurnFallback && attackResult.Attacker != null && attackResult.Attacker.IsHero)
            {
                int attackerStrength = attackResult.Attacker.Stats?.Strength.ToInt() ?? 10;
                g.TimelineBar?.PushbackOnAttack(opp, attackerStrength);
            }

            // Preserve original yield
            yield return Wait.None();
        }

        /// <summary>
        /// Runs multiple attacks in sequence. Each attack finishes before the next starts,
        /// with a brief delay in between.
        /// </summary>
        public static IEnumerator MultiAttackRoutine(List<AttackResult> attackResults)
        {
            if (attackResults == null || attackResults.Count == 0)
                yield break;

            foreach (var attackResult in attackResults)
            {
                yield return SingleAttackRoutine(attackResult);
                yield return Wait.For(Interval.TenthSecond); // Short delay to produce domino effect
            }
        }
    }
}
