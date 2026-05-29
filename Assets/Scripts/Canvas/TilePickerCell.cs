using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts.Canvas
{
    /// <summary>
    /// TILEPICKERCELL - A single board-tile-sized clickable cell, used during a tile-pick
    /// targeting session. Fires hover-enter / hover-exit so the shared
    /// <see cref="TargetShapePreview"/> can paint the spell's pending area as the cursor moves
    /// across the grid; the cell's <see cref="UnityEngine.UI.Button"/> handles the click.
    /// </summary>
    public sealed class TilePickerCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Action onHover;
        private Action onUnhover;

        public void Bind(Action onHover, Action onUnhover)
        {
            this.onHover = onHover;
            this.onUnhover = onUnhover;
        }

        public void OnPointerEnter(PointerEventData eventData) { onHover?.Invoke(); }
        public void OnPointerExit (PointerEventData eventData) { onUnhover?.Invoke(); }
    }
}
