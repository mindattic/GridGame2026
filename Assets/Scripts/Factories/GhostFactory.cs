using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for Ghost - replaces GhostPrefab.prefab
    /// Hierarchy:
    /// - Ghost (root)
    ///   - Thumbnail (SpriteRenderer)
    ///   - Frame (SpriteRenderer, inactive)
    /// </summary>
    public static class GhostFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: Ghost ===
            var root = new GameObject("Ghost");
            root.layer = 0; // Default layer
            root.tag = "Ghost";

            var rootTransform = root.transform;
            rootTransform.localPosition = Vector3.zero;
            rootTransform.localRotation = Quaternion.identity;
            rootTransform.localScale = Vector3.one;

            // GhostInstance (custom component)
            root.AddComponent<GhostInstance>();

            // === CHILD 0: Thumbnail ===
            var thumbnail = new GameObject("Thumbnail");
            thumbnail.layer = LayerMask.NameToLayer("Actors");
            thumbnail.tag = "Ghost";

            var thumbnailTransform = thumbnail.transform;
            thumbnailTransform.SetParent(rootTransform, false);
            thumbnailTransform.localPosition = Vector3.zero;
            thumbnailTransform.localRotation = Quaternion.identity;
            thumbnailTransform.localScale = Vector3.one;

            thumbnail.layer = 0; // Default layer

            var thumbnailSR = thumbnail.AddComponent<SpriteRenderer>();
            thumbnailSR.color = Color.white;
            thumbnailSR.shadowCastingMode = ShadowCastingMode.Off;
            thumbnailSR.receiveShadows = false;
            thumbnailSR.sortingLayerName = "Board"; // SortingLayer 2
            thumbnailSR.sortingOrder = 0;
            thumbnailSR.drawMode = SpriteDrawMode.Sliced;
            thumbnailSR.size = Vector2.one;

            // === CHILD 1: Frame ===
            var frame = new GameObject("Frame");
            frame.layer = 0; // Default layer
            frame.tag = "Portrait";
            frame.SetActive(false); // Inactive by default

            var frameTransform = frame.transform;
            frameTransform.SetParent(rootTransform, false);
            frameTransform.localPosition = Vector3.zero;
            frameTransform.localRotation = Quaternion.identity;
            frameTransform.localScale = new Vector3(2.56f, 2.56f, 1f);

            var frameSR = frame.AddComponent<SpriteRenderer>();
            frameSR.color = Color.white;
            frameSR.shadowCastingMode = ShadowCastingMode.Off;
            frameSR.receiveShadows = false;
            frameSR.sortingLayerName = "Board"; // SortingLayer 2
            frameSR.sortingOrder = 1;
            frameSR.drawMode = SpriteDrawMode.Sliced;
            frameSR.size = Vector2.one;

            // Parent if specified
            if (parent != null)
            {
                rootTransform.SetParent(parent, false);
            }

            return root;
        }
    }
}
