using Scripts.Helpers;
using Scripts.Models;
using System.Collections;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances.Actor
{
    /// <summary>
    /// ACTORHEALTHTEXT - Numeric HP readout in the actor's corner.
    ///
    /// PURPOSE:
    /// Replaces the old HealthBar + ActionBar sprite stack with a single
    /// right-aligned TMP label. Shows "X/Y" at full HP, otherwise just
    /// the current value. Color shifts with the HP ratio.
    ///
    /// COLORS:
    /// - >= 66%: green
    /// - >= 33% (and < 66%): white
    /// - <  33%: red
    ///
    /// ANIMATION:
    /// On HP change, waits TickDelay seconds, then lerps the displayed
    /// number from PreviousHP toward HP over TickDuration seconds. Color
    /// snaps to the *target* HP ratio at the start of the tick.
    /// </summary>
    public class ActorHealthText
    {
        private const float TickDelay = 0.25f;
        private const float TickDuration = 0.5f;

        private static readonly Color ColorHigh = new Color(0.45f, 0.95f, 0.45f, 1f);
        private static readonly Color ColorMid  = new Color(1.00f, 1.00f, 1.00f, 1f);
        private static readonly Color ColorLow  = new Color(1.00f, 0.30f, 0.30f, 1f);

        private ActorInstance instance;
        private float displayedHP;
        private Coroutine tickCoroutine;

        public bool IsAnimating { get; private set; }
        public bool IsEmpty => !IsAnimating && instance.Stats.HP < 1;

        protected ActorRenderers render => instance.Render;
        protected ActorStats stats => instance.Stats;

        public void Initialize(ActorInstance parentInstance)
        {
            instance = parentInstance;
            displayedHP = stats.HP;
            ApplyText(displayedHP);
            ApplyColor(stats.HP);
        }

        /// <summary>Pull current HP into the display, animating if it changed.</summary>
        public void Refresh()
        {
            if (render.healthText == null) return;

            ApplyColor(stats.HP);

            if (Mathf.Approximately(displayedHP, stats.HP))
            {
                ApplyText(stats.HP);
                return;
            }

            if (tickCoroutine != null)
                instance.StopCoroutine(tickCoroutine);

            if (instance.IsActive)
                tickCoroutine = instance.StartCoroutine(TickRoutine());
            else
            {
                displayedHP = stats.HP;
                ApplyText(displayedHP);
            }
        }

        private IEnumerator TickRoutine()
        {
            IsAnimating = true;

            float from = displayedHP;
            float to = stats.HP;

            yield return new WaitForSeconds(TickDelay);

            float elapsed = 0f;
            while (elapsed < TickDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / TickDuration);
                displayedHP = Mathf.Lerp(from, to, t);
                ApplyText(displayedHP);
                yield return null;
            }

            displayedHP = to;
            ApplyText(displayedHP);
            stats.PreviousHP = stats.HP;
            IsAnimating = false;
            tickCoroutine = null;
        }

        private void ApplyText(float value)
        {
            int shown = Mathf.Max(0, Mathf.RoundToInt(value));
            int max = Mathf.Max(1, Mathf.RoundToInt(stats.MaxHP));
            render.healthText.text = $"{shown}/{max}";
        }

        private void ApplyColor(float hp)
        {
            float ratio = stats.MaxHP > 0f ? hp / stats.MaxHP : 0f;
            Color target = ratio >= 0.66f ? ColorHigh : (ratio >= 0.33f ? ColorMid : ColorLow);
            target.a = render.healthText.color.a;
            render.healthText.color = target;
        }
    }
}
