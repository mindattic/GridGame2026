using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using c = Scripts.Helpers.CanvasHelper;
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
    /// PORTRAIT2DINSTANCE - Canvas-space portrait (Image + RectTransform) with slide-in effect.
    /// <para>PURPOSE: Canvas UI portrait that slides in from an off-screen edge during dramatic
    /// sequences (pincer attack, ability reveal). Parents under the ScreenSpaceOverlay Canvas
    /// so it renders on top of the HUD (mana bar, timeline bar) — the 3D variant sits beneath
    /// overlay UI.</para>
    /// <para>WHEN TO USE: Slide-in flourishes that must visually dominate the screen. For
    /// portraits rooted in the board (dissolve-on-death, pop-in above an actor sprite), use
    /// Portrait3DInstance.</para>
    /// <para>LANE LOCKING: fixedX/fixedY pin the portrait to a specific screen axis so paired
    /// slide-ins (e.g. two pincer attackers) can share consistent lanes.</para>
    /// <para>RELATED FILES: Portrait2DFactory.cs, Portrait3DInstance.cs, PortraitManager.cs</para>
    /// </summary>
    public class Portrait2DInstance : MonoBehaviour
    {
        #region Components

        public RectTransform rectTransform { get; private set; }
        public Image image { get; private set; }

        #endregion

        #region State

        public Direction direction;
        protected AnimationCurve slideCurve;
        public ActorInstance actor;
        protected bool isBeingDestroyed = false;

        // Lane locking: when set, pin this axis so paired slide-ins stay aligned.
        public float? fixedX = null;
        public float? fixedY = null;

        /// <summary>Animation speed multiplier.</summary>
        [Range(0.1f, 10f)]
        public float speedMultiplier = 1.75f;

        // Runtime state retained for API parity with the 3D variant.
        public float startTime;
        public Vector2 startPosition;

        #endregion

        #region Properties

        public Transform parent
        {
            get => transform.parent;
            set
            {
                if (rectTransform != null)
                    rectTransform.SetParent(value, false);
                else
                    transform.SetParent(value, true);
            }
        }

        public Vector3 position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector3 scale
        {
            get => rectTransform != null ? (Vector3)rectTransform.localScale : transform.localScale;
            set
            {
                if (rectTransform != null) rectTransform.localScale = value;
                else transform.localScale = value;
            }
        }

        public Sprite sprite
        {
            get => image != null ? image.sprite : null;
            set { if (image != null) image.sprite = value; }
        }

        #endregion

        #region Lifecycle

        /// <summary>Initializes component references, centers pivot/anchors, and builds the slide curve.</summary>
        protected void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();

            if (rectTransform != null)
            {
                rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            // Shared slide curve (overshoot then settle) — matches Portrait3DInstance.
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

        /// <summary>
        /// Slides the portrait from off-screen to off-screen along the configured direction using
        /// the slide curve. Lane offsets come from fixedX/fixedY (if set) or a random cross-axis
        /// offset otherwise — this keeps paired portraits on consistent lanes for readability.
        /// </summary>
        public IEnumerator SlideInRoutine()
        {
            if (image == null || rectTransform == null)
                yield break;

            Rect canvas = c.CanvasRect.rect;
            float halfCanvasW = canvas.width * 0.5f;
            float halfCanvasH = canvas.height * 0.5f;
            float halfPortraitW = rectTransform.rect.width * rectTransform.localScale.x * 0.5f;
            float halfPortraitH = rectTransform.rect.height * rectTransform.localScale.y * 0.5f;
            const float padding = 2f;

            float offscreenRightX = halfCanvasW + halfPortraitW + padding;
            float offscreenLeftX = -offscreenRightX;
            float offscreenTopY = halfCanvasH + halfPortraitH + padding;
            float offscreenBottomY = -offscreenTopY;

            float crossSpan = (direction == Direction.East || direction == Direction.West) ? canvas.height : canvas.width;
            float offsetAmount = RNG.Float(0f, crossSpan * Increment.Percent10);
            float offset = RNG.Int(1, 2) == 1 ? offsetAmount : -offsetAmount;
            bool isVertical = direction == Direction.North || direction == Direction.South;

            float laneX = isVertical ? (fixedX ?? offset) : 0f;
            float laneY = !isVertical ? (fixedY ?? offset) : 0f;

            Vector2 destination;
            switch (direction)
            {
                case Direction.East:
                    destination = new Vector2(offscreenRightX, laneY);
                    break;
                case Direction.West:
                    destination = new Vector2(offscreenLeftX, laneY);
                    break;
                case Direction.North:
                    destination = new Vector2(laneX, offscreenTopY);
                    break;
                default: // South
                    destination = new Vector2(laneX, offscreenBottomY);
                    break;
            }

            float startV = slideCurve.Evaluate(0f);
            Vector2 startPos = (direction == Direction.East || direction == Direction.West)
                ? new Vector2(destination.x * startV, destination.y)
                : new Vector2(destination.x, destination.y * startV);
            rectTransform.anchoredPosition = startPos;

            float startTimeLocal = Time.time;
            float curveLength = slideCurve.keys[slideCurve.length - 1].time;
            float elapsedTime = 0f;

            while (elapsedTime < curveLength)
            {
                elapsedTime = (Time.time - startTimeLocal) * Mathf.Max(0.0001f, speedMultiplier);
                float v = slideCurve.Evaluate(elapsedTime);

                Vector2 pos = (direction == Direction.East || direction == Direction.West)
                    ? new Vector2(destination.x * v, destination.y)
                    : new Vector2(destination.x, destination.y * v);

                rectTransform.anchoredPosition = pos;
                yield return Wait.None();
            }

            Despawn();
        }

        #endregion

        #region Helpers

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
