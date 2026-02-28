using Assets.Scripts.Libraries;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// TILEFACTORY - Programmatically creates Tile GameObjects.
    /// 
    /// PURPOSE:
    /// Replaces TilePrefab.prefab with code-driven creation.
    /// Creates fully configured tile GameObjects at runtime.
    /// 
    /// FACTORY PATTERN:
    /// All factories in this project follow the same pattern:
    /// 1. Static class with Create(Transform parent) method
    /// 2. Returns fully configured GameObject
    /// 3. No prefab dependencies - everything built in code
    /// 4. Uses Libraries for assets (SpriteLibrary, FontLibrary, etc.)
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// Tile (GameObject)
    /// ??? SpriteRenderer (tile sprite)
    /// ??? TileInstance (grid position data)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Sprite: SpriteLibrary.Sprites["Tile"]
    /// - Layer: Default (0)
    /// - Tag: "Tile"
    /// - Sorting: Board layer, order 1
    /// - Alpha: ~39% for semi-transparent grid
    /// 
    /// USAGE:
    /// ```csharp
    /// var tileGO = TileFactory.Create(boardTransform);
    /// var tile = tileGO.GetComponent<TileInstance>();
    /// tile.Initialize(column, row);
    /// ```
    /// 
    /// CALLED BY:
    /// - BoardInstance.GenerateTiles() during board setup
    /// 
    /// RELATED FILES:
    /// - TileInstance.cs: Component attached to tile
    /// - BoardInstance.cs: Creates tile grid
    /// - SpriteLibrary.cs: Provides tile sprite
    /// </summary>
    public static class TileFactory
    {
        /// <summary>
        /// Creates a new Tile GameObject with all components configured.
        /// </summary>
        /// <param name="parent">Optional parent transform (usually Board).</param>
        /// <returns>Fully configured tile GameObject.</returns>
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
            spriteRenderer.sprite = SpriteLibrary.Sprites["Tile"];
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.39215687f); // ~39% alpha
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Board";
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
