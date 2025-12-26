using UnityEngine;

namespace Assets.Scripts.Models
{
    [System.Serializable]
    public class ThumbnailSettings
    {
        // Offset is an offset from the center of the image, expressed in mask-size units
        // and consumed by TimelineBlockInstance. It is derived from PixelPosition.
        [SerializeField]
        private Vector2 offset;
        public Vector2 Offset => offset; // read-only, always computed from pixels

        public Vector2 Scale;

        // Focus point in source texture pixels (easier to set by sampling the image)
        // Example for 1024x1024 textures: (512,512) means centered.
        public Vector2Int PixelPosition;

        // Source texture size in pixels (defaults to 1024 as portraits are 1024x1024)
        public int TextureSize = 1024;

        public ThumbnailSettings()
        {
            offset = Vector2.zero;
            Scale = Vector2.one;
            TextureSize = 1024;
            // default to image center so offset becomes zero
            PixelPosition = new Vector2Int(TextureSize / 2, TextureSize / 2);
            OffsetFromPixels();
        }

        // Legacy: construct from offset+scale. We compute PixelPosition and then derive offset.
        public ThumbnailSettings(Vector2 position, Vector2 scale)
        {
            Scale = scale;
            TextureSize = 1024;
            PixelPosition = PixelFromOffset(position, scale, TextureSize);
            OffsetFromPixels();
        }

        // Preferred: construct from pixel focus position and scale
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