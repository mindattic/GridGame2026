using Assets.Helpers;
using Assets.Scripts.Models;

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
