using Assets.Helpers;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class AbilityLibrary
    {
        // ============== ACTIVE ABILITIES ==============
        
        public static Ability Heal() => new Ability
        {
            name = "Spark of Healing",
            category = AbilityCategory.Active,
            type = AbilityType.TargetAny,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Heal") ? SpriteLibrary.AbilityButtons["Heal"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Heal,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 15,
            Description = "Launches a healing spark that flies to the target and restores HP."
        };

        public static Ability ShieldRush() => new Ability
        {
            name = "Shield Bash",
            category = AbilityCategory.Active,
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
            category = AbilityCategory.Active,
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
            category = AbilityCategory.Active,
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Smite") ? SpriteLibrary.AbilityButtons["Smite"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Smite,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 20,
            Description = "Calls down holy power to smite the target with a radiant explosion."
        };

        // ============== PASSIVE ABILITIES ==============
        
        public static Ability DoubleAttack() => new Ability
        {
            name = "Double Attack",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.DoubleAttack,
            ExtraAttacks = 1,
            ManaCost = 0,
            Description = "Attacks twice per turn against each adjacent target."
        };

        public static Ability TripleAttack() => new Ability
        {
            name = "Triple Attack",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.TripleAttack,
            ExtraAttacks = 2,
            ManaCost = 0,
            Description = "Attacks three times per turn against each adjacent target."
        };

        public static Ability DoubleMove() => new Ability
        {
            name = "Double Move",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.DoubleMove,
            ExtraMoves = 1,
            ManaCost = 0,
            Description = "Moves twice per turn instead of once."
        };

        public static Ability TripleMove() => new Ability
        {
            name = "Triple Move",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.TripleMove,
            ExtraMoves = 2,
            ManaCost = 0,
            Description = "Moves three times per turn instead of once."
        };

        // ============== REACTIVE ABILITIES ==============
        
        public static Ability CounterAttack() => new Ability
        {
            name = "Counter Attack",
            category = AbilityCategory.Reactive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.CounterAttack,
            ManaCost = 0,
            Description = "When attacked, automatically strikes back at the attacker."
        };
    }
}
