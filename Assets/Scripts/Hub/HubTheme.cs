using UnityEngine;
using UnityEngine.UI;
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
    /// HUBTHEME - The game's single visual language: palette, button transitions, formatting.
    /// <para>PURPOSE: Single source of truth for the navy + gold JRPG aesthetic (FFBE-inspired:
    /// dark panels, thin steel borders, gold accents, clean text) used by EVERY scene — vendors,
    /// StageSelect, Hub, meta screens (Title/Settings/Profile/SaveFile/Credits/PostBattle), and
    /// the Bestiary. Builders consume it via Editor/Builders/UiKit.cs; runtime row factories
    /// consume it directly. Never hand-type a color that exists here.</para>
    /// <para>TYPOGRAPHY (see UiFonts.cs): Attic = display (scene titles, announcements);
    /// Outfit = body (buttons, rows, stats, descriptions).</para>
    /// <para>RELATED FILES: UiFonts.cs, Editor/Builders/UiKit.cs, HubItemRowFactory.cs,
    /// HubToast.cs, VendorManager.cs (representative consumer)</para>
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

        // Boxes & borders ("simple boxes": flat fill + thin steel border)
        public static readonly Color PanelBorder = new Color(0.32f, 0.40f, 0.58f, 1f);  // steel blue
        public static readonly Color ListBg      = new Color(0f, 0f, 0f, 0.35f);        // translucent list well

        // List rows (promoted from HubItemRowFactory / StageSelectManager literals)
        public static readonly Color RowBg       = new Color(0.20f, 0.24f, 0.34f, 1f);
        public static readonly Color RowSelected = new Color(0.36f, 0.50f, 0.78f, 1f);
        public static readonly Color RowLocked   = new Color(0.10f, 0.12f, 0.16f, 1f);

        // Scrollbar
        public static readonly Color ScrollTrack  = new Color(0f, 0f, 0f, 0.35f);
        public static readonly Color ScrollHandle = new Color(0.35f, 0.42f, 0.58f, 1f);

        /// <summary>The one ColorBlock every interactive button uses — subtle brighten on hover,
        /// darken on press, grey when disabled. (Was duplicated ad hoc in Alchemist/Party/Hub.)</summary>
        public static ColorBlock ButtonColors => new ColorBlock
        {
            normalColor      = Color.white,
            highlightedColor = new Color(1.15f, 1.15f, 1.20f, 1f),
            pressedColor     = new Color(0.65f, 0.65f, 0.80f, 1f),
            selectedColor    = new Color(1.00f, 1.00f, 1.10f, 1f),
            disabledColor    = new Color(0.55f, 0.55f, 0.55f, 0.60f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f,
        };

        /// <summary>Formats a gold amount: 1234 → "1,234g".</summary>
        public static string FormatGold(int gold) => $"{gold:N0}g";

        /// <summary>Returns "yes" in gold if affordable, "no" in red otherwise.</summary>
        public static string ColorByAffordable(string text, bool affordable)
            => affordable
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(Accent)}>{text}</color>"
                : $"<color=#{ColorUtility.ToHtmlStringRGB(Danger)}>{text}</color>";
    }
}
