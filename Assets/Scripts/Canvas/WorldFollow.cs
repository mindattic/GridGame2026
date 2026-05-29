using UnityEngine;

namespace Scripts.Canvas
{
    /// <summary>
    /// WORLDFOLLOW - Pins a Screen-Space-Overlay RectTransform to the screen-projection of a
    /// world-space Transform plus a world-space offset. Used by the DebuffIconBar so the per-
    /// actor icon strip tracks the actor as it slides around the board.
    /// </summary>
    public sealed class WorldFollow : MonoBehaviour
    {
        private RectTransform self;
        private RectTransform canvasRT;
        private Transform worldTarget;
        private Vector3 worldOffset;

        public void Bind(Transform target, Vector3 worldOffset)
        {
            this.worldTarget = target;
            this.worldOffset = worldOffset;
            self = (RectTransform)transform;
            // Walk up to the root Canvas RectTransform.
            var t = transform.parent;
            while (t != null && t.GetComponent<UnityEngine.Canvas>() == null) t = t.parent;
            canvasRT = t != null ? t.GetComponent<RectTransform>() : null;
        }

        private void LateUpdate()
        {
            if (worldTarget == null || self == null || canvasRT == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = worldTarget.position + worldOffset;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, null, out var local))
                self.anchoredPosition = local;
        }
    }
}
