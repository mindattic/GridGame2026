using UnityEngine;
using c = Scripts.Helpers.CanvasHelper;
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
    /// SCREENMETRICS - Unified screen-relative sizing system.
    ///
    /// PURPOSE:
    /// Provides a single, consistent API for sizing any element relative
    /// to the current screen/canvas dimensions. Guarantees identical visual
    /// proportions across all screen sizes and aspect ratios.
    ///
    /// DESIGN PHILOSOPHY:
    /// Instead of hardcoding pixel values (128px, 64px, 48f font) that
    /// only look correct on one resolution, express every measurement as
    /// a fraction of a reference dimension:
    ///
    ///   "This button should be 8% of screen height."
    ///   "This font should be 3% of the shorter axis."
    ///   "This icon should be 1 tile-width wide."
    ///
    /// The system adapts automatically — no per-device tuning needed.
    ///
    /// COORDINATE SPACES:
    /// ```
    /// ┌─────────────────────────────────────────────┐
    /// │              DEVICE SCREEN                   │
    /// │  pixels: 2400 x 1080 (varies per device)    │
    /// │                                             │
    /// │  ┌─────────────────────────────────────┐    │
    /// │  │         CANVAS RECT                 │    │
    /// │  │  units: scaled by CanvasScaler      │    │
    /// │  │                                     │    │
    /// │  │  ┌─────────────────────────────┐    │    │
    /// │  │  │      SAFE AREA              │    │    │
    /// │  │  │  excludes notch/cutout      │    │    │
    /// │  │  └─────────────────────────────┘    │    │
    /// │  └─────────────────────────────────────┘    │
    /// │                                             │
    /// │  ┌─────────────────────────────────────┐    │
    /// │  │     WORLD VISIBLE RECT              │    │
    /// │  │  orthographic camera extents        │    │
    /// │  └─────────────────────────────────────┘    │
    /// └─────────────────────────────────────────────┘
    /// ```
    ///
    /// MEASUREMENT AXES:
    /// - Width:  Horizontal canvas dimension
    /// - Height: Vertical canvas dimension
    /// - Min:    Shorter of width/height (aspect-safe)
    /// - Max:    Longer of width/height
    /// - Diag:   Diagonal (rarely used, but available)
    ///
    /// CHOOSING AN AXIS:
    /// ┌──────────────────────┬──────────────────────────────────────┐
    /// │ Use Case             │ Recommended Axis                     │
    /// ├──────────────────────┼──────────────────────────────────────┤
    /// │ Font sizes           │ Height (or Min for safety)           │
    /// │ Button heights       │ Height                               │
    /// │ Button widths        │ Width                                │
    /// │ Icons / squares      │ Min (stays square on all ratios)     │
    /// │ Padding / margins    │ Min                                  │
    /// │ Full-width bars      │ Width                                │
    /// │ Board-relative       │ Tile (uses g.TileSize)               │
    /// └──────────────────────┴──────────────────────────────────────┘
    ///
    /// USAGE:
    /// ```csharp
    /// // Font that is 3% of screen height
    /// label.fontSize = ScreenMetrics.Font(0.03f);
    ///
    /// // Button that is 80% wide, 6% tall
    /// rect.sizeDelta = new Vector2(
    ///     ScreenMetrics.W(0.80f),
    ///     ScreenMetrics.H(0.06f));
    ///
    /// // Square icon that is 5% of the shorter axis
    /// float side = ScreenMetrics.Min(0.05f);
    /// icon.sizeDelta = new Vector2(side, side);
    ///
    /// // Padding that is 1% of shorter axis
    /// float pad = ScreenMetrics.Min(0.01f);
    ///
    /// // Half a tile width (world space)
    /// float halfTile = ScreenMetrics.Tile(0.5f);
    ///
    /// // World-space size: 10% of visible height
    /// float worldH = ScreenMetrics.WorldH(0.10f);
    /// ```
    ///
    /// SAFE AREA:
    /// ```csharp
    /// // Insets for notch/cutout (canvas units)
    /// float topInset = ScreenMetrics.SafeInsetTop;
    /// float leftInset = ScreenMetrics.SafeInsetLeft;
    /// ```
    ///
    /// RELATED FILES:
    /// - CanvasHelper.cs: Provides canvas/scaler references
    /// - UnitConversionHelper.cs: Coordinate space conversions
    /// - GameHelper.cs: g.TileSize for board-relative sizing
    /// - Common.cs: Increment/Interval constants
    ///
    /// ACCESS: ScreenMetrics.W(), ScreenMetrics.H(), etc. (static)
    /// </summary>
    public static class ScreenMetrics
    {
        // =================================================================
        // Canvas-space sizing (UI: RectTransform, fonts, padding)
        // =================================================================

        /// <summary>Returns a fraction of canvas width. W(0.5f) = half the canvas width.</summary>
        public static float W(float fraction)
        {
            return CanvasWidth * fraction;
        }

        /// <summary>Returns a fraction of canvas height. H(0.5f) = half the canvas height.</summary>
        public static float H(float fraction)
        {
            return CanvasHeight * fraction;
        }

        /// <summary>Returns a fraction of the shorter canvas axis. Guarantees square proportions on any aspect ratio.</summary>
        public static float Min(float fraction)
        {
            return Mathf.Min(CanvasWidth, CanvasHeight) * fraction;
        }

        /// <summary>Returns a fraction of the longer canvas axis.</summary>
        public static float Max(float fraction)
        {
            return Mathf.Max(CanvasWidth, CanvasHeight) * fraction;
        }

        /// <summary>Returns a fraction of the canvas diagonal.</summary>
        public static float Diag(float fraction)
        {
            float w = CanvasWidth;
            float h = CanvasHeight;
            return Mathf.Sqrt(w * w + h * h) * fraction;
        }

        /// <summary>Returns a Vector2 sized as fractions of (width, height).</summary>
        public static Vector2 Size(float wFraction, float hFraction)
        {
            return new Vector2(W(wFraction), H(hFraction));
        }

        /// <summary>Returns a square Vector2 sized as a fraction of the shorter axis.</summary>
        public static Vector2 Square(float fraction)
        {
            float s = Min(fraction);
            return new Vector2(s, s);
        }

        // =================================================================
        // Font sizing
        // =================================================================

        /// <summary>
        /// Returns a font size as a fraction of canvas height, rounded to nearest int.
        /// Font(0.03f) on a 1920-tall canvas → 58.
        /// </summary>
        public static float Font(float fractionOfHeight)
        {
            return Mathf.Round(CanvasHeight * fractionOfHeight);
        }

        /// <summary>
        /// Returns a font size as a fraction of the shorter axis.
        /// Use when text must look the same relative size on both
        /// landscape and portrait orientations.
        /// </summary>
        public static float FontMin(float fractionOfMin)
        {
            return Mathf.Round(Mathf.Min(CanvasWidth, CanvasHeight) * fractionOfMin);
        }

        // =================================================================
        // World-space sizing (sprites, board, camera)
        // =================================================================

        /// <summary>Returns a fraction of the visible world width (orthographic camera).</summary>
        public static float WorldW(float fraction)
        {
            return WorldWidth * fraction;
        }

        /// <summary>Returns a fraction of the visible world height (orthographic camera).</summary>
        public static float WorldH(float fraction)
        {
            return WorldHeight * fraction;
        }

        /// <summary>Returns a fraction of the shorter visible world axis.</summary>
        public static float WorldMin(float fraction)
        {
            return Mathf.Min(WorldWidth, WorldHeight) * fraction;
        }

        /// <summary>Returns a Vector2 sized as fractions of (worldWidth, worldHeight).</summary>
        public static Vector2 WorldSize(float wFraction, float hFraction)
        {
            return new Vector2(WorldW(wFraction), WorldH(hFraction));
        }

        // =================================================================
        // Tile-relative sizing (board elements)
        // =================================================================

        /// <summary>
        /// Returns a multiple of the current tile size.
        /// Tile(1) = one tile, Tile(0.5f) = half a tile.
        /// Uses GameHelper.TileSize, which is already screen-adaptive.
        /// </summary>
        public static float Tile(float multiplier)
        {
            return GameHelper.TileSize * multiplier;
        }

        /// <summary>Returns a square Vector2 of tile-relative size.</summary>
        public static Vector2 TileSquare(float multiplier)
        {
            float s = Tile(multiplier);
            return new Vector2(s, s);
        }

        // =================================================================
        // Safe area (notch/cutout insets, in canvas units)
        // =================================================================

        /// <summary>Top inset for safe area (notch), in canvas units.</summary>
        public static float SafeInsetTop
        {
            get
            {
                float px = Mathf.Max(0f, UnityEngine.Screen.height - UnityEngine.Screen.safeArea.yMax);
                return PixelsToCanvas(px);
            }
        }

        /// <summary>Bottom inset for safe area, in canvas units.</summary>
        public static float SafeInsetBottom
        {
            get
            {
                float px = Mathf.Max(0f, UnityEngine.Screen.safeArea.yMin);
                return PixelsToCanvas(px);
            }
        }

        /// <summary>Left inset for safe area, in canvas units.</summary>
        public static float SafeInsetLeft
        {
            get
            {
                float px = Mathf.Max(0f, UnityEngine.Screen.safeArea.xMin);
                return PixelsToCanvas(px);
            }
        }

        /// <summary>Right inset for safe area, in canvas units.</summary>
        public static float SafeInsetRight
        {
            get
            {
                float px = Mathf.Max(0f, UnityEngine.Screen.width - UnityEngine.Screen.safeArea.xMax);
                return PixelsToCanvas(px);
            }
        }

        /// <summary>Canvas width minus left and right safe insets.</summary>
        public static float SafeWidth => CanvasWidth - SafeInsetLeft - SafeInsetRight;

        /// <summary>Canvas height minus top and bottom safe insets.</summary>
        public static float SafeHeight => CanvasHeight - SafeInsetTop - SafeInsetBottom;

        // =================================================================
        // Aspect ratio queries
        // =================================================================

        /// <summary>Screen aspect ratio (width / height). Landscape > 1, portrait &lt; 1.</summary>
        public static float AspectRatio
        {
            get
            {
                float h = CanvasHeight;
                return h > 0f ? CanvasWidth / h : 1f;
            }
        }

        /// <summary>True if the screen is wider than it is tall.</summary>
        public static bool IsLandscape => CanvasWidth >= CanvasHeight;

        /// <summary>True if the screen is taller than it is wide.</summary>
        public static bool IsPortrait => CanvasHeight > CanvasWidth;

        /// <summary>True if aspect ratio is ultra-wide (≥ 2:1).</summary>
        public static bool IsUltraWide => AspectRatio >= 2f;

        /// <summary>True if aspect ratio is roughly 16:9 (±10%).</summary>
        public static bool Is16x9 => AspectRatio >= 1.6f && AspectRatio <= 1.95f;

        /// <summary>True if aspect ratio is roughly 4:3 (±10%).</summary>
        public static bool Is4x3 => AspectRatio >= 1.2f && AspectRatio <= 1.45f;

        // =================================================================
        // Raw dimension accessors
        // =================================================================

        /// <summary>Full canvas width in canvas units.</summary>
        public static float CanvasWidth
        {
            get
            {
                var rect = c.CanvasRect;
                return rect != null ? rect.rect.width : UnityEngine.Screen.width;
            }
        }

        /// <summary>Full canvas height in canvas units.</summary>
        public static float CanvasHeight
        {
            get
            {
                var rect = c.CanvasRect;
                return rect != null ? rect.rect.height : UnityEngine.Screen.height;
            }
        }

        /// <summary>Visible world width from the orthographic camera.</summary>
        public static float WorldWidth
        {
            get
            {
                if (Camera.main == null) return 10f;
                var r = UnitConversionHelper.World.VisibleRect();
                return r.width;
            }
        }

        /// <summary>Visible world height from the orthographic camera.</summary>
        public static float WorldHeight
        {
            get
            {
                if (Camera.main == null) return 10f;
                var r = UnitConversionHelper.World.VisibleRect();
                return r.height;
            }
        }

        // =================================================================
        // Internal helpers
        // =================================================================

        /// <summary>Converts device pixels to canvas units using the current UI scale.</summary>
        private static float PixelsToCanvas(float pixels)
        {
            return UnitConversionHelper.UiScale.PixelsToCanvasUnits(pixels);
        }
    }
}
