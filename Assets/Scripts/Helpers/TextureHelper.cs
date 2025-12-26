using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Helpers
{

    public static class TextureHelper
    {
        public static Texture2D CreateNewTexture(Texture2D originalTexture, Rect rect)
        {
            // Ensure the rect dimensions are within the bounds of the original texture
            int rectX = Mathf.Clamp((int)rect.x, 0, originalTexture.width);
            int rectY = Mathf.Clamp((int)rect.y, 0, originalTexture.height);
            int rectWidth = Mathf.Clamp((int)rect.width, 0, originalTexture.width - rectX);
            int rectHeight = Mathf.Clamp((int)rect.height, 0, originalTexture.height - rectY);

            // Data a new texture with the specified dimensions
            Texture2D newTexture = new Texture2D(rectWidth, rectHeight);

            // Copy the pixel actors from the original texture to the new texture
            Color[] pixels = originalTexture.GetPixels(rectX, rectY, rectWidth, rectHeight);
            newTexture.SetPixels(pixels);

            // Apply the changes to the new texture
            newTexture.Apply();

            return newTexture;
        }
    }


}
