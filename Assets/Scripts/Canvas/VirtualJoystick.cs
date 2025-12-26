using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach this to the joystick background. Assign Handle in the Inspector.
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private float maxRadius = 60f;     // pixels from center to edge
    [SerializeField] private float deadZone = 0.1f;     // normalized (0..1)

    private RectTransform rect;
    private Vector2 pointerLocal;                       // last local pointer pos
    private Vector2 output;                             // -1..1 vector

    public Vector2 Direction => output;                 // Consumers read this

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (handle == null)
        {
            var t = transform.Find("Handle") as RectTransform;
            if (t != null) handle = t;
        }
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rect == null || handle == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out pointerLocal))
            return;

        // Local center is (0,0) with centered pivot/anchors
        var clamped = Vector2.ClampMagnitude(pointerLocal, maxRadius);
        handle.anchoredPosition = clamped;

        var raw = clamped / Mathf.Max(1f, maxRadius); // normalized -1..1
        float mag = raw.magnitude;
        if (mag < deadZone)
            output = Vector2.zero;
        else
            output = raw; // keep magnitude for analog speed
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        output = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    // Added: allow external reset (e.g., on encounter)
    public void ResetOutput()
    {
        output = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}