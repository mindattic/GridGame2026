using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Models;
using System.Collections;
using System.Linq;
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
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// PINCERATTACKSEQUENCE - Executes a single pincer attack pair.
    /// 
    /// PURPOSE:
    /// Animates and resolves one pincer attack between two attackers and their
    /// trapped opponents. Handles damage dealing, VFX, and death triggers.
    /// 
    /// PINCER ATTACK VISUALIZATION:
    /// ```
    /// [Attacker1] ─────────────────── [Attacker2]
    ///       ↘                               ↙
    ///          [Enemy1] [Enemy2] [Enemy3]
    ///              ↓ DAMAGE ↓
    ///          (all opponents hit)
    /// ```
    /// 
    /// SEQUENCE FLOW:
    /// 1. Display attacker portraits (3D portrait pair)
    /// 2. Grow animation on both attackers (anticipation)
    /// 3. Shrink animation on both attackers (attack windup)
    /// 4. Find closest opponent for each attacker
    /// 5. Execute attack routines (bump toward opponent, VFX, damage)
    /// 6. If opponent dying → Spin animation + "Respite" text
    /// 
    /// ATTACK RESULTS:
    /// The pair contains pre-calculated attackResults1/attackResults2:
    /// - Damage values per opponent
    /// - Critical hit flags
    /// - Status effects to apply
    /// 
    /// ANIMATIONS USED:
    /// - GrowRoutine(): Attacker scales up (anticipation)
    /// - ShrinkRoutine(): Attacker scales down (windup)
    /// - BumpRoutine(): Attacker moves toward and hits target
    /// - Spin360AndWaitRoutine(): Victory spin for killing blow
    /// 
    /// DATA SOURCE:
    /// PincerAttackPair contains:
    /// - attacker1, attacker2: The two heroes forming the pincer
    /// - opponents: Enemies trapped between them
    /// - attackResults1/2: Calculated damage per attacker
    /// 
    /// RELATED FILES:
    /// - PincerAttackManager.cs: Detects pincers, queues this sequence
    /// - PincerAttackPair.cs: Data model for pincer participants
    /// - AttackHelper.cs: Damage calculation and VFX
    /// - ActorAnimation.cs: Animation methods
    /// - DeathSequence.cs: Handles deaths after this completes
    /// </summary>
    public class PincerAttackSequence : SequenceEvent
    {
        private PincerAttackPair pair;

        /// <summary>Creates sequence for a pincer attack pair.</summary>
        public PincerAttackSequence(PincerAttackPair pair)
        {
            this.pair = pair;
        }

        /// <summary>
        /// Main sequence routine - executes the pincer attack animation and damage.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            // Early exit if attack results not calculated
            if (pair.attackResults1?.Any() != true || pair.attackResults2?.Any() != true)
                yield break;

            // Display attackers portraits
            yield return g.PortraitManager.SpawnPair3DRoutine(
                new ActorPair(pair.attacker1, pair.attacker2)
            );

            // Grow animation on both attackers simultaneously (anticipation)
            yield return CoroutineHelper.WaitForAll(
                GameManager.instance,
                pair.attacker1.Animation.GrowRoutine(),
                pair.attacker2.Animation.GrowRoutine()
            );

            // Shrink animation on both attackers simultaneously (windup)
            yield return CoroutineHelper.WaitForAll(
                GameManager.instance,
                pair.attacker1.Animation.ShrinkRoutine(),
                pair.attacker2.Animation.ShrinkRoutine()
            );

            // Find closest opponent for each attacker's bump animation
            var opp1 = Geometry.GetClosestOpponent(pair.attacker1, pair.attackResults1);
            var opp2 = Geometry.GetClosestOpponent(pair.attacker2, pair.attackResults2);

            // Build attack routines (damage, VFX, combat text)
            var routine1 = AttackHelper.MultiAttackRoutine(pair.attackResults1);
            var routine2 = AttackHelper.MultiAttackRoutine(pair.attackResults2);

            // Execute attack for attacker1
            if (opp1 != null && opp1.IsDying)
                yield return pair.attacker1.Animation.Spin360AndWaitRoutine(RespiteRoutine(opp1));
            else
                yield return pair.attacker1.Animation.BumpRoutine(opp1, routine1);

            // Execute attack for attacker2
            if (opp2 != null && opp2.IsDying)
                yield return pair.attacker2.Animation.Spin360AndWaitRoutine(RespiteRoutine(opp2));
            else
                yield return pair.attacker2.Animation.BumpRoutine(opp2, routine2);
        }

        /// <summary>Shows "Respite" text when killing an enemy.</summary>
        private IEnumerator RespiteRoutine(ActorInstance opponent)
        {
            if (opponent != null)
                g.CombatTextManager.Spawn("Respite", opponent.Position, "Heal");
            yield return null;
        }

    }
}
