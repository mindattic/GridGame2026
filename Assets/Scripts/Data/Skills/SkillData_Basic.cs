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
/// SKILLDATA_BASIC - Basic skill definitions.
/// 
/// PURPOSE:
/// Static definitions for fundamental skills available
/// to heroes from the start.
/// 
/// SKILLS:
/// - BasicHeal: Active, restores HP, requires Healer tag
/// - BasicStrike: Active, basic attack, no restrictions
/// 
/// RELATED FILES:
/// - SkillLibrary.cs: Registers these skills
/// - SkillDefinition.cs: Skill data structure
/// </summary>
public static class SkillData_Basic
{
    public static readonly SkillDefinition BasicHeal = new SkillDefinition
    {
        Id = "skill_basic_heal",
        DisplayName = "Heal",
        Description = "Restore moderate HP to an ally.",
        Type = SkillType.Active,
        ManaCost = 10,
        MaxUsesPerBattle = 3,
        RequiredTags = { ActorTag.Healer }
    };

    public static readonly SkillDefinition BasicStrike = new SkillDefinition
    {
        Id = "skill_basic_strike",
        DisplayName = "Strike",
        Description = "A simple offensive attack.",
        Type = SkillType.Active,
        ManaCost = 5,
        MaxUsesPerBattle = 0
    };
}

}
