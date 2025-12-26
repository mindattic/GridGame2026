using Assets.Scripts.Models;
using System;
using UnityEngine;
using static Game.Instances.Actor.ActorLayer;
using UnityEngine.UIElements;
using static UnityEditor.FilePathAttribute;
using UnityEngine.TextCore.Text;
using Assets.Scripts.Libraries;
using Assets.Helpers;

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

    //Copy constructor
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

    public void AssignStats()
    {
        // Treat None as a null-equivalent; no stats to assign.
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
