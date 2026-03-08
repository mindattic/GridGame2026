using System.Collections.Generic;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Data.Skills
{
/// <summary>
/// SKILLDATA_TRAINING - Trainable ability definitions.
/// 
/// PURPOSE:
/// Defines skills that heroes can learn by spending gold
/// at the Training vendor. Includes both active abilities
/// and passive stat upgrades.
/// 
/// RELATED FILES:
/// - TrainingLibrary.cs: Registers these training options
/// - TrainingDefinition.cs: Training data structure
/// - TrainingSectionController.cs: Training UI
/// </summary>
public static class SkillData_Training
{
    // === MAGIC SKILLS ===

    public static readonly SkillDefinition Fireball = new SkillDefinition
    {
        Id = "skill_fireball",
        DisplayName = "Fireball",
        Description = "Hurl a ball of fire at an enemy.",
        Type = SkillType.Active,
        ManaCost = 15,
        MaxUsesPerBattle = 0,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly SkillDefinition GroupHeal = new SkillDefinition
    {
        Id = "skill_group_heal",
        DisplayName = "Group Heal",
        Description = "Restore HP to all allies.",
        Type = SkillType.Active,
        ManaCost = 25,
        MaxUsesPerBattle = 2,
        RequiredTags = { ActorTag.Healer },
    };

    // === PASSIVE UPGRADES ===

    public static readonly SkillDefinition ArmorUp = new SkillDefinition
    {
        Id = "skill_armor_up",
        DisplayName = "+10% Armor",
        Description = "Passive: Increases armor effectiveness by 10%.",
        Type = SkillType.Passive,
        ManaCost = 0,
        MaxUsesPerBattle = 0,
    };

    public static readonly SkillDefinition CritChanceUp = new SkillDefinition
    {
        Id = "skill_crit_up",
        DisplayName = "+5% Crit",
        Description = "Passive: Increases critical hit chance by 5%.",
        Type = SkillType.Passive,
        ManaCost = 0,
        MaxUsesPerBattle = 0,
    };

    // === TRAINING WRAPPERS ===

    public static readonly TrainingDefinition TrainFireball = new TrainingDefinition
    {
        Id = "train_fireball",
        DisplayName = "Learn Fireball",
        Description = "Teach a magic hero the Fireball ability.",
        SkillId = "skill_fireball",
        GoldCost = 150,
        MinLevel = 3,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly TrainingDefinition TrainGroupHeal = new TrainingDefinition
    {
        Id = "train_group_heal",
        DisplayName = "Learn Group Heal",
        Description = "Teach a healer the Group Heal ability.",
        SkillId = "skill_group_heal",
        GoldCost = 200,
        MinLevel = 5,
        RequiredTags = { ActorTag.Healer },
    };

    public static readonly TrainingDefinition TrainArmorUp = new TrainingDefinition
    {
        Id = "train_armor_up",
        DisplayName = "Train +10% Armor",
        Description = "Permanently improve a hero's armor.",
        SkillId = "skill_armor_up",
        GoldCost = 100,
        MinLevel = 2,
    };

    public static readonly TrainingDefinition TrainCritUp = new TrainingDefinition
    {
        Id = "train_crit_up",
        DisplayName = "Train +5% Crit",
        Description = "Permanently improve a hero's critical hit chance.",
        SkillId = "skill_crit_up",
        GoldCost = 120,
        MinLevel = 4,
    };
}

}
