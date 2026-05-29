using UnityEngine;
using Scripts.Utilities;

namespace Scripts.Canvas
{
    /// <summary>
    /// WORLDFOLLOWFROMTILE - Same idea as <see cref="WorldFollow"/> but the world target is a
    /// fixed board-grid tile (Vector2Int) — useful for tile-pick UI cells / preview highlights
    /// that don't track a moving Transform.
    /// </summary>
    public sealed class WorldFollowFromTile : MonoBehaviour
    {
        private RectTransform self;
        private RectTransform canvasRT;
        private Vector2Int tile;

        public void BindTile(Vector2Int tile)
        {
            this.tile = tile;
            self = (RectTransform)transform;
            var t = transform.parent;
            while (t != null && t.GetComponent<UnityEngine.Canvas>() == null) t = t.parent;
            canvasRT = t != null ? t.GetComponent<RectTransform>() : null;
        }

        private void LateUpdate()
        {
            if (self == null || canvasRT == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 world = Geometry.CalculatePositionByLocation(tile);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, null, out var local))
                self.anchoredPosition = local;
        }
    }
}
