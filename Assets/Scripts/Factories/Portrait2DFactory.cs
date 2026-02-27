using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for Portrait2D (UI) - replaces Portrait2DPrefab.prefab
    /// </summary>
    public static class Portrait2DFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("Portrait2D");
            root.layer = LayerMask.NameToLayer("Default"); // Layer 0

            // RectTransform
            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(1024f, 1024f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // CanvasRenderer
            root.AddComponent<CanvasRenderer>();

            // Image
            var image = root.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            // PortraitInstance (custom component)
            var portraitInstance = root.AddComponent<PortraitInstance>();
            portraitInstance.direction = Direction.None;
            portraitInstance.speedMultiplier = 1.75f;
            portraitInstance.startTime = 0f;
            portraitInstance.startPosition = Vector2.zero;

            // Parent if specified
            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }
    }
}
