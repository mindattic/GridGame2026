using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Models
{
    public class StageLineRenderer : CanvasLineRenderer
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button endButton;

        private void Update()
        {
            if (startButton != null && endButton != null)
            {
                UpdateLine(startButton, endButton);
            }
        }
    }
}