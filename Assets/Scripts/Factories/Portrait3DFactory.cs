using UnityEngine;
using UnityEngine.Rendering;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Factories
{
    /// <summary>
    /// PORTRAIT3DFACTORY - Creates world-space portrait GameObjects.
    /// 
    /// PURPOSE:
    /// Creates character portrait sprites in world space for combat
    /// sequences and dramatic effects.
    /// 
    /// VISUAL EFFECT:
    /// ```
    /// World Space View:
    /// 
    ///   [Portrait]  ← World-space sprite
    ///       ↓
    ///   [Actor]     ← On game board
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// Portrait3D (root)
    /// ├── Transform (scaled 0.5x)
    /// ├── SpriteRenderer (portrait sprite)
    /// ├── SortingGroup (ActorAbove layer)
    /// └── PortraitInstance (animation behavior)
    /// ```
    /// 
    /// VS PORTRAIT2D:
    /// - Portrait2D: UI space (Image component)
    /// - Portrait3D: World space (SpriteRenderer)
    /// 
    /// CONFIGURATION:
    /// - Tag: "Portrait"
    /// - Scale: 0.5 (half size)
    /// - SortingLayer: ActorAbove (renders over actors)
    /// - speedMultiplier: 1.75x animation speed
    /// 
    /// CALLED BY:
    /// - PortraitManager.SlideIn3DRoutine()
    /// 
    /// RELATED FILES:
    /// - Portrait2DFactory.cs: UI-space variant
    /// - PortraitInstance.cs: Animation behavior
    /// - PortraitManager.cs: Manages portraits
    /// </summary>
    public static class Portrait3DFactory
    {
        /// <summary>Creates a new world-space portrait.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("Portrait3D");
            root.layer = 0;
            root.tag = "Portrait";

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
            sortingGroup.sortingLayerName = "ActorAbove";
            sortingGroup.sortingOrder = 0;

            // PortraitInstance
            var portraitInstance = root.AddComponent<PortraitInstance>();
            portraitInstance.direction = Direction.None;
            portraitInstance.speedMultiplier = 1.75f;
            portraitInstance.startTime = 0f;
            portraitInstance.startPosition = Vector2.zero;

            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
