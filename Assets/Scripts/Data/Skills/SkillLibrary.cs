using System.Collections.Generic;
using Assets.Helpers;
using Assets.Scripts.Libraries;

/// <summary>
/// SKILLLIBRARY - Central registry for skill definitions.
/// 
/// PURPOSE:
/// Provides lookup, enumeration, and equip validation
/// for skill definitions.
/// 
/// USAGE:
/// ```csharp
/// var heal = SkillLibrary.Get("basic_heal");
/// bool canEquip = SkillLibrary.CanEquip(hero, skill);
/// ```
/// 
/// RELATED FILES:
/// - SkillDefinition.cs: Skill data structure
/// - SkillData_Basic.cs: Basic skill definitions
/// - HeroLoadout.cs: Equipped skills
/// </summary>
public static class SkillLibrary
{
    private static Dictionary<string, SkillDefinition> skills = new Dictionary<string, SkillDefinition>();
    private static bool initialized;

    /// <summary>Ensures library is populated once.</summary>
    private static void Ensure()
    {
        if (initialized) return;
        initialized = true;
        Register(SkillData_Basic.BasicHeal);
        Register(SkillData_Basic.BasicStrike);
    }

    private static void Register(SkillDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.Id)) return;
        if (!skills.ContainsKey(def.Id)) skills.Add(def.Id, def);
    }

    /// <summary>Gets a skill by Id or null if missing.</summary>
    public static SkillDefinition Get(string id)
    {
        Ensure();
        if (string.IsNullOrEmpty(id)) return null;
        skills.TryGetValue(id, out var def);
        return def;
    }

    /// <summary>Enumerates all skills.</summary>
    public static IEnumerable<SkillDefinition> All()
    {
        Ensure();
        return skills.Values;
    }

    /// <summary>
    /// Validates whether a hero can equip a skill based on tag requirements.
    /// </summary>
    public static bool CanHeroEquip(CharacterClass hero, SkillDefinition skill)
    {
  if (skill == null) return false;
        var data = ActorLibrary.Get(hero);
   if (data == null) return false;
    if (skill.RequiredTags == null || skill.RequiredTags.Count ==0) return true;
    foreach (var tag in skill.RequiredTags)
   {
   if ((data.Tags & tag) != tag) return false;
  }
        return true;
    }
}
