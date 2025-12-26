using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class PrefabLibrary
    {
        private static Dictionary<string, GameObject> prefabs;
        private static bool isLoaded = false;

        public static Dictionary<string, GameObject> Prefabs
        {
            get
            {
                if (!isLoaded)
                    Load();
                return prefabs;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;
            prefabs = new Dictionary<string, GameObject>
            {
                { "AbilityButtonPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/AbilityButtonPrefab") },
                { "ActorPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/ActorPrefab") },
                { "AttackLinePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/AttackLinePrefab") },
                { "CanvasParticlePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/CanvasParticlePrefab") },
                { "CoinPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/CoinPrefab") },
                { "ConfirmationDialogPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/ConfirmationDialogPrefab") },
                { "DestinationMarkerPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/DestinationMarkerPrefab") },
                { "CombatTextPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/CombatTextPrefab") },
                { "DottedLinePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/DottedLinePrefab") },
                { "FootstepPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/FootstepPrefab") },
                { "GhostPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/GhostPrefab") },
                { "KeyboardDialog", AssetHelper.LoadAsset<GameObject>("Prefabs/KeyboardDialog") },
                { "KeyButtonPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/KeyButtonPrefab") },
                { "MessageBoxPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/MessageBoxPrefab") },
                { "OverworldEncounterInstancePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/OverworldEncounterInstancePrefab") },
                { "Portrait2DPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/Portrait2DPrefab") },
                { "Portrait3DPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/Portrait3DPrefab") },
                { "RosterSlidePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/RosterSlidePrefab") },
                { "SaveFileButtonPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/SaveFileButtonPrefab") },
                { "ScreenWidthButtonPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/ScreenWidthButtonPrefab") },
                { "SynergyLinePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/SynergyLinePrefab") },
                { "SynergyStrandPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/SynergyStrandPrefab") },
                { "ProjectilePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/ProjectilePrefab") },
                { "StatRow", AssetHelper.LoadAsset<GameObject>("Prefabs/StatRow") },
                { "SupportLinePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/SupportLinePrefab") },
                { "TargetLinePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/TargetLinePrefab") },
                { "TilePrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/TilePrefab") },
                { "TooltipPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/TooltipPrefab") },
                { "HeroExperiencePane", AssetHelper.LoadAsset<GameObject>("Prefabs/HeroExperiencePane") },
                { "SettingSlider", AssetHelper.LoadAsset<GameObject>("Prefabs/SettingSlider") },
                { "SettingToggle", AssetHelper.LoadAsset<GameObject>("Prefabs/SettingToggle") },
                { "SettingDropdown", AssetHelper.LoadAsset<GameObject>("Prefabs/SettingDropdown") },
                { "PauseMenu", AssetHelper.LoadAsset<GameObject>("Prefabs/PauseMenu") },
                { "TimelineTagPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/TimelineTagPrefab") },
                { "TutorialPopup", AssetHelper.LoadAsset<GameObject>("Prefabs/TutorialPopup") },
            };
            isLoaded = true;
        }

        public static GameObject Get(string key)
        {
            if (!isLoaded) Load();
            if (prefabs.TryGetValue(key, out var prefab) && prefab != null)
                return prefab;
            Debug.LogError($"Prefab '{key}' not found or is null in PrefabRepo.");
            return null;
        }
    }
}
