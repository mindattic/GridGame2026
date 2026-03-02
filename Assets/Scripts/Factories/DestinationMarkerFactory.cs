using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Libraries;
using UnityEngine;
using UnityEngine.Rendering;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
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
    /// DESTINATIONMARKERFACTORY - Creates movement destination markers.
    /// 
    /// PURPOSE:
    /// Creates visual markers showing where a hero will land when dropped.
    /// Auto-destroys when the hero arrives at the destination.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// [Hero] � � � � [X]
    ///                 ?
    ///         destination marker
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// DestinationMarker (root)
    /// ??? RectTransform (positioning)
    /// ??? CanvasRenderer (UI rendering)
    /// ??? SpriteRenderer (marker sprite)
    /// ??? DestinationMarker (behavior)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Size: 24x24 pixels
    /// - SortingOrder: 600 (above VFX, below combat text)
    /// - arriveDistance: 0.1 units (auto-destroy threshold)
    /// - destroyAtZero: true (self-destructs on arrival)
    /// 
    /// CALLED BY:
    /// - InputManager during drag operations
    /// 
    /// RELATED FILES:
    /// - DestinationMarker.cs: Marker behavior component
    /// - SpriteLibrary.cs: Provides marker sprite
    /// - InputManager.cs: Creates markers during drag
    /// </summary>
    public static class DestinationMarkerFactory
    {
        /// <summary>Creates a new destination marker.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("DestinationMarker");
            root.layer = LayerMask.NameToLayer("Default");

            // RectTransform
            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(24f, 24f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteLibrary.GUI["DestinationMarker"];
            spriteRenderer.color = Color.white;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 600;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            // DestinationMarker behavior
            var marker = root.AddComponent<DestinationMarker>();
            marker.arriveDistance = 0.1f;
            marker.destroyAtZero = true;

            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }
    }
}
