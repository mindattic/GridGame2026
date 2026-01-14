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
                        
                        // Push the enemy's timeline tag back (stronger effect when closer to trigger and based on attacker's Strength)
                        int attackerStrength = attackResult.Attacker?.Stats != null 
                            ? attackResult.Attacker.Stats.Strength.ToInt() 
                            : 10;
                        g.TimelineBar?.PushbackOnAttack(opp, attackerStrength);
                        
                        // Optional: let VFX continue; do not block on full duration
                        yield return Wait.None();
                        yield break;
                    }
                }
            }

            // Fallback: apply damage immediately
            opp.Damage(attackResult);
            
            // Push the enemy's timeline tag back if it's an enemy
            if (opp.IsEnemy)
            {
                int attackerStrength = attackResult.Attacker?.Stats != null 
                    ? attackResult.Attacker.Stats.Strength.ToInt() 
                    : 10;
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
