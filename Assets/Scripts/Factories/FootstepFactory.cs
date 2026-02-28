using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// FOOTSTEPFACTORY - Creates footstep trail effect GameObjects.
    /// 
    /// PURPOSE:
    /// Creates footstep sprites that appear behind moving actors,
    /// creating a trail effect that fades over time.
    /// 
    /// VISUAL EFFECT:
    /// ```
    /// [Actor moving right]
    ///   ??    ??    ??
    ///  left  right  left
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// Footstep (root)
    /// ??? Transform (scaled 1.5625x)
    /// ??? SpriteRenderer (footstep sprite)
    /// ??? FootstepInstance (fade behavior)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Tag: "Footstep"
    /// - Scale: 1.5625 (enlarged for visibility)
    /// - Alpha: 50% (semi-transparent)
    /// - SortingOrder: 50 (above tiles, below actors)
    /// 
    /// SPRITE:
    /// Sprite assigned dynamically by FootstepManager based on
    /// foot direction (left/right).
    /// 
    /// CALLED BY:
    /// - FootstepManager.Spawn()
    /// 
    /// RELATED FILES:
    /// - FootstepInstance.cs: Fade animation behavior
    /// - FootstepManager.cs: Spawns footsteps during movement
    /// - SpriteLibrary.cs: Footstep sprites
    /// </summary>
    public static class FootstepFactory
    {
        /// <summary>Creates a new footstep effect.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("Footstep");
            root.layer = LayerMask.NameToLayer("Default");
            root.tag = "Footstep";

            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(1.5625f, 1.5625f, 1f);

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5019608f);
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 50;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            root.AddComponent<FootstepInstance>();

            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
