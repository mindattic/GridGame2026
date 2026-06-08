using Scripts.Models;
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
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// COLORBLINDHELPER - Okabe-Ito colorblind-safe palette substitutions for mana orbs and debuff icons.
    ///
    /// <para>When <see cref="ProfileSettings.ColorblindMode"/> is true, red and green are replaced
    /// with Vermillion and Bluish-green (Okabe-Ito) — the pair most commonly confused by
    /// deuteranopes and protanopes. All other colors are unchanged.</para>
    ///
    /// <para>Call <see cref="GetManaColor"/> from <see cref="ManaOrbLine.ColorFor"/> and
    /// <see cref="GetDebuffColor"/> from <see cref="DebuffIconBar.ColorFor"/> so both systems
    /// share a single toggle.</para>
    /// </summary>
    public static class ColorblindHelper
    {
        // Okabe-Ito palette (2008) — the standard perceptually-distinct 8-color set.
        private static readonly Color Vermillion    = new Color(0.835f, 0.369f, 0.000f); // replaces Red
        private static readonly Color BluishGreen   = new Color(0.000f, 0.620f, 0.451f); // replaces Green (poisoned)
        private static readonly Color Orange        = new Color(0.902f, 0.624f, 0.000f); // replaces Green (protection — positive buff)

        private static bool IsOn => ProfileHelper.CurrentProfile?.Settings?.ColorblindMode ?? false;

        /// <summary>Returns the orb display color, applying the colorblind palette when enabled.</summary>
        public static Color GetManaColor(ManaType t)
        {
            if (IsOn)
            {
                switch (t)
                {
                    case ManaType.Red:   return Vermillion;
                    case ManaType.Green: return BluishGreen;
                }
            }
            // Fall through to the standard palette for all other types (and when mode is off).
            return ManaOrbLine.ColorForStandard(t);
        }

        /// <summary>Returns the debuff icon color, applying the colorblind palette when enabled.</summary>
        public static Color GetDebuffColor(string buffId)
        {
            if (IsOn)
            {
                switch (buffId)
                {
                    case "burning":    return Vermillion;  // was red
                    case "poisoned":   return BluishGreen; // was green
                    case "protection": return Orange;      // was green (positive buff — distinct from poisoned)
                }
            }
            return DebuffIconBar.ColorForStandard(buffId);
        }
    }
}
