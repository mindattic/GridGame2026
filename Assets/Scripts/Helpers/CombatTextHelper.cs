using Scripts.Models;
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
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// COMBATTEXTHELPER - Combat text style selection.
    /// 
    /// PURPOSE:
    /// Determines the appropriate text style key based on
    /// attack result (critical, miss, normal).
    /// 
    /// STYLE MAPPING:
    /// - Critical hit → "CriticalHit" (yellow, large)
    /// - Miss → "Miss" (gray)
    /// - Normal → "Damage" (white)
    /// 
    /// USAGE:
    /// ```csharp
    /// var style = CombatTextHelper.GetStyle(attackResult);
    /// CombatTextManager.Spawn(damage.ToString(), pos, style);
    /// ```
    /// 
    /// RELATED FILES:
    /// - TextStyleLibrary.cs: Style definitions
    /// - CombatTextManager.cs: Text spawning
    /// </summary>
    public static class CombatTextHelper
    {
        /// <summary>Returns the appropriate combat text style key based on hit type.</summary>
        public static string GetStyle(AttackResult attackResult)
        {
            if (attackResult.HitType == HitOutcome.Critical)
                return "CriticalHit";
            if (attackResult.HitType == HitOutcome.Miss)
                return "Miss";

            return "Damage";
        }
    }

}
