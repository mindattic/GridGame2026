using System.Collections.Generic;
using UnityEngine;

namespace Assets.Helper
{
    /// <summary>
    /// Provides robust Bezier control point generation for distinct projectile travel styles.
    /// All public methods return a control point chain for cubic Bezier evaluation.
    /// The list format is: P0, C1, C2, P1, C1, C2, P2, ... where each 3-tuple after P0 adds a segment.
    /// </summary>
    public static class BezierCurveHelper
    {
        // --------------------------------------------------------------------
        // Public Styles
        // --------------------------------------------------------------------

        /// <summary>
        /// Very gentle S-curve from start to target.
        /// Produces a single soft weave with mild lateral and vertical variation that scales with distance.
        /// </summary>
        public static List<Vector3> Gentle(Vector3 startPosition, ActorInstance target, float lateralScale = 0.12f, float verticalScale = 0.06f, float smoothness = 0.0f)
        {
            Vector3 start = startPosition;
            Vector3 end = target.Position;

            float distance = Vector3.Distance(start, end);
            if (distance < 1e-6f)
                return SingleSegment(start);

            Vector3 dir = (end - start).normalized;
            Vector3 perp = SafePerpendicular(dir);

            float lateral = distance * Mathf.Max(0f, lateralScale);
            float up = distance * Mathf.Max(0f, verticalScale);

            List<Vector3> anchors = new List<Vector3>(4);

            Vector3 a1 = start + dir * (distance * 0.33f) + perp * (lateral * 0.7f) + Vector3.up * (up * 0.6f);
            Vector3 a2 = start + dir * (distance * 0.66f) - perp * (lateral * 0.7f) - Vector3.up * (up * 0.4f);

            anchors.Add(start);
            anchors.Add(a1);
            anchors.Add(a2);
            anchors.Add(end);

            return BuildBezierChain(anchors, smoothness);
        }

        /// <summary>
        /// Wiggle path that oscillates left and right while moving toward the target.
        /// Creates multiple alternating offsets perpendicular to travel. Use waves to control the count.
        /// </summary>
        public static List<Vector3> Wiggle(Vector3 startPosition, ActorInstance target, int waves = 4, float amplitudeScale = 0.18f, float verticalBobScale = 0.05f, float smoothness = 0.0f)
        {
            Vector3 start = startPosition;
            Vector3 end = target.Position;

            float distance = Vector3.Distance(start, end);
            if (distance < 1e-6f)
                return SingleSegment(start);

            Vector3 dir = (end - start).normalized;
            Vector3 perp = SafePerpendicular(dir);

            float amp = distance * Mathf.Max(0f, amplitudeScale);
            float bob = distance * Mathf.Max(0f, verticalBobScale);

            int k = Mathf.Max(1, waves);
            List<Vector3> anchors = new List<Vector3>(k + 2);
            anchors.Add(start);

            for (int i = 1; i <= k; i++)
            {
                float t = (float)i / (k + 1);
                float sign = (i % 2 == 1) ? 1f : -1f;

                Vector3 p = start + dir * (distance * t)
                                 + perp * (amp * sign)
                                 + Vector3.up * (bob * Mathf.Sin(Mathf.PI * t));
                anchors.Add(p);
            }

            anchors.Add(end);
            return BuildBezierChain(anchors, smoothness);
        }

        /// <summary>
        /// Lobbed toss like a grenade with optional forward bounces at the end.
        /// The initial arc peaks near mid travel. Bounces are smaller forward hops that decay.
        /// </summary>
        public static List<Vector3> Lobbed(Vector3 startPosition, ActorInstance target, float peakHeightScale = 0.9f, int bounces = 2, float bounceForwardScale = 0.25f, float bounceHeightScale = 0.35f, float bounceDamping = 0.5f, float smoothness = 0.0f)
        {
            Vector3 start = startPosition;
            Vector3 end = target.Position;

            float distance = Vector3.Distance(start, end);
            if (distance < 1e-6f)
                return SingleSegment(start);

            Vector3 dir = (end - start).normalized;

            float peak = distance * Mathf.Max(0.05f, peakHeightScale);
            float forward = distance * Mathf.Max(0f, bounceForwardScale);
            float bounceH = distance * Mathf.Max(0f, bounceHeightScale);

            List<Vector3> anchors = new List<Vector3>();

            Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * peak;
            anchors.Add(start);
            anchors.Add(mid);
            anchors.Add(end);

            Vector3 landing = end;
            float fwd = forward;
            float h = bounceH;
            int n = Mathf.Max(0, bounces);

            for (int i = 0; i < n; i++)
            {
                Vector3 apex = landing + dir * (fwd * 0.5f) + Vector3.up * h;
                Vector3 nextLanding = landing + dir * fwd;

                anchors.Add(apex);
                anchors.Add(nextLanding);

                landing = nextLanding;
                fwd *= Mathf.Clamp01(bounceDamping);
                h *= Mathf.Clamp01(bounceDamping);
                if (fwd < 0.01f * distance || h < 0.01f * distance)
                    break;
            }

            return BuildBezierChain(anchors, smoothness);
        }

        /// <summary>
        /// Homing spiral that orbits the target while closing in.
        /// Generates a decaying spiral on the horizontal plane around target, then finishes at the target.
        /// </summary>
        public static List<Vector3> HomingSpiral(Vector3 startPosition, ActorInstance target, int turns = 2, int pointsPerTurn = 4, float startRadiusScale = 0.4f, float endRadiusScale = 0.05f, float verticalDriftScale = 0.15f, float smoothness = 0.0f)
        {
            Vector3 start = startPosition;
            Vector3 center = target.Position;

            float d = Vector3.Distance(start, center);
            if (d < 1e-6f)
                return SingleSegment(start);

            Vector3 up = Vector3.up;

            Vector3 planar = start - center;
            planar.y = 0f;
            float r0 = planar.magnitude;
            if (r0 < 1e-4f)
                r0 = d * Mathf.Max(0.05f, startRadiusScale);

            float rStart = Mathf.Max(0.01f, r0 * Mathf.Max(0.01f, startRadiusScale));
            float rEnd = Mathf.Max(0.001f, r0 * Mathf.Max(0.0f, endRadiusScale));

            Vector3 basisX = planar.sqrMagnitude > 1e-8f ? planar.normalized : Vector3.right;
            Vector3 basisZ = Vector3.Cross(up, basisX).normalized;

            int segments = Mathf.Max(1, turns) * Mathf.Max(3, pointsPerTurn);
            List<Vector3> anchors = new List<Vector3>(segments + 2);
            anchors.Add(start);

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * Mathf.PI * 2f * Mathf.Max(1, turns);
                float radius = Mathf.Lerp(rStart, rEnd, t);

                Vector3 radial = basisX * Mathf.Cos(angle) + basisZ * Mathf.Sin(angle);
                Vector3 pos = center + radial * radius;

                float y = Mathf.Lerp(start.y, center.y, t) + (d * verticalDriftScale) * Mathf.Sin(angle * 0.5f) * (1f - t);
                pos.y = y;

                anchors.Add(pos);
            }

            anchors.Add(center);
            return BuildBezierChain(anchors, smoothness);
        }

        /// <summary>
        /// Overshooting path that launches indirectly, passes the target, then curves back to hit it.
        /// The path first heads off-axis, then overshoots forward beyond the target, then returns.
        /// </summary>
        public static List<Vector3> Overshooting(Vector3 startPosition, ActorInstance target, float sideOffsetScale = 0.25f, float forwardLeadScale = 0.35f, float overshootForwardScale = 0.25f, float overshootHeightScale = 0.12f, float smoothness = 0.1f)
        {
            Vector3 start = startPosition;
            Vector3 end = target.Position;

            float distance = Vector3.Distance(start, end);
            if (distance < 1e-6f)
                return SingleSegment(start);

            Vector3 dir = (end - start).normalized;
            Vector3 perp = SafePerpendicular(dir);

            float side = distance * Mathf.Max(0f, sideOffsetScale);
            float lead = distance * Mathf.Max(0f, forwardLeadScale);
            float over = distance * Mathf.Max(0f, overshootForwardScale);
            float overH = distance * Mathf.Max(0f, overshootHeightScale);

            List<Vector3> anchors = new List<Vector3>(5);

            Vector3 indirect = start + dir * lead + perp * side + Vector3.up * (overH * 0.5f);
            Vector3 beyond = end + dir * over + Vector3.up * overH;
            Vector3 preReturn = Vector3.Lerp(beyond, end, 0.5f) - perp * (side * 0.5f);

            anchors.Add(start);
            anchors.Add(indirect);
            anchors.Add(beyond);
            anchors.Add(preReturn);
            anchors.Add(end);

            return BuildBezierChain(anchors, smoothness);
        }

        // --------------------------------------------------------------------
        // Utilities for variations and multi-projectile effects
        // --------------------------------------------------------------------

        /// <summary>
        /// Offsets a generated path laterally and vertically to create formations like flocks or volleys.
        /// Offset is relative to the overall start to end direction. Positive lateral is to the right.
        /// </summary>
        public static List<Vector3> OffsetPath(List<Vector3> controlPoints, Vector3 start, Vector3 end, float lateralOffset, float upOffset)
        {
            if (controlPoints == null || controlPoints.Count == 0)
                return new List<Vector3>();

            Vector3 dir = (end - start).sqrMagnitude > 1e-6f ? (end - start).normalized : Vector3.forward;
            Vector3 perp = SafePerpendicular(dir);

            Vector3 offset = perp * lateralOffset + Vector3.up * upOffset;

            List<Vector3> shifted = new List<Vector3>(controlPoints.Count);
            for (int i = 0; i < controlPoints.Count; i++)
                shifted.Add(controlPoints[i] + offset);

            return shifted;
        }

        // --------------------------------------------------------------------
        // Core Spline Builder
        // --------------------------------------------------------------------

        /// <summary>
        /// Converts an anchor list into a chained cubic Bezier control point list.
        /// Uses a Catmull-Rom style tangent with adjustable smoothness.
        /// smoothness in [0, 1]. Zero gives standard Catmull-Rom. One dampens tangents heavily.
        /// </summary>
        private static List<Vector3> BuildBezierChain(List<Vector3> anchors, float smoothness)
        {
            List<Vector3> cps = new List<Vector3>();
            if (anchors == null || anchors.Count == 0)
                return cps;

            if (anchors.Count == 1)
                return SingleSegment(anchors[0]);

            float s = Mathf.Clamp01(smoothness);
            float tangentScale = 0.5f * (1f - s);

            int n = anchors.Count - 1;

            cps.Add(anchors[0]);

            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = anchors[Mathf.Max(0, i - 1)];
                Vector3 p1 = anchors[i];
                Vector3 p2 = anchors[i + 1];
                Vector3 p3 = anchors[Mathf.Min(anchors.Count - 1, i + 2)];

                Vector3 m1 = (p2 - p0) * tangentScale;
                Vector3 m2 = (p3 - p1) * tangentScale;

                Vector3 c1 = p1 + m1 / 3f;
                Vector3 c2 = p2 - m2 / 3f;

                cps.Add(c1);
                cps.Add(c2);
                cps.Add(p2);
            }

            return cps;
        }

        // --------------------------------------------------------------------
        // Math Helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Computes a lateral perpendicular to the given direction with a safe fallback for near vertical travel.
        /// </summary>
        private static Vector3 SafePerpendicular(Vector3 direction)
        {
            const float nearParallel = 0.98f;

            float upDot = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up));
            if (upDot > nearParallel)
            {
                Vector3 p = Vector3.Cross(direction, Vector3.right);
                if (p.sqrMagnitude > 1e-8f) return p.normalized;
                p = Vector3.Cross(direction, Vector3.forward);
                return p.sqrMagnitude > 1e-8f ? p.normalized : Vector3.right;
            }

            Vector3 perp = Vector3.Cross(direction, Vector3.up);
            return perp.sqrMagnitude > 1e-8f ? perp.normalized : Vector3.right;
        }

        /// <summary>
        /// Produces a degenerate Bezier with a single point when start and target coincide.
        /// </summary>
        private static List<Vector3> SingleSegment(Vector3 point)
        {
            List<Vector3> cps = new List<Vector3>(4);
            cps.Add(point);
            cps.Add(point);
            cps.Add(point);
            cps.Add(point);
            return cps;
        }
    }
}
