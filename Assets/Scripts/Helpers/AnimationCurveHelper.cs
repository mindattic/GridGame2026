using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
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

namespace Scripts.Helpers
{
    /// <summary>
    /// ANIMATIONCURVEHELPER - Pre-built animation curves.
    /// 
    /// PURPOSE:
    /// Provides common animation curve presets for use
    /// in animations and tweens.
    /// 
    /// CURVES:
    /// - EaseInOut: Smooth acceleration and deceleration
    /// - Linear: Constant speed
    /// - EaseOut: Fast start, slow end
    /// - EaseIn: Slow start, fast end
    /// - Bounce: Overshoot and settle
    /// - SingleWave: One oscillation
    /// 
    /// USAGE:
    /// ```csharp
    /// float t = AnimationCurveHelper.EaseInOut.Evaluate(progress);
    /// ```
    /// </summary>
    public static class AnimationCurveHelper
    {
        /// <summary>Smooth ease-in and ease-out curve.</summary>
        public static AnimationCurve EaseInOut => AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>Linear movement curve, constant speed.</summary>
        public static AnimationCurve Linear => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 1)
        );

        /// <summary>Fast start that slows toward the end.</summary>
        public static AnimationCurve EaseOut => new AnimationCurve(
            new Keyframe(0, 0, 0, 2),
            new Keyframe(1, 1, 0, 0)
        );

        /// <summary>Slow start that speeds up toward the end.</summary>
        public static AnimationCurve EaseIn => new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(1, 1, 2, 0)
        );

        /// <summary>Bounce effect that overshoots and settles.</summary>
        public static AnimationCurve Bounce => new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(0.75f, 0.8f),
            new Keyframe(1, 1)
        );

        /// <summary>Wave motion with one oscillation.</summary>
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
