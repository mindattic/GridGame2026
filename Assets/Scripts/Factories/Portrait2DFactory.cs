using UnityEngine;
using UnityEngine.UI;
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
    /// PORTRAIT2DFACTORY - Creates UI canvas portrait GameObjects.
    /// 
    /// PURPOSE:
    /// Creates large character portrait images for UI overlays during
    /// combat sequences (pincer attacks, special abilities, etc).
    /// 
    /// VISUAL EFFECT:
    /// ```
    /// ┌─────────────────────────────────┐
    /// │ [Hero A]          [Hero B]      │ ← Portraits slide in
    /// │    ↘                 ↙          │
    /// │       [Combat Area]             │
    /// └─────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// Portrait2D (root)
    /// ├── RectTransform (1024x1024)
    /// ├── CanvasRenderer (UI rendering)
    /// ├── Image (portrait sprite)
    /// └── PortraitInstance (animation behavior)
    /// ```
    /// 
    /// VS PORTRAIT3D:
    /// - Portrait2D: UI space (Image component)
    /// - Portrait3D: World space (SpriteRenderer)
    /// 
    /// CONFIGURATION:
    /// - Default size: 1024x1024
    /// - speedMultiplier: 1.75x slide animation speed
    /// 
    /// CALLED BY:
    /// - PortraitManager.SlideIn2DRoutine()
    /// 
    /// RELATED FILES:
    /// - Portrait3DFactory.cs: World-space variant
    /// - PortraitInstance.cs: Animation behavior
    /// - PortraitManager.cs: Manages portraits
    /// </summary>
    public static class Portrait2DFactory
    {
        /// <summary>Creates a new UI portrait.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("Portrait2D");
            root.layer = LayerMask.NameToLayer("Default");

            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(1024f, 1024f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();

            var image = root.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            var portraitInstance = root.AddComponent<PortraitInstance>();
            portraitInstance.direction = Direction.None;
            portraitInstance.speedMultiplier = 1.75f;
            portraitInstance.startTime = 0f;
            portraitInstance.startPosition = Vector2.zero;

            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }
    }
}
