using Scripts.Models;
using System;
using UnityEngine;
using static Scripts.Instances.Actor.ActorLayer;
using UnityEngine.UIElements;
using static UnityEditor.FilePathAttribute;
using UnityEngine.TextCore.Text;
using Scripts.Libraries;
using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
/// <summary>
/// STAGEACTOR - Enemy spawn configuration for a stage.
/// 
/// PURPOSE:
/// Defines an enemy to spawn in a stage wave, including
/// character class, level, team, and spawn location.
/// 
/// PROPERTIES:
/// - CharacterClass: Enemy type to spawn
/// - Level: Enemy level
/// - Team: Which team (Enemy)
/// - SpawnTurn: Turn number to spawn on
/// - Location: Grid position (or random if null)
/// - Stats: Calculated stats based on level
/// 
/// RELATED FILES:
/// - StageWave.cs: Contains list of StageActors
/// - StageManager.cs: Spawns actors
/// - ActorLibrary.cs: Actor data lookup
/// </summary>
[Serializable]
public class StageActor
{
    public CharacterClass CharacterClass;
    public int Level = 1;
    [NonSerialized] public Team Team;
    [NonSerialized] public int SpawnTurn;
    [NonSerialized] public Vector2Int? Location;
    [NonSerialized] public ActorStats Stats;

    public StageActor() { }

    public StageActor(StageActor other)
    {
        CharacterClass = other.CharacterClass;
        Team = other.Team;
        Level = other.Level;
        SpawnTurn = other.SpawnTurn;
        Location = other.Location;
        AssignStats();
    }

    public StageActor(CharacterClass characterClass, Team team, int level, Vector2Int? location = null)
    {
        CharacterClass = characterClass;
        Team = team;
        Level = level;
        SpawnTurn = 0;
        Location = location.HasValue ? location.Value : RNG.UnoccupiedLocation;
        AssignStats();
    }

    /// <summary>Assign stats.</summary>
    public void AssignStats()
    {
        if (CharacterClass == CharacterClass.None)
        {
            Stats = null;
            return;
        }

        var actor = ActorLibrary.Get(CharacterClass);
        if (actor != null)
        {
            Stats = actor.GetStats(Level);
        }
        else
        {
            Debug.LogError($"StageActor failed to assign Stats for characterClass: {CharacterClass}");
            Stats = null;
        }
    }
}

}
