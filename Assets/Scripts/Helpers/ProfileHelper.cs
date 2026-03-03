using Scripts.Libraries;
using Scripts.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using g = Scripts.Helpers.GameHelper;
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

namespace Scripts.Helpers
{
    /// <summary>
    /// PROFILEHELPER - Player profile and save data management.
    /// 
    /// PURPOSE:
    /// Manages player profiles, save files, settings, and game progression.
    /// Handles persistence to disk via JSON serialization.
    /// 
    /// DATA HIERARCHY:
    /// ```
    /// Profiles/
    /// ├── Profile1/
    /// │   ├── Settings.json     ← Player preferences
    /// │   └── Saves/
    /// │       ├── Save001.json  ← Save slot 1
    /// │       ├── Save002.json  ← Save slot 2
    /// │       └── Save003.json  ← Save slot 3
    /// └── Profile2/
    ///     └── ...
    /// ```
    /// 
    /// KEY PROPERTIES:
    /// - Profiles: All player profiles
    /// - CurrentProfile: Active profile
    /// - CurrentSave: Active save within profile
    /// - Settings: Player preferences
    /// 
    /// KEY METHODS:
    /// - SelectProfile(key): Sets active profile
    /// - Save(force): Persists current state to disk
    /// - Load(): Loads profile from disk
    /// - HasProfiles(): Checks if any profiles exist
    /// 
    /// DEFAULT DATA:
    /// Provides default values for new profiles including
    /// starter roster, party composition, and settings.
    /// 
    /// RELATED FILES:
    /// - Profile.cs: Profile data model
    /// - SaveState.cs: Save file data model
    /// - ProfileSettings.cs: Settings data model
    /// - ProfileSelectManager.cs: Profile selection UI
    /// - SaveFileSelectManager.cs: Save slot selection UI
    /// </summary>
    public static class ProfileHelper
    {
        #region Default Values

        public const string SettingsFileName = "Settings.json";

        /// <summary>Default settings for new profiles.</summary>
        public static ProfileSettings DefaultSettings = new ProfileSettings()
        {
            ActorPanMultiplier = 0.05f,
            GameSpeed = 1.0f,
            DragSensitivity = 0.05f,
            CoinCountMultiplier = 0.05f,
            ApplyMovementTilt = false,
            ReloadThumbnailSettings = false
        };

        /// <summary>Default global save data.</summary>
        public static GlobalSaveData DefaultGlobal = new GlobalSaveData()
        {
            TotalCoins = 0,
        };

        /// <summary>Default stage progress.</summary>
        public static StageSaveData DefaultStage = new StageSaveData()
        {
            CurrentStage = RNG.Stage(Map.Test),
            CurrentWave = 0,
        };

        /// <summary>Default roster (all unlocked heroes).</summary>
        public static RosterSaveData DefaultRoster = new RosterSaveData()
        {
            Members = new List<CharacterLevelPair>()
            {
                new CharacterLevelPair(CharacterClass.Paladin),
                new CharacterLevelPair(CharacterClass.Barbarian),
                new CharacterLevelPair(CharacterClass.Cleric),
                new CharacterLevelPair(CharacterClass.GreenNinja),
                new CharacterLevelPair(CharacterClass.Pugilist),
                new CharacterLevelPair(CharacterClass.RedNinja),
                new CharacterLevelPair(CharacterClass.Ronin),
                new CharacterLevelPair(CharacterClass.Sellsword),
                new CharacterLevelPair(CharacterClass.Thief),
                new CharacterLevelPair(CharacterClass.Vampire),
            }
        };

        /// <summary>Default starting party composition.</summary>
        public static PartySaveData DefaultParty = new PartySaveData()
        {
            Members = new List<CharacterLevelPair>()
            {
                new CharacterLevelPair(CharacterClass.Paladin),
                new CharacterLevelPair(CharacterClass.Barbarian),
                new CharacterLevelPair(CharacterClass.Cleric),
            }
        };

        public static OverworldSaveData DefaultOverworld = new OverworldSaveData
        {
            MapName = Map.Test.ToString(),
            HeroX = 0,
            HeroY = 0,
            HeroDirection = "Idle"
        };

        // ---------------------------------------------------------------------
        // Backing Fields
        // ---------------------------------------------------------------------

        private static List<string> folders = new List<string>();
        private static Dictionary<string, Profile> profiles = new Dictionary<string, Profile>();
        private static string currentProfileKey;

        // ---------------------------------------------------------------------
        // Public Properties
        // ---------------------------------------------------------------------

        // True if any profile folders were discovered.
        public static bool HasFolders => folders.Any();

        // True if a current profile key is set and exists in the profiles map.
        public static bool HasCurrentProfile =>
            HasProfiles() &&
            !string.IsNullOrWhiteSpace(currentProfileKey) &&
            profiles.ContainsKey(currentProfileKey);

        // True if there is a current profile and it has a selected save.
        public static bool HasCurrentSave =>
            HasCurrentProfile &&
            CurrentProfile.HasSaves &&
            CurrentProfile.CurrentSave != null;

        // The active profile or null if not available.
        public static Profile CurrentProfile => HasCurrentProfile ? profiles[currentProfileKey] : null;

        // All loaded profiles keyed by profile name.
        public static Dictionary<string, Profile> Profiles => profiles;

        // Convenience: get/set Overworld save from current save
        public static OverworldSaveData Overworld
        {
            get => CurrentProfile?.CurrentSave?.Overworld ?? DefaultOverworld;
            set
            {
                if (!HasCurrentSave) return;
                CurrentProfile.CurrentSave.Overworld = value ?? new OverworldSaveData(DefaultOverworld);
            }
        }

        // ---------------------------------------------------------------------
        // Load and Presence Checks
        // ---------------------------------------------------------------------

        /// <summary>
        /// Returns true if at least one profile exists.
        /// Attempts to load profiles if not already in memory.
        /// If none are found, navigates to ProfileCreate and returns false.
        /// </summary>
        public static bool HasProfiles()
        {
            if (profiles.Any())
                return true;

            Load();

            if (profiles.Any())
                return true;

            // No profiles available. Send the user to create one.
            SceneManager.LoadScene(SceneHelper.ProfileCreate);
            return false;
        }

        /// <summary>
        /// Initializes the profiles list from disk.
        /// Ensures the Profiles folder exists. Loads each profile folder found.
        /// Sets a default current profile if none is chosen.
        /// </summary>
        public static bool Load()
        {
            try
            {
                if (!Directory.Exists(FolderHelper.Folder.Profiles))
                {
                    Debug.LogWarning($"[ProfileHelper] Profiles folder not found. Creating: {FolderHelper.Folder.Profiles}");
                    Directory.CreateDirectory(FolderHelper.Folder.Profiles);
                }

                folders = Directory.GetDirectories(FolderHelper.Folder.Profiles).ToList();
                profiles.Clear();

                foreach (var folder in folders)
                {
                    string key = new DirectoryInfo(folder).Name;
                    var profile = GetProfile(key);
                    if (profile != null)
                    {
                        profiles[key] = profile;

                        // Ensure a current save exists for each loaded profile
                        EnsureCurrentSave(profile);
                    }
                    else
                    {
                        Debug.LogWarning($"[ProfileHelper] Failed to load profile: {key}");
                    }
                }

                if (!profiles.Any())
                {
                    Debug.LogWarning("[ProfileHelper] No valid profiles loaded.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(currentProfileKey) || !profiles.ContainsKey(currentProfileKey))
                    currentProfileKey = profiles.Keys.First();

                // Also ensure current save on the selected profile
                EnsureCurrentSave(CurrentProfile);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] Load failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reloads profiles from disk. Simple wrapper for Load.
        /// </summary>
        public static void Reload()
        {
            Load();
        }

        // ---------------------------------------------------------------------
        // Create / Delete / Select
        // ---------------------------------------------------------------------

        /// <summary>
        /// Creates a new profile with an initial save and default settings.
        /// Returns the profile key.
        /// </summary>
        public static string CreateProfile(string value)
        {
            // Derive a usable key. If the input is empty, use a GUID.
            string baseKey = !string.IsNullOrWhiteSpace(value) ? value.Trim() : $"{Guid.NewGuid():N}";
            string key = baseKey;
            string folder = Path.Combine(FolderHelper.Folder.Profiles, key);

            // Ensure uniqueness by appending a numeric suffix if needed.
            int i = 1;
            while (Directory.Exists(folder))
            {
                key = $"{baseKey}{i:D3}";
                folder = Path.Combine(FolderHelper.Folder.Profiles, key);
                i++;
            }

            try
            {
                Directory.CreateDirectory(folder);

                var newProfile = new Profile
                {
                    Key = key,
                    Folder = folder,
                    SaveStates = new List<SaveState>()
                };

                // Create an initial save using defaults.
                var newSave = new SaveState(
                    1,
                    DateTime.UtcNow,
                    new GlobalSaveData(ProfileHelper.DefaultGlobal),
                    new StageSaveData(ProfileHelper.DefaultStage),
                    new RosterSaveData(ProfileHelper.DefaultRoster),
                    new PartySaveData(ProfileHelper.DefaultParty),
                    new OverworldSaveData(DefaultOverworld)
                );

                newProfile.SaveStates.Add(newSave);
                profiles[key] = newProfile;

                // Select the new profile.
                SelectProfile(key);

                // Persist save to disk.
                string savesPath = Path.Combine(newProfile.Folder, "Saves");
                Directory.CreateDirectory(savesPath);
                File.WriteAllText(Path.Combine(savesPath, newSave.FileName), JsonConvert.SerializeObject(newSave, Formatting.Indented));

                // Load or create settings for the profile.
                newProfile.Settings = LoadOrCreateSettings(newProfile);

                return key;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] CreateProfile failed for '{key}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a profile by key. Removes from disk and memory.
        /// Returns true if removed.
        /// </summary>
        public static bool DeleteProfile(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (!profiles.ContainsKey(key))
                return false;

            try
            {
                string folder = Path.Combine(FolderHelper.Folder.Profiles, key);
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);

                profiles.Remove(key);

                if (currentProfileKey == key)
                    currentProfileKey = profiles.Keys.FirstOrDefault();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] DeleteProfile failed for '{key}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Selects an existing profile by key. Returns true if successful.
        /// </summary>
        public static bool SelectProfile(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (!profiles.ContainsKey(key))
                return false;

            currentProfileKey = key;
            return true;
        }

        // ---------------------------------------------------------------------
        // Profile Load
        // ---------------------------------------------------------------------

        /// <summary>
        /// Builds a Profile object from disk for the given key.
        /// Returns null if the folder is missing or the load fails.
        /// </summary>
        public static Profile GetProfile(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            string folder = Path.Combine(FolderHelper.Folder.Profiles, key);
            if (!Directory.Exists(folder))
                return null;

            var profile = new Profile
            {
                Key = key,
                Folder = folder,
                SaveStates = new List<SaveState>()
            };

            try
            {
                string savesPath = Path.Combine(folder, "Saves");
                if (Directory.Exists(savesPath))
                {
                    var saveFiles = Directory.GetFiles(savesPath, "*.json");
                    foreach (var file in saveFiles)
                    {
                        try
                        {
                            var text = File.ReadAllText(file);
                            if (string.IsNullOrWhiteSpace(text))
                                continue;

                            var save = JsonConvert.DeserializeObject<SaveState>(text);
                            if (save != null)
                                profile.SaveStates.Add(save);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error reading save '{file}': {ex.Message}");
                        }
                    }

                    // Most recent first.
                    profile.SaveStates = profile.SaveStates.OrderByDescending(x => x.Timestamp).ToList();
                }

                // Load settings or fall back to defaults.
                profile.Settings = LoadOrCreateSettings(profile);

                // Ensure a current save exists/selected
                EnsureCurrentSave(profile);

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] GetProfile failed for '{key}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ensures the profile has a valid CurrentSave. Creates and persists a default one if missing.
        /// </summary>
        public static void EnsureCurrentSave(Profile profile)
        {
            if (profile == null) return;

            if (profile.SaveStates == null)
                profile.SaveStates = new List<SaveState>();

            if (profile.SaveStates.Any())
            {
                if (profile.CurrentSave == null)
                    profile.CurrentSave = profile.LatestSave;
                // Ensure Overworld exists
                if (profile.CurrentSave.Overworld == null)
                    profile.CurrentSave.Overworld = new OverworldSaveData(DefaultOverworld);
                return;
            }

            // Create and persist a default save
            try
            {
                var newSave = new SaveState(
                    1,
                    DateTime.UtcNow,
                    new GlobalSaveData(DefaultGlobal),
                    new StageSaveData(DefaultStage),
                    new RosterSaveData(DefaultRoster),
                    new PartySaveData(DefaultParty),
                    new OverworldSaveData(DefaultOverworld));

                profile.SaveStates.Add(newSave);
                profile.CurrentSave = newSave;

                string savesDir = Path.Combine(profile.Folder, "Saves");
                Directory.CreateDirectory(savesDir);
                File.WriteAllText(Path.Combine(savesDir, newSave.FileName), JsonConvert.SerializeObject(newSave, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] EnsureCurrentSave failed for '{profile?.Key}': {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a snapshot of the current save without changing CurrentSave.
        /// The snapshot is written as AutoSave_yyyyMMdd_HHmmss.json and old snapshots are pruned.
        /// </summary>
        public static bool AutoSave(string reason = null, int maxSnapshots = 10)
        {
            if (!HasCurrentSave) return false;

            try
            {
                var profile = CurrentProfile;
                var src = profile.CurrentSave;
                var snapshot = new SaveState(
                    profile.SaveStates.Count + 1,
                    DateTime.UtcNow,
                    new GlobalSaveData(src.Global),
                    new StageSaveData(src.Stage),
                    new RosterSaveData(src.Roster),
                    new PartySaveData(src.Party),
                    new OverworldSaveData(src.Overworld ?? DefaultOverworld));

                string savesDir = Path.Combine(profile.Folder, "Saves");
                Directory.CreateDirectory(savesDir);

                string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string filename = $"AutoSave_{stamp}.json";
                string path = Path.Combine(savesDir, filename);

                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, Formatting.Indented));

                // Optionally keep a record in memory (not required for functionality)
                profile.SaveStates.Insert(0, snapshot);

                // Prune old autosaves
                var autos = Directory.GetFiles(savesDir, "AutoSave_*.json")
                                     .OrderByDescending(f => f)
                                     .ToList();
                foreach (var old in autos.Skip(maxSnapshots))
                {
                    try { File.Delete(old); } catch { /* ignore */ }
                }

                if (!string.IsNullOrEmpty(reason))
                    Debug.Log($"[ProfileHelper] AutoSave created: {filename} ({reason})");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] AutoSave failed: {ex.Message}");
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // Settings
        // ---------------------------------------------------------------------

        // Renamed from LoadSettings to avoid overload conflict with the public void LoadSettings method.
        /// <summary>Load or create settings.</summary>
        private static ProfileSettings LoadOrCreateSettings(Profile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Folder))
                return ProfileHelper.DefaultSettings;

            try
            {
                string path = Path.Combine(profile.Folder, ProfileHelper.SettingsFileName);
                if (File.Exists(path))
                {
                    var text = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(text))
                        return JsonConvert.DeserializeObject<ProfileSettings>(text) ?? ProfileHelper.DefaultSettings;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load settings for '{profile?.Key}': {ex.Message}");
            }

            // Persist defaults if missing or failed.
            SaveSettings(profile, ProfileHelper.DefaultSettings);
            return ProfileHelper.DefaultSettings;
        }

        /// <summary>Save settings.</summary>
        private static void SaveSettings(Profile profile, ProfileSettings settings)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Folder) || settings == null)
                return;

            try
            {
                string path = Path.Combine(profile.Folder, ProfileHelper.SettingsFileName);
                File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save settings for '{profile?.Key}': {ex.Message}");
            }
        }

        /// <summary>Ensure settings.</summary>
        public static void EnsureSettings(Profile profile)
        {
            if (profile == null) return;
            if (profile.Settings == null)
            {
                profile.Settings = new ProfileSettings(ProfileHelper.DefaultSettings);
                SaveSettings(profile);
            }
        }

        /// <summary>Gets the settings path.</summary>
        public static string GetSettingsPath(Profile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Folder)) return null;
            return Path.Combine(profile.Folder, SettingsFileName);
        }

        /// <summary>Load settings.</summary>
        public static void LoadSettings(Profile profile = null)
        {
            profile ??= CurrentProfile;
            if (profile == null) return;
            var path = GetSettingsPath(profile);
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var loaded = JsonConvert.DeserializeObject<ProfileSettings>(json);
                        if (loaded != null)
                            profile.Settings = loaded;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load settings: {ex.Message}");
            }
            EnsureSettings(profile);
        }

        /// <summary>Save settings.</summary>
        public static void SaveSettings(Profile profile = null)
        {
            profile ??= CurrentProfile;
            if (profile == null) return;
            EnsureSettings(profile);
            var path = GetSettingsPath(profile);
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var json = JsonConvert.SerializeObject(profile.Settings, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save settings: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------------
        // Save Management
        // ---------------------------------------------------------------------

        /// <summary>Save.</summary>
        public static bool Save(bool overwrite = true)
        {
            return overwrite ? OverwriteSave() : CreateSave();
        }

        /// <summary>Creates the save.</summary>
        private static bool CreateSave()
        {
            if (!HasCurrentSave)
                return false;

            try
            {
                var newSave = new SaveState(
                    CurrentProfile.SaveStates.Count,
                    DateTime.UtcNow,
                    CurrentProfile.CurrentSave.Global,
                    CurrentProfile.CurrentSave.Stage,
                    CurrentProfile.CurrentSave.Roster,
                    CurrentProfile.CurrentSave.Party,
                    CurrentProfile.CurrentSave.Overworld ?? new OverworldSaveData(DefaultOverworld));

                string savesDir = Path.Combine(CurrentProfile.Folder, "Saves");
                Directory.CreateDirectory(savesDir);

                string path = Path.Combine(savesDir, newSave.FileName);
                File.WriteAllText(path, JsonConvert.SerializeObject(newSave, Formatting.Indented));

                CurrentProfile.SaveStates.Add(newSave);
                CurrentProfile.CurrentSave = newSave;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] CreateSave failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Overwrite save.</summary>
        private static bool OverwriteSave()
        {
            if (!HasCurrentSave)
                return false;

            try
            {
                var existingSave = new SaveState(CurrentProfile.CurrentSave);

                string savesDir = Path.Combine(CurrentProfile.Folder, "Saves");
                Directory.CreateDirectory(savesDir);

                string path = Path.Combine(savesDir, existingSave.FileName);
                File.WriteAllText(path, JsonConvert.SerializeObject(existingSave, Formatting.Indented));

                CurrentProfile.SaveStates.Remove(CurrentProfile.CurrentSave);
                CurrentProfile.SaveStates.Insert(0, existingSave);
                CurrentProfile.CurrentSave = existingSave;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileHelper] OverwriteSave failed: {ex.Message}");
                return false;
            }
        }


        // ---------------------------------------------------------------------
        // Party Management
        // ---------------------------------------------------------------------

        /// <summary>
        /// Adds a character to the party if not already present. Saves on success.
        /// </summary>
        public static void AddToParty(CharacterClass characterClass)
        {
            if (!HasCurrentSave || characterClass == CharacterClass.None)
                return;

            var party = CurrentProfile.CurrentSave.Party?.Members;
            if (party == null)
                return;

            if (party.Any(hero => hero.CharacterClass == characterClass ))
                return;

            party.Add(new CharacterLevelPair(characterClass));
            Save(true);
        }

        /// <summary>
        /// Removes a character from the party if present. Saves on success.
        /// </summary>
        public static void RemoveFromParty(CharacterClass characterClass)
        {
            if (!HasCurrentSave || characterClass == CharacterClass.None)
                return;

            var party = CurrentProfile.CurrentSave.Party?.Members;
            if (party == null)
                return;

            if (party.RemoveAll(hero => hero.CharacterClass == characterClass) > 0)
                Save(true);
        }

        // ---------------------------------------------------------------------
        // Overworld helpers
        // ---------------------------------------------------------------------
        /// <summary>Save overworld position.</summary>
        public static void SaveOverworldPosition(Vector2 heroLocal, string mapName, string facing)
        {
            if (!HasCurrentSave) return;
            var ow = CurrentProfile.CurrentSave.Overworld ?? new OverworldSaveData(DefaultOverworld);
            ow.MapName = string.IsNullOrWhiteSpace(mapName) ? ow.MapName : mapName;
            ow.HeroX = heroLocal.x;
            ow.HeroY = heroLocal.y;
            if (!string.IsNullOrEmpty(facing)) ow.HeroDirection = facing;
            CurrentProfile.CurrentSave.Overworld = ow;
            Save(true);
        }

        /// <summary>Gets the overworld.</summary>
        public static OverworldSaveData GetOverworld() => Overworld;

        #endregion
    }
}
