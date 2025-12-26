using UnityEngine;

namespace Game.Models
{
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
