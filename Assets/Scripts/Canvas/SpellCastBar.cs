using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Canvas
{
    /// <summary>
    /// SPELLCASTBAR - A solid colored line below the timeline that <b>shrinks rightward</b> while
    /// a spell casts. Multiple concurrent casts produce <b>multiple stacked bars</b> (each in its
    /// own slot below the previous), color-coded by the spell's primary mana cost color so the
    /// player can recognize what's casting at a glance.
    ///
    /// <para>Future polish (called out by user): show a tiny icon at the right end of the bar.</para>
    ///
    /// <para>Behavior:</para>
    /// <list type="bullet">
    ///   <item>Bar starts FULL and shrinks toward the right as cast time elapses.</item>
    ///   <item>While casting the player can still drag heroes (cast itself is NOT a pause).</item>
    ///   <item>When width reaches zero the spell resolves and we hold <see cref="InputMode.None"/>
    ///   for a brief beat (<see cref="ResolveLockSeconds"/>) so the resolution reads on-screen,
    ///   then restore the prior input mode.</item>
    /// </list>
    /// </summary>
    public sealed class SpellCastBar : MonoBehaviour
    {
        public const float ResolveLockSeconds = 0.30f;
        public const float SlotStrideY = 30f;
        public const int MaxConcurrent = 4;

        /// <summary>Every cast bar currently on screen — used to find the next free vertical slot.</summary>
        private static readonly List<SpellCastBar> Active = new List<SpellCastBar>();

        /// <summary>True when the cap is full and new bars should be refused (queue/clean overflow).</summary>
        public static bool IsAtCapacity => Active.Count >= MaxConcurrent;

        private RectTransform rootRT;
        private RectTransform fillRT;
        private float fullWidth;
        private float total;
        private float elapsed;
        private bool resolved;
        private string spellName;
        private Action onResolved;
        private float resolveLockTimer;
        private Scripts.Models.InputMode previousInputMode;
        private int slotIndex;
        private float baseAnchoredY;
        private Scripts.Instances.Actor.ActorInstance casterRef;

        /// <summary>Begin the countdown. Bar shrinks over <paramref name="seconds"/>; on completion,
        /// <paramref name="onResolved"/> fires and the bar destroys itself after a brief lock.</summary>
        public void Begin(
            string spellName,
            float seconds,
            RectTransform fill,
            float fullWidth,
            float baseAnchoredY,
            Action onResolved,
            Scripts.Instances.Actor.ActorInstance caster = null)
        {
            this.spellName = spellName;
            this.total = Mathf.Max(0.001f, seconds);
            this.elapsed = 0f;
            this.fillRT = fill;
            this.fullWidth = fullWidth;
            this.baseAnchoredY = baseAnchoredY;
            this.onResolved = onResolved;
            this.casterRef = caster;
            this.resolved = false;
            if (fillRT != null) fillRT.sizeDelta = new Vector2(fullWidth, fillRT.sizeDelta.y);

            // Register and place in the next free vertical slot.
            rootRT = (RectTransform)transform;
            slotIndex = FindLowestFreeSlot();
            Active.Add(this);
            ApplySlotPosition();
        }

        private void ApplySlotPosition()
        {
            if (rootRT == null) return;
            var p = rootRT.anchoredPosition;
            rootRT.anchoredPosition = new Vector2(p.x, baseAnchoredY - slotIndex * SlotStrideY);
        }

        private static int FindLowestFreeSlot()
        {
            // Smallest non-negative integer not in use by an active bar.
            for (int i = 0; ; i++)
            {
                bool taken = false;
                for (int k = 0; k < Active.Count; k++)
                    if (Active[k] != null && Active[k].slotIndex == i) { taken = true; break; }
                if (!taken) return i;
            }
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        /// <summary>Static scene-unload sweep — defensively clears the registry in case a bar was
        /// destroyed without OnDestroy running (e.g., scene reload). Wired via RuntimeInitialize.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            Active.Clear();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private static void OnSceneChanged(UnityEngine.SceneManagement.Scene from, UnityEngine.SceneManagement.Scene to)
        {
            Active.RemoveAll(b => b == null);
        }

        private void Update()
        {
            if (!resolved)
            {
                // Fix #2: caster died or left the board mid-cast — interrupt without resolving.
                if (casterRef != null && (!casterRef.IsPlaying || casterRef.Stats == null || casterRef.Stats.HP <= 0f))
                {
                    Debug.LogWarning($"[SpellCastBar] '{spellName}' interrupted — caster gone.");
                    Destroy(gameObject);
                    return;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / total);

                // Shrink toward the right: fill is anchored to the RIGHT edge of its track, so
                // shrinking width pulls the LEFT side rightward (bar appears to vanish toward right).
                if (fillRT != null)
                    fillRT.sizeDelta = new Vector2(fullWidth * (1f - t), fillRT.sizeDelta.y);

                if (t >= 1f) Resolve();
                return;
            }

            // Post-resolve lock: hold for a short beat, then restore prior input mode (never clobber).
            resolveLockTimer += Time.deltaTime;
            if (resolveLockTimer >= ResolveLockSeconds)
            {
                if (g.InputManager != null) g.InputManager.InputMode = previousInputMode;
                Destroy(gameObject);
            }
        }

        private void Resolve()
        {
            resolved = true;
            Debug.Log($"[SpellCastBar] '{spellName}' resolved.");
            if (g.InputManager != null)
            {
                previousInputMode = g.InputManager.InputMode;
                g.InputManager.InputMode = Scripts.Models.InputMode.None;
            }
            onResolved?.Invoke();
        }
    }
}
