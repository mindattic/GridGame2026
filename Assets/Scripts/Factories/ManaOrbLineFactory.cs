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
        public const float CellSpacing = 4f;

        public static ManaOrbLine Create(Transform parent, ManaBank bank, int capacity = DefaultCapacity)
        {
            var rootGO = new GameObject("ManaOrbLine", typeof(RectTransform));
            rootGO.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)rootGO.transform;
            rt.SetParent(parent, false);

            // Row 14 — between the 6-slot ability bar (Row 13) and the character card (Row 15).
            // Y from Scripts.Utilities.HudLayout (single source of truth shared with GameBuilder).
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, Scripts.Utilities.HudLayout.Row14Y_FromBot);
            float width = capacity * CellSize + (capacity - 1) * CellSpacing;
            rt.sizeDelta = new Vector2(width, CellSize + 4f);

            var hlg = rootGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = CellSpacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
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
                crt.SetParent(rootGO.transform, false);
                crt.sizeDelta = new Vector2(CellSize, CellSize);

                var img = cellGO.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.15f); // empty by default
                cells[i] = img;
            }

            var line = rootGO.AddComponent<ManaOrbLine>();
            line.SetCells(cells);
            line.Bind(bank);
            return line;
        }
    }
}
