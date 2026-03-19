using System.Collections.Generic;
using Scripts.Helpers;
using UnityEngine;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Libraries
{
    /// <summary>
    /// ABILITYLIBRARY - Registry of all abilities in the game.
    ///
    /// PURPOSE:
    /// Factory methods for creating Ability instances with
    /// pre-configured settings (name, type, cost, effects).
    /// Also bridges SkillDefinition IDs to Ability factories
    /// so trained skills can be equipped on the ability bar.
    ///
    /// ABILITY CATEGORIES:
    /// - Active: Player-activated (Heal, Fire, Smite)
    /// - Passive: Always-on modifiers (DoubleAttack, Focus)
    /// - Reactive: Auto-triggered (CounterAttack, Cover)
    ///
    /// LOOKUP METHODS:
    /// - Get(name): Lookup by ability display name
    /// - GetBySkillId(skillId): Lookup by SkillDefinition.Id
    /// - FromConsumable(item): Create from consumable item
    ///
    /// RELATED FILES:
    /// - Ability.cs: Ability data structure
    /// - AbilityManager.cs: Ability execution
    /// - SkillData_Training.cs: Trainable skill definitions
    /// - SpriteLibrary.cs: Ability button sprites
    /// </summary>
    public static class AbilityLibrary
    {
        // ============== NAME LOOKUP ==============

        private static Dictionary<string, System.Func<Ability>> registry;

        /// <summary>Ensures the name→factory registry is populated.
        /// Names must match the 'name' field in each factory method exactly.</summary>
        private static void EnsureRegistry()
        {
            if (registry != null) return;
            registry = new Dictionary<string, System.Func<Ability>>(System.StringComparer.OrdinalIgnoreCase)
            {
                // Active — Combat
                { "Spark of Healing", Heal },
                { "Shield Bash",      ShieldRush },
                { "Trap",             Trap },
                { "Smite",            Smite },
                { "Strike",           Strike },
                // Active — Offensive Magic
                { "Fire",             Fire },
                { "Ice",              Ice },
                { "Thunder",          Thunder },
                { "Fireball",         Fireball },
                { "Fira",             Fira },
                // Active — Support Magic
                { "Group Heal",       GroupHeal },
                { "Esuna",            Esuna },
                { "Protect",          Protect },
                { "Regen",            Regen },
                // Passive
                { "Double Attack",    DoubleAttack },
                { "Triple Attack",    TripleAttack },
                { "Double Move",      DoubleMove },
                { "Triple Move",      TripleMove },
                { "+10% Armor",       ArmorUpPassive },
                { "+5% Crit",         CritUpPassive },
                { "Focus",            FocusPassive },
                { "Evasion Up",       EvasionUpPassive },
                { "HP Up",            HPUpPassive },
                // Reactive
                { "Counter Attack",   CounterAttack },
                { "Cover",            CoverReactive },
            };
        }

        /// <summary>Looks up an ability by name. Returns a new instance or null.</summary>
        public static Ability Get(string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName)) return null;
            EnsureRegistry();
            return registry.TryGetValue(abilityName, out var factory) ? factory() : null;
        }

        // ============== SKILL ID LOOKUP ==============

        private static Dictionary<string, System.Func<Ability>> skillIdRegistry;

        /// <summary>Builds the SkillDefinition.Id → factory bridge.</summary>
        private static void EnsureSkillIdRegistry()
        {
            if (skillIdRegistry != null) return;
            skillIdRegistry = new Dictionary<string, System.Func<Ability>>(System.StringComparer.OrdinalIgnoreCase)
            {
                // Basic skills
                { "skill_basic_heal",    Heal },
                { "skill_basic_strike",  Strike },
                // Offensive magic
                { "skill_fire",          Fire },
                { "skill_ice",           Ice },
                { "skill_thunder",       Thunder },
                { "skill_fireball",      Fireball },
                { "skill_fira",          Fira },
                // Support magic
                { "skill_group_heal",    GroupHeal },
                { "skill_esuna",         Esuna },
                { "skill_protect",       Protect },
                { "skill_regen",         Regen },
                // Passive upgrades
                { "skill_armor_up",      ArmorUpPassive },
                { "skill_crit_up",       CritUpPassive },
                { "skill_focus",         FocusPassive },
                { "skill_evasion_up",    EvasionUpPassive },
                { "skill_hp_up",         HPUpPassive },
                // Reactive
                { "skill_counter",       CounterAttack },
                { "skill_cover",         CoverReactive },
            };
        }

        /// <summary>
        /// Looks up an ability by SkillDefinition.Id (e.g. "skill_fireball").
        /// Bridges the training system to the ability system.
        /// Returns a new instance or null if no mapping exists.
        /// </summary>
        public static Ability GetBySkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            EnsureSkillIdRegistry();
            return skillIdRegistry.TryGetValue(skillId, out var factory) ? factory() : null;
        }

        // ============== ACTIVE ABILITIES — COMBAT ==============

        /// <summary>Heal — restores HP to one ally.</summary>
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

        /// <summary>Strike — basic melee attack available to any hero.</summary>
        public static Ability Strike() => new Ability
        {
            name = "Strike",
            category = AbilityCategory.Active,
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Strike") ? SpriteLibrary.AbilityButtons["Strike"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Strike,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 5,
            Description = "A simple offensive attack."
        };

        /// <summary>Shield rush — linear charge that can trigger pincer.</summary>
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

        /// <summary>Trap — roots target for one turn.</summary>
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

        /// <summary>Smite — holy damage to one enemy.</summary>
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

        // ============== ACTIVE ABILITIES — OFFENSIVE MAGIC ==============

        /// <summary>Fire — single-target fire damage.</summary>
        public static Ability Fire() => new Ability
        {
            name = "Fire",
            category = AbilityCategory.Active,
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Fire") ? SpriteLibrary.AbilityButtons["Fire"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Fire,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 10,
            Description = "Conjure a burst of flame that scorches one enemy."
        };

        /// <summary>Ice — single-target ice damage.</summary>
        public static Ability Ice() => new Ability
        {
            name = "Ice",
            category = AbilityCategory.Active,
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Ice") ? SpriteLibrary.AbilityButtons["Ice"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Ice,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 10,
            Description = "Conjure a shard of ice that pierces one enemy."
        };

        /// <summary>Thunder — single-target lightning damage.</summary>
        public static Ability Thunder() => new Ability
        {
            name = "Thunder",
            category = AbilityCategory.Active,
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Thunder") ? SpriteLibrary.AbilityButtons["Thunder"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Thunder,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 10,
            Description = "Call down a bolt of lightning to strike one enemy."
        };

        /// <summary>Fireball — stronger fire magic (trained, requires Magic tag).</summary>
        public static Ability Fireball() => new Ability
        {
            name = "Fireball",
            category = AbilityCategory.Active,
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Fire") ? SpriteLibrary.AbilityButtons["Fire"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Fireball,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 15,
            Description = "Hurl a ball of fire at an enemy, dealing heavy fire damage."
        };

        /// <summary>Fira — powerful tier-2 fire magic.</summary>
        public static Ability Fira() => new Ability
        {
            name = "Fira",
            category = AbilityCategory.Active,
            type = AbilityType.TargetOpponent,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Fire") ? SpriteLibrary.AbilityButtons["Fire"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Fira,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 30,
            Description = "Unleash an intense firestorm on a single enemy."
        };

        // ============== ACTIVE ABILITIES — SUPPORT MAGIC ==============

        /// <summary>Group Heal — restore HP to all allies.</summary>
        public static Ability GroupHeal() => new Ability
        {
            name = "Group Heal",
            category = AbilityCategory.Active,
            type = AbilityType.Self,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Heal") ? SpriteLibrary.AbilityButtons["Heal"] : null,
            TotalNumberOfTargets = 0, // targets all allies
            Effect = AbilityEffect.GroupHeal,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 25,
            Description = "A wave of restorative light heals the entire party."
        };

        /// <summary>Esuna — cure status ailments from one ally.</summary>
        public static Ability Esuna() => new Ability
        {
            name = "Esuna",
            category = AbilityCategory.Active,
            type = AbilityType.TargetAlly,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Heal") ? SpriteLibrary.AbilityButtons["Heal"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Esuna,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 20,
            Description = "Purge all status ailments from one ally."
        };

        /// <summary>Protect — boost one ally's physical defense.</summary>
        public static Ability Protect() => new Ability
        {
            name = "Protect",
            category = AbilityCategory.Active,
            type = AbilityType.TargetAlly,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Heal") ? SpriteLibrary.AbilityButtons["Heal"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Protect,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 15,
            Description = "Surround an ally with a protective barrier that reduces physical damage."
        };

        /// <summary>Regen — grant HP regeneration over time to one ally.</summary>
        public static Ability Regen() => new Ability
        {
            name = "Regen",
            category = AbilityCategory.Active,
            type = AbilityType.TargetAlly,
            button = SpriteLibrary.AbilityButtons.ContainsKey("Heal") ? SpriteLibrary.AbilityButtons["Heal"] : null,
            TotalNumberOfTargets = 1,
            Effect = AbilityEffect.Regen,
            TargetingMode = AbilityTargetingMode.AnyActor,
            ManaCost = 18,
            Description = "Bless an ally with gradual HP recovery each turn."
        };

        // ============== PASSIVE ABILITIES ==============

        /// <summary>Double attack.</summary>
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

        /// <summary>Triple attack.</summary>
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

        /// <summary>Double move.</summary>
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

        /// <summary>Triple move.</summary>
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

        /// <summary>+10% Armor (trained passive).</summary>
        public static Ability ArmorUpPassive() => new Ability
        {
            name = "+10% Armor",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.ArmorUp,
            ManaCost = 0,
            Description = "Passive: Increases armor effectiveness by 10%."
        };

        /// <summary>+5% Crit (trained passive).</summary>
        public static Ability CritUpPassive() => new Ability
        {
            name = "+5% Crit",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.CritUp,
            ManaCost = 0,
            Description = "Passive: Increases critical hit chance by 5%."
        };

        /// <summary>Focus — +10% damage (trained passive).</summary>
        public static Ability FocusPassive() => new Ability
        {
            name = "Focus",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.Focus,
            ManaCost = 0,
            Description = "Passive: Increases all damage dealt by 10%."
        };

        /// <summary>Evasion Up — +10% dodge (trained passive).</summary>
        public static Ability EvasionUpPassive() => new Ability
        {
            name = "Evasion Up",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.EvasionUp,
            ManaCost = 0,
            Description = "Passive: Increases evasion rate by 10%."
        };

        /// <summary>HP Up — +15% max HP (trained passive).</summary>
        public static Ability HPUpPassive() => new Ability
        {
            name = "HP Up",
            category = AbilityCategory.Passive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.HPUp,
            ManaCost = 0,
            Description = "Passive: Increases maximum HP by 15%."
        };

        // ============== REACTIVE ABILITIES ==============

        /// <summary>Counter attack — auto-strike back when hit.</summary>
        public static Ability CounterAttack() => new Ability
        {
            name = "Counter Attack",
            category = AbilityCategory.Reactive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.CounterAttack,
            ManaCost = 0,
            Description = "When attacked, automatically strikes back at the attacker."
        };

        /// <summary>Cover — take damage for a nearby ally.</summary>
        public static Ability CoverReactive() => new Ability
        {
            name = "Cover",
            category = AbilityCategory.Reactive,
            type = AbilityType.Passive,
            Effect = AbilityEffect.Cover,
            ManaCost = 0,
            Description = "Automatically intercept attacks aimed at adjacent low-HP allies."
        };

        // ============== ITEM-BACKED ABILITIES ==============

        /// <summary>
        /// Creates an ability from a consumable item definition.
        /// The ability, when activated, consumes the item and applies its effect.
        /// </summary>
        public static Ability FromConsumable(ItemDefinition item)
        {
            if (item == null || !item.IsConsumable) return null;

            var targeting = item.BaseHealing > 0 ? AbilityType.TargetAlly : AbilityType.Self;
            var sprite = SpriteLibrary.AbilityButtons.ContainsKey("Heal")
                ? SpriteLibrary.AbilityButtons["Heal"]
                : null;

            return new Ability
            {
                name = item.DisplayName,
                category = AbilityCategory.Active,
                type = targeting,
                button = sprite,
                TotalNumberOfTargets = 1,
                Effect = AbilityEffect.UseItem,
                TargetingMode = AbilityTargetingMode.AnyActor,
                ManaCost = 0,
                Description = item.Description,
                SourceItem = item,
            };
        }
    }
}
