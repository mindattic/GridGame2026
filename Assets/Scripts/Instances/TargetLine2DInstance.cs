using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using g = Scripts.Helpers.GameHelper;
using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Config;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
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

namespace Scripts.Instances
{
    /// <summary>
    /// TARGETLINE2DINSTANCE - Canvas-space FFXII-style targeting arc (Bezier + tapered ribbon + bead).
    /// <para>PURPOSE: Same visual grammar as TargetLine3DInstance, but composed entirely in
    /// ScreenSpaceOverlay canvas coordinates so it renders on top of HUD UI (mana bar, timeline
    /// bar). Endpoints are resolved to world each frame, projected to the screen via the main
    /// camera, then unprojected into the host canvas's local space.</para>
    /// <para>RENDER TECHNIQUE:
    /// <list type="bullet">
    /// <item>Quadratic-Bezier sampled at TargetLineInstanceConfig.Segments points.</item>
    /// <item>Two TargetLine2DGraphic passes — thin core + wider low-alpha glow.</item>
    /// <item>Bead Image slides source → destination at BeadLoopsPerSecond with a sin() pulse.</item>
    /// </list>
    /// </para>
    /// <para>RELATED FILES: TargetLine2DFactory.cs, TargetLine2DGraphic.cs, TargetLine3DInstance.cs, TargetLineManager.cs, TargetPoint.cs</para>
    /// </summary>
    public class TargetLine2DInstance : MonoBehaviour
    {
        public Transform parent
        {
            get => transform.parent;
            set => transform.SetParent(value, true);
        }

        public float alpha = 1f;
        private TargetLine2DGraphic coreGraphic;
        private TargetLine2DGraphic glowGraphic;
        private RectTransform hostRect;
        private RectTransform beadRT;
        private Image beadImage;
        private Color lineColor = new Color(0f, 1f, 1f, 1f); // default cyan
        private Vector2 curveStart;
        private Vector2 curveControl;
        private Vector2 curveEnd;
        private bool curveValid;

        // Persistent endpoint sources (for arcs shown via TargetLineManager.Show2D).
        private TargetPoint? persistentA;
        private TargetPoint? persistentB;

        /// <summary>Initializes component references for the core/glow graphics and the bead.</summary>
        private void Awake()
        {
            hostRect = GetComponent<RectTransform>();
            var coreTransform = transform.Find("Core");
            if (coreTransform != null) coreGraphic = coreTransform.GetComponent<TargetLine2DGraphic>();
            var glowTransform = transform.Find("Glow");
            if (glowTransform != null) glowGraphic = glowTransform.GetComponent<TargetLine2DGraphic>();
            var beadTransform = transform.Find("Bead");
            if (beadTransform != null)
            {
                beadRT = beadTransform as RectTransform;
                beadImage = beadTransform.GetComponent<Image>();
            }
            ApplyColor();
        }

        /// <summary>Sets the arc's color — applied to core, glow, and bead with alpha multipliers.</summary>
        public void SetColor(Color c)
        {
            lineColor = c;
            ApplyColor();
        }

        /// <summary>Binds this arc to live endpoint sources so it follows moving actors / UI each frame.</summary>
        public void BindEndpoints(TargetPoint a, TargetPoint b)
        {
            persistentA = a;
            persistentB = b;
        }

        /// <summary>Clears bound endpoints.</summary>
        public void UnbindEndpoints()
        {
            persistentA = null;
            persistentB = null;
        }

        /// <summary>Per-frame: resolve endpoints → canvas-local, rebuild arc, advance bead.</summary>
        private void Update()
        {
            if (persistentA.HasValue && persistentB.HasValue)
            {
                var cam = Camera.main;
                var worldA = persistentA.Value.GetWorldPosition(cam);
                var worldB = persistentB.Value.GetWorldPosition(cam);
                UpdateArcPoints(worldA, worldB);
            }

            if (beadRT != null && curveValid)
            {
                float t = (Time.time * TargetLineInstanceConfig.BeadLoopsPerSecond) % 1f;
                var p = EvaluateBezier(curveStart, curveControl, curveEnd, t);
                beadRT.anchoredPosition = p;
                float pulse = 1f + TargetLineInstanceConfig.BeadPulseAmplitude
                              * Mathf.Sin(Time.time * TargetLineInstanceConfig.BeadPulseHz * Mathf.PI * 2f);
                float size = TargetLineInstanceConfig.BeadSize2D * pulse;
                beadRT.sizeDelta = new Vector2(size, size);
            }
        }

        /// <summary>Updates the arc using TargetPoint endpoints (world, canvas, or actor).</summary>
        public void UpdateArcPoints(TargetPoint a, TargetPoint b)
        {
            var cam = Camera.main;
            UpdateArcPoints(a.GetWorldPosition(cam), b.GetWorldPosition(cam));
        }

        /// <summary>Projects two world positions into canvas-local space and rebuilds the arc mesh.</summary>
        public void UpdateArcPoints(Vector3 worldStart, Vector3 worldEnd)
        {
            if (hostRect == null || coreGraphic == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            // ScreenSpaceOverlay canvases use null as the reference camera for screen→local.
            Vector2 screenA = cam.WorldToScreenPoint(worldStart);
            Vector2 screenB = cam.WorldToScreenPoint(worldEnd);
            Vector2 localA, localB;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect, screenA, null, out localA);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect, screenB, null, out localB);

            int segments = TargetLineInstanceConfig.Segments;
            float distance = Vector2.Distance(localA, localB);

            // Degenerate (endpoints coincide): collapse the polyline so no stray quad draws.
            if (distance < 0.001f)
            {
                curveStart = localA; curveControl = localA; curveEnd = localB; curveValid = true;
                PopulatePoints(coreGraphic, localA, localA, localA, segments);
                PopulatePoints(glowGraphic, localA, localA, localA, segments);
                return;
            }

            // Bow the arc perpendicular to the chord in 2D. "Up" in canvas space means higher-y
            // — we always want the bow pointing toward the top of the screen, so flip the sign
            // of the perpendicular if its y is negative.
            var chord = localB - localA;
            var perp = new Vector2(-chord.y, chord.x).normalized;
            if (perp.y < 0f) perp = -perp;

            float dynamicHeight = distance * TargetLineInstanceConfig.ArcHeightFraction;
            var control = Vector2.Lerp(localA, localB, 0.5f) + perp * dynamicHeight;
            curveStart = localA; curveControl = control; curveEnd = localB; curveValid = true;

            PopulatePoints(coreGraphic, localA, control, localB, segments);
            PopulatePoints(glowGraphic, localA, control, localB, segments);
        }

        /// <summary>Samples a quadratic Bezier into the graphic's Points list and requests a re-mesh.</summary>
        private static void PopulatePoints(TargetLine2DGraphic graphic, Vector2 p0, Vector2 p1, Vector2 p2, int segments)
        {
            if (graphic == null) return;
            graphic.Points.Clear();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                graphic.Points.Add(EvaluateBezier(p0, p1, p2, t));
            }
            graphic.SetVerticesDirty();
        }

        /// <summary>Quadratic Bezier (2D) B(t) = (1-t)²·p0 + 2(1-t)t·p1 + t²·p2.</summary>
        private static Vector2 EvaluateBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        /// <summary>Applies the current line color (with alpha multiplier) to core, glow, and bead.</summary>
        private void ApplyColor()
        {
            float coreA = alpha;
            float glowA = alpha * TargetLineInstanceConfig.GlowAlpha;
            if (coreGraphic != null) coreGraphic.color = new Color(lineColor.r, lineColor.g, lineColor.b, coreA);
            if (glowGraphic != null) glowGraphic.color = new Color(lineColor.r, lineColor.g, lineColor.b, glowA);
            if (beadImage != null) beadImage.color = new Color(lineColor.r, lineColor.g, lineColor.b, alpha);
        }

        /// <summary>Fades out and destroys the arc.</summary>
        public void Despawn()
        {
            StartCoroutine(DespawnRoutine());
        }

        private IEnumerator DespawnRoutine()
        {
            yield return FadeRoutine(alpha, 0f);
            Destroy(gameObject);
        }

        private IEnumerator FadeRoutine(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < TargetLineInstanceConfig.FadeDuration)
            {
                elapsed += Time.deltaTime;
                alpha = Mathf.Lerp(from, to, elapsed / TargetLineInstanceConfig.FadeDuration);
                ApplyColor();
                yield return Wait.None();
            }
            alpha = to;
            ApplyColor();
        }
    }
}
