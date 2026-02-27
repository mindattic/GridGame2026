using Assets.Helpers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class FontLibrary
    {
        private static Dictionary<string, TMP_FontAsset> fonts;
        private static bool isLoaded = false;

        public static Dictionary<string, TMP_FontAsset> Fonts
        {
            get
            {
                if (!isLoaded)
                    Load();
                return fonts;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;

            fonts = new Dictionary<string, TMP_FontAsset>
            {
                // CombatText fonts
                { "Damage", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Attic") },
                { "Heal", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Attic") },
                { "GainExperience", AssetHelper.LoadAsset<TMP_FontAsset>("Fonts/Arial") },

                // UI fonts
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

        /// <summary>
        /// Retrieves a font asset by key.
        /// </summary>
        public static TMP_FontAsset Get(string key)
        {
            if (!isLoaded) Load();
            if (fonts.TryGetValue(key, out var font))
                return font;

            Debug.LogError($"Font '{key}' not found in FontRepo.");
            return null;
        }
    }
}
