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

    // === ELEMENTAL MAGIC ===

    public static readonly SkillDefinition Fire = new SkillDefinition
    {
        Id = "skill_fire",
        DisplayName = "Fire",
        Description = "Conjure a burst of flame that scorches one enemy.",
        Type = SkillType.Active,
        ManaCost = 10,
        MaxUsesPerBattle = 0,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly SkillDefinition Ice = new SkillDefinition
    {
        Id = "skill_ice",
        DisplayName = "Ice",
        Description = "Conjure a shard of ice that pierces one enemy.",
        Type = SkillType.Active,
        ManaCost = 10,
        MaxUsesPerBattle = 0,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly SkillDefinition Thunder = new SkillDefinition
    {
        Id = "skill_thunder",
        DisplayName = "Thunder",
        Description = "Call down a bolt of lightning to strike one enemy.",
        Type = SkillType.Active,
        ManaCost = 10,
        MaxUsesPerBattle = 0,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly SkillDefinition Fira = new SkillDefinition
    {
        Id = "skill_fira",
        DisplayName = "Fira",
        Description = "Unleash an intense firestorm on a single enemy.",
        Type = SkillType.Active,
        ManaCost = 30,
        MaxUsesPerBattle = 0,
        RequiredTags = { ActorTag.Magic },
    };

    // === SUPPORT MAGIC ===

    public static readonly SkillDefinition Esuna = new SkillDefinition
    {
        Id = "skill_esuna",
        DisplayName = "Esuna",
        Description = "Purge all status ailments from one ally.",
        Type = SkillType.Active,
        ManaCost = 20,
        MaxUsesPerBattle = 0,
        RequiredTags = { ActorTag.Healer },
    };

    public static readonly SkillDefinition Protect = new SkillDefinition
    {
        Id = "skill_protect",
        DisplayName = "Protect",
        Description = "Surround an ally with a barrier that reduces physical damage.",
        Type = SkillType.Active,
        ManaCost = 15,
        MaxUsesPerBattle = 0,
        RequiredTags = { ActorTag.Healer },
    };

    public static readonly SkillDefinition Regen = new SkillDefinition
    {
        Id = "skill_regen",
        DisplayName = "Regen",
        Description = "Bless an ally with gradual HP recovery each turn.",
        Type = SkillType.Active,
        ManaCost = 18,
        MaxUsesPerBattle = 0,
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

    public static readonly SkillDefinition Focus = new SkillDefinition
    {
        Id = "skill_focus",
        DisplayName = "Focus",
        Description = "Passive: Increases all damage dealt by 10%.",
        Type = SkillType.Passive,
        ManaCost = 0,
        MaxUsesPerBattle = 0,
    };

    public static readonly SkillDefinition EvasionUp = new SkillDefinition
    {
        Id = "skill_evasion_up",
        DisplayName = "Evasion Up",
        Description = "Passive: Increases evasion rate by 10%.",
        Type = SkillType.Passive,
        ManaCost = 0,
        MaxUsesPerBattle = 0,
    };

    public static readonly SkillDefinition HPUp = new SkillDefinition
    {
        Id = "skill_hp_up",
        DisplayName = "HP Up",
        Description = "Passive: Increases maximum HP by 15%.",
        Type = SkillType.Passive,
        ManaCost = 0,
        MaxUsesPerBattle = 0,
    };

    // === REACTIVE SKILLS ===

    public static readonly SkillDefinition Counter = new SkillDefinition
    {
        Id = "skill_counter",
        DisplayName = "Counter Attack",
        Description = "Automatically strikes back when hit by a physical attack.",
        Type = SkillType.Passive, // reactive treated as passive sub-type
        ManaCost = 0,
        MaxUsesPerBattle = 0,
    };

    public static readonly SkillDefinition Cover = new SkillDefinition
    {
        Id = "skill_cover",
        DisplayName = "Cover",
        Description = "Automatically intercept attacks aimed at low-HP allies.",
        Type = SkillType.Passive,
        ManaCost = 0,
        MaxUsesPerBattle = 0,
    };

    // === TRAINING WRAPPERS ===

    // -- Offensive Magic --

    public static readonly TrainingDefinition TrainFire = new TrainingDefinition
    {
        Id = "train_fire",
        DisplayName = "Learn Fire",
        Description = "Teach a mage basic fire magic.",
        SkillId = "skill_fire",
        GoldCost = 80,
        MinLevel = 2,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly TrainingDefinition TrainIce = new TrainingDefinition
    {
        Id = "train_ice",
        DisplayName = "Learn Ice",
        Description = "Teach a mage basic ice magic.",
        SkillId = "skill_ice",
        GoldCost = 80,
        MinLevel = 2,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly TrainingDefinition TrainThunder = new TrainingDefinition
    {
        Id = "train_thunder",
        DisplayName = "Learn Thunder",
        Description = "Teach a mage basic lightning magic.",
        SkillId = "skill_thunder",
        GoldCost = 80,
        MinLevel = 2,
        RequiredTags = { ActorTag.Magic },
    };

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

    public static readonly TrainingDefinition TrainFira = new TrainingDefinition
    {
        Id = "train_fira",
        DisplayName = "Learn Fira",
        Description = "Teach a mage the powerful Fira spell.",
        SkillId = "skill_fira",
        GoldCost = 250,
        MinLevel = 8,
        RequiredTags = { ActorTag.Magic },
    };

    // -- Support Magic --

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

    public static readonly TrainingDefinition TrainEsuna = new TrainingDefinition
    {
        Id = "train_esuna",
        DisplayName = "Learn Esuna",
        Description = "Teach a healer to cure status ailments.",
        SkillId = "skill_esuna",
        GoldCost = 120,
        MinLevel = 4,
        RequiredTags = { ActorTag.Healer },
    };

    public static readonly TrainingDefinition TrainProtect = new TrainingDefinition
    {
        Id = "train_protect",
        DisplayName = "Learn Protect",
        Description = "Teach a healer the Protect barrier spell.",
        SkillId = "skill_protect",
        GoldCost = 100,
        MinLevel = 3,
        RequiredTags = { ActorTag.Healer },
    };

    public static readonly TrainingDefinition TrainRegen = new TrainingDefinition
    {
        Id = "train_regen",
        DisplayName = "Learn Regen",
        Description = "Teach a healer the Regen recovery spell.",
        SkillId = "skill_regen",
        GoldCost = 180,
        MinLevel = 6,
        RequiredTags = { ActorTag.Healer },
    };

    // -- Passives --

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

    public static readonly TrainingDefinition TrainFocus = new TrainingDefinition
    {
        Id = "train_focus",
        DisplayName = "Train Focus",
        Description = "Permanently improve a hero's damage output by 10%.",
        SkillId = "skill_focus",
        GoldCost = 100,
        MinLevel = 3,
    };

    public static readonly TrainingDefinition TrainEvasionUp = new TrainingDefinition
    {
        Id = "train_evasion_up",
        DisplayName = "Train Evasion Up",
        Description = "Permanently improve a hero's dodge chance by 10%.",
        SkillId = "skill_evasion_up",
        GoldCost = 150,
        MinLevel = 5,
    };

    public static readonly TrainingDefinition TrainHPUp = new TrainingDefinition
    {
        Id = "train_hp_up",
        DisplayName = "Train HP Up",
        Description = "Permanently increase a hero's maximum HP by 15%.",
        SkillId = "skill_hp_up",
        GoldCost = 80,
        MinLevel = 2,
    };

    // -- Reactive --

    public static readonly TrainingDefinition TrainCounter = new TrainingDefinition
    {
        Id = "train_counter",
        DisplayName = "Learn Counter",
        Description = "Teach a hero to counter physical attacks.",
        SkillId = "skill_counter",
        GoldCost = 200,
        MinLevel = 6,
    };

    public static readonly TrainingDefinition TrainCover = new TrainingDefinition
    {
        Id = "train_cover",
        DisplayName = "Learn Cover",
        Description = "Teach a hero to shield low-HP allies from damage.",
        SkillId = "skill_cover",
        GoldCost = 150,
        MinLevel = 4,
    };
}

}
