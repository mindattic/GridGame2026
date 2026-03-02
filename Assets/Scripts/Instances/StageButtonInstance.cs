using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
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

namespace Scripts.Instances
{
/// <summary>
/// STAGEBUTTONINSTANCE - Stage selection button data.
/// 
/// PURPOSE:
/// Holds data for a stage selection button in the stage
/// select screen, primarily the stage identifier.
/// 
/// PROPERTIES:
/// - stageName: Identifier linking to StageLibrary entry
/// 
/// RELATED FILES:
/// - StageSelectManager.cs: Creates and manages buttons
/// - StageLibrary.cs: Stage definitions
/// - ScreenWidthButtonFactory.cs: Button creation
/// </summary>
public class StageButtonInstance : MonoBehaviour
{
    [SerializeField] public string stageName;  
}

}
