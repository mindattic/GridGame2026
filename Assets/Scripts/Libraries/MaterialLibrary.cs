using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class MaterialLibrary
    {
        private static Dictionary<string, Material> materials;
        private static bool isLoaded = false;

        public static Dictionary<string, Material> Materials
        {
            get
            {
                if (!isLoaded)
                    Load();
                return materials;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;
            materials = new Dictionary<string, Material>
            {
                { "EnemyParallax", AssetHelper.LoadAsset<Material>("Materials/EnemyParallax") },
                { "PlayerParallax", AssetHelper.LoadAsset<Material>("Materials/PlayerParallax") },
                { "SpriteOutline", AssetHelper.LoadAsset<Material>("Materials/SpriteOutline") },
                { "SpritePan",     AssetHelper.LoadAsset<Material>("Materials/SpritePan") }
            };
            isLoaded = true;
        }
    }
}
