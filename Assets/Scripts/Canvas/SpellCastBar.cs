using System;
using System.Collections.Generic;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Canvas
{
    /// <summary>
    /// SPELLCASTBAR - A spell-cast ICON that "loads" left→right on a lane just BELOW the timeline,
    /// in <b>parallel</b> with the enemy icons (US-#3). The icon is the spell's sprite; it travels
    /// from the bar's spawn edge (u=0) to the trigger (u=1) over the cast time and resolves on
    /// arrival — mirroring how enemy icons load, on its own lane. Multiple concurrent casts each
    /// stack one lane lower (slot index).
    ///
    /// <para>Casting is NOT a pause — the player can keep dragging heroes while a cast loads
    /// (contrast <see cref="TimelineBarInstance.SpawnSpellIcon"/>, the on-timeline path that suspends
    /// input via a third turn state). On resolve the game holds <see cref="Scripts.Models.InputMode.None"/>
    /// for a brief beat (<see cref="ResolveLockSeconds"/>) so the resolution reads, then restores the
    /// prior input mode.</para>
    ///
    /// <para>This component lives ON the cast-lane icon GameObject created by
    /// <see cref="TimelineBarInstance.CreateCastLaneIcon"/>, so destroying it removes the icon.</para>
    /// </summary>
    public sealed class SpellCastBar : MonoBehaviour
    {
        public const float ResolveLockSeconds = 0.30f;
        public const int MaxConcurrent = 4;

        /// <summary>Every cast currently on screen — used to find the next free vertical lane.</summary>
        private static readonly List<SpellCastBar> Active = new List<SpellCastBar>();

        /// <summary>True when the cap is full and new casts should be refused.</summary>
        public static bool IsAtCapacity => Active.Count >= MaxConcurrent;

        /// <summary>Lowest non-negative lane index not currently in use. The factory picks this
        /// BEFORE creating the icon (so its lane Y is correct) and passes it to <see cref="Begin"/>.</summary>
        public static int NextFreeSlot()
        {
            for (int i = 0; ; i++)
            {
                bool taken = false;
                for (int k = 0; k < Active.Count; k++)
                    if (Active[k] != null && Active[k].slotIndex == i) { taken = true; break; }
                if (!taken) return i;
            }
        }

        private RectTransform iconRT;
        private float leftX;
        private float rightX;
        private float total;
        private float elapsed;
        private bool resolved;
        private string spellName;
        private Action onResolved;
        private float resolveLockTimer;
        private Scripts.Models.InputMode previousInputMode;
        private int slotIndex;
        private Scripts.Instances.Actor.ActorInstance casterRef;

        /// <summary>Begin the countdown. The icon travels <paramref name="leftX"/>→<paramref name="rightX"/>
        /// over <paramref name="seconds"/>; on reaching the trigger, <paramref name="onResolved"/> fires
        /// and the icon destroys itself after a brief input lock.</summary>
        public void Begin(
            string spellName,
            float seconds,
            RectTransform icon,
            float leftX,
            float rightX,
            int slotIndex,
            Action onResolved,
            Scripts.Instances.Actor.ActorInstance caster = null)
        {
            this.spellName = spellName;
            this.total = Mathf.Max(0.001f, seconds);
            this.elapsed = 0f;
            this.iconRT = icon;
            this.leftX = leftX;
            this.rightX = rightX;
            this.slotIndex = slotIndex;
            this.onResolved = onResolved;
            this.casterRef = caster;
            this.resolved = false;

            Active.Add(this);
            ApplyTravel(0f);
        }

        /// <summary>Position the icon at progress <paramref name="t"/> (0=spawn edge, 1=trigger),
        /// keeping its lane Y.</summary>
        private void ApplyTravel(float t)
        {
            if (iconRT == null) return;
            var p = iconRT.anchoredPosition;
            iconRT.anchoredPosition = new Vector2(Mathf.Lerp(leftX, rightX, Mathf.Clamp01(t)), p.y);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        /// <summary>Static scene-unload sweep — defensively clears the registry in case an icon was
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
                // Caster died or left the board mid-cast — interrupt without resolving.
                if (casterRef != null && (!casterRef.IsPlaying || casterRef.Stats == null || casterRef.Stats.HP <= 0f))
                {
                    Debug.LogWarning($"[SpellCastBar] '{spellName}' interrupted — caster gone.");
                    Destroy(gameObject);
                    return;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / total);
                ApplyTravel(t);

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
