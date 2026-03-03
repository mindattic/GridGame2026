using Scripts.Helpers;
using Scripts.Models;
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

namespace Scripts.Libraries
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

        /// <summary>Forces reload of materials on next access.</summary>
        public static void Reset()
        {
            isLoaded = false;
            materials = null;
        }

        /// <summary>Load.</summary>
        private static void Load()
        {
            if (isLoaded) return;

            // Create Sprites-Default material using the correct shader
            // This matches what Unity uses for default SpriteRenderers
            var spritesDefaultShader = Shader.Find("Sprites/Default");
            var spritesDefault = new Material(spritesDefaultShader);
            spritesDefault.name = "Sprites-Default"; // Match the exact name Unity uses

            // Create Sprite-Unlit-Default material for URP
            var spriteUnlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            var spriteUnlitDefault = new Material(spriteUnlitShader);
            spriteUnlitDefault.name = "Sprite-Unlit-Default"; // Match the exact name

            materials = new Dictionary<string, Material>
            {
                { "SpritesDefault", spritesDefault },
                { "SpriteUnlitDefault", spriteUnlitDefault },
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
