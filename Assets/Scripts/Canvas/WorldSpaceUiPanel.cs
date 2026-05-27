using UnityEngine;
using UnityEngine.UI;
using Scripts.Helpers;
using g = Scripts.Helpers.GameHelper;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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

namespace Scripts.Canvas
{
    /// <summary>
    /// WORLDSPACEUIPANEL - The reusable rig for placing HUD UI in WORLD space (not a
    /// ScreenSpaceOverlay "windshield sticker").
    ///
    /// <para>WHY: An Overlay canvas always draws last, on top of everything, ignoring sorting
    /// layers — so VFX and portraits can never appear in front of the HUD. A world-space canvas
    /// renders as scene geometry on the <c>UI</c> sorting layer (placed above the board/actors but
    /// below VFX/Coin/Portrait), so the whole game shares ONE coordinate + sorting system and
    /// effects/portraits can pop in front of the UI when they should.</para>
    ///
    /// <para>HOW IT WORKS: a Canvas in <see cref="RenderMode.WorldSpace"/> bound to the game camera,
    /// with a GraphicRaycaster (so uGUI buttons stay clickable) and a sortingLayer/order. Content is
    /// authored in a fixed reference-pixel space (<see cref="ReferencePixelsWide"/>) under
    /// <see cref="Content"/>; the panel's transform scale maps that pixel space to a requested width
    /// in WORLD units, so a panel sizes off the board's world metrics (tileSize / visible rect),
    /// not raw device pixels. Existing uGUI Image/TMP/Button/layout components work unchanged —
    /// they just live here instead of the overlay canvas.</para>
    ///
    /// <para>USAGE:
    /// <code>
    /// var panel = WorldSpaceUiPanel.Create("ActionTitle", worldWidth: 6f, worldHeight: 1.2f);
    /// panel.PlaceInTopBand();              // negative space above the board
    /// // ...build Image/TMP/Button under panel.Content (in reference pixels)...
    /// </code>
    /// </para>
    ///
    /// <para>RELATED FILES: SortingHelper.cs (UI layer), UnitConversionHelper.cs (VisibleRect),
    /// FadeOverlayInstance.cs (the one piece that stays a true overlay).</para>
    /// </summary>
    public class WorldSpaceUiPanel : MonoBehaviour
    {
        /// <summary>Fixed pixel resolution that content under <see cref="Content"/> is authored in.
        /// The panel scales this to the requested world width, so consumers use familiar pixel
        /// coordinates while the result is sized in world units.</summary>
        public const float ReferencePixelsWide = 1000f;

        public UnityEngine.Canvas Canvas { get; private set; }
        public RectTransform Content { get; private set; }

        private float worldWidth;
        private float worldHeight;

        /// <summary>
        /// Builds a world-space UI panel of the requested world size on the UI sorting layer.
        /// Content is authored in a 1000-wide reference-pixel space under <see cref="Content"/>.
        /// </summary>
        public static WorldSpaceUiPanel Create(string name, float worldWidth, float worldHeight, int sortingOrder = 0)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingLayerName = SortingHelper.Layer.UI;
            canvas.sortingOrder = sortingOrder;

            // Crisp text/edges in world space — author at high effective DPI then scale down.
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            scaler.referencePixelsPerUnit = 100f;

            // Keeps uGUI buttons clickable via the camera.
            go.AddComponent<GraphicRaycaster>();

            var rt = go.GetComponent<RectTransform>();
            float pixelsHigh = ReferencePixelsWide * (worldWidth > 0f ? worldHeight / worldWidth : 0.6f);
            rt.sizeDelta = new Vector2(ReferencePixelsWide, pixelsHigh);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Map the reference-pixel width to the requested world width.
            float scale = worldWidth / ReferencePixelsWide;
            go.transform.localScale = new Vector3(scale, scale, scale);

            var panel = go.AddComponent<WorldSpaceUiPanel>();
            panel.Canvas = canvas;
            panel.Content = rt;
            panel.worldWidth = worldWidth;
            panel.worldHeight = worldHeight;
            return panel;
        }

        /// <summary>Places the panel at an explicit world position (panel center).</summary>
        public void PlaceAt(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        /// <summary>
        /// Places the panel centered in the negative-space band ABOVE the board's play area
        /// (the top reserve of the visible world rect). Horizontal-centered.
        /// </summary>
        public void PlaceInTopBand()
        {
            var vr = UnitConversionHelper.World.VisibleRect();
            float bandCenterY = vr.yMax - worldHeight * 0.5f - vr.height * 0.02f;
            transform.position = new Vector3(vr.center.x, bandCenterY, 0f);
        }

        /// <summary>
        /// Places the panel centered in the negative-space band BELOW the board's play area
        /// (the bottom reserve of the visible world rect). Horizontal-centered.
        /// </summary>
        public void PlaceInBottomBand()
        {
            var vr = UnitConversionHelper.World.VisibleRect();
            float bandCenterY = vr.yMin + worldHeight * 0.5f + vr.height * 0.02f;
            transform.position = new Vector3(vr.center.x, bandCenterY, 0f);
        }

        /// <summary>Sets the panel's visibility via the canvas (cheap show/hide).</summary>
        public void SetVisible(bool visible)
        {
            if (Canvas != null) Canvas.enabled = visible;
        }
    }
}
