using System.Collections.Generic;

namespace Scripts.Core.Board
{
    /// <summary>
    /// PINCERCANDIDATE - One detected pincer formation, expressed purely as ids.
    /// <para>PURPOSE: The result type returned by <see cref="PincerDetector"/>. It names
    /// the two attacking endpoints, the contiguous line of opponents between them, and
    /// the supporters lending each endpoint bonuses - all by <see cref="BoardActor.Id"/>,
    /// never by MonoBehaviour reference. The caller maps these ids back to live actors.</para>
    /// <para>Candidates are returned in board-scan order; turn/chain ordering is applied
    /// separately by the caller (see PincerAttackManager.OrderPairsByChainsThenNearest).</para>
    /// <para>RELATED FILES: PincerDetector.cs, BoardActor.cs, PincerAttackManager.cs</para>
    /// </summary>
    public sealed class PincerCandidate
    {
        public int Attacker1Id;
        public int Attacker2Id;
        public List<int> OpponentIds = new();
        public List<int> Supporter1Ids = new();
        public List<int> Supporter2Ids = new();
    }
}
