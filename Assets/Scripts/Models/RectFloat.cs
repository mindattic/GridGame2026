using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

namespace Scripts.Models
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
