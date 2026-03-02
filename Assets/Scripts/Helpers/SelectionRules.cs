using Scripts.Helpers;
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
public static class SelectionRules
{
    public static bool CanControlHero(ActorInstance candidate)
    {
        if (candidate == null || !candidate.IsPlaying || !candidate.IsHero)
            return false;

        var mode = GameHelper.TurnSelectionMode;

        // Free selection: no restrictions
        if (mode == TurnSelectionMode.FreeSelect)
            return true;

        // Active only: must match the current hero controlled by TurnManager
        if (mode == TurnSelectionMode.ActiveOnly)
        {
            var activeHero = GameHelper.TurnManager != null && GameHelper.TurnManager.ActiveActor != null && GameHelper.TurnManager.ActiveActor.IsHero
                ? GameHelper.TurnManager.ActiveActor
                : null;
            return activeHero != null && candidate == activeHero;
        }

        // Prefer active with bonus: allowed, bonus handled by caller
        return true;
    }
}

}
