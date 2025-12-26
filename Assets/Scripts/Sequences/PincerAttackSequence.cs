using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections;
using System.Linq;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    // SequenceEvent for processing a single attacking PincerAttackPair
    public class PincerAttackSequence : SequenceEvent
    {
        private PincerAttackPair pair;

        public PincerAttackSequence(PincerAttackPair pair)
        {
            this.pair = pair;
        }

        public override IEnumerator ProcessRoutine()
        {
            if (pair.attackResults1?.Any() != true || pair.attackResults2?.Any() != true)
                yield break;

            // Display attackers portraits
            yield return g.PortraitManager.SpawnPair3DRoutine(
                new ActorPair(pair.attacker1, pair.attacker2)
            );

            // GrowRoutine both attackers simultaneously
            yield return CoroutineHelper.WaitForAll(
                GameManager.instance,
                pair.attacker1.Animation.GrowRoutine(),
                pair.attacker2.Animation.GrowRoutine()
            );

            // Shrink both attackers simultaneously
            yield return CoroutineHelper.WaitForAll(
                GameManager.instance,
                pair.attacker1.Animation.ShrinkRoutine(),
                pair.attacker2.Animation.ShrinkRoutine()
            );

            // Pick an adjacent target per attacker
            var opp1 = Geometry.GetClosestOpponent(pair.attacker1, pair.attackResults1);
            var opp2 = Geometry.GetClosestOpponent(pair.attacker2, pair.attackResults2);

            // Build attack routines
            var routine1 = AttackHelper.MultiAttackRoutine(pair.attackResults1);
            var routine2 = AttackHelper.MultiAttackRoutine(pair.attackResults2);

            // If an opponent isDying, spin instead of bump and show Respite at apex
            if (opp1 != null && opp1.IsDying)
                yield return pair.attacker1.Animation.Spin360AndWaitRoutine(RespiteRoutine(opp1));
            else
                yield return pair.attacker1.Animation.BumpRoutine(opp1, routine1);

            if (opp2 != null && opp2.IsDying)
                yield return pair.attacker2.Animation.Spin360AndWaitRoutine(RespiteRoutine(opp2));
            else
                yield return pair.attacker2.Animation.BumpRoutine(opp2, routine2);
        }

        private IEnumerator RespiteRoutine(ActorInstance opponent)
        {
            if (opponent != null)
                g.CombatTextManager.Spawn("Respite", opponent.Position, "Heal");
            yield return null;
        }

    }
}
