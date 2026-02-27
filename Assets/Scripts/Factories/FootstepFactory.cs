using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for Footstep - replaces FootstepPrefab.prefab
    /// </summary>
    public static class FootstepFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("Footstep");
            root.layer = LayerMask.NameToLayer("Default"); // Layer 0
            root.tag = "Footstep";

            // Transform
            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(1.5625f, 1.5625f, 1f);

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5019608f); // White with ~50% alpha
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 50; // Above tiles, below actors
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            // Sprite is set dynamically by FootstepManager.Spawn()

            // FootstepInstance (custom component)
            root.AddComponent<FootstepInstance>();

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
