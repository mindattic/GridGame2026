using UnityEngine;
using UnityEngine.UI;
using Scripts.Models;

namespace Scripts.Canvas
{
    /// <summary>
    /// MANAORBLINE - The Row-14 HUD strip: a horizontal row of orb cells (default 12) that mirrors
    /// the team's <see cref="ManaBank"/> line.
    ///
    /// <para>Each cell is an <see cref="Image"/>. Cells with an orb in them are colored by
    /// <see cref="ColorFor"/>; empty cells show a faint outline so the capacity is always visible.
    /// The line polls the bank cheaply each frame so it stays correct as orbs are harvested or
    /// spent — no event wiring required for V1.</para>
    ///
    /// <para>Built code-only via <see cref="Scripts.Factories.ManaOrbLineFactory"/>; no prefab.</para>
    /// </summary>
    public sealed class ManaOrbLine : MonoBehaviour
    {
        private ManaBank bank;
        private Image[] cells;
        private int lastSignature = -1; // (total << 4) ^ first-orb-hash — repaint only on change

        /// <summary>Attach a bank — the line will reflect it from this point on.</summary>
        public void Bind(ManaBank bank)
        {
            this.bank = bank;
            Refresh();
        }

        /// <summary>Wired by the factory after the cells are spawned.</summary>
        internal void SetCells(Image[] orbCells) { cells = orbCells; }

        /// <summary>How fast a wild (Colorless) orb cycles through the spectrum (hue turns/second).</summary>
        private const float WildCycleSpeed = 0.45f;

        private void Update()
        {
            // Fix #7: only repaint when the bank's snapshot actually changed (still cheap, but
            // skips most frames). Signature mixes total count with the leftmost few colors so
            // any visible change forces a refresh.
            int sig = ComputeSignature();
            if (sig != lastSignature)
            {
                lastSignature = sig;
                Refresh();
            }

            // US-031: wild (Colorless) orbs are "all colors at once" — animate them every frame so
            // they flash through the spectrum, distinct from the static elemental orbs.
            AnimateWildOrbs();
        }

        /// <summary>Per-frame rainbow cycle for any Colorless orb cells (US-031).</summary>
        private void AnimateWildOrbs()
        {
            if (cells == null || bank == null) return;
            int filled = bank.Total;
            float hue = Mathf.Repeat(Time.time * WildCycleSpeed, 1f);
            for (int i = 0; i < cells.Length && i < filled; i++)
            {
                if (cells[i] != null && bank.Orbs[i] == ManaType.Colorless)
                    cells[i].color = Color.HSVToRGB(hue, 0.75f, 1f);
            }
        }

        private int ComputeSignature()
        {
            if (bank == null) return 0;
            int total = bank.Total;
            int sig = total;
            // Hash every filled orb, not just the first 4 — otherwise a color change at a slot
            // past index 3 (total unchanged) leaves lastSignature equal and the line never repaints.
            int sample = Mathf.Min(total, bank.Orbs.Count);
            for (int i = 0; i < sample; i++) sig = (sig * 31) ^ (int)bank.Orbs[i];
            return sig;
        }

        public void Refresh()
        {
            if (cells == null) return;
            int filled = bank != null ? bank.Total : 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (i < filled)
                {
                    cells[i].color = ColorFor(bank.Orbs[i]);
                }
                else
                {
                    cells[i].color = new Color(1f, 1f, 1f, 0.15f); // empty slot — faint outline
                }
            }
        }

        /// <summary>Capacity (cells in the line).</summary>
        public int Capacity => cells != null ? cells.Length : 0;

        /// <summary>The world-space position of slot <paramref name="index"/>'s center — used as the target for a dropping ManaOrb to fly toward.</summary>
        public Vector3 GetSlotWorldPosition(int index)
        {
            if (cells == null || index < 0 || index >= cells.Length) return transform.position;
            return cells[index].rectTransform.position;
        }

        /// <summary>The index of the first empty cell (leftmost free slot). -1 if the line is full.</summary>
        public int FirstEmptyIndex()
        {
            int filled = bank != null ? bank.Total : 0;
            return (filled < Capacity) ? filled : -1;
        }

        /// <summary>The display color for each orb type — routes through the colorblind palette when enabled (US-094).</summary>
        public static Color ColorFor(ManaType t) => Scripts.Helpers.ColorblindHelper.GetManaColor(t);

        /// <summary>Standard (non-colorblind) orb colors. Called by ColorblindHelper for unaffected types.</summary>
        public static Color ColorForStandard(ManaType t)
        {
            switch (t)
            {
                case ManaType.Blue:      return new Color(0.35f, 0.55f, 1.00f);
                case ManaType.Red:       return new Color(1.00f, 0.35f, 0.35f);
                case ManaType.Green:     return new Color(0.40f, 0.85f, 0.45f);
                case ManaType.White:     return new Color(0.95f, 0.95f, 0.95f);
                case ManaType.Black:     return new Color(0.20f, 0.15f, 0.25f);
                case ManaType.Colorless: return new Color(0.70f, 0.70f, 0.75f);
                default:                 return Color.gray;
            }
        }
    }
}
