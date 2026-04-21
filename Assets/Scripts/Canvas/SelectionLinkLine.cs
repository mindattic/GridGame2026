using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
using Scripts.Helpers;
using Scripts.Instances.Actor;

namespace Scripts.Canvas
{
    /// <summary>
    /// SELECTIONLINKLINE - Red link between a selected actor and its TimelineIcon.
    /// <para>PURPOSE: When the player selects an enemy actor (or its TimelineIcon),
    /// draws a thin red line connecting the icon's bottom-center on the timeline
    /// to the actor's top-center on the board. Helps the player visually pair
    /// the timeline indicator with the unit it represents for planning attacks.</para>
    /// <para>USAGE: <c>SelectionLinkLine.Bind(actor)</c> shows / retargets the line.
    /// Pass <c>null</c> to hide. The line auto-hides if the actor has no icon
    /// (heroes don't appear on the timeline).</para>
    /// <para>RELATED FILES: SelectionManager.cs (calls Bind on Select), TimelineIcon.cs,
    /// TimelineBarInstance.cs (icon lookup), UnitConversionHelper.cs (world→canvas).</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SelectionLinkLine : Graphic
    {
        private static readonly Color LinkColor = new Color(0.95f, 0.20f, 0.20f, 0.9f);
        private const float LineThickness = 2.5f;
        private const float ActorTopOffsetWorld = 0.55f;

        private static SelectionLinkLine instance;

        private ActorInstance boundActor;
        private Vector2 cachedStartCanvas;
        private Vector2 cachedEndCanvas;
        private bool hasEndpoints;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            color = LinkColor;
            raycastTarget = false;
        }

        protected override void OnDestroy()
        {
            if (instance == this) instance = null;
            base.OnDestroy();
        }

        /// <summary>
        /// Lazily creates the singleton SelectionLinkLine under the Canvas root and
        /// binds it to the given actor. Pass null to hide.
        /// </summary>
        public static void Bind(ActorInstance actor)
        {
            EnsureInstance();
            if (instance == null) return;
            instance.boundActor = actor;
            instance.RefreshNow();
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var canvasRect = CanvasHelper.CanvasRect;
            if (canvasRect == null) return;

            var go = new GameObject("SelectionLinkLine", typeof(RectTransform), typeof(CanvasRenderer));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRect, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.AddComponent<SelectionLinkLine>();
        }

        private void LateUpdate()
        {
            // Recompute every frame — the icon slides along the timeline and the actor can move.
            RefreshNow();
        }

        private void RefreshNow()
        {
            if (boundActor == null || !boundActor.IsPlaying || g.TimelineBar == null)
            {
                if (hasEndpoints) { hasEndpoints = false; SetVerticesDirty(); }
                return;
            }

            var icon = g.TimelineBar.GetIconFor(boundActor);
            if (icon == null || icon.Rect == null)
            {
                if (hasEndpoints) { hasEndpoints = false; SetVerticesDirty(); }
                return;
            }

            var canvasRect = CanvasHelper.CanvasRect;
            if (canvasRect == null) return;

            // Icon bottom-center in canvas-local space.
            var corners = new Vector3[4];
            icon.Rect.GetWorldCorners(corners);
            Vector3 iconBottomCenterWorld = (corners[0] + corners[3]) * 0.5f; // BL + BR
            Vector2 iconScreen = RectTransformUtility.WorldToScreenPoint(null, iconBottomCenterWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, iconScreen, null, out var iconLocal);

            // Actor top-center in canvas-local space.
            Vector3 actorTopWorld = boundActor.Position + new Vector3(0f, ActorTopOffsetWorld, 0f);
            Vector2 actorCanvas = UnitConversionHelper.World.ToCanvas(canvasRect, actorTopWorld);
            // ToCanvas returns coords relative to the canvas rect. Our rect is fullscreen
            // under the canvas, so we share its local frame and can use the value directly.
            Vector2 actorLocal = actorCanvas;

            cachedStartCanvas = iconLocal;
            cachedEndCanvas = actorLocal;
            hasEndpoints = true;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!hasEndpoints) return;

            Vector2 a = cachedStartCanvas;
            Vector2 b = cachedEndCanvas;
            Vector2 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.5f) return;

            Vector2 perp = new Vector2(-dir.y, dir.x) / len * (LineThickness * 0.5f);

            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            int idx = vh.currentVertCount;
            v.position = a + perp; vh.AddVert(v);
            v.position = b + perp; vh.AddVert(v);
            v.position = b - perp; vh.AddVert(v);
            v.position = a - perp; vh.AddVert(v);
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }
    }
}
