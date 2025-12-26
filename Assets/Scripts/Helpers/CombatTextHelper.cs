using Assets.Scripts.Models;

namespace Assets.Helpers
{
    public static class CombatTextHelper
    {
        /// <summary>
        /// Returns the appropriate combat text style key based on hit type.
        /// </summary>
        public static string GetStyle(AttackResult attackResult)
        {
            if (attackResult.HitType == HitOutcome.Critical)
                return "CriticalHit"; // big, yellow
            if (attackResult.HitType == HitOutcome.Miss)
                return "Miss"; // use Miss style

            return "Damage"; // normal damage style
        }
    }

}
