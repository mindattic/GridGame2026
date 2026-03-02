using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// ATTACKHELPER - Static utilities for combat damage and attack VFX.
    /// 
    /// PURPOSE:
    /// Provides reusable attack routines for applying damage, playing VFX,
    /// and handling hit outcomes (damage, miss, critical).
    /// 
    /// KEY METHODS:
    /// - SingleAttackRoutine(): Process one AttackResult with VFX
    /// - MultiAttackRoutine(): Process multiple attacks in sequence
    /// - ApplyDamage(): Apply damage to target with all side effects
    /// 
    /// ATTACK FLOW:
    /// 1. Check for miss → Play dodge animation, exit
    /// 2. Play VFX (e.g., BlueSlash4) at target position
    /// 3. Wait until VFX apex (trigger point)
    /// 4. Apply damage via opponent.Damage(result)
    /// 5. Push enemy timeline tag back (if hero attacking)
    /// 
    /// HIT OUTCOMES (HitOutcome enum):
    /// - Miss: Attack dodged, no damage
    /// - Hit: Normal damage
    /// - Critical: Bonus damage with extra VFX
    /// 
    /// TIMELINE PUSHBACK:
    /// When heroes attack enemies, the enemy's timeline tag is pushed back:
    /// - Only during hero turn
    /// - Pushback amount based on attacker's Strength stat
    /// - Delays enemy turn, giving player more time
    /// 
    /// RELATED FILES:
    /// - AttackResult.cs: Data model for attack outcome
    /// - PincerAttackSequence.cs: Uses MultiAttackRoutine
    /// - EnemyAttackSequence.cs: Uses SingleAttackRoutine
    /// - VisualEffectLibrary.cs: Provides attack VFX
    /// - TimelineBarInstance.cs: PushbackOnAttack method
    /// </summary>
    public static class AttackHelper
    {
        /// <summary>
        /// Applies damage for a single attack result with VFX.
        /// If miss, plays dodge animation. Otherwise plays slash VFX and applies damage.
        /// </summary>
        public static IEnumerator SingleAttackRoutine(AttackResult attackResult)
        {
            var opp = attackResult?.Opponent;
            if (opp == null || !opp.IsPlaying)
                yield break;

            // Handle miss with dodge feedback
            if (attackResult.HitType == HitOutcome.Miss)
            {
                yield return opp.AttackMissRoutine();
                yield return Wait.None();
                yield break;
            }

            // For attacks against enemies, play VFX and apply damage at apex
            if (opp.IsEnemy)
            {
                var vfx = VisualEffectLibrary.Get("BlueSlash4");
                if (vfx != null)
                {
                    var inst = g.VisualEffectManager.SpawnInstance(vfx, opp.Position, null);
                    if (inst != null)
                    {
                        // Wait until slash reaches apex, then apply damage
                        yield return inst.WaitUntilTrigger(vfx);
                        opp.Damage(attackResult);

                        // Push enemy timeline back if hero attacking during hero turn
                        bool isHeroTurn = g.TurnManager == null || g.TurnManager.IsHeroTurn;
                        if (isHeroTurn && attackResult.Attacker != null && attackResult.Attacker.IsHero)
                        {
                            int attackerStrength = attackResult.Attacker.Stats?.Strength.ToInt() ?? 10;
                            g.TimelineBar?.PushbackOnAttack(opp, attackerStrength);
                        }

                        yield return Wait.None();
                        yield break;
                    }
                }
            }

            // Fallback: apply damage immediately
            opp.Damage(attackResult);

            // Timeline pushback for hero attacks
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
