using UnityEngine;
using Scripts.Models;

namespace Scripts.Utilities
{
    /// <summary>
    /// PROJECTILEMOTION - Static math for the different projectile path shapes
    /// (<see cref="ProjectileMotion"/>). Each method takes start/end positions, an optional live
    /// target (for homing), and a 0..1 progress, and returns a world position on the curve at
    /// that progress.
    ///
    /// <para>Pure math — no Unity GameObject creation. The projectile factory calls
    /// <see cref="Evaluate"/> each frame and assigns the result to the projectile's transform.</para>
    /// </summary>
    public static class ProjectileMotionEval
    {
        /// <summary>Evaluates the position along <paramref name="motion"/> at progress
        /// <paramref name="t"/> (0..1). <paramref name="target"/> can be null for non-homing motions.</summary>
        public static Vector3 Evaluate(
            global::Scripts.Models.ProjectileMotion motion,
            Vector3 from,
            Vector3 to,
            Transform target,
            float t)
        {
            t = Mathf.Clamp01(t);
            switch (motion)
            {
                case global::Scripts.Models.ProjectileMotion.None:    return from;
                case global::Scripts.Models.ProjectileMotion.Straight: return Vector3.Lerp(from, to, t);
                case global::Scripts.Models.ProjectileMotion.Bezier:   return Bezier(from, to, t, arcHeight: 1.2f);
                case global::Scripts.Models.ProjectileMotion.Homing:   return Homing(from, target != null ? target.position : to, t);
                case global::Scripts.Models.ProjectileMotion.Spiral:   return Spiral(from, to, t, turns: 1.5f, radius: 0.5f);
                case global::Scripts.Models.ProjectileMotion.Twist:    return Spiral(from, to, t, turns: 0.75f, radius: 0.3f);
                case global::Scripts.Models.ProjectileMotion.Strike:   return Strike(from, to, t, dropFromHeight: 4f);
                default: return Vector3.Lerp(from, to, t);
            }
        }

        /// <summary>Quadratic Bezier with vertical apex — a tossing arc.</summary>
        public static Vector3 Bezier(Vector3 from, Vector3 to, float t, float arcHeight)
        {
            var mid = (from + to) * 0.5f + Vector3.up * arcHeight;
            float u = 1f - t;
            return u * u * from + 2f * u * t * mid + t * t * to;
        }

        /// <summary>Re-lerps toward the (potentially moving) live target each step.</summary>
        public static Vector3 Homing(Vector3 from, Vector3 liveTarget, float t)
        {
            // Ease toward the moving target — accelerates as it approaches.
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            return Vector3.Lerp(from, liveTarget, eased);
        }

        /// <summary>Corkscrew toward target — straight-line base + perpendicular sin/cos offsets.</summary>
        public static Vector3 Spiral(Vector3 from, Vector3 to, float t, float turns, float radius)
        {
            var basePos = Vector3.Lerp(from, to, t);
            var dir = (to - from).normalized;
            // Pick a perpendicular axis (world up cross dir; fall back if degenerate).
            var perp = Vector3.Cross(dir, Vector3.up);
            if (perp.sqrMagnitude < 0.001f) perp = Vector3.right;
            perp.Normalize();
            var perp2 = Vector3.Cross(dir, perp);
            // Fade the radius so it tightens into the target on landing.
            float r = radius * (1f - t);
            float a = t * Mathf.PI * 2f * turns;
            return basePos + perp * (Mathf.Cos(a) * r) + perp2 * (Mathf.Sin(a) * r);
        }

        /// <summary>Top-down strike — start high above target, fall straight down at the end of t.</summary>
        public static Vector3 Strike(Vector3 from, Vector3 to, float t, float dropFromHeight)
        {
            // First 40% of t: get above the target (lateral). Last 60%: drop down on it.
            if (t < 0.4f)
            {
                float lateralT = t / 0.4f;
                var above = to + Vector3.up * dropFromHeight;
                return Vector3.Lerp(from, above, lateralT);
            }
            float dropT = (t - 0.4f) / 0.6f;
            var aboveTarget = to + Vector3.up * dropFromHeight;
            return Vector3.Lerp(aboveTarget, to, dropT * dropT); // accelerating fall
        }
    }
}
