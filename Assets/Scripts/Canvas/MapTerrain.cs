//using UnityEngine;

//// Collider-based collision provider for the overworld.
//// Replaces all black/white pixel sampling with Physics2D queries against obstacle colliders.
//[ExecuteAlways]
//public sealed class MapTerrain : MonoBehaviour
//{
//    [Header("Physics2D Sampling")]
//    [Tooltip("Layer mask for obstacle colliders that the hero should not walk through.")]
//    [SerializeField] private LayerMask obstacleMask = ~0;

//    [Tooltip("Small radius used for point overlap checks.")]
//    [SerializeField, Min(0f)] private float pointCheckRadius = 0.02f;

//    // No-op retained for back-compat with callers
//    public void ForceRefresh() { }

//    // True if there is no obstacle collider at the point.
//    public bool IsWalkableLocal(Vector2 p)
//    {
//        return Physics2D.OverlapCircle(p, Mathf.Max(0.0001f, pointCheckRadius), obstacleMask) == null;
//    }

//    // Optional radial probe version retained for API compatibility.
//    public bool IsWalkableLocal(Vector2 p, float probeRadius, int probeRays)
//    {
//        if (!IsWalkableLocal(p)) return false;
//        if (probeRadius <= 0f || probeRays <= 0) return true;

//        float step = 360f / probeRays;
//        for (int i = 0; i < probeRays; i++)
//        {
//            float ang = step * i * Mathf.Deg2Rad;
//            Vector2 offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * probeRadius;
//            if (!IsWalkableLocal(p + offset))
//                return false;
//        }
//        return true;
//    }

//    // Estimate an outward normal (away from nearby obstacles) using short raycasts.
//    // Returns Vector2.zero if no obstacles are detected within the sample radius.
//    public Vector2 EstimateObstacleNormal(Vector2 p, float sampleRadius = 0.2f, int rays = 8)
//    {
//        if (sampleRadius <= 0f || rays <= 0) return Vector2.zero;

//        Vector2 towardBlocked = Vector2.zero;
//        float step = 360f / Mathf.Max(1, rays);
//        for (int i = 0; i < rays; i++)
//        {
//            float ang = step * i * Mathf.Deg2Rad;
//            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
//            var hit = Physics2D.Raycast(p, dir, sampleRadius, obstacleMask);
//            if (hit.collider != null)
//            {
//                // Blocked in this direction – accumulate the direction vector
//                towardBlocked += dir;
//            }
//        }

//        if (towardBlocked.sqrMagnitude < 1e-6f)
//            return Vector2.zero;

//        // Outward normal is away from blocked area
//        return (-towardBlocked).normalized;
//    }
//}