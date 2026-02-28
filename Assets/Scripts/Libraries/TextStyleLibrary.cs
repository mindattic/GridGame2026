using Assets.Helper;
using Assets.Scripts.Models;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    /// <summary>
    /// TEXTSTYLELIBRARY - Registry of combat text styles.
    /// 
    /// PURPOSE:
    /// Defines text styles for floating combat text including
    /// font, size, color, and animation type.
    /// 
    /// STYLES:
    /// - Damage: White, float animation
    /// - Heal: Green, float animation
    /// - CriticalHit: Yellow, bounce animation
    /// - Miss: Gray, float animation
    /// - LevelUp: Cyan, bounce animation
    /// 
    /// USAGE:
    /// ```csharp
    /// var style = TextStyleLibrary.Get("Damage");
    /// CombatTextManager.Spawn("-42", pos, style);
    /// ```
    /// 
    /// RELATED FILES:
    /// - TextStyle.cs: Style data structure
    /// - CombatTextManager.cs: Text spawning
    /// - CombatTextInstance.cs: Text animation
    /// </summary>
    public static class TextStyleLibrary
    {
        private static Dictionary<string, TextStyle> textStyles;
        private static bool isLoaded = false;

        public static Dictionary<string, TextStyle> TextStyles
        {
            get
            {
                if (!isLoaded)
                    Load();
                return textStyles;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;
            textStyles = new Dictionary<string, TextStyle>
            {
                { "Damage", new TextStyle("Damage", FontLibrary.Get("Damage"), 24, ColorHelper.Solid.White, TextMotion.Float) },
                { "Heal", new TextStyle("Heal", FontLibrary.Get("Heal"), 24, ColorHelper.Solid.Green, TextMotion.Float) },
                { "CriticalHit", new TextStyle("CriticalHit", FontLibrary.Get("Damage"), 32, ColorHelper.Solid.Yellow, TextMotion.Bounce) },
                { "GlancingBlow", new TextStyle("GlancingBlow", FontLibrary.Get("Damage"), 24, ColorHelper.Solid.Gray, TextMotion.Float) },
                { "LevelUp", new TextStyle("LevelUp", FontLibrary.Get("GainExperience"), 40, ColorHelper.Solid.Cyan, TextMotion.Bounce) },
                { "GainExperience", new TextStyle("GainExperience", FontLibrary.Get("GainExperience"), 12, ColorHelper.Solid.White, TextMotion.Float) },
                { "Miss", new TextStyle("Miss", FontLibrary.Get("Damage"), 24, ColorHelper.Solid.Gray, TextMotion.Float) },
            };
            isLoaded = true;
        }

        /// <summary>
        /// Retrieves a single text style by key.
        /// </summary>
        public static TextStyle Get(string key)
        {
            if (!isLoaded) Load();
            if (textStyles.TryGetValue(key, out var entry))
                return entry;

            Debug.LogError($"Floating Text '{key}' not found in TextStyleRepo.");
            return null;
        }
    }
}
