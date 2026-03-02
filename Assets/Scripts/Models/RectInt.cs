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
