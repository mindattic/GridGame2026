using Scripts.Helpers;
using Scripts.Libraries;
using System;
using System.Collections.Generic;
using UnityEngine;
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

namespace Scripts.Models
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
        public InventorySaveData Inventory;
        public EquipmentSaveData Equipment;
        public TrainingSaveData Training;

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
            this.Inventory = other.Inventory != null ? new InventorySaveData(other.Inventory) : new InventorySaveData();
            this.Equipment = other.Equipment != null ? new EquipmentSaveData(other.Equipment) : new EquipmentSaveData();
            this.Training = other.Training != null ? new TrainingSaveData(other.Training) : new TrainingSaveData();
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
            Overworld = new OverworldSaveData();
            Inventory = new InventorySaveData();
            Equipment = new EquipmentSaveData();
            Training = new TrainingSaveData();
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
            Inventory = new InventorySaveData();
            Equipment = new EquipmentSaveData();
            Training = new TrainingSaveData();
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

    /// <summary>
    /// INVENTORYSAVEDATA - Persisted player inventory.
    /// 
    /// PURPOSE:
    /// Stores item ownership as a flat list of (itemId, count, durability)
    /// entries that serialize to JSON. Loaded into PlayerInventory at runtime.
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public int Gold = 200;
        public List<InventoryEntrySave> Items = new List<InventoryEntrySave>();

        public InventorySaveData() { }
        public InventorySaveData(InventorySaveData other)
        {
            Gold = other.Gold;
            Items = new List<InventoryEntrySave>();
            if (other.Items != null)
            {
                foreach (var e in other.Items)
                    Items.Add(new InventoryEntrySave(e));
            }
        }
    }

    /// <summary>Single persisted inventory entry.</summary>
    [Serializable]
    public class InventoryEntrySave
    {
        public string ItemId;
        public int Count;
        public int CurrentDurability;

        public InventoryEntrySave() { }
        public InventoryEntrySave(InventoryEntrySave other)
        {
            ItemId = other.ItemId;
            Count = other.Count;
            CurrentDurability = other.CurrentDurability;
        }
        public InventoryEntrySave(string itemId, int count, int durability = 0)
        {
            ItemId = itemId;
            Count = count;
            CurrentDurability = durability;
        }
    }

    /// <summary>
    /// EQUIPMENTSAVEDATA - Persisted hero equipment assignments.
    /// </summary>
    [Serializable]
    public class EquipmentSaveData
    {
        public List<HeroEquipmentSave> Heroes = new List<HeroEquipmentSave>();

        public EquipmentSaveData() { }
        public EquipmentSaveData(EquipmentSaveData other)
        {
            Heroes = new List<HeroEquipmentSave>();
            if (other.Heroes != null)
            {
                foreach (var h in other.Heroes)
                    Heroes.Add(new HeroEquipmentSave(h));
            }
        }

        /// <summary>Gets or creates equipment entry for a hero.</summary>
        public HeroEquipmentSave GetOrCreate(CharacterClass hero)
        {
            var existing = Heroes.Find(h => h.CharacterClass == hero);
            if (existing != null) return existing;
            var entry = new HeroEquipmentSave { CharacterClass = hero };
            Heroes.Add(entry);
            return entry;
        }
    }

    /// <summary>Equipment assignments for a single hero.</summary>
    [Serializable]
    public class HeroEquipmentSave
    {
        public CharacterClass CharacterClass;
        public string WeaponId;
        public string ArmorId;
        public string HelmetId;
        public string BootsId;
        public string RingId;
        public string AmuletId;

        public HeroEquipmentSave() { }
        public HeroEquipmentSave(HeroEquipmentSave other)
        {
            CharacterClass = other.CharacterClass;
            WeaponId = other.WeaponId;
            ArmorId = other.ArmorId;
            HelmetId = other.HelmetId;
            BootsId = other.BootsId;
            RingId = other.RingId;
            AmuletId = other.AmuletId;
        }

        /// <summary>Gets the item ID equipped in the given slot.</summary>
        public string GetSlot(Scripts.Data.Items.EquipmentSlot slot)
        {
            switch (slot)
            {
                case Scripts.Data.Items.EquipmentSlot.Weapon: return WeaponId;
                case Scripts.Data.Items.EquipmentSlot.Armor: return ArmorId;
                case Scripts.Data.Items.EquipmentSlot.Helmet: return HelmetId;
                case Scripts.Data.Items.EquipmentSlot.Boots: return BootsId;
                case Scripts.Data.Items.EquipmentSlot.Ring: return RingId;
                case Scripts.Data.Items.EquipmentSlot.Amulet: return AmuletId;
                default: return null;
            }
        }

        /// <summary>Sets the item ID for the given slot.</summary>
        public void SetSlot(Scripts.Data.Items.EquipmentSlot slot, string itemId)
        {
            switch (slot)
            {
                case Scripts.Data.Items.EquipmentSlot.Weapon: WeaponId = itemId; break;
                case Scripts.Data.Items.EquipmentSlot.Armor: ArmorId = itemId; break;
                case Scripts.Data.Items.EquipmentSlot.Helmet: HelmetId = itemId; break;
                case Scripts.Data.Items.EquipmentSlot.Boots: BootsId = itemId; break;
                case Scripts.Data.Items.EquipmentSlot.Ring: RingId = itemId; break;
                case Scripts.Data.Items.EquipmentSlot.Amulet: AmuletId = itemId; break;
            }
        }
    }

    /// <summary>
    /// TRAININGSAVEDATA - Persisted learned skills per hero.
    /// </summary>
    [Serializable]
    public class TrainingSaveData
    {
        public List<HeroTrainingSave> Heroes = new List<HeroTrainingSave>();

        public TrainingSaveData() { }
        public TrainingSaveData(TrainingSaveData other)
        {
            Heroes = new List<HeroTrainingSave>();
            if (other.Heroes != null)
            {
                foreach (var h in other.Heroes)
                    Heroes.Add(new HeroTrainingSave(h));
            }
        }

        /// <summary>Gets or creates training entry for a hero.</summary>
        public HeroTrainingSave GetOrCreate(CharacterClass hero)
        {
            var existing = Heroes.Find(h => h.CharacterClass == hero);
            if (existing != null) return existing;
            var entry = new HeroTrainingSave { CharacterClass = hero };
            Heroes.Add(entry);
            return entry;
        }

        /// <summary>Returns true if hero has learned a specific training.</summary>
        public bool HasLearned(CharacterClass hero, string trainingId)
        {
            var entry = Heroes.Find(h => h.CharacterClass == hero);
            return entry != null && entry.LearnedTrainingIds.Contains(trainingId);
        }
    }

    /// <summary>Learned training IDs for a single hero.</summary>
    [Serializable]
    public class HeroTrainingSave
    {
        public CharacterClass CharacterClass;
        public List<string> LearnedTrainingIds = new List<string>();

        public HeroTrainingSave() { }
        public HeroTrainingSave(HeroTrainingSave other)
        {
            CharacterClass = other.CharacterClass;
            LearnedTrainingIds = new List<string>(other.LearnedTrainingIds);
        }
    }
}
