using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for Portrait3D (World) - replaces Portrait3DPrefab.prefab
    /// </summary>
    public static class Portrait3DFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("Portrait3D");
            root.layer = 0; // Default layer
            root.tag = "Portrait";

            // Transform
            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.white;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 1;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            // SortingGroup
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "ActorAbove"; // SortingLayer 12
            sortingGroup.sortingOrder = 0;

            // PortraitInstance (custom component)
            var portraitInstance = root.AddComponent<PortraitInstance>();
            portraitInstance.direction = Direction.None;
            portraitInstance.speedMultiplier = 1.75f;
            portraitInstance.startTime = 0f;
            portraitInstance.startPosition = Vector2.zero;

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
