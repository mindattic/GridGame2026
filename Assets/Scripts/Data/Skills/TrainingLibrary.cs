using System.Collections.Generic;
using System.Linq;
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
/// TRAININGLIBRARY - Central registry for trainable abilities.
/// 
/// PURPOSE:
/// Provides lookup and enumeration of training definitions
/// for the Training vendor in the Hub.
/// 
/// USAGE:
/// ```csharp
/// var available = TrainingLibrary.ForHero(CharacterClass.Paladin, level);
/// ```
/// 
/// RELATED FILES:
/// - TrainingDefinition.cs: Training data structure
/// - SkillData_Training.cs: Static definitions
/// - TrainingSectionController.cs: Training UI
/// </summary>
public static class TrainingLibrary
{
    private static Dictionary<string, TrainingDefinition> trainings = new Dictionary<string, TrainingDefinition>();
    private static bool initialized;

    /// <summary>Ensures library is populated once.</summary>
    private static void Ensure()
    {
        if (initialized) return;
        initialized = true;

        // Also register the skills themselves into SkillLibrary
        SkillLibrary.RegisterExternal(SkillData_Training.Fireball);
        SkillLibrary.RegisterExternal(SkillData_Training.GroupHeal);
        SkillLibrary.RegisterExternal(SkillData_Training.ArmorUp);
        SkillLibrary.RegisterExternal(SkillData_Training.CritChanceUp);

        Register(SkillData_Training.TrainFireball);
        Register(SkillData_Training.TrainGroupHeal);
        Register(SkillData_Training.TrainArmorUp);
        Register(SkillData_Training.TrainCritUp);
    }

    /// <summary>Registers a training definition.</summary>
    private static void Register(TrainingDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.Id)) return;
        if (!trainings.ContainsKey(def.Id)) trainings.Add(def.Id, def);
    }

    /// <summary>Gets a training definition by Id or null.</summary>
    public static TrainingDefinition Get(string id)
    {
        Ensure();
        if (string.IsNullOrEmpty(id)) return null;
        trainings.TryGetValue(id, out var t);
        return t;
    }

    /// <summary>Enumerates all training definitions.</summary>
    public static IEnumerable<TrainingDefinition> All()
    {
        Ensure();
        return trainings.Values;
    }

    /// <summary>Returns training options available for a specific hero based on tags and level.</summary>
    public static IEnumerable<TrainingDefinition> ForHero(CharacterClass hero, int level)
    {
        Ensure();
        var data = ActorLibrary.Get(hero);
        if (data == null) return Enumerable.Empty<TrainingDefinition>();

        return trainings.Values.Where(t =>
        {
            if (level < t.MinLevel) return false;
            if (t.RequiredTags == null || t.RequiredTags.Count == 0) return true;
            foreach (var tag in t.RequiredTags)
            {
                if ((data.Tags & tag) != tag) return false;
            }
            return true;
        });
    }
}

}
