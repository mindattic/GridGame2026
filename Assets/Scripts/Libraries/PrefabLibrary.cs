using Assets.Helpers;
using Assets.Scripts.Factories;
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
                { "ActorPrefab", AssetHelper.LoadAsset<GameObject>("Prefabs/ActorPrefab") },
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
