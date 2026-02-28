using UnityEngine;

namespace Game.Models
{
    /// <summary>
    /// RECTFLOAT - Float-based rectangle structure.
    /// 
    /// PURPOSE:
    /// Simple rectangle class using float edges (Top, Right, Bottom, Left)
    /// for world-space bounds calculations.
    /// 
    /// PROPERTIES:
    /// - Top/Right/Bottom/Left: Edge positions
    /// - Width: Right - Left
    /// - Height: Top - Bottom
    /// - Center: Midpoint of rectangle
    /// - Size: Width × Height as Vector2
    /// 
    /// USAGE:
    /// Used for board bounds, camera bounds, etc.
    /// 
    /// RELATED FILES:
    /// - BoardInstance.cs: Board bounds
    /// - Geometry.cs: Spatial calculations
    /// </summary>
    public class RectFloat
    {
        public RectFloat() { }
        public RectFloat(float top, float right, float bottom, float left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        //Fields
        public float Top;
        public float Right;
        public float Bottom;
        public float Left;

        //Properties
        public float Width => Right - Left;
        public float Height => Top - Bottom;
        public Vector2 Center => new Vector2(Width / 2, Height / 2);
        public Vector2 Size => new Vector2(Width, Height);
    }
}
