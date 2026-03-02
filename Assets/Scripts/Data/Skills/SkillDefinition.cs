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
/// SKILLDEFINITION - Equipable skill data template.
/// 
/// PURPOSE:
/// Describes a skill that can be equipped and used in battle,
/// including resource costs and tag requirements.
/// 
/// PROPERTIES:
/// - Id: Unique identifier
/// - DisplayName: UI display text
/// - Description: Tooltip text
/// - Type: Active or Passive
/// - ManaCost: Mana required to use
/// - MaxUsesPerBattle: Usage limit (0 = unlimited)
/// - RequiredTags: Actor tags needed to equip
/// 
/// RELATED FILES:
/// - SkillLibrary.cs: Skill registry
/// - HeroLoadout.cs: Equipped skills
/// - AbilityManager.cs: Skill execution
/// </summary>
[System.Serializable]
public class SkillDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public SkillType Type;
    public int ManaCost;
    public int MaxUsesPerBattle;
    public List<ActorTag> RequiredTags = new List<ActorTag>();
}

/// <summary>Skill type classification.</summary>
public enum SkillType
{
    Active,
    Passive
}

}
