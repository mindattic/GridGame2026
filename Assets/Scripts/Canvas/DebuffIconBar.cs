using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Instances.Actor;
using Scripts.Managers;
using Scripts.Models;

namespace Scripts.Canvas
{
    /// <summary>
    /// DEBUFFICONBAR - Icon strip in the upper-right of an actor showing active buffs/debuffs.
    ///
    /// <para>Each cell is a colored disk with a single-letter id + a <b>radial yellow ring</b>
    /// around it that ticks down clockwise as the buff's remaining duration drops. When the ring
    /// completes (fillAmount = 0), the buff has expired and the cell hides. No more numeric
    /// countdowns — the ring carries that information.</para>
    ///
    /// <para>Max <see cref="MaxVisible"/> cells (default 3). Overflow cycles every
    /// <see cref="CycleSeconds"/> so every buff is seen eventually.</para>
    /// </summary>
    public sealed class DebuffIconBar : MonoBehaviour
    {
        public const int MaxVisible = 3;
        public const float CycleSeconds = 1.5f;
        public const float IconSize = 26f;
        public const float IconSpacing = 4f;

        private ActorInstance owner;
        private Image[] cellImages;
        private TMP_Text[] cellLetters;
        private Image[] cellRings;
        private readonly Dictionary<string, int> startDurations = new Dictionary<string, int>();
        private int cycleOffset;
        private float cycleTimer;

        public void Bind(ActorInstance owner, Image[] images, TMP_Text[] letters, Image[] rings)
        {
            this.owner = owner;
            cellImages = images;
            cellLetters = letters;
            cellRings = rings;
        }

        private void Update()
        {
            if (owner == null || cellImages == null) return;

            var active = BuffSystem.GetAll(owner);
            int total = active != null ? active.Count : 0;

            // Track the max-duration each buff started at so the ring's fillAmount represents
            // (remaining / starting) accurately even after stacking.
            for (int i = 0; i < total; i++)
            {
                var id = active[i].Definition.Id;
                if (!startDurations.TryGetValue(id, out var d) || d < active[i].RemainingDuration)
                    startDurations[id] = Mathf.Max(active[i].RemainingDuration, active[i].Definition.DefaultDuration);
            }

            if (total > MaxVisible)
            {
                cycleTimer += Time.deltaTime;
                if (cycleTimer >= CycleSeconds)
                {
                    cycleTimer = 0f;
                    cycleOffset = (cycleOffset + 1) % total;
                }
            }
            else { cycleOffset = 0; cycleTimer = 0f; }

            for (int i = 0; i < MaxVisible; i++)
            {
                if (i >= total)
                {
                    cellImages[i].enabled = false;
                    if (cellLetters[i] != null) cellLetters[i].text = "";
                    if (cellRings[i] != null) cellRings[i].enabled = false;
                    continue;
                }

                int idx = (cycleOffset + i) % total;
                var bi = active[idx];
                cellImages[i].enabled = true;
                cellImages[i].color = ColorFor(bi.Definition.Id);
                if (cellLetters[i] != null) cellLetters[i].text = LetterFor(bi.Definition.Id).ToString();

                if (cellRings[i] != null)
                {
                    cellRings[i].enabled = true;
                    float start = startDurations.TryGetValue(bi.Definition.Id, out var s) && s > 0 ? s : Mathf.Max(1, bi.Definition.DefaultDuration);
                    cellRings[i].fillAmount = Mathf.Clamp01(bi.RemainingDuration / start);
                }
            }
        }

        public static char LetterFor(string buffId)
        {
            switch (buffId)
            {
                case "burning":     return 'B';
                case "frozen":      return 'F';
                case "wet":         return 'W';
                case "warm":        return 'M';
                case "sleep":       return 'Z';
                case "protection":  return 'P';
                case "poisoned":    return 'X';
                case "slowed":      return 'S';
                case "silenced":    return '!';
                case "blinded":     return '@'; // eye-blocked look
                default:            return '?';
            }
        }

        /// <summary>Debuff icon color — routes through the colorblind palette for red/green pairs when enabled (US-094).</summary>
        public static Color ColorFor(string buffId) => Scripts.Helpers.ColorblindHelper.GetDebuffColor(buffId);

        /// <summary>Standard (non-colorblind) debuff colors. Called by ColorblindHelper for unaffected buff IDs.</summary>
        public static Color ColorForStandard(string buffId)
        {
            switch (buffId)
            {
                case "burning":     return new Color(1.00f, 0.45f, 0.10f);
                case "frozen":      return new Color(0.55f, 0.85f, 1.00f);
                case "wet":         return new Color(0.20f, 0.55f, 0.90f);
                case "warm":        return new Color(1.00f, 0.70f, 0.40f);
                case "sleep":       return new Color(0.80f, 0.60f, 1.00f);
                case "protection":  return new Color(0.40f, 0.80f, 0.45f);
                case "poisoned":    return new Color(0.50f, 0.85f, 0.40f);
                case "slowed":      return new Color(0.40f, 0.65f, 0.85f);
                case "silenced":    return new Color(0.85f, 0.55f, 1.00f);
                case "blinded":     return new Color(0.35f, 0.35f, 0.45f); // dim grey-blue
                default:            return new Color(0.7f, 0.7f, 0.7f);
            }
        }
    }
}
