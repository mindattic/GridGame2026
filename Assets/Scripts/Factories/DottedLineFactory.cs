using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for DottedLine - replaces DottedLinePrefab.prefab
    /// </summary>
    public static class DottedLineFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("DottedLine");
            root.layer = 0; // Default layer
            root.tag = "DottedLine";

            // Transform
            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.76862746f); // White with ~77% alpha
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Board"; // SortingLayer 2
            spriteRenderer.sortingOrder = 0;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = Vector2.one;
            // Sprite is set dynamically by DottedLineInstance.Spawn()

            // DottedLineInstance (custom component)
            var dottedLineInstance = root.AddComponent<DottedLineInstance>();
            dottedLineInstance.location = Vector2Int.zero;
            dottedLineInstance.segment = DottedLineSegment.Vertical;
            dottedLineInstance.connectedLocations.Clear();

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
