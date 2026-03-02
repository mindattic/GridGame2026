using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
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

namespace Scripts.Libraries
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
    /// CONVERTED TO FACTORIES:
    /// - ActorPrefab → ActorFactory.Create()
    /// - TilePrefab → TileFactory.Create()
    /// - CoinPrefab → CoinFactory.Create()
    /// - etc.
    /// 
    /// USAGE:
    /// ```csharp
    /// var prefab = PrefabLibrary.Prefabs["SomePrefab"];
    /// var instance = Instantiate(prefab, parent);
    /// ```
    /// 
    /// FACTORY PATTERN PREFERENCE:
    /// Prefer creating factories for new GameObject types.
    /// See ActorFactory.cs, TileFactory.cs for examples.
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
