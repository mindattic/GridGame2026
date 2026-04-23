using UnityEngine;
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
    /// HUBTHEME - Shared color palette and formatting helpers for the Hub UI.
    /// <para>PURPOSE: Single source of truth for the navy + gold JRPG aesthetic used by every
    /// HubSection. Keeps Shop, Blacksmith, Inn, etc. visually identical without each section
    /// inventing its own colors.</para>
    /// <para>RELATED FILES: HubManager.cs, HubSection.cs, HubItemRowFactory.cs</para>
    /// </summary>
    public static class HubTheme
    {
        // Core palette
        public static readonly Color PanelBg     = new Color(0.06f, 0.08f, 0.14f, 0.92f);
        public static readonly Color HeaderBg    = new Color(0.10f, 0.14f, 0.24f, 1f);
        public static readonly Color NavIdle     = new Color(0.14f, 0.18f, 0.28f, 1f);
        public static readonly Color NavActive   = new Color(0.28f, 0.42f, 0.70f, 1f);
        public static readonly Color NavHover    = new Color(0.22f, 0.30f, 0.48f, 1f);
        public static readonly Color Accent      = new Color(1f, 0.78f, 0.28f, 1f);  // gold
        public static readonly Color AccentDim   = new Color(0.80f, 0.62f, 0.22f, 1f);
        public static readonly Color TextLight   = Color.white;
        public static readonly Color TextMuted   = new Color(0.75f, 0.75f, 0.80f, 1f);
        public static readonly Color TextDim     = new Color(0.55f, 0.55f, 0.60f, 1f);
        public static readonly Color Danger      = new Color(0.90f, 0.35f, 0.35f, 1f);
        public static readonly Color Success     = new Color(0.40f, 0.85f, 0.45f, 1f);

        /// <summary>Formats a gold amount: 1234 → "1,234g".</summary>
        public static string FormatGold(int gold) => $"{gold:N0}g";

        /// <summary>Returns "yes" in gold if affordable, "no" in red otherwise.</summary>
        public static string ColorByAffordable(string text, bool affordable)
            => affordable
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(Accent)}>{text}</color>"
                : $"<color=#{ColorUtility.ToHtmlStringRGB(Danger)}>{text}</color>";
    }
}
