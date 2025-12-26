using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// Runtime mover that advances a projectile using a coroutine and handles teardown.
    /// </summary>
    public class ProjectileNode
    {
        private readonly Transform transform;
        private readonly ProjectileSettings settings;
        private readonly float speedUnitsPerSec;
        private readonly float arriveRadiusUnits;

        private float t;

        private VisualEffectInstance trail;
        private string trailName;
        private bool trailSpawned;

        private Vector3 start;
        private Vector3 end;
        private Vector3 direction;

        private Vector3 basisRight;
        private Vector3 basisUp;
        private Vector3 basisOrtho;

        private Vector3 velocity;

        /// <summary>
        /// Build the node with tile scaled pacing and motion bases.
        /// </summary>
        public ProjectileNode(Transform transform, ProjectileSettings settings)
        {
            this.transform = transform;
            this.settings = settings;

            start = settings.startPosition;

            Transform destTf = this.settings.target != null ? this.settings.target.transform : this.settings.followTarget;
            end = destTf != null ? destTf.position : this.settings.staticTargetPosition;

            direction = (end - start).sqrMagnitude > 1e-8f ? (end - start).normalized : Vector3.forward;

            CalculateBasis(direction, out basisRight, out basisUp, out basisOrtho);

            float distance = Mathf.Max(0.001f, Vector3.Distance(start, end));
            float distTiles = distance / g.TileSize;

            float seconds = Mathf.Max(0.05f, this.settings.travelSeconds);
            float idealTilesPerSec = Mathf.Max(0.01f, distTiles / seconds);
            float tilesPerSec = Mathf.Clamp(idealTilesPerSec, Mathf.Max(0.01f, this.settings.minTilesPerSec), Mathf.Max(this.settings.minTilesPerSec + 0.01f, this.settings.maxTilesPerSec));
            speedUnitsPerSec = tilesPerSec * g.TileSize;

            arriveRadiusUnits = Mathf.Max(0.01f, this.settings.arriveRadiusTiles * g.TileSize);

            t = 0f;

            this.transform.position = start;
            if (this.settings.faceDirection) this.transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// Current world position of the node.
        /// </summary>
        public Vector3 position => transform != null ? transform.position : end;

        /// <summary>
        /// Parents a single trail instance to this node. Only spawns once.
        /// </summary>
        public void AttachTrail(VisualEffectAsset asset)
        {
            if (trailSpawned) return;
            trailSpawned = true;

            var instance = g.VisualEffectManager.SpawnInstance(asset, transform.position, transform, null);
            trail = instance;
            trailName = instance != null ? instance.name : null;

            // Ensure tile-aware scale for the trail in case parent hierarchy would otherwise shrink it
            if (trail != null && asset != null)
            {
                // Re-apply scale the same way as VFXInstance does (world = TileScale * RelativeScale)
                // This ensures consistency for projectile visuals regardless of parent scale.
                ApplyTrailWorldScale(trail.transform, g.TileScale, asset.RelativeScale);
            }
        }

        private void ApplyTrailWorldScale(Transform tf, Vector3 tileScale, Vector3 relative)
        {
            if (tf == null) return;
            Vector3 desiredWorld = new Vector3(
                Mathf.Max(1e-4f, tileScale.x * relative.x),
                Mathf.Max(1e-4f, tileScale.y * relative.y),
                Mathf.Max(1e-4f, (relative.z == 0f ? 1f : tileScale.z * relative.z))
            );

            Vector3 parentLossy = Vector3.one;
            if (tf.parent != null) parentLossy = tf.parent.lossyScale;
            float ix = parentLossy.x != 0f ? 1f / parentLossy.x : 1f;
            float iy = parentLossy.y != 0f ? 1f / parentLossy.y : 1f;
            float iz = parentLossy.z != 0f ? 1f / parentLossy.z : 1f;

            tf.localScale = new Vector3(desiredWorld.x * ix, desiredWorld.y * iy, desiredWorld.z * iz);
        }

        /// <summary>
        /// Gradually shrinks the projectile trail each frame and then disposes of the trail and node.
        /// </summary>
        public IEnumerator ShrinkAndDisposeRoutine(float perFrameScaleFactor = 0.97f, float minScale = 0.01f)
        {
            if (trail != null && trail.transform != null)
            {
                var tf = trail.transform;
                // Ensure we start from current local scale
                Vector3 s = tf.localScale;
                if (s == Vector3.zero)
                    s = Vector3.one;

                // Target 100ms fade: compute frames based on deltaTime average; keep simple frame loop cap
                float elapsed = 0f;
                const float targetSeconds = 0.1f; // 100 ms
                while (tf != null && tf.gameObject != null && tf.localScale.magnitude > minScale && elapsed < targetSeconds)
                {
                    tf.localScale = tf.localScale * perFrameScaleFactor;
                    elapsed += Time.deltaTime;
                    yield return null; // next frame
                }

                // Despawn the trail via VfxManager so it's unregistered
                if (!string.IsNullOrEmpty(trailName))
                    g.VisualEffectManager?.Despawn(trailName);
                else if (trail != null && trail.gameObject != null)
                    GameObject.Destroy(trail.gameObject);

                trail = null;
                trailName = null;
            }

            // Finally, destroy the node GameObject
            Cleanup();
        }

        /// <summary>
        /// Advance to destination.
        /// </summary>
        public IEnumerator TravelRoutine()
        {
            while (true)
            {
                Transform destTf = settings.target != null ? settings.target.transform : settings.followTarget;
                end = destTf != null ? destTf.position : settings.staticTargetPosition;

                direction = (end - start).sqrMagnitude > 1e-10f ? (end - start).normalized : direction;
                CalculateBasis(direction, out basisRight, out basisUp, out basisOrtho);

                StepParametric(Time.deltaTime);

                if (settings.faceDirection)
                {
                    Vector3 v = (transform.position - start);
                    if (v.sqrMagnitude > 1e-6f)
                        transform.rotation = Quaternion.LookRotation(v.normalized);
                }

                bool arrived = Vector3.Distance(transform.position, end) <= arriveRadiusUnits || t >= 0.9999f;
                if (arrived)
                {
                    transform.position = end;
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Clean up trail and node.
        /// </summary>
        public void Cleanup()
        {
             if (transform != null && transform.gameObject != null)
                GameObject.Destroy(transform.gameObject);
        }

        /// <summary>
        /// Linear parametric step for straight, wiggle, lobbed, and spiral variants.
        /// </summary>
        private void StepParametric(float dt)
        {
            float baseDist = Mathf.Max(0.001f, Vector3.Distance(start, end));
            float dtT = (speedUnitsPerSec / baseDist) * dt;
            t = Mathf.Clamp01(t + dtT);

            Vector3 p = EvaluateStyle(t);
            p.z = 0;
            transform.position = p;
        }

        /// <summary>
        /// Ricochet within optional bounds while biasing toward the target to ensure arrival.
        /// </summary>
        private void StepRicochet(float dt)
        {
            Vector3 pos = transform.position;

            Vector3 desiredDir = (end - pos);
            if (desiredDir.sqrMagnitude > 1e-8f)
            {
                desiredDir.Normalize();
                velocity = Vector3.Lerp(velocity, desiredDir * speedUnitsPerSec, Mathf.Clamp01(settings.ricochetBiasTowardTarget * dt));
            }

            pos += velocity * dt;

            if (settings.worldBoundsForRicochet.HasValue)
            {
                var b = settings.worldBoundsForRicochet.Value;

                if (pos.x < b.min.x || pos.x > b.max.x)
                {
                    velocity.x = -velocity.x;
                    pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
                }
                if (pos.y < b.min.y || pos.y > b.max.y)
                {
                    velocity.y = -velocity.y;
                    pos.y = Mathf.Clamp(pos.y, b.min.y, b.max.y);
                }
                if (pos.z < b.min.z || pos.z > b.max.z)
                {
                    velocity.z = -velocity.z;
                    pos.z = Mathf.Clamp(pos.z, b.min.z, b.max.z);
                }
            }

            transform.position = pos;
        }

        /// <summary>
        /// Evaluate current motion style at t.
        /// </summary>
        private Vector3 EvaluateStyle(float t)
        {
            switch (settings.motionStyle)
            {
                case MotionStyle.Straight:
                    return LerpLine(t);

                case MotionStyle.Wiggle:
                    {
                        float amp = settings.wiggleAmplitudeTiles * g.TileSize;
                        float angle = 2f * Mathf.PI * settings.wiggleHz * t;
                        return LerpLine(t) + basisRight * (amp * Mathf.Sin(angle));
                    }

                case MotionStyle.LobbedArc:
                    {
                        Vector3 p = LerpLine(t);
                        float h = Mathf.Sin(t * Mathf.PI) * (settings.lobbedHeightTiles * g.TileSize);
                        return p + basisUp * h;
                    }

                case MotionStyle.HomingSpiral:
                    {
                        Vector3 centerLine = Vector3.Lerp(start, end, t);
                        int turns = Mathf.Max(1, settings.spiralTurns);
                        float angle = t * turns * 2f * Mathf.PI;
                        float r0 = Mathf.Max(0.01f, settings.spiralStartRadiusTiles * g.TileSize);
                        float radius = Mathf.Lerp(r0, 0f, t);
                        Vector3 radial = basisRight * Mathf.Cos(angle) + basisOrtho * Mathf.Sin(angle);
                        return centerLine + radial * radius;
                    }

                default:
                    return LerpLine(t);
            }
        }

        private Vector3 LerpLine(float t)
        {
            return Vector3.Lerp(start, end, t);
        }

        private static void CalculateBasis(Vector3 dir, out Vector3 right, out Vector3 up, out Vector3 ortho)
        {
            var n = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.forward;

            up = Vector3.up;
            float d = Mathf.Abs(Vector3.Dot(n, up));
            if (d > 0.98f)
                up = Vector3.right;

            right = Vector3.Cross(up, n).normalized;
            ortho = Vector3.Cross(n, right).normalized;
            up = Vector3.Cross(n, right).normalized;
        }
    }
}
