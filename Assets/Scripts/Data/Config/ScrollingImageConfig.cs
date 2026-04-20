using UnityEngine;

namespace Scripts.Data.Config
{
    /// <summary>
    /// SCROLLINGIMAGECONFIG - Static tuning values for ScrollingImage.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// ScrollingImage with compile-time values. <c>DefaultScrollSpeed</c> is
    /// static readonly (Vector2 cannot be const); ScrollingImage seeds a
    /// private instance field with this value so SetScrollSpeed / SetScrollX /
    /// SetScrollY can still mutate it at runtime.</para>
    /// <para>USAGE: Referenced from ScrollingImage.Awake / Update.</para>
    /// <para>RELATED FILES: ScrollingImage.cs, ScrollingRawImage.cs, CityScroll.cs</para>
    /// </summary>
    public static class ScrollingImageConfig
    {
        // Default UV offset velocity in units per second. Instance field in
        // ScrollingImage is seeded with this so runtime setters can replace it.
        public static readonly Vector2 DefaultScrollSpeed = new Vector2(0.1f, 0.0f);

        // If true, ignore Time.timeScale when advancing the UV offset.
        public const bool UseUnscaledTime = true;

        // Material property name whose textureOffset is mutated for Image mode.
        public const string TextureProperty = "_MainTex";
    }
}
