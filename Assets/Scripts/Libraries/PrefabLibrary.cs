using Assets.Helpers;
using Assets.Scripts.Factories;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    /// <summary>
    /// PREFABLIBRARY - Static registry for Unity prefabs.
    /// 
    /// PURPOSE:
    /// Provides centralized access to Unity prefab assets that still require
    /// traditional prefab instantiation (not factory-created).
    /// 
    /// NOTE ON FACTORIES:
    /// Most GameObjects in this project are created via Factories (code-driven)
    /// rather than prefabs. PrefabLibrary exists only for:
    /// - Complex prefabs with many nested components
    /// - Third-party prefabs
    /// - Legacy prefabs not yet converted to factories
    /// 
    /// CURRENT PREFABS:
    /// - "ActorPrefab": Actor GameObject with all child components
    ///   (Too complex to create via factory currently)
    /// 
    /// USAGE:
    /// ```csharp
    /// var prefab = PrefabLibrary.Prefabs["ActorPrefab"];
    /// var instance = Instantiate(prefab, parent);
    /// 
    /// // Or use Get() method with error logging:
    /// var prefab = PrefabLibrary.Get("ActorPrefab");
    /// ```
    /// 
    /// LOADING:
    /// Uses AssetHelper.LoadAsset<GameObject>() to load from Resources.
    /// Lazy-loaded on first access.
    /// 
    /// FACTORY PATTERN PREFERENCE:
    /// Prefer creating factories for new GameObject types.
    /// See TileFactory.cs, CombatTextFactory.cs for examples.
    /// 
    /// RELATED FILES:
    /// - All *Factory.cs files: Code-driven GameObject creation
    /// - AssetHelper.cs: Asset loading utilities
    /// </summary>
    public static class PrefabLibrary
    {
        private static Dictionary<string, GameObject> prefabs;
        private static bool isLoaded = false;

        /// <summary>Dictionary of all loaded prefabs. Lazy-loads on first access.</summary>
        public static Dictionary<string, GameObject> Prefabs
        {
            get
            {
                if (!isLoaded)
                    Load();
                return prefabs;
            }
        }

        /// <summary>Loads all prefabs from Resources.</summary>
        private static void Load()
        {
            if (isLoaded) return;
            prefabs = new Dictionary<string, GameObject>
            {
                { "ActorPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/ActorPrefab") },
            };
            isLoaded = true;
        }

        /// <summary>
        /// Gets a prefab by key with error logging if not found.
        /// </summary>
        public static GameObject Get(string key)
        {
            if (!isLoaded) Load();
            if (prefabs.TryGetValue(key, out var prefab) && prefab != null)
                return prefab;
            Debug.LogError($"Prefab '{key}' not found or is null in PrefabLibrary.");
            return null;
        }
    }
}
