using UnityEngine;

namespace Assets.Helper
{
    public static class AnimationCurveHelper
    {
        /// <summary>
        /// A smooth ease-in and ease-out curve for natural acceleration and deceleration.
        /// </summary>
        public static AnimationCurve EaseInOut => AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// A linear Move curve, maintaining a constant speed from start to finish.
        /// </summary>
        public static AnimationCurve Linear => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 1)
        );

        /// <summary>
        /// A fast start that slows down toward the end.
        /// </summary>
        public static AnimationCurve EaseOut => new AnimationCurve(
            new Keyframe(0, 0, 0, 2),
            new Keyframe(1, 1, 0, 0)
        );

        /// <summary>
        /// A slow start that speeds up toward the end.
        /// </summary>
        public static AnimationCurve EaseIn => new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(1, 1, 2, 0)
        );

        /// <summary>
        /// A bounce effect that overshoots and settles back.
        /// </summary>
        public static AnimationCurve Bounce => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.5f, 1.2f), // Overshoot
            new Keyframe(0.75f, 0.8f), // Rebound
            new Keyframe(1, 1)
        );

        /// <summary>
        /// A wave motion with one oscillation.
        /// </summary>
        public static AnimationCurve SingleWave => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.25f, 1),
            new Keyframe(0.5f, 0),
            new Keyframe(0.75f, -1),
            new Keyframe(1, 0)
        );

        /// <summary>
        /// A wave motion with two oscillations.
        /// </summary>
        public static AnimationCurve DoubleWave => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.2f, 1),
            new Keyframe(0.4f, 0),
            new Keyframe(0.6f, -1),
            new Keyframe(0.8f, 0),
            new Keyframe(1, 1)
        );

        /// <summary>
        /// A sudden jump with a sharp drop, useful for explosive effects.
        /// </summary>
        public static AnimationCurve SharpSpike => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.2f, 1),
            new Keyframe(0.3f, -0.5f),
            new Keyframe(0.4f, 0.75f),
            new Keyframe(0.6f, -0.25f),
            new Keyframe(1, 1)
        );

        /// <summary>
        /// An elastic Move that springs back and forth before settling.
        /// </summary>
        public static AnimationCurve Elastic => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.3f, 1.2f),  // Overshoot
            new Keyframe(0.5f, -0.8f), // Undershoot
            new Keyframe(0.7f, 1.1f),  // Rebound
            new Keyframe(1, 1)
        );

        /// <summary>
        /// A steep drop followed by a slow recovery.
        /// </summary>
        public static AnimationCurve FallAndRecover => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.2f, -1.2f),
            new Keyframe(0.5f, -0.5f),
            new Keyframe(1, 1)
        );
    }

}