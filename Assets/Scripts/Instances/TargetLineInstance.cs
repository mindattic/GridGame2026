using UnityEngine;
using UnityEngine.Rendering;
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
    /// TARGETLINEINSTANCE - FFXII-style targeting arc (Bezier + billboard strip + additive glow + traveling bead).
    /// <para>PURPOSE: Draws a tapered, glowing curved line between two endpoints. Endpoints can be
    /// world points, on-board actors, or canvas-overlay RectTransforms (via TargetPoint). Color is
    /// applied at runtime via SetColor — callers tint per targeting kind (e.g. red = selected enemy,
    /// cyan = heal cast).</para>
    /// <para>RENDER TECHNIQUE (FFXII):
    /// <list type="bullet">
    /// <item>Quadratic-Bezier curve sampled at TargetLineInstanceConfig.Segments points.</item>
    /// <item>Each segment is a camera-facing quad via Unity LineRenderer (billboard strip).</item>
    /// <item>Two passes: thin bright core + wider low-alpha halo, both additive — brightens the
    /// geometry underneath rather than occluding it.</item>
    /// <item>Tapered width curve: 0 → peak → 0 (sharp points at source and destination).</item>
    /// <item>Traveling bead slides source → destination at BeadLoopsPerSecond for direction cue.</item>
    /// </list>
    /// </para>
    /// <para>RELATED FILES: TargetLineFactory.cs, TargetLineManager.cs, TargetPoint.cs</para>
    /// </summary>
    public class TargetLineInstance : MonoBehaviour
    {
        public Transform parent
        {
            get => transform.parent;
            set => transform.SetParent(value, true);
        }

        public float alpha = 1f;
        private LineRenderer coreLR;
        private LineRenderer glowLR;
        private SpriteRenderer beadSR;
        private Color lineColor = new Color(0f, 1f, 1f, 1f); // default cyan
        private Vector3 curveStart;
        private Vector3 curveControl;
        private Vector3 curveEnd;
        private bool curveValid;

        // Persistent endpoint sources (for arcs shown via TargetLineManager.Show).
        private TargetPoint? persistentA;
        private TargetPoint? persistentB;

        public SortingGroup sortingGroup => GetComponent<SortingGroup>();

        /// <summary>Initializes component references, LineRenderers, and the direction bead.</summary>
        private void Awake()
        {
            coreLR = GetComponent<LineRenderer>();
            var glowTransform = transform.Find("Glow");
            if (glowTransform != null) glowLR = glowTransform.GetComponent<LineRenderer>();
            var beadTransform = transform.Find("Bead");
            if (beadTransform != null) beadSR = beadTransform.GetComponent<SpriteRenderer>();

            if (coreLR != null)
            {
                coreLR.positionCount = TargetLineInstanceConfig.Segments + 1;
                coreLR.useWorldSpace = true;
            }
            if (glowLR != null)
            {
                glowLR.positionCount = TargetLineInstanceConfig.Segments + 1;
                glowLR.useWorldSpace = true;
            }
            ApplyColor();
        }

        /// <summary>Sets the arc's color — applied to core, glow, and bead in additive-friendly form.</summary>
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

        /// <summary>Clears bound endpoints (arc will stop updating until new sources are bound).</summary>
        public void UnbindEndpoints()
        {
            persistentA = null;
            persistentB = null;
        }

        /// <summary>Runs per-frame update logic.</summary>
        private void Update()
        {
            // Follow bound endpoints.
            if (persistentA.HasValue && persistentB.HasValue)
            {
                var cam = Camera.main;
                var start = persistentA.Value.GetWorldPosition(cam);
                var end = persistentB.Value.GetWorldPosition(cam);
                UpdateArcPoints(start, end);
            }

            // Advance the direction bead along the arc.
            if (beadSR != null && curveValid)
            {
                float t = (Time.time * TargetLineInstanceConfig.BeadLoopsPerSecond) % 1f;
                var p = EvaluateBezier(curveStart, curveControl, curveEnd, t);
                beadSR.transform.position = p;
                float pulse = 1f + TargetLineInstanceConfig.BeadPulseAmplitude
                              * Mathf.Sin(Time.time * TargetLineInstanceConfig.BeadPulseHz * Mathf.PI * 2f);
                beadSR.transform.localScale = Vector3.one * TargetLineInstanceConfig.BeadSize * pulse;
            }
        }

        /// <summary>Updates arc using TargetPoint endpoints (world, canvas, or actor, in any combination).</summary>
        public void UpdateArcPoints(TargetPoint a, TargetPoint b)
        {
            var cam = Camera.main;
            UpdateArcPoints(a.GetWorldPosition(cam), b.GetWorldPosition(cam));
        }

        /// <summary>Updates the arc points — quadratic Bezier with dynamic arc height ∝ distance.</summary>
        public void UpdateArcPoints(Vector3 start, Vector3 end)
        {
            var cam = Camera.main;
            int segments = TargetLineInstanceConfig.Segments;
            float distance = Vector3.Distance(start, end);

            // Degenerate (endpoints coincide): collapse every segment to the same point so
            // a LineRenderer with a fresh widthCurve doesn't draw a stray zero-length quad.
            if (distance < 0.001f)
            {
                curveStart = start; curveControl = start; curveEnd = end; curveValid = true;
                for (int i = 0; i <= segments; i++)
                {
                    if (coreLR != null) coreLR.SetPosition(i, start);
                    if (glowLR != null) glowLR.SetPosition(i, start);
                }
                return;
            }

            // Bow perpendicular to the chord within the camera's viewing plane. Using cam.up
            // directly collapses the arc to a straight line whenever the chord is ~parallel
            // to cam.up (e.g. a timeline icon sitting directly above its actor) — the control
            // point just slides along the chord instead of away from it.
            var chord = end - start;
            var camForward = cam != null ? cam.transform.forward : Vector3.forward;
            var perp = Vector3.Cross(chord.normalized, camForward);
            if (perp.sqrMagnitude < 0.0001f)
                perp = cam != null ? cam.transform.up : Vector3.up;
            perp.Normalize();
            var camUp = cam != null ? cam.transform.up : Vector3.up;
            if (Vector3.Dot(perp, camUp) < 0f) perp = -perp;

            float dynamicHeight = distance * TargetLineInstanceConfig.ArcHeightFraction;
            var control = Vector3.Lerp(start, end, 0.5f) + perp * dynamicHeight;
            curveStart = start; curveControl = control; curveEnd = end; curveValid = true;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                var point = EvaluateBezier(start, control, end, t);
                if (coreLR != null) coreLR.SetPosition(i, point);
                if (glowLR != null) glowLR.SetPosition(i, point);
            }
        }

        /// <summary>Quadratic Bezier B(t) = (1-t)²·p0 + 2(1-t)t·p1 + t²·p2.</summary>
        private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        /// <summary>Applies the current line color (with alpha multiplier) to core, glow, and bead.</summary>
        private void ApplyColor()
        {
            float coreA = alpha;
            float glowA = alpha * TargetLineInstanceConfig.GlowAlpha;
            var core = new Color(lineColor.r, lineColor.g, lineColor.b, coreA);
            var glow = new Color(lineColor.r, lineColor.g, lineColor.b, glowA);
            if (coreLR != null) { coreLR.startColor = core; coreLR.endColor = core; }
            if (glowLR != null) { glowLR.startColor = glow; glowLR.endColor = glow; }
            if (beadSR != null) beadSR.color = new Color(lineColor.r, lineColor.g, lineColor.b, alpha);
        }

        /// <summary>Despawn with fade.</summary>
        public void Despawn()
        {
            StartCoroutine(DespawnRoutine());
        }

        /// <summary>Coroutine that executes the despawn sequence.</summary>
        private IEnumerator DespawnRoutine()
        {
            yield return FadeRoutine(alpha, 0f);
            Destroy(gameObject);
        }

        /// <summary>Coroutine that executes the fade sequence.</summary>
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
