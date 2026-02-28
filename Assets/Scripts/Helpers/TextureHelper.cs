using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Helpers
{
    /// <summary>
    /// TEXTUREHELPER - Texture manipulation utilities.
    /// 
    /// PURPOSE:
    /// Provides methods for creating new textures from
    /// portions of existing textures.
    /// 
    /// USAGE:
    /// ```csharp
    /// var cropped = TextureHelper.CreateNewTexture(original, cropRect);
    /// ```
    /// </summary>
    public static class TextureHelper
    {
        /// <summary>Creates a new texture from a rectangular region of the original.</summary>
        public static Texture2D CreateNewTexture(Texture2D originalTexture, Rect rect)
        {
            int rectX = Mathf.Clamp((int)rect.x, 0, originalTexture.width);
            int rectY = Mathf.Clamp((int)rect.y, 0, originalTexture.height);
            int rectWidth = Mathf.Clamp((int)rect.width, 0, originalTexture.width - rectX);
            int rectHeight = Mathf.Clamp((int)rect.height, 0, originalTexture.height - rectY);

            Texture2D newTexture = new Texture2D(rectWidth, rectHeight);

            Color[] pixels = originalTexture.GetPixels(rectX, rectY, rectWidth, rectHeight);
            newTexture.SetPixels(pixels);
            newTexture.Apply();

            return newTexture;
        }
    }
}
