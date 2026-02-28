using UnityEngine;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// CANVASTHUMBNAILSETTINGS - Canvas-specific portrait cropping.
    /// 
    /// PURPOSE:
    /// Defines cropping/positioning for actor portraits in UI
    /// canvas elements (e.g., timeline blocks, cards).
    /// 
    /// PROPERTIES:
    /// - X, Y: Position offset within mask
    /// - Width, Height: Crop dimensions
    /// - Scale: UI scale multiplier
    /// 
    /// RELATED FILES:
    /// - ActorData.cs: Contains canvas thumbnail settings
    /// - TimelineTag.cs: Uses for timeline portraits
    /// </summary>
    [System.Serializable]
    public class CanvasThumbnailSettings
    {
        public float X;
        public float Y;
        public int Width;
        public int Height;
        public Vector2 Scale;

        public CanvasThumbnailSettings() { }

        public CanvasThumbnailSettings(float x, float y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Scale = new Vector2(1f, 1f);
        }

        public CanvasThumbnailSettings(float x, float y, int width, int height, Vector2 scale)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Scale = scale;
        }

        public CanvasThumbnailSettings(CanvasThumbnailSettings other)
        {
            if (other == null) return;
            X = other.X;
            Y = other.Y;
            Width = other.Width;
            Height = other.Height;
            Scale = other.Scale;
        }

        public static CanvasThumbnailSettings Default => SetDefault();

        public static CanvasThumbnailSettings SetDefault()
        {
            // Pos X = 0, Pos Y = -150, Width = 96, Height = 96, Scale = (4,4)
            return new CanvasThumbnailSettings(0f, -150f, 96, 96, new Vector2(4f, 4f));
        }
    }
}
