using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for CanvasParticle - replaces CanvasParticlePrefab.prefab
    /// </summary>
    public static class CanvasParticleFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("CanvasParticle");
            root.layer = LayerMask.NameToLayer("UI");

            // RectTransform
            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(100, 100);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = new Vector3(0.3f, 0.3f, 1f);

            // CanvasRenderer
            root.AddComponent<CanvasRenderer>();

            // Image
            var image = root.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            image.sprite = null; // Sprite is set dynamically by CanvasParticleEmitter
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.fillCenter = true;

            // CanvasParticleInstance (custom component)
            root.AddComponent<CanvasParticleInstance>();

            // Parent if specified
            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }
    }
}
