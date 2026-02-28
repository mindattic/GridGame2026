using Assets.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    /// <summary>
    /// MAPDATA - Map layer sprites.
    /// 
    /// Contains terrain, surface, and canopy sprite layers
    /// for a single map.
    /// </summary>
    public class MapData
    {
        public string Name;
        public Sprite Terrain;
        public Sprite Surface;
        public Sprite Canopy;
    }

    /// <summary>
    /// MAPLIBRARY - Registry of map data.
    /// 
    /// PURPOSE:
    /// Lazy-loads and caches MapData for overworld maps
    /// including terrain, surface, and canopy layers.
    /// 
    /// USAGE:
    /// ```csharp
    /// var map = MapLibrary.Get(Map.GreenValley);
    /// terrainRenderer.sprite = map.Terrain;
    /// ```
    /// 
    /// RELATED FILES:
    /// - OverworldManager.cs: Uses map data
    /// - Map enum: Map identifiers
    /// </summary>
    public static class MapLibrary
    {
        private static Dictionary<string, MapData> maps;
        private static bool isLoaded = false;

        public static Dictionary<string, MapData> Maps
        {
            get
            {
                if (!isLoaded) Load();
                return maps;
            }
        }

        public static MapData Get(string name)
        {
            if (!isLoaded) Load();
            return maps.TryGetValue(name, out var data) ? data : null;
        }

        public static MapData Get(Map map)
        {
            if (!isLoaded) Load();
            var name = map.ToString();
            return maps.TryGetValue(name, out var data) ? data : null;
        }

        private static void Load()
        {
            if (isLoaded) return;
            maps = new Dictionary<string, MapData>();
            var test = Create(Map.Test);
            maps[test.Name] = test;
            var green = Create(Map.GreenValley);
            maps[green.Name] = green;
            isLoaded = true;
        }

        private static MapData Create(Map map)
        {
            string name = map.ToString();
            return new MapData
            {
                Name = name,
                Terrain = AssetHelper.LoadAsset<Sprite>($"Maps/{name}/Terrain"),
                Surface = AssetHelper.LoadAsset<Sprite>($"Maps/{name}/Surface"),
                Canopy = AssetHelper.LoadAsset<Sprite>($"Maps/{name}/Canopy")
            };
        }
    }
}
