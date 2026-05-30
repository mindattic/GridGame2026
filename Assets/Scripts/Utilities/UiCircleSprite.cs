using UnityEngine;

namespace Scripts.Utilities
{
    /// <summary>
    /// UICIRCLESPRITE - Procedural anti-aliased white circle sprite, cached and reusable.
    ///
    /// <para>Default Unity UI <see cref="UnityEngine.UI.Image"/> with no sprite renders as a
    /// square; assigning this sprite + tinting the Image's color gives a clean circular disk
    /// without needing a .png asset shipped with the project.</para>
    ///
    /// <para>One 64×64 RGBA texture is built on first call and cached for the lifetime of the
    /// process. All callers share the same sprite (Image.color provides the per-instance tint).</para>
    /// </summary>
    public static class UiCircleSprite
    {
        private const int Size = 64;
        private static Sprite cached;

        public static Sprite Get()
        {
            if (cached != null) return cached;

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            float r = Size * 0.5f;
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Anti-aliased edge: 1-pixel feather from solid to transparent at the radius.
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            cached = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
            cached.hideFlags = HideFlags.HideAndDontSave;
            return cached;
        }
    }
}
