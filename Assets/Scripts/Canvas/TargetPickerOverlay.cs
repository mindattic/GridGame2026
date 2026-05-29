using System;
using UnityEngine;

namespace Scripts.Canvas
{
    /// <summary>
    /// TARGETPICKEROVERLAY - The runtime UI shell that hosts a single targeting session. Listens
    /// for ESC / right-click to cancel; callers fire <see cref="NotifyPick"/> / <see cref="NotifyCancel"/>
    /// when their picker UI confirms or aborts. Either path destroys the overlay.
    /// </summary>
    public sealed class TargetPickerOverlay : MonoBehaviour
    {
        public event Action OnPicked;
        public event Action OnCancelled;

        private bool finished;

        public void NotifyPick()
        {
            if (finished) return;
            finished = true;
            OnPicked?.Invoke();
            Destroy(gameObject);
        }

        public void NotifyCancel()
        {
            if (finished) return;
            finished = true;
            OnCancelled?.Invoke();
            Destroy(gameObject);
        }

        private void Update()
        {
            if (finished) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                NotifyCancel();
        }
    }

    /// <summary>Tiny scale pulse for actor target rings.</summary>
    public sealed class TargetRingPulse : MonoBehaviour
    {
        public float speed = 4f;
        public float amplitude = 0.15f;
        private Vector3 baseScale;
        private void Awake() { baseScale = transform.localScale; }
        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * speed) * amplitude;
            transform.localScale = baseScale * s;
        }
    }
}
