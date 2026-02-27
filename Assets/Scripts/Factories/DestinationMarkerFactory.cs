using Assets.Helpers;
using Assets.Scripts.Canvas;
using Assets.Scripts.Libraries;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for DestinationMarker - replaces DestinationMarkerPrefab.prefab
    /// </summary>
    public static class DestinationMarkerFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("DestinationMarker");
            root.layer = LayerMask.NameToLayer("Default"); // Layer 0

            // RectTransform
            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(24f, 24f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // CanvasRenderer
            root.AddComponent<CanvasRenderer>();

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteLibrary.GUI["DestinationMarker"];
            spriteRenderer.color = Color.white;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 600; // Above VFX, below CombatText
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            // DestinationMarker (custom component)
            var marker = root.AddComponent<DestinationMarker>();
            marker.arriveDistance = 0.1f;
            marker.destroyAtZero = true;

            // Parent if specified
            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }
    }
}
