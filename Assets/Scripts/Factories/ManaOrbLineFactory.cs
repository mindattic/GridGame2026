using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Models;

namespace Scripts.Factories
{
    /// <summary>
    /// MANAORBLINEFACTORY - Builds the Row-14 mana-orb HUD strip from code (no prefab). Anchors
    /// bottom-center of the supplied canvas, lays out N cells with HorizontalLayoutGroup, binds
    /// the result to the given <see cref="ManaBank"/>.
    ///
    /// <para>USAGE:
    /// <code>
    /// var canvas = GameObject.Find("Canvas").transform;
    /// var line = ManaOrbLineFactory.Create(canvas, bank);
    /// </code></para>
    /// </summary>
    public static class ManaOrbLineFactory
    {
        public const int DefaultCapacity = 12;
        public const float CellSize = 28f;
        public const float CellSpacing = 14f;   // wider gap so orbs read as slotted along the belt

        // Belt geometry.
        private const float BeltHeight   = CellSize + 26f;  // taller than the orbs so they sit "inside" it
        private const float BeltSideInset = 16f;            // belt stops just short of the screen edges
        private static readonly Color BeltColor   = new Color(0.07f, 0.07f, 0.10f, 0.88f); // dark tray
        private static readonly Color BeltRimColor = new Color(1f, 1f, 1f, 0.10f);          // faint top rim

        public static ManaOrbLine Create(Transform parent, ManaBank bank, int capacity = DefaultCapacity)
        {
            // ── Root: a SCREEN-WIDE belt anchored to the bottom, sitting directly ABOVE the
            //    ability bar (Row 13). Stretches full width; height is the belt thickness. ──
            var rootGO = new GameObject("ManaOrbLine", typeof(RectTransform));
            rootGO.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)rootGO.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);   // full screen width
            rt.anchorMax = new Vector2(1f, 0f);   // anchored to the bottom edge
            rt.pivot     = new Vector2(0.5f, 0.5f);
            // Belt bottom edge meets the ability bar's top edge → belt sits just above it.
            float beltY = Scripts.Utilities.HudLayout.Row13Y_FromBot
                        + Scripts.Utilities.HudLayout.RowHeight * 0.5f
                        + BeltHeight * 0.5f;
            rt.anchoredPosition = new Vector2(0f, beltY);
            rt.sizeDelta = new Vector2(0f, BeltHeight);   // width follows the stretch anchors

            // ── Belt background — the dark "tray" the orbs slot into, inset from the screen edges. ──
            var beltGO = new GameObject("Belt", typeof(RectTransform), typeof(Image));
            beltGO.layer = rootGO.layer;
            var beltRt = (RectTransform)beltGO.transform;
            beltRt.SetParent(rootGO.transform, false);
            beltRt.anchorMin = new Vector2(0f, 0.5f);
            beltRt.anchorMax = new Vector2(1f, 0.5f);
            beltRt.pivot = new Vector2(0.5f, 0.5f);
            beltRt.sizeDelta = new Vector2(-BeltSideInset * 2f, BeltHeight);
            beltRt.anchoredPosition = Vector2.zero;
            var beltImg = beltGO.GetComponent<Image>();
            beltImg.sprite = null;              // plain rectangle (no sprite = solid UI quad; NOT a stretched circle)
            beltImg.type = Image.Type.Simple;
            beltImg.color = BeltColor;
            beltImg.raycastTarget = false;

            // Thin highlight rim along the top of the belt for a little depth.
            var rimGO = new GameObject("BeltRim", typeof(RectTransform), typeof(Image));
            rimGO.layer = rootGO.layer;
            var rimRt = (RectTransform)rimGO.transform;
            rimRt.SetParent(beltGO.transform, false);
            rimRt.anchorMin = new Vector2(0f, 1f);
            rimRt.anchorMax = new Vector2(1f, 1f);
            rimRt.pivot = new Vector2(0.5f, 1f);
            rimRt.sizeDelta = new Vector2(0f, 2f);
            rimRt.anchoredPosition = Vector2.zero;
            var rimImg = rimGO.GetComponent<Image>();
            rimImg.color = BeltRimColor;
            rimImg.raycastTarget = false;

            // ── Orb row — centered cluster, equidistant spacing, laid over the belt. ──
            var orbsGO = new GameObject("Orbs", typeof(RectTransform));
            orbsGO.layer = rootGO.layer;
            var orbsRt = (RectTransform)orbsGO.transform;
            orbsRt.SetParent(rootGO.transform, false);
            orbsRt.anchorMin = new Vector2(0f, 0.5f);
            orbsRt.anchorMax = new Vector2(1f, 0.5f);
            orbsRt.pivot = new Vector2(0.5f, 0.5f);
            orbsRt.sizeDelta = new Vector2(0f, CellSize);
            orbsRt.anchoredPosition = Vector2.zero;

            var hlg = orbsGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = CellSpacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;   // equidistant + centered on screen
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var cells = new Image[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var cellGO = new GameObject($"Orb{i:D2}", typeof(RectTransform), typeof(Image));
                cellGO.layer = rootGO.layer;
                var crt = (RectTransform)cellGO.transform;
                crt.SetParent(orbsGO.transform, false);
                crt.sizeDelta = new Vector2(CellSize, CellSize);

                var img = cellGO.GetComponent<Image>();
                img.sprite = Scripts.Utilities.UiCircleSprite.Get(); // round, not square
                img.color = new Color(1f, 1f, 1f, 0.15f);            // empty slot by default
                cells[i] = img;
            }

            var line = rootGO.AddComponent<ManaOrbLine>();
            line.SetCells(cells);
            line.Bind(bank);
            return line;
        }
    }
}
