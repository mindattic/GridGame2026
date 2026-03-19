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

        // Register the skills themselves into SkillLibrary
        // -- Offensive magic
        SkillLibrary.RegisterExternal(SkillData_Training.Fire);
        SkillLibrary.RegisterExternal(SkillData_Training.Ice);
        SkillLibrary.RegisterExternal(SkillData_Training.Thunder);
        SkillLibrary.RegisterExternal(SkillData_Training.Fireball);
        SkillLibrary.RegisterExternal(SkillData_Training.Fira);
        // -- Support magic
        SkillLibrary.RegisterExternal(SkillData_Training.GroupHeal);
        SkillLibrary.RegisterExternal(SkillData_Training.Esuna);
        SkillLibrary.RegisterExternal(SkillData_Training.Protect);
        SkillLibrary.RegisterExternal(SkillData_Training.Regen);
        // -- Passives
        SkillLibrary.RegisterExternal(SkillData_Training.ArmorUp);
        SkillLibrary.RegisterExternal(SkillData_Training.CritChanceUp);
        SkillLibrary.RegisterExternal(SkillData_Training.Focus);
        SkillLibrary.RegisterExternal(SkillData_Training.EvasionUp);
        SkillLibrary.RegisterExternal(SkillData_Training.HPUp);
        // -- Reactive
        SkillLibrary.RegisterExternal(SkillData_Training.Counter);
        SkillLibrary.RegisterExternal(SkillData_Training.Cover);

        // Register training wrappers
        // -- Offensive magic
        Register(SkillData_Training.TrainFire);
        Register(SkillData_Training.TrainIce);
        Register(SkillData_Training.TrainThunder);
        Register(SkillData_Training.TrainFireball);
        Register(SkillData_Training.TrainFira);
        // -- Support magic
        Register(SkillData_Training.TrainGroupHeal);
        Register(SkillData_Training.TrainEsuna);
        Register(SkillData_Training.TrainProtect);
        Register(SkillData_Training.TrainRegen);
        // -- Passives
        Register(SkillData_Training.TrainArmorUp);
        Register(SkillData_Training.TrainCritUp);
        Register(SkillData_Training.TrainFocus);
        Register(SkillData_Training.TrainEvasionUp);
        Register(SkillData_Training.TrainHPUp);
        // -- Reactive
        Register(SkillData_Training.TrainCounter);
        Register(SkillData_Training.TrainCover);
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
