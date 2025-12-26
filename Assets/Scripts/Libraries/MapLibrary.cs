using Assets.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public class MapData
    {
        public string Name;
        public Sprite Terrain;
        public Sprite Surface;
        public Sprite Canopy;
    }

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
