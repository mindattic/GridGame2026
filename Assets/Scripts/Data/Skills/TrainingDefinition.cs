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
/// TRAININGDEFINITION - Trainable ability with gold cost.
/// 
/// PURPOSE:
/// Defines a skill that can be purchased at the Training
/// vendor in the Hub. Heroes spend gold to learn new
/// abilities or passive upgrades.
/// 
/// RELATED FILES:
/// - TrainingLibrary.cs: Registry
/// - TrainingSectionController.cs: Training UI
/// - SkillDefinition.cs: The skill being trained
/// </summary>
[System.Serializable]
public class TrainingDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public string SkillId;
    public int GoldCost;
    public int MinLevel;
    public List<ActorTag> RequiredTags = new List<ActorTag>();
}

}
