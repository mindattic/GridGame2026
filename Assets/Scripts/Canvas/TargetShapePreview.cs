using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Models;
using Scripts.Services;
using Scripts.Utilities;

namespace Scripts.Canvas
{
    /// <summary>
    /// TARGETSHAPEPREVIEW - A pool of colored tile highlights that previews the pending hit-area
    /// of a spell while the player hovers during target selection.
    ///
    /// <para><see cref="ShowAt"/> takes an anchor + shape + radius, resolves the affected tiles
    /// via <see cref="TargetShapeResolver.Resolve"/>, recycles highlight images to cover them,
    /// hides the unused remainder. <see cref="HideAll"/> resets. Each highlight tracks its tile
    /// in world space via <see cref="WorldFollowFromTile"/> so the previews land on the actual
    /// tile centers regardless of board offset.</para>
    /// </summary>
    public sealed class TargetShapePreview : MonoBehaviour
    {
        public const float HighlightSize = 84f;
        public static readonly Color FillColor = new Color(1f, 0.85f, 0.3f, 0.45f);

        private readonly List<Image> pool = new List<Image>();

        public void HideAll()
        {
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null) pool[i].gameObject.SetActive(false);
        }

        public void ShowAt(Vector2Int anchor, TargetShape shape, int radius, int boardW, int boardH)
        {
            var tiles = TargetShapeResolver.Resolve(anchor, shape, radius, boardW, boardH);
            EnsurePool(tiles.Count);

            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < tiles.Count;
                pool[i].gameObject.SetActive(active);
                if (!active) continue;
                pool[i].GetComponent<WorldFollowFromTile>().BindTile(tiles[i]);
            }
        }

        private void EnsurePool(int requested)
        {
            while (pool.Count < requested)
            {
                var go = new GameObject($"PreviewCell_{pool.Count}",
                    typeof(RectTransform), typeof(Image), typeof(WorldFollowFromTile));
                go.layer = gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.sizeDelta = new Vector2(HighlightSize, HighlightSize);
                var img = go.GetComponent<Image>();
                img.color = FillColor;
                img.raycastTarget = false;
                pool.Add(img);
            }
        }
    }
}
