using Scripts.Helpers;
using System.Collections.Generic;
using TMPro;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Libraries
{
    /// <summary>
    /// FONTLIBRARY - Static registry for TextMeshPro font assets.
    /// 
    /// PURPOSE:
    /// Centralized font loading and caching. Fonts are loaded once on first
    /// access and cached for reuse across all UI components.
    /// 
    /// AVAILABLE FONTS:
    /// - Damage/Heal: Combat text fonts (Attic)
    /// - GainExperience: Experience gain text (Arial)
    /// - Attic: Stylized display font
    /// - Arial: Standard sans-serif
    /// - Avenir: Modern sans-serif (timeline)
    /// - Chicago: Retro pixel font (tooltips)
    /// - Consolas: Monospace (debug/console)
    /// - Roboto: Clean sans-serif
    /// - Segoe: Windows-style font
    /// 
    /// USAGE:
    /// ```csharp
    /// // Via dictionary access
    /// var font = FontLibrary.Fonts["Attic"];
    /// textMesh.font = font;
    /// 
    /// // Via Get method (with error logging)
    /// var font = FontLibrary.Get("Chicago");
    /// ```
    /// 
    /// FONT ASSIGNMENTS BY FACTORY:
    /// - CombatTextFactory: Attic
    /// - TimelineTagFactory: Avenir
    /// - TooltipFactory: Chicago
    /// - MessageBoxFactory: Attic
    /// 
    /// LLM CONTEXT:
    /// Use FontLibrary.Fonts["FontName"] in factories when creating
    /// TextMeshProUGUI components. Fonts are TMP_FontAsset objects.
    /// </summary>
    public static class FontLibrary
    {
        #region Fields

        private static Dictionary<string, TMP_FontAsset> fonts;
        private static bool isLoaded = false;

        #endregion

        #region Properties

        /// <summary>
        /// Dictionary of all loaded font assets. Lazy-loads on first access.
        /// </summary>
        public static Dictionary<string, TMP_FontAsset> Fonts
        {
            get
            {
                if (!isLoaded)
                    Load();
                return fonts;
            }
        }

        #endregion

        #region Loading

        /// <summary>
        /// Loads all font assets from Resources. Called lazily on first access.
        /// </summary>
        private static void Load()
        {
            if (isLoaded) return;

            fonts = new Dictionary<string, TMP_FontAsset>
            {
                // Combat text fonts (floating damage/heal numbers)
                { "Damage", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Attic") },
                { "Heal", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Attic") },
                { "GainExperience", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Arial") },

                // UI fonts (for factories and components)
                { "Attic", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Attic") },
                { "Arial", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Arial") },
                { "Avenir", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Avenir") },
                { "Chicago", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Chicago") },
                { "Consolas", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Consolas") },
                { "Roboto", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Roboto") },
                { "Segoe", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Segoe") },
            };

            isLoaded = true;
        }

        #endregion

        #region Access Methods

        /// <summary>
        /// Retrieves a font asset by key with error logging if not found.
        /// </summary>
        /// <param name="key">Font name key (e.g., "Attic", "Chicago")</param>
        /// <returns>TMP_FontAsset or null if not found</returns>
        public static TMP_FontAsset Get(string key)
        {
            if (!isLoaded) Load();
            if (fonts.TryGetValue(key, out var font))
                return font;

            Debug.LogError($"Font '{key}' not found in FontLibrary.");
            return null;
        }

        #endregion
    }
}
