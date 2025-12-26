using Assets.Helpers;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class AbilityLibrary
    {
        public static Ability Heal() => new Ability
        {
            name = "Spark of Healing",
            type = AbilityType.TargetAny, // heal can target anyone per request
            button = SpriteLibrary.AbilityButtons.ContainsKey("Heal") ? SpriteLibrary.AbilityButtons["Heal"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Heal,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 15,
            Description = "Launches a healing spark that flies to the target and restores HP."
        };

        public static Ability ShieldRush() => new Ability
        {
            name = "Shield Bash", // existing content uses Shield Bash
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("ShieldBash") ? SpriteLibrary.AbilityButtons["ShieldBash"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.ShieldRush,
            TargetingMode = AbilityTargetingMode.Linear,
            ManaCost = 20,
            Description = "Rush in a straight line to bash an enemy. Can trigger a Pincer Attack."
        };

        public static Ability Trap() => new Ability
        {
            name = "Trap",
            type = AbilityType.TargetAny,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Trap") ? SpriteLibrary.AbilityButtons["Trap"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Trap,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 25,
            Description = "Fires a trap that roots the target for their next turn."
        };

        public static Ability Smite() => new Ability
        {
            name = "Smite",
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Smite") ? SpriteLibrary.AbilityButtons["Smite"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Smite,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 20,
            Description = "Calls down holy power to smite the target with a radiant explosion."
        };
    }
}
