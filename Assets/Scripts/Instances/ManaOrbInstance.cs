using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Models;

namespace Scripts.Instances
{
    /// <summary>
    /// MANAORBINSTANCE - One bouncing mana orb dropped by an enemy (on death or pincer) that
    /// flies/bounces from its drop point on the board to the first empty slot in the
    /// <see cref="ManaOrbLine"/> HUD strip, then commits to the team's <see cref="ManaBank"/>.
    ///
    /// <para>Lives as a UI <see cref="Image"/> under the main Canvas (screen-space). Travel is a
    /// quadratic Bezier (start → midpoint with vertical lift → slot) so it reads as a "bouncing
    /// pickup," echoing the coin pattern.</para>
    /// </summary>
    public sealed class ManaOrbInstance : MonoBehaviour
    {
        private RectTransform rt;
        private Image img;
        private ManaBank targetBank;
        private ManaOrbLine targetLine;
        private ManaType color;
        private int targetSlot;
        private float elapsed;
        private float duration;
        private Vector2 startCanvas;
        private Vector2 endCanvas;
        private Vector2 arcCanvas;     // bezier control point
        private bool committed;

        public const float DefaultFlightSeconds = 0.55f;
        public const float ArcLiftPixels = 110f;

        /// <summary>Configure and start the flight. Call right after spawn.</summary>
        public void Launch(
            ManaBank bank,
            ManaOrbLine line,
            ManaType color,
            Vector2 fromCanvas,
            int slotIndex,
            float seconds = DefaultFlightSeconds)
        {
            rt = (RectTransform)transform;
            img = GetComponent<Image>();
            targetBank = bank;
            targetLine = line;
            this.color = color;
            targetSlot = slotIndex;
            duration = seconds;
            elapsed = 0f;
            startCanvas = fromCanvas;

            img.color = ManaOrbLine.ColorFor(color);
            rt.anchoredPosition = startCanvas;

            // End point chosen at launch; the slot may shift if other orbs land first but for V1
            // we lock the destination so the visuals stay readable. Refresh handles the bank state.
            var canvasRt = (RectTransform)transform.parent;
            var slotWorld = line.GetSlotWorldPosition(slotIndex);
            endCanvas = WorldUiToAnchoredCanvas(canvasRt, slotWorld);

            // Arc control: midpoint with a vertical lift toward the top of the screen.
            arcCanvas = (startCanvas + endCanvas) * 0.5f + new Vector2(0f, ArcLiftPixels);
        }

        private void Update()
        {
            if (committed || targetBank == null) return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Fix #9: late in the flight, re-target to the CURRENT first-empty slot so a faster
            // peer that landed first doesn't leave us flying to an already-filled position.
            // (Recompute when we still have time to redirect, but stop near the end so the
            // ending position stays stable.)
            if (t < 0.6f && targetLine != null)
            {
                int liveSlot = targetLine.FirstEmptyIndex();
                if (liveSlot >= 0 && liveSlot != targetSlot)
                {
                    targetSlot = liveSlot;
                    var canvasRt = (RectTransform)transform.parent;
                    var slotWorld = targetLine.GetSlotWorldPosition(targetSlot);
                    endCanvas = WorldUiToAnchoredCanvas(canvasRt, slotWorld);
                    arcCanvas = (startCanvas + endCanvas) * 0.5f + new Vector2(0f, ArcLiftPixels);
                }
            }

            float u = 1f - t;
            rt.anchoredPosition = u * u * startCanvas + 2f * u * t * arcCanvas + t * t * endCanvas;

            if (t >= 1f)
            {
                targetBank.Add(color, 1);
                committed = true;
                Destroy(gameObject);
            }
        }

        /// <summary>Convert a world-position taken off a Canvas Overlay UI element back to a sibling's anchored position.</summary>
        public static Vector2 WorldUiToAnchoredCanvas(RectTransform canvasRt, Vector3 worldPosFromOverlayUi)
        {
            // Overlay-canvas UI elements have RectTransform.position in screen-space pixels (z=0).
            // Convert to canvas-local anchored coords for a sibling RectTransform under the canvas.
            Vector2 screen = new Vector2(worldPosFromOverlayUi.x, worldPosFromOverlayUi.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local);
            return local;
        }
    }
}
