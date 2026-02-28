// --- File: Assets/Scripts/Instances/Actor/ActorAnimation.cs ---
using Assets.Helper;
using Assets.Scripts.Models;
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Instances.Actor
{
    /// <summary>
    /// ACTORANIMATION - Coroutine-based animation system for actors.
    /// 
    /// PURPOSE:
    /// Provides reusable animation routines for actor visual effects
    /// including shaking, dodging, bumping, growing, spinning, and fading.
    /// 
    /// AVAILABLE ANIMATIONS:
    /// ```
    /// Shake     - Rapid position jitter (damage feedback)
    /// Dodge     - Quick sidestep motion (miss/evade)
    /// Bump      - Forward lunge and return (attack motion)
    /// Grow      - Scale pulse effect (power up)
    /// Spin      - Rotation effect (special ability)
    /// Fade      - Alpha transition (death/spawn)
    /// Wiggle    - Oscillating rotation (idle/attention)
    /// ```
    /// 
    /// USAGE:
    /// ```csharp
    /// actor.Animation.Shake(0.5f, 0.3f);
    /// actor.Animation.Bump(target.Position, onComplete: DoNextAction);
    /// ```
    /// 
    /// COROUTINE BASED:
    /// All animations are coroutines that can be chained or
    /// run in parallel. Optional completion callbacks supported.
    /// 
    /// RELATED FILES:
    /// - ActorInstance.cs: Owns the Animation component
    /// - ActorRenderers.cs: Provides visual components to animate
    /// - ActorMovement.cs: Movement-specific animations
    /// </summary>
    public class ActorAnimation
    {
        #region References

        protected ActorRenderers render => instance.Render;
        protected ActorStats stats => instance.Stats;
        private bool isActive => instance.IsActive;
        private bool isAlive => instance.IsAlive;
        private bool isPlaying => instance.IsPlaying;
        private Quaternion rotation { get => instance.Rotation; set => instance.Rotation = value; }
        private Vector3 position { get => instance.Position; set => instance.Position = value; }
        private Vector3 scale { get => instance.Scale; set => instance.Scale = value; }

        private ActorInstance instance;
        private float wiggleFocus;
        private float wiggleAmplitude;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes this Animation module for the owning actor.
        /// </summary>
        public void Initialize(ActorInstance parentInstance)
        {
            instance = parentInstance;

            wiggleFocus = g.TileSize * 48f;
            wiggleAmplitude = 15f;
        }

        #endregion

        #region Shake Animation

        /// <summary>
        /// Triggers a shake animation on the actor (damage feedback).
        /// </summary>
        public void Shake(float intensity, float duration = 0f, IEnumerator routine = null)
        {
            if (!isActive || !isAlive)
                return;

            instance.StartCoroutine(ShakeRoutine(intensity, duration, routine));
        }

        /// <summary>
        /// Coroutine that applies randomized positional offsets for shake effect.
        /// </summary>
        private IEnumerator ShakeRoutine(float intensity, float duration, IEnumerator routine = null)
        {
            var originalPosition = instance.currentTile.position;
            float elapsedTime = 0f;

            if (intensity <= 0f || duration <= 0f)
                yield break;

            while (intensity > 0f && elapsedTime < duration)
            {
                var shakeOffset = new Vector3(
                    RNG.Float(-intensity, intensity),
                    RNG.Float(-intensity, intensity),
                    0f
                );

                instance.ThumbnailPosition = originalPosition + shakeOffset;

                yield return Wait.OneTick();

                if (duration > 0f)
                    elapsedTime += Interval.OneTick;
            }

            if (routine != null)
                yield return instance.StartCoroutine(routine);

            instance.ThumbnailPosition = originalPosition;
        }

        /// <summary>
        /// ProcessRoutine the dodge Animation as a fire and forget. Optional routine runs at the midpoint.
        /// </summary>
        public void Dodge(IEnumerator routine = null)
        {
            if (!isActive || !isAlive)
                return;

            instance.StartCoroutine(DodgeRoutine(routine));
        }

        /// <summary>
        /// Executes a two phase dodge where the actor twists forward then returns to the original state.
        /// If a routine routine is provided, it runs after the forward twist completes.
        /// </summary>
        public IEnumerator DodgeRoutine(IEnumerator routine = null)
        {
            var rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            var scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.9f);

            float duration = 0.075f;
            float returnDuration = 0.2f;

            var startRotation = Vector3.zero;
            var targetRotation = new Vector3(15f, 70f, 15f);

            var randomDirection = new Vector3(
               RNG.Boolean ? -1f : 1f,
               RNG.Boolean ? -1f : 1f,
               RNG.Boolean ? -1f : 1f
            );

            float elapsedTime = 0f;
            Coroutine runningCoroutine = null;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);

                float curveValue = rotationCurve.Evaluate(progress);
                Vector3 currentRotation = Vector3.LerpUnclamped(startRotation, targetRotation, curveValue);
                currentRotation.Scale(randomDirection);

                float scaleFactor = scaleCurve.Evaluate(progress);
                scale = g.TileScale * scaleFactor;

                rotation = Geometry.Rotation(currentRotation);

                yield return Wait.None();
            }

            // Run additional routine (if provided)
            if (routine != null && runningCoroutine == null)
                yield return instance.StartCoroutine(routine);

            elapsedTime = 0f;
            while (elapsedTime < returnDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / returnDuration);

                float curveValue = rotationCurve.Evaluate(progress);
                Vector3 currentRotation = Vector3.LerpUnclamped(targetRotation, startRotation, curveValue);
                currentRotation.Scale(randomDirection);

                float scaleFactor = Mathf.LerpUnclamped(0.9f, 1f, progress);
                scale = g.TileScale * scaleFactor;

                rotation = Geometry.Rotation(currentRotation);

                yield return Wait.OneTick();
            }

            scale = g.TileScale;
            rotation = Geometry.Rotation(Vector3.zero);
        }

        /// <summary>
        /// Starts a bump animation toward the target. Optional routine runs at the bump apex.
        /// </summary>
        public void Bump(ActorInstance target, IEnumerator routine = null)
            => instance.StartCoroutine(BumpRoutine(target, routine));

        /// <summary>
        /// BumpRoutine sequence:
        /// 1) Windup backward.
        /// 2) Lunge forward to apex and optionally run the routine routine.
        /// 3) Return to start.
        /// </summary>
        /// 
        public IEnumerator BumpRoutine(ActorInstance target, IEnumerator routine = null)
        {
            g.SortingManager.OnBump(instance, target);

            var direction = instance.GetDirectionTo(target, mustBeAdjacent: true);

            var windupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            var bumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            var returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            var windupDuration = 0.15f;
            var bumpDuration = 0.1f;
            var returnDuration = 0.3f;

            var startPosition = instance.currentTile.position;
            var windupPosition = Geometry.GetDirectionalPosition(startPosition, direction.Opposite(), g.TileSize * Increment.Percent33);
            var bumpPosition = Geometry.GetDirectionalPosition(startPosition, direction, g.TileSize * Increment.Percent33);

            float elapsedTime;

            elapsedTime = 0f;
            while (elapsedTime < windupDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / windupDuration);
                position = Vector3.Lerp(startPosition, windupPosition, windupCurve.Evaluate(progress));
                yield return Wait.OneTick();
            }

            position = windupPosition;

            elapsedTime = 0f;
            float targetRotationZ = (direction == Direction.East) ? -15f : 15f;

            while (elapsedTime < bumpDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / bumpDuration);
                position = Vector3.Lerp(windupPosition, bumpPosition, bumpCurve.Evaluate(progress));
                rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, targetRotationZ, progress));
                yield return Wait.OneTick();
            }

            position = bumpPosition;
            rotation = Quaternion.Euler(0f, 0f, targetRotationZ);

            //Bump has reached it's apex:
            if (routine != null)
                // BLOCK until the impact routine completes (e.g., dodge on miss)
                yield return instance.StartCoroutine(routine);


            elapsedTime = 0f;
            while (elapsedTime < returnDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / returnDuration);
                position = Vector3.Lerp(bumpPosition, startPosition, returnCurve.Evaluate(progress));
                rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(targetRotationZ, 0f, progress));
                yield return Wait.OneTick();
            }

            position = startPosition;
            rotation = Quaternion.identity;
        }

        /// <summary>
        /// ProcessRoutine a growth Animation. Optional routine runs after growth finishes.
        /// </summary>
        public void Grow(float maxSize = 0f, IEnumerator routine = null)
        {
            if (!instance.IsActive)
                return;

            instance.StartCoroutine(GrowRoutine(maxSize, routine));
        }

        /// <summary>
        /// Increases the actor scale up to a maximum, then optionally runs the routine routine.
        /// </summary>
        public IEnumerator GrowRoutine(float maxSize = 0f, IEnumerator routine = null)
        {
            float targetMax = maxSize > 0f ? maxSize : g.TileSize * 1.1f;
            float minSize = scale.x;
            float increment = g.TileSize * 0.01f;
            float size = minSize;
            scale = new Vector3(size, size, 0f);

            while (size < targetMax)
            {
                size += increment;
                size = Mathf.Clamp(size, minSize, targetMax);
                scale = new Vector3(size, size, 0f);
                yield return Wait.OneTick();
            }

            if (routine != null)
                yield return instance.StartCoroutine(routine);

            scale = new Vector3(targetMax, targetMax, 0f);
        }

        /// <summary>
        /// ProcessRoutine a shrink Animation. Optional routine runs after shrink finishes.
        /// </summary>
        public void Shrink(float minSize = 0f, IEnumerator routine = null)
        {
            if (!instance.IsActive)
                return;

            instance.StartCoroutine(ShrinkRoutine(minSize, routine));
        }

        /// <summary>
        /// Decreases the actor scale down to a minimum, then optionally runs the routine routine.
        /// </summary>
        public IEnumerator ShrinkRoutine(float minSize = 0f, IEnumerator routine = null)
        {
            float targetMin = minSize > 0f ? minSize : g.TileSize;
            float maxSize = scale.x;
            float increment = g.TileSize * 0.01f;
            float size = maxSize;
            scale = new Vector3(size, size, 0f);

            while (size > targetMin)
            {
                size -= increment;
                size = Mathf.Clamp(size, targetMin, maxSize);
                scale = new Vector3(size, size, 0f);
                yield return Wait.OneTick();
            }

            if (routine != null)
                yield return instance.StartCoroutine(routine);

            scale = new Vector3(targetMin, targetMin, 0f);
        }

        /// <summary>
        /// ProcessRoutine a 90 degree spin. Optional routine runs at the 90 degree point.
        /// </summary>
        public void Spin90(IEnumerator routine = null)
        {
            if (!isActive || !isAlive)
                return;

            instance.StartCoroutine(Spin90Routine(routine));
        }

        /// <summary>
        /// Rotates the actor 90 degrees around Y, optionally runs the routine routine at 90,
        /// then rotates back to zero.
        /// </summary>
        private IEnumerator Spin90Routine(IEnumerator routine = null)
        {
            bool hasTriggered = false;
            float rotY = 0f;
            float spinFocus = g.TileSize * 24f;
            rotation = Geometry.Rotation(0f, rotY, 0f);
            Coroutine runningCoroutine = null;

            bool isDone = false;
            while (!isDone)
            {
                rotY += !hasTriggered ? spinFocus : -spinFocus;

                if (!hasTriggered && rotY >= 90f)
                {
                    rotY = 90f;

                    // Run additional routine (if provided)
                    if (routine != null && runningCoroutine == null)
                        yield return instance.StartCoroutine(routine);

                    hasTriggered = true;
                }

                isDone = hasTriggered && rotY <= 0f;
                if (isDone)
                    rotY = 0f;

                rotation = Geometry.Rotation(0f, rotY, 0f);
                yield return Wait.OneTick();
            }

            rotation = Geometry.Rotation(0f, 0f, 0f);
        }

        /// <summary>
        /// ProcessRoutine a 360 degree spin. Optional routine runs after 240 degrees.
        /// </summary>
        public void Spin360(IEnumerator routine = null)
        {
            if (!isActive || !isAlive)
                return;

            instance.StartCoroutine(Spin360Routine(routine));
        }

        /// <summary>
        /// New: same as Spin360 but yields until the spin completes, for sequence control.
        /// </summary>
        public IEnumerator Spin360AndWaitRoutine(IEnumerator routine = null)
        {
            if (!isActive || !isAlive)
                yield break;

            yield return Spin360Routine(routine);
        }

        /// <summary>
        /// Rotates the actor 360 degrees around Y. If a routine routine is provided,
        /// it runs once after passing 240 degrees.
        /// </summary>
        private IEnumerator Spin360Routine(IEnumerator routine = null)
        {
            bool hasTriggered = false;
            float rotY = 0f;
            float speed = g.TileSize * 24f;
            rotation = Geometry.Rotation(0f, rotY, 0f);
            Coroutine runningCoroutine = null;

            bool isDone = false;
            while (!isDone)
            {
                rotY += speed;
                rotation = Geometry.Rotation(0f, rotY, 0f);

                if (!hasTriggered && rotY >= 240f)
                {
                    // Run additional routine (if provided)
                    if (routine != null && runningCoroutine == null)
                        yield return instance.StartCoroutine(routine);

                    hasTriggered = true;
                }

                isDone = rotY >= 360f;
                yield return Wait.OneTick();
            }

            rotation = Geometry.Rotation(0f, 0f, 0f);
        }

        /// <summary>
        /// ProcessRoutine a overlay in by increasing renderer alpha. Optional routine runs after overlay completes.
        /// </summary>
        public void FadeIn(float delay = 0f, IEnumerator routine = null)
        {
            if (!isActive || !isAlive)
                return;

            instance.StartCoroutine(FadeInRoutine(delay, routine));
        }

        /// <summary>
        /// Gradually increases alpha to 1. If a routine routine is provided, it runs before finalizing.
        /// </summary>
        private IEnumerator FadeInRoutine(float delay, IEnumerator routine = null)
        {
            float increment = 0.05f;
            float alpha = 0f;
            render.SetAlpha(alpha);

            yield return Wait.For(delay);

            while (alpha < 1f)
            {
                alpha += increment;
                alpha = Mathf.Clamp(alpha, 0f, 1f);
                render.SetAlpha(alpha);
                yield return Wait.OneTick();
            }

            if (routine != null)
                yield return instance.StartCoroutine(routine);

            alpha = 1f;
            render.SetAlpha(alpha);
        }

        /// <summary>
        /// ProcessRoutine a Weapon wiggle when AP is full. Optional routine runs after wiggle stops.
        /// </summary>
        //public void WeaponWiggle(IEnumerator routine = null)
        //{
        //    if (stats.AP < stats.MaxAP || !isActive || !isAlive)
        //        return;

        //    instance.StartCoroutine(WeaponWiggleRoutine(routine));
        //}

        /// <summary>
        /// Oscillates the Weapon icon while AP remains full, then optionally runs the routine routine.
        /// </summary>
        //private IEnumerator WeaponWiggleRoutine(IEnumerator routine = null)
        //{
        //    float start = -45f;
        //    float rotZ = start;
        //    render.weaponIcon.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

        //    while (instance.Stats.AP == instance.Stats.MaxAP)
        //    {
        //        rotZ = start + Mathf.Sin(Time.time * wiggleFocus) * wiggleAmplitude;
        //        render.weaponIcon.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        //        yield return Wait.OneTick();
        //    }

        //    if (routine != null)
        //        yield return instance.StartCoroutine(routine);

        //    rotZ = start;
        //    render.weaponIcon.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        //}

        /// <summary>
        /// ProcessRoutine a wiggle on the turn delay text with damping, then settles back to zero. Optional routine runs after settle.
        /// </summary>
        //public void TurnDelayWiggle(IEnumerator routine = null)
        //{
        //    if (!isActive || !isAlive)
        //        return;

        //    instance.StartCoroutine(TurnDelayWiggleRoutine(routine));
        //}

        /// <summary>
        /// Oscillates the turn delay text with damping, then smoothly returns to zero. Optionally runs a routine routine.
        /// </summary>
        //private IEnumerator TurnDelayWiggleRoutine(IEnumerator routine = null)
        //{
        //    float timeElapsed = 0f;
        //    float amplitude = 10f;
        //    float dampingRate = 0.99f;
        //    float cutoff = 0.1f;
        //    render.turnDelayText.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        //    while (amplitude > cutoff)
        //    {
        //        timeElapsed += Time.deltaTime;
        //        float rotZ = Mathf.Sin(timeElapsed * wiggleFocus) * amplitude;
        //        render.turnDelayText.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        //        amplitude *= dampingRate;
        //        yield return Wait.OneTick();
        //    }

        //    float currentZ = render.turnDelayText.transform.rotation.eulerAngles.z;
        //    while (Mathf.Abs(Mathf.DeltaAngle(currentZ, 0f)) > cutoff)
        //    {
        //        timeElapsed += Time.deltaTime * wiggleFocus;
        //        currentZ = Mathf.LerpAngle(currentZ, 0f, timeElapsed);
        //        render.turnDelayText.transform.rotation = Quaternion.Euler(0f, 0f, currentZ);
        //        yield return Wait.OneTick();
        //    }

        //    if (routine != null)
        //        yield return instance.StartCoroutine(routine);

        //    render.turnDelayText.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        //}

        #endregion
    }
}
