using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// GHOSTFACTORY - Creates ghost trail effect GameObjects.
    /// 
    /// PURPOSE:
    /// Creates semi-transparent "ghost" copies of actors that fade out,
    /// creating a motion trail effect during drag operations.
    /// 
    /// VISUAL EFFECT:
    /// ```
    /// [Actor] ? Current position
    ///   ??    ? Ghost (fading)
    ///     ??  ? Ghost (more faded)
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// Ghost (root)
    /// ??? GhostInstance (behavior)
    /// ??? Thumbnail (SpriteRenderer - actor sprite)
    /// ??? Frame (SpriteRenderer - optional border, inactive)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Tag: "Ghost"
    /// - SortingLayer: Board
    /// - Thumbnail copies actor sprite
    /// - Fades out over time via GhostInstance
    /// 
    /// CALLED BY:
    /// - GhostManager.Spawn()
    /// 
    /// RELATED FILES:
    /// - GhostInstance.cs: Fade animation component
    /// - GhostManager.cs: Spawns ghosts during drag
    /// - InputManager.cs: Triggers ghost creation
    /// </summary>
    public static class GhostFactory
    {
        /// <summary>Creates a new ghost trail GameObject.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: Ghost ===
            var root = new GameObject("Ghost");
            root.layer = 0;
            root.tag = "Ghost";

            var rootTransform = root.transform;
            rootTransform.localPosition = Vector3.zero;
            rootTransform.localRotation = Quaternion.identity;
            rootTransform.localScale = Vector3.one;

            root.AddComponent<GhostInstance>();

            // === CHILD: Thumbnail (actor sprite) ===
            var thumbnail = new GameObject("Thumbnail");
            thumbnail.layer = LayerMask.NameToLayer("Actors");
            thumbnail.tag = "Ghost";

            var thumbnailTransform = thumbnail.transform;
            thumbnailTransform.SetParent(rootTransform, false);
            thumbnailTransform.localPosition = Vector3.zero;
            thumbnailTransform.localRotation = Quaternion.identity;
            thumbnailTransform.localScale = Vector3.one;

            thumbnail.layer = 0;

            var thumbnailSR = thumbnail.AddComponent<SpriteRenderer>();
            thumbnailSR.color = Color.white;
            thumbnailSR.shadowCastingMode = ShadowCastingMode.Off;
            thumbnailSR.receiveShadows = false;
            thumbnailSR.sortingLayerName = "Board";
            thumbnailSR.sortingOrder = 0;
            thumbnailSR.drawMode = SpriteDrawMode.Sliced;
            thumbnailSR.size = Vector2.one;

            // === CHILD: Frame (optional border, inactive) ===
            var frame = new GameObject("Frame");
            frame.layer = 0;
            frame.tag = "Portrait";
            frame.SetActive(false);

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
