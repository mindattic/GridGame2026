using UnityEngine;

namespace Game.Models
{
    public class RectInt
    {
        //Fields
        public int Top;
        public int Right;
        public int Bottom;
        public int Left;

        //Properties
        public int Width => Right - Left;
        public int Height => Top - Bottom;
        public Vector2Int Center => new Vector2Int(Width / 2, Height / 2);
        public Vector2Int Size => new Vector2Int(Width, Height);

    }
}
