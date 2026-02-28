using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    /// <summary>
    /// MATERIALLIBRARY - Registry of shader materials.
    /// 
    /// PURPOSE:
    /// Lazy-loads and caches Material references for
    /// special rendering effects.
    /// 
    /// MATERIALS:
    /// - EnemyParallax: Parallax effect for enemies
    /// - PlayerParallax: Parallax effect for players
    /// - SpriteOutline: Outline shader for selection
    /// - SpritePan: Pan/scroll effect
    /// 
    /// USAGE:
    /// ```csharp
    /// var mat = MaterialLibrary.Materials["SpriteOutline"];
    /// renderer.material = mat;
    /// ```
    /// 
    /// RELATED FILES:
    /// - ActorParallax.cs: Uses parallax materials
    /// - Resources/Materials/: Material assets
    /// </summary>
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

            // Create SpritesDefault programmatically (replicates Unity's Sprites-Default)
            var spritesDefault = new Material(Shader.Find("Sprites/Default"));
            spritesDefault.name = "SpritesDefault";

            materials = new Dictionary<string, Material>
            {
                { "SpritesDefault", spritesDefault },
                { "SpriteUnlitDefault", new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")) },
                { "EnemyParallax", AssetHelper.LoadAsset<Material>("Materials/EnemyParallax") },
                { "PlayerParallax", AssetHelper.LoadAsset<Material>("Materials/PlayerParallax") },
                { "RadialFill", AssetHelper.LoadAsset<Material>("Materials/RadialFill") },
                { "SpriteOutline", AssetHelper.LoadAsset<Material>("Materials/SpriteOutline") },
                { "SpritePan",     AssetHelper.LoadAsset<Material>("Materials/SpritePan") }
            };
            isLoaded = true;
        }
    }
}
