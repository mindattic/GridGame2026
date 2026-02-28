using Assets.Helpers;
using Assets.Scripts.Libraries;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PROFILE MODELS - Player profile and settings data structures.
/// 
/// PURPOSE:
/// Defines data structures for player profiles, save data,
/// and settings used by ProfileHelper.
/// 
/// CLASSES:
/// - ProfileSettings: Player preferences (speed, sensitivity)
/// - Profile: Container for settings + save slots
/// - SaveState: Individual save file data
/// - Various SaveData classes: Roster, Party, Stage, etc.
/// </summary>

#region Attributes

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class SettingDisplayNameAttribute : Attribute
{
    public string Name { get; }
    public SettingDisplayNameAttribute(string name) { Name = name; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class SettingRangeAttribute : Attribute
{
    public float Min { get; }
    public float Max { get; }
    public float Increment { get; }
    public SettingRangeAttribute(float min, float max, float increment = 0f) { Min = min; Max = max; Increment = increment; }
}

#endregion

/// <summary>Player preferences and game settings.</summary>
[Serializable]
public class ProfileSettings
{
    [SettingDisplayName("Actor Pan Multiplier"), SettingRange(0f, 1f, 0.01f)]
    public float ActorPanMultiplier;

    [SettingDisplayName("Game Speed"), SettingRange(0.25f, 3f, 0.05f)]
    public float GameSpeed;

    [SettingDisplayName("Drag Sensitivity"), SettingRange(0.01f, 0.10f, 0.01f)]
    public float DragSensitivity;

    [SettingDisplayName("Coin Count Multiplier"), SettingRange(0f, 5f, 0.05f)]
    public float CoinCountMultiplier;

    [SettingDisplayName("Apply Movement Tilt")]
    public bool ApplyMovementTilt;

    [SettingDisplayName("Reload Thumbnail Settings")]
    public bool ReloadThumbnailSettings;


    // Enum settings
    //[SettingDisplayName("Texture Resolution")]
    //public TextureResolution TextureResolution;

    public ProfileSettings() { }

    public ProfileSettings(ProfileSettings other)
    {
        ActorPanMultiplier = other.ActorPanMultiplier;
        GameSpeed = other.GameSpeed;
        DragSensitivity = other.DragSensitivity;
        CoinCountMultiplier = other.CoinCountMultiplier;
        ApplyMovementTilt = other.ApplyMovementTilt;
        ReloadThumbnailSettings = other.ReloadThumbnailSettings;
    }
}

namespace Game.Models.Profile
{
    [Serializable]
    public class Profile
    {
        public string Key;
        public string Folder;
        public List<SaveState> SaveStates;
        public SaveState CurrentSave;
        public bool HasSaves => SaveStates != null && SaveStates.Count > 0;
        public SaveState LatestSave => HasSaves ? SaveStates[0] : null;
        public ProfileSettings Settings;
    }

    [Serializable]
    public class SaveState
    {
        public int Index;
        public DateTime Timestamp;
        public string FileName;
        public GlobalSaveData Global;
        public StageSaveData Stage;
        public RosterSaveData Roster;
        public PartySaveData Party;
        public OverworldSaveData Overworld;

        public SaveState() { }
        public SaveState(SaveState other)
        {
            this.Index = other.Index;
            this.Timestamp = other.Timestamp;
            this.FileName = other.FileName;
            this.Global = new GlobalSaveData(other.Global);
            this.Stage = new StageSaveData(other.Stage);
            this.Roster = new RosterSaveData(other.Roster);
            this.Party = new PartySaveData(other.Party);
            this.Overworld = other.Overworld != null ? new OverworldSaveData(other.Overworld) : null;
        }
        public SaveState(int index, DateTime timestamp, GlobalSaveData global, StageSaveData stage, RosterSaveData roster, PartySaveData party)
        {
            Index = index;
            Timestamp = timestamp;
            FileName = $"Save_{timestamp:yyyyMMdd_HHmmss}.json";
            Global = global;
            Stage = stage;
            Roster = roster;
            Party = party;
            Overworld = new OverworldSaveData(); // default
        }
        public SaveState(int index, DateTime timestamp, GlobalSaveData global, StageSaveData stage, RosterSaveData roster, PartySaveData party, OverworldSaveData overworld)
        {
            Index = index;
            Timestamp = timestamp;
            FileName = $"Save_{timestamp:yyyyMMdd_HHmmss}.json";
            Global = global;
            Stage = stage;
            Roster = roster;
            Party = party;
            Overworld = overworld;
        }
    }

    [Serializable]
    public class GlobalSaveData
    {
        public int TotalCoins;

        public GlobalSaveData() { }
        public GlobalSaveData(GlobalSaveData other)
        {
            this.TotalCoins = other.TotalCoins;
        }
    }

    [Serializable]
    public class StageSaveData
    {
        public string CurrentStage;
        public int CurrentWave;

        public StageSaveData() { }
        public StageSaveData(StageSaveData other)
        {
            this.CurrentStage = other.CurrentStage;
            this.CurrentWave = other.CurrentWave;
        }
    }

    [Serializable]
    public class RosterSaveData
    {
        public List<CharacterLevelPair> Members = new List<CharacterLevelPair>();

        public RosterSaveData() { }
        public RosterSaveData(RosterSaveData other) { this.Members = other.Members; }
    }

    [Serializable]
    public class PartySaveData
    {
        public List<CharacterLevelPair> Members = new List<CharacterLevelPair>();

        public PartySaveData() { }
        public PartySaveData(PartySaveData other) { this.Members = other.Members; }
    }

    [Serializable]
    public class CharacterLevelPair
    {
        public CharacterClass CharacterClass;

        // Persist only the accumulated (lifetime) XP. Level and current XP are derived at runtime.
        public int TotalXP;

        public CharacterLevelPair() { }
        public CharacterLevelPair(CharacterClass characterClass, int totalXP = 0)
        {
            CharacterClass = characterClass;
            TotalXP = totalXP;
        }
    }

    // New: OverworldSaveData
    [Serializable]
    public class OverworldSaveData
    {
        public string MapName = Map.Test.ToString();
        public float HeroX;
        public float HeroY;
        public string HeroDirection = "Idle"; // MoveDirection as name

        public OverworldSaveData() { }
        public OverworldSaveData(OverworldSaveData other)
        {
            MapName = other.MapName;
            HeroX = other.HeroX;
            HeroY = other.HeroY;
            HeroDirection = other.HeroDirection;
        }
        public OverworldSaveData(string mapName, Vector2 pos, string facing)
        {
            MapName = mapName;
            HeroX = pos.x;
            HeroY = pos.y;
            HeroDirection = string.IsNullOrWhiteSpace(facing) ? "Idle" : facing;
        }
    }
}
