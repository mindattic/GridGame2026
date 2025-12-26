using System.Collections.Generic;

/// <summary>
/// SkillDefinition describes an equipable skill usable in battle. Includes tag constraints and resource costs.
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

/// <summary>
/// Skill type classification.
/// </summary>
public enum SkillType
{
    Active,
    Passive
}
