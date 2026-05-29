using UnityEngine;

namespace Scripts.Utilities
{
    /// <summary>
    /// HUDLAYOUT - Single source of truth for the 15-row vertical HUD grid (1170×2532 canvas).
    /// Both <c>GameBuilder</c> (scene-time) and runtime factories (<c>ShieldButtonFactory</c>,
    /// <c>ManaOrbLineFactory</c>) read from here so positions can't drift across files.
    ///
    /// <para>Row 1: Money | 2: Timeline | 3: ActionTitle | 4–12: 6×8 Board | 13: Ability bar |
    /// 14: Orb line | 15: Character card.</para>
    /// </summary>
    public static class HudLayout
    {
        public const float CanvasHeight = 2532f;
        public const float RowHeight    = CanvasHeight / 15f;        // ≈168.8

        // Top-anchored Y offsets (negative — go DOWN from the canvas top).
        public static readonly float Row1Y_FromTop = -RowHeight * 0.5f;   // ≈ -84
        public static readonly float Row2Y_FromTop = -RowHeight * 1.5f;   // ≈ -253
        public static readonly float Row3Y_FromTop = -RowHeight * 2.5f;   // ≈ -422

        // Bottom-anchored Y offsets (positive — go UP from the canvas bottom).
        public static readonly float Row13Y_FromBot = RowHeight * 2.5f;   // ≈ 422
        public static readonly float Row14Y_FromBot = RowHeight * 1.5f;   // ≈ 253
        public static readonly float Row15Y_FromBot = RowHeight * 0.5f;   // ≈ 84

        // Center-pivot canvas equivalents (when anchored at (0.5, 0.5)).
        public static readonly float Row2Y_Centered = CanvasHeight * 0.5f + Row2Y_FromTop; // ≈ 1013
    }
}
