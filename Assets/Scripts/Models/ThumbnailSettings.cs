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
    /// THUMBNAILSETTINGS - Actor portrait cropping configuration.
    /// 
    /// PURPOSE:
    /// Defines how to crop and position an actor's portrait
    /// sprite within a display mask (e.g., timeline, card).
    /// 
    /// COORDINATES:
    /// - PixelPosition: Focus point in source texture pixels
    /// - Offset: Derived mask-space offset from center
    /// - Scale: Zoom/scale factor
    /// - TextureSize: Source texture dimensions (default 1024)
    /// 
    /// USAGE:
    /// Set PixelPosition to the face/focus area of the portrait.
    /// Offset is auto-calculated for mask positioning.
    /// 
    /// RELATED FILES:
    /// - ActorData.cs: Contains thumbnail settings
    /// - TimelineTag.cs: Uses for portrait display
    /// - ActorCard.cs: Uses for card portrait
    /// </summary>
    [System.Serializable]
    public class ThumbnailSettings
    {
        [SerializeField]
        private Vector2 offset;
        public Vector2 Offset => offset;

        public Vector2 Scale;
        public Vector2Int PixelPosition;
        public int TextureSize = 1024;

        public ThumbnailSettings()
        {
            offset = Vector2.zero;
            Scale = Vector2.one;
            TextureSize = 1024;
            PixelPosition = new Vector2Int(TextureSize / 2, TextureSize / 2);
            OffsetFromPixels();
        }

        public ThumbnailSettings(Vector2 position, Vector2 scale)
        {
            Scale = scale;
            TextureSize = 1024;
            PixelPosition = PixelFromOffset(position, scale, TextureSize);
            OffsetFromPixels();
        }

        public ThumbnailSettings(Vector2Int pixelPosition, Vector2 scale, int textureSize = 1024)
        {
            Scale = scale;
            TextureSize = Mathf.Max(1, textureSize);
            PixelPosition = pixelPosition;
            OffsetFromPixels();
        }

        // Copy Constructor
        public ThumbnailSettings(ThumbnailSettings other)
        {
            if (other == null)
            {
                offset = Vector2.zero;
                Scale = Vector2.one;
                TextureSize = 1024;
                PixelPosition = new Vector2Int(TextureSize / 2, TextureSize / 2);
            }
            else
            {
                Scale = other.Scale;
                TextureSize = other.TextureSize > 0 ? other.TextureSize : 1024;
                PixelPosition = other.PixelPosition;

                // Backward compatibility: if PixelPosition appears to be default center, prefer existing offset.
                var center = new Vector2Int(Mathf.Max(1, TextureSize) / 2, Mathf.Max(1, TextureSize) / 2);
                bool pixelIsDefaultCenter = PixelPosition == center || PixelPosition == Vector2Int.zero;
                if (!pixelIsDefaultCenter)
                {
                    OffsetFromPixels();
                }
                else
                {
                    // use incoming offset directly
                    offset = other.Offset;
                }
            }
        }

        // Recalculate offset (from center, in mask-size units) from the pixel focus point.
        // Offset.x = (cx - px) * (scale.x / T)
        // Offset.y = (py - cy) * (scale.y / T)
        public void OffsetFromPixels()
        {
            int T = Mathf.Max(1, TextureSize);

            // Clamp the pixel focus to the texture bounds so offsets don't explode (e.g., negative X)
            int clampedPX = Mathf.Clamp(PixelPosition.x, 0, T - 1);
            int clampedPY = Mathf.Clamp(PixelPosition.y, 0, T - 1);
            PixelPosition = new Vector2Int(clampedPX, clampedPY);

            float cx = T * 0.5f;
            float cy = T * 0.5f;
            float px = clampedPX;
            float py = clampedPY;

            // Convert pixel focus into normalized offset from center, scaled by portrait scale.
            float ox = (cx - px) * (Scale.x / T);
            float oy = (py - cy) * (Scale.y / T);

            // To keep the portrait covering the mask, clamp offset so it can't push the image past the mask edges.
            // Given portrait width = Scale.x * s and mask width = s, the max safe center offset is (Scale.x - 1)/2 (in mask-size units).
            float maxOX = Mathf.Max(0f, (Scale.x - 1f) * 0.5f);
            float maxOY = Mathf.Max(0f, (Scale.y - 1f) * 0.5f);
            ox = Mathf.Clamp(ox, -maxOX, maxOX);
            oy = Mathf.Clamp(oy, -maxOY, maxOY);

            offset = new Vector2(ox, oy);
        }

        // Helper to estimate a pixel coordinate from an existing offset/scale, for backward compatibility.
        private static Vector2Int PixelFromOffset(Vector2 position, Vector2 scale, int textureSize)
        {
            int T = Mathf.Max(1, textureSize);
            float cx = T * 0.5f;
            float cy = T * 0.5f;

            // Invert the conversion from OffsetFromPixels
            float px = cx - (position.x * T / Mathf.Max(0.0001f, scale.x));
            float py = cy + (position.y * T / Mathf.Max(0.0001f, scale.y));

            int ipx = Mathf.Clamp(Mathf.RoundToInt(px), 0, T - 1);
            int ipy = Mathf.Clamp(Mathf.RoundToInt(py), 0, T - 1);
            return new Vector2Int(ipx, ipy);
        }

        // Call this if you change Scale after setting PixelPosition so the derived offset updates.
        public void OnScaleChanged()
        {
            OffsetFromPixels();
        }

        // Call this if you change TextureSize after construction.
        public void OnTextureSizeChanged()
        {
            OffsetFromPixels();
        }

        // Call this if you change PixelPosition after construction.
        public void OnPixelPositionChanged()
        {
            OffsetFromPixels();
        }
    }
}
