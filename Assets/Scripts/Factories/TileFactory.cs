using Assets.Scripts.Libraries;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for Tile - replaces TilePrefab.prefab
    /// </summary>
    public static class TileFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("Tile");
            root.layer = 0; // Default layer
            root.tag = "Tile";

            // Transform
            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteLibrary.Sprites["Tile"]; // Load the tile sprite
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.39215687f); // White with ~39% alpha
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Board"; // Use Board sorting layer
            spriteRenderer.sortingOrder = 1;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = Vector2.one;

            // TileInstance (custom component)
            var tileInstance = root.AddComponent<TileInstance>();
            tileInstance.location = Vector2Int.zero;
            tileInstance.spriteRenderer = spriteRenderer;

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
