using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Instances;
using Scripts.Models;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Factories
{
    /// <summary>
    /// MANAORBFACTORY - Spawns a bouncing <see cref="ManaOrbInstance"/> from a world position on
    /// the board (an enemy death point or pincer kill location) toward the first empty slot in the
    /// live <see cref="ManaOrbLine"/>.
    ///
    /// <para>Returns null if the orb line is full (in which case the orb is dropped — designer call
    /// for whether to silently swallow or visually fizzle).</para>
    /// </summary>
    public static class ManaOrbFactory
    {
        public const float OrbDiameter = 22f;

        public static ManaOrbInstance Drop(Vector3 worldOrigin, ManaType color)
        {
            var pool = g.ManaPoolManager;
            if (pool == null || pool.OrbLine == null) return null;

            int slot = pool.OrbLine.FirstEmptyIndex();
            if (slot < 0) return null; // line is full

            var canvas = pool.OrbLine.transform.parent; // the Canvas
            if (canvas == null) return null;
            var canvasRt = (RectTransform)canvas;

            var go = new GameObject($"ManaOrb_{color}", typeof(RectTransform), typeof(Image), typeof(ManaOrbInstance));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(OrbDiameter, OrbDiameter);

            // Render as a round disk, not a square quad.
            go.GetComponent<Image>().sprite = Scripts.Utilities.UiCircleSprite.Get();

            // Convert the world origin to canvas-anchored space.
            Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(Camera.main, worldOrigin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPt, null, out var startCanvas);

            var orb = go.GetComponent<ManaOrbInstance>();
            orb.Launch(pool.Bank, pool.OrbLine, color, startCanvas, slot);
            return orb;
        }
    }
}
