using UnityEngine;
using TMPro;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Hub
{
    /// <summary>
    /// UIFONTS - The game's two-font typography system, loadable from runtime AND editor code.
    /// <para>PURPOSE: One place that answers "which font?" for every piece of UI:
    /// <list type="bullet">
    /// <item><b>Display (Attic)</b> — scene titles, headers, big announcements, gold text.</item>
    /// <item><b>Body (Outfit)</b> — buttons, list rows, stats, descriptions, everything readable.</item>
    /// </list>
    /// Runtime resolves through FontLibrary (Addressables: "Fonts/Attic" / "Fonts/Outfit");
    /// edit-mode (scene builders) resolves through AssetDatabase so builders and runtime
    /// factories produce identical text. Before this existed, runtime-created rows had no font
    /// set at all and fell back to LiberationSans — the root of the "every scene feels
    /// disconnected" problem.</para>
    /// <para>RELATED FILES: HubTheme.cs, FontLibrary.cs, Editor/Builders/UiKit.cs</para>
    /// </summary>
    public static class UiFonts
    {
        private static TMP_FontAsset attic;
        private static TMP_FontAsset outfit;

        /// <summary>Attic — the display face. Scene titles, headers, announcements.</summary>
        public static TMP_FontAsset Display => attic != null ? attic : attic = Load("Attic", "Assets/Fonts/Attic.asset");

        /// <summary>Outfit — the body face. Buttons, rows, stats, descriptions.</summary>
        public static TMP_FontAsset Body => outfit != null ? outfit : outfit = Load("Outfit", "Assets/Fonts/Outfit.asset");

        private static TMP_FontAsset Load(string libraryKey, string editorPath)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(editorPath);
#endif
            return FontLibrary.Get(libraryKey);
        }
    }
}
