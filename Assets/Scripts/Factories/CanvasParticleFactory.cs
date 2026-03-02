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
    /// CANVASPARTICLEFACTORY - Creates UI canvas particle GameObjects.
    /// 
    /// PURPOSE:
    /// Creates individual particle objects for UI canvas effects like
    /// falling leaves, snow, or other ambient particles.
    /// 
    /// VISUAL EFFECT:
    /// ```
    /// ??      ??
    ///   ??  ??    ??  ? Particles drift across UI
    ///     ??   ??
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// CanvasParticle (root)
    /// ??? RectTransform (positioning)
    /// ??? CanvasRenderer (UI rendering)
    /// ??? Image (particle sprite)
    /// ??? CanvasParticleInstance (animation behavior)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Layer: UI
    /// - Default size: 100x100
    /// - Default scale: 0.3 (30%)
    /// - Sprite set dynamically by emitter
    /// 
    /// PARTICLE BEHAVIOR:
    /// CanvasParticleInstance handles:
    /// - Drift movement
    /// - Rotation
    /// - Fade out
    /// - Self-destruction when off-screen
    /// 
    /// CALLED BY:
    /// - CanvasParticleEmitter.SpawnParticle()
    /// 
    /// RELATED FILES:
    /// - CanvasParticleInstance.cs: Particle behavior
    /// - CanvasParticleEmitter.cs: Spawns particles
    /// - SpriteLibrary.cs: Particle sprites
    /// </summary>
    public static class CanvasParticleFactory
    {
        /// <summary>Creates a new canvas particle.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("CanvasParticle");
            root.layer = LayerMask.NameToLayer("UI");

            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(100, 100);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = new Vector3(0.3f, 0.3f, 1f);

            root.AddComponent<CanvasRenderer>();

            var image = root.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            image.sprite = null; // Set dynamically by emitter
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.fillCenter = true;

            root.AddComponent<CanvasParticleInstance>();

            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }
    }
}
