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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// COLORHELPER - Color palette and conversion utilities.
    /// 
    /// PURPOSE:
    /// Provides a centralized color palette with named colors and
    /// utility methods for creating colors from RGB/RGBA values.
    /// 
    /// UTILITY METHODS:
    /// - RGB(r, g, b): Create color from 0-255 values
    /// - RGBA(r, g, b, a): Create color with alpha from 0-255 values
    /// 
    /// COLOR CATEGORIES:
    /// - Solid.*: Fully opaque general-purpose colors
    /// - HealthBar.*: Health bar gradient colors
    /// - ActionBar.*: Action point bar colors
    /// - Quality.*: Item/character quality tiers
    /// 
    /// USAGE:
    /// ```csharp
    /// sprite.color = ColorHelper.Solid.Red;
    /// bar.color = ColorHelper.HealthBar.Green;
    /// var custom = ColorHelper.RGB(128, 64, 255);
    /// ```
    /// 
    /// RELATED FILES:
    /// - ActorRenderers.cs: Uses color constants
    /// - HealthBarFactory.cs: Uses health bar colors
    /// - CombatTextFactory.cs: Uses damage text colors
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>Creates a Color from RGB values (0-255 range).</summary>
        public static Color RGB(float r, float g, float b)
        {
            return new Color(
                Mathf.Clamp(r, 0, 255) / 255f,
                Mathf.Clamp(g, 0, 255) / 255f,
                Mathf.Clamp(b, 0, 255) / 255f,
                1f);
        }

        /// <summary>Creates a Color from RGBA values (0-255 range).</summary>
        public static Color RGBA(float r, float g, float b, float a)
        {
            return new Color(
                Mathf.Clamp(r, 0, 255) / 255f,
                Mathf.Clamp(g, 0, 255) / 255f,
                Mathf.Clamp(b, 0, 255) / 255f,
                Mathf.Clamp(a, 0, 255) / 255f);
        }

        #region Solid Colors

        /// <summary>Fully opaque general-purpose color palette.</summary>
        public static class Solid
        {
            // Neutrals
            public static Color White = RGB(255, 255, 255);
            public static Color Black = RGB(0, 0, 0);
            public static Color Gray = RGB(128, 128, 128);
            public static Color LightGray = RGB(211, 211, 211);
            public static Color DarkGray = RGB(64, 64, 64);
            public static Color Silver = RGB(192, 192, 192);
            public static Color GunMetal = RGB(42, 52, 57);
            public static Color Gold = RGB(255, 223, 0);

            // Primary
            public static Color Red = RGB(255, 0, 0);
            public static Color Green = RGB(0, 255, 0);
            public static Color Blue = RGB(0, 122, 255);

            // Secondary
            public static Color Yellow = RGB(255, 255, 0);
            public static Color Cyan = RGB(0, 255, 255);
            public static Color Magenta = RGB(255, 0, 255);

            // Common UI accents
            public static Color Orange = RGB(255, 165, 0);
            public static Color DarkOrange = RGB(255, 140, 0);
            public static Color OrangeRed = RGB(255, 69, 0);

            // Blues
            public static Color LightBlue = RGB(128, 128, 255);
            public static Color RoyalBlue = RGB(65, 105, 225);
            public static Color DarkBlue = RGB(0, 51, 102);
            public static Color Navy = RGB(0, 0, 128);

            // Greens
            public static Color Lime = RGB(50, 205, 50);
            public static Color DarkGreen = RGB(0, 128, 0);
            public static Color Teal = RGB(0, 128, 128);

            // Reds/Pinks
            public static Color LightRed = RGB(255, 128, 128);
            public static Color Crimson = RGB(220, 20, 60);
            public static Color Pink = RGB(255, 105, 180);

            // Purples
            public static Color Purple = RGB(128, 0, 128);
            public static Color Violet = RGB(238, 130, 238);
            public static Color Indigo = RGB(75, 0, 130);

            // Browns
            public static Color Brown = RGB(165, 42, 42);
            public static Color Tan = RGB(210, 180, 140);
        }

        // Health bar color set (UI). Includes fills and helpful utility tints.
        public static class HealthBar
        {
            // Status colors
            public static Color Green = RGB(0, 255, 0);           // Safe/high HP
            public static Color Yellow = RGB(255, 255, 0);        // Caution/mid HP
            public static Color Red = RGB(255, 0, 0);             // Danger/low HP

            // Utility tints
            public static Color Heal = RGB(100, 255, 160);        // Healing feedback
            public static Color Damage = RGB(255, 80, 80);        // Damage flash
            public static Color Drain = RGB(220, 80, 80);         // Drain overlay
            public static Color Outline = Solid.White;            // Bar outline
            public static Color Back = RGBA(0, 0, 0, 128);        // Bar background
            public static Color Ghost = RGBA(255, 255, 255, 64);  // Delayed/ghost fill
        }

        // Action (AP) bar color set (UI). Tuned for readability.
        public static class ActionBar
        {
            public static Color Blue = RGB(0, 196, 255);          // Default fill
            public static Color Ready = RGB(0, 255, 180);         // Full/ready
            public static Color Cooldown = RGB(255, 80, 80);      // Cooling down
            public static Color Warning = RGB(255, 200, 0);       // Near threshold
            public static Color Yellow = Color.yellow;            // Accent
            public static Color Pink = RGB(100, 75, 80);          // Accent
            public static Color White = Color.white;              // Outline
            public static Color Back = RGBA(0, 0, 0, 128);        // Background
            public static Color Pulse = RGBA(0, 196, 255, 96);    // Pulse/animation overlay
        }

        // Translucent: semi-opaque variants (alpha ~50%). Good for overlays and fills.
        public static class Translucent
        {
            // Neutrals
            public static Color White = RGBA(255, 255, 255, 128);
            public static Color Black = RGBA(0, 0, 0, 128);
            public static Color DarkBlack = RGBA(0, 0, 0, 196);
            public static Color Gray = RGBA(128, 128, 128, 128);
            public static Color LightGray = RGBA(211, 211, 211, 128);
            public static Color DarkGray = RGBA(64, 64, 64, 128);
            public static Color Silver = RGBA(192, 192, 192, 128);
            public static Color GunMetal = RGBA(42, 52, 57, 128);
            public static Color Gold = RGBA(255, 223, 0, 128);

            // Primary/Secondary
            public static Color Red = RGBA(255, 0, 0, 128);
            public static Color Green = RGBA(0, 255, 0, 128);
            public static Color Blue = RGBA(0, 122, 255, 128);
            public static Color Yellow = RGBA(255, 255, 0, 128);
            public static Color Cyan = RGBA(0, 255, 255, 128);
            public static Color Magenta = RGBA(255, 0, 255, 128);

            // Accents
            public static Color Orange = RGBA(255, 165, 0, 128);
            public static Color DarkOrange = RGBA(255, 140, 0, 128);
            public static Color OrangeRed = RGBA(255, 69, 0, 128);

            // Blues
            public static Color LightBlue = RGBA(128, 128, 255, 128);
            public static Color RoyalBlue = RGBA(65, 105, 225, 128);
            public static Color DarkBlue = RGBA(0, 51, 102, 128);
            public static Color Navy = RGBA(0, 0, 128, 128);

            // Greens
            public static Color Lime = RGBA(50, 205, 50, 128);
            public static Color DarkGreen = RGBA(0, 128, 0, 128);
            public static Color Teal = RGBA(0, 128, 128, 128);

            // Reds/Pinks
            public static Color LightRed = RGBA(255, 128, 128, 128);
            public static Color Crimson = RGBA(220, 20, 60, 128);
            public static Color Pink = RGBA(255, 105, 180, 128);

            // Purples
            public static Color Purple = RGBA(128, 0, 128, 128);
            public static Color Violet = RGBA(238, 130, 238, 128);
            public static Color Indigo = RGBA(75, 0, 130, 128);

            // Browns
            public static Color Brown = RGBA(165, 42, 42, 128);
            public static Color Tan = RGBA(210, 180, 140, 128);
        }

        // Transparent: zero-alpha variants as convenient constants.
        public static class Transparent
        {
            public static Color White = RGBA(255, 255, 255, 0);
            public static Color Black = RGBA(0, 0, 0, 0);
            public static Color Red = RGBA(255, 0, 0, 0);
            public static Color Green = RGBA(0, 255, 0, 0);
            public static Color Blue = RGBA(0, 122, 255, 0);
            public static Color Yellow = RGBA(255, 255, 0, 0);
            public static Color Cyan = RGBA(0, 255, 255, 0);
            public static Color Magenta = RGBA(255, 0, 255, 0);
            public static Color Gold = RGBA(255, 223, 0, 0);
        }

        /// <summary>Tile overlay palette (board highlights). Alpha ~96 for subtle overlays.</summary>
        public static class Tile
        {
            public static Color White = RGBA(255, 255, 255, 96);
            public static Color Yellow = RGBA(255, 255, 0, 96);
            public static Color Blue = RGBA(0, 122, 255, 96);
            public static Color Green = RGBA(0, 255, 0, 96);
            public static Color Red = RGBA(255, 0, 0, 96);
            public static Color Orange = RGBA(255, 165, 0, 96);
            public static Color Cyan = RGBA(0, 255, 255, 96);
            public static Color Magenta = RGBA(255, 0, 255, 96);
            public static Color GunMetal = RGBA(42, 52, 57, 96);
        }

        #endregion
    }
}
