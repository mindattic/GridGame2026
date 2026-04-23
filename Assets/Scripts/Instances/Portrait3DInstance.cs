using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
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
    /// PORTRAIT3DINSTANCE - World-space portrait (SpriteRenderer) with slide/pop/dissolve effects.
    /// <para>PURPOSE: Anchored-to-board portrait that animates in world space — slides from
    /// off-screen during dramatic sequences, rotates + fades for the pop-in/pop-out "flag" on
    /// top of an actor, or shake-dissolves on actor death. Renders on the "Portrait" sorting
    /// layer, so it sits above actor sprites but beneath any ScreenSpaceOverlay Canvas UI.</para>
    /// <para>WHEN TO USE: Effects that should read as part of the board (dissolve on death,
    /// pop-in above an actor sprite). For portraits that must visually dominate the HUD
    /// (e.g. pincer attack slide-ins), use Portrait2DInstance.</para>
    /// <para>RELATED FILES: Portrait3DFactory.cs, Portrait2DInstance.cs, PortraitManager.cs</para>
    /// </summary>
    public class Portrait3DInstance : MonoBehaviour
    {
        #region Components

        public SpriteRenderer spriteRenderer { get; private set; }

        #endregion

        #region State

        public Direction direction;
        protected AnimationCurve slideCurve;
        public ActorInstance actor;
        protected bool isBeingDestroyed = false;

        /// <summary>Animation speed multiplier.</summary>
        [Range(0.1f, 10f)]
        public float speedMultiplier = 1.75f;

        public float startTime;
        public Vector2 startPosition;
        private float popInRotY = 0f;
        private Quaternion lastPopInRot = Quaternion.identity;
        private Vector3 popOutFrontRestorePos;

        #endregion

        #region Properties

        public Transform parent
        {
            get => transform.parent;
            set => transform.SetParent(value, true);
        }

        public Vector3 position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector3 scale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        public Sprite sprite
        {
            get => spriteRenderer != null ? spriteRenderer.sprite : null;
            set { if (spriteRenderer != null) spriteRenderer.sprite = value; }
        }

        public SortingGroup sortingGroup => GetComponent<SortingGroup>();

        /// <summary>Sets the sorting layer + order on the portrait's SortingGroup.</summary>
        public void SetSorting(string sortingLayer, int sortingOrder = 0)
        {
            var sg = sortingGroup;
            if (sg == null) return;
            sg.sortingLayerID = SortingLayer.NameToID(sortingLayer);
            sg.sortingOrder = sortingOrder;
        }

        #endregion

        #region Lifecycle

        /// <summary>Initializes component references and state.</summary>
        protected void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Shared slide curve (overshoot then settle).
            slideCurve = new AnimationCurve(
                new Keyframe(0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new Keyframe(0.8f, 0.05202637f, 0.0f, 0.0f, 0.33333334f, 0.70263505f),
                new Keyframe(1.2f, -0.05f, 0.0f, 0.0f, 0.33333334f, 0.33322528f),
                new Keyframe(1.993103f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f)
            )
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
        }

        /// <summary>Cleans up resources when the object is destroyed.</summary>
        private void OnDestroy() => isBeingDestroyed = true;

        #endregion

        #region Slide

        /// <summary>Slides the portrait from off-screen world coords along the given direction.</summary>
        public IEnumerator SlideIn()
        {
            if (spriteRenderer == null)
                yield break;

            // Preserve the alpha the manager configured (e.g. Opacity.Translucent.Alpha196)
            // so Portrait3D matches Portrait2D's translucent slide-in look.
            Vector3 destination = Vector3.zero;

            switch (direction)
            {
                case Direction.North:
                    position = new Vector3(1, -10, 1);
                    destination = new Vector3(1, 10, 1);
                    break;
                case Direction.East:
                    position = new Vector3(-10, 1, 1);
                    destination = new Vector3(10, 1, 1);
                    break;
                case Direction.South:
                    position = new Vector3(-1, 10, 1);
                    destination = new Vector3(-1, -10, 1);
                    break;
                case Direction.West:
                    position = new Vector3(10, -1, 1);
                    destination = new Vector3(-10, -1, 1);
                    break;
            }

            float curveLength = slideCurve.keys[slideCurve.length - 1].time;
            float t0 = Time.time - startTime;
            float elapsed = 0f;

            while (elapsed < curveLength)
            {
                elapsed = (Time.time - startTime) * Mathf.Max(0.0001f, speedMultiplier);
                float v = slideCurve.Evaluate(elapsed);

                switch (direction)
                {
                    case Direction.North:
                    case Direction.South:
                        position = new Vector3(destination.x, destination.y * v, destination.z);
                        break;
                    case Direction.East:
                    case Direction.West:
                        position = new Vector3(destination.x * v, destination.y, destination.z);
                        break;
                }

                yield return Wait.None();
            }

            Despawn();
        }

        #endregion

        #region Pop In / Out

        /// <summary>Rotates in, holds, then rotates out above the actor's front anchor.</summary>
        public IEnumerator PopInOut(float fadeDuration = 0.25f, float holdDuration = 0.25f, float rotateDuration = 0.2f)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            Color baseColor = spriteRenderer.color;
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            yield return PopIn(rotateDuration, fadeDuration);

            for (float elapsed = 0; elapsed < holdDuration; elapsed += Time.deltaTime)
            {
                if (isBeingDestroyed || spriteRenderer == null)
                    yield break;

                Vector3 frontAnchorPos = actor.Render.front.transform.position;
                AlignPortraitWithFront(frontAnchorPos);
                yield return Wait.None();
            }

            yield return PopOut(rotateDuration, fadeDuration);
        }

        /// <summary>Rotates the portrait in above the actor's front anchor + fades alpha 0→1.</summary>
        public IEnumerator PopIn(float rotateDuration = 0.2f, float fadeDuration = 0.25f)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            Transform front = actor.Render.front.transform;
            Vector3 originalFrontPos = front.position;
            float yOffset = -g.TileSize * 0.33f;

            float y = RNG.Float(20f, 25f);
            popInRotY = RNG.Float() < 0.5f ? -y : y;
            Quaternion startRot = front.rotation;
            Quaternion targetRot = Quaternion.Euler(75, popInRotY, 0);
            lastPopInRot = targetRot;

            for (float elapsed = 0; elapsed < rotateDuration; elapsed += Time.deltaTime)
            {
                if (isBeingDestroyed || spriteRenderer == null)
                    yield break;

                float t = elapsed / rotateDuration;
                front.rotation = Quaternion.Slerp(startRot, targetRot, t);
                Vector3 loweredPos = originalFrontPos + new Vector3(0, yOffset, 0);
                front.position = Vector3.Lerp(originalFrontPos, loweredPos, t);
                AlignPortraitWithFront(front.position);
                yield return Wait.None();
            }
            front.rotation = targetRot;
            front.position = originalFrontPos + new Vector3(0, yOffset, 0);
            AlignPortraitWithFront(front.position);

            Color c = spriteRenderer.color;
            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                if (isBeingDestroyed || spriteRenderer == null)
                    yield break;

                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float alpha = Mathf.Lerp(0, 1, t);
                spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
                AlignPortraitWithFront(front.position);
                yield return Wait.None();
            }
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);
            AlignPortraitWithFront(front.position);

            popOutFrontRestorePos = originalFrontPos;
        }

        /// <summary>Rotates out + fades alpha 1→0 to restore the actor's front transform.</summary>
        public IEnumerator PopOut(float rotateDuration = 0.2f, float fadeDuration = 0.25f)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            Transform front = actor.Render.front.transform;
            Vector3 loweredPos = front.position;
            Vector3 originalPos = popOutFrontRestorePos;

            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);

            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                if (isBeingDestroyed || spriteRenderer == null)
                    yield break;

                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float alpha = Mathf.Lerp(1, 0, t);
                spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
                AlignPortraitWithFront(front.position);
                yield return Wait.None();
            }
            spriteRenderer.color = new Color(c.r, c.g, c.b, 0f);
            AlignPortraitWithFront(front.position);

            Quaternion startRot = front.rotation;
            Quaternion targetRot = Quaternion.Euler(0, 0, 0);
            for (float elapsed = 0; elapsed < rotateDuration; elapsed += Time.deltaTime)
            {
                if (isBeingDestroyed || spriteRenderer == null)
                    yield break;

                float t = elapsed / rotateDuration;
                front.rotation = Quaternion.Slerp(startRot, targetRot, t);
                front.position = Vector3.Lerp(loweredPos, originalPos, t);
                AlignPortraitWithFront(front.position);
                yield return Wait.None();
            }
            front.rotation = targetRot;
            front.position = originalPos;
            AlignPortraitWithFront(front.position);

            Despawn();
        }

        #endregion

        #region Dissolve

        /// <summary>Shakes + fades + shrinks the portrait while an optional follow-up routine plays.</summary>
        public IEnumerator DissolveRoutine(IEnumerator routine = null)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                yield break;

            float alpha = 1f;
            spriteRenderer.color = new Color(1, 1, 1, alpha);
            Coroutine runningCoroutine = null;

            while (alpha > 0f)
            {
                if (isBeingDestroyed || spriteRenderer == null)
                    yield break;

                position = startPosition;
                position += new Vector3(RNG.Range(ShakeIntensity.Medium), RNG.Range(ShakeIntensity.Medium), 1);
                transform.localScale *= 0.99f;

                alpha = Mathf.Clamp01(alpha - Increment.Percent1);

                if (routine != null && runningCoroutine == null && alpha <= Opacity.Percent10)
                    runningCoroutine = StartCoroutine(routine);

                spriteRenderer.color = new Color(1, 1, 1, alpha);
                yield return Wait.None();
            }

            Despawn();
        }

        #endregion

        #region Helpers

        /// <summary>Aligns the portrait to sit just above the actor's front transform in world space.</summary>
        private void AlignPortraitWithFront(Vector3 frontAnchorPos)
        {
            if (isBeingDestroyed || spriteRenderer == null)
                return;

            float halfPortraitHeight = spriteRenderer.bounds.size.y / 2f;
            transform.position = frontAnchorPos + Vector3.up * halfPortraitHeight;
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }

        /// <summary>Destroys the GameObject.</summary>
        protected void Despawn()
        {
            if (isBeingDestroyed) return;
            isBeingDestroyed = true;
            Destroy(gameObject);
        }

        #endregion
    }
}
