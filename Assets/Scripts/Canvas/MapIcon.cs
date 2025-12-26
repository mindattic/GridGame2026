using UnityEngine;

[DisallowMultipleComponent]
public class MapIcon : MonoBehaviour
{
    public enum HoverMode { Static, Hover }

    [Header("Mode")]
    [SerializeField] private HoverMode mode = HoverMode.Hover;

    [Header("Hover Settings")]
    [SerializeField] private float amplitude = 8f;      // pixels or local units
    [SerializeField] private float speed = 1.2f;        // cycles per second
    [SerializeField] private float phaseOffset = 0f;    // radians
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rect;
    private bool isUI;
    private Vector3 baseLocalPos;
    private Vector2 baseAnchoredPos;

    private float TimeNow => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        isUI = rect != null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isUI)
            baseAnchoredPos = rect.anchoredPosition;
        else
            baseLocalPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (mode == HoverMode.Static)
        {
            // keep at base
            if (isUI) rect.anchoredPosition = baseAnchoredPos;
            else transform.localPosition = baseLocalPos;
            return;
        }

        float yOffset = Mathf.Sin((TimeNow * speed * Mathf.PI * 2f) + phaseOffset) * amplitude;

        if (isUI)
        {
            var p = baseAnchoredPos;
            p.y += yOffset;
            rect.anchoredPosition = p;
        }
        else
        {
            var p = baseLocalPos;
            p.y += yOffset;
            transform.localPosition = p;
        }
    }

    // Runtime toggle
    public void SetHoverEnabled(bool enabled)
    {
        mode = enabled ? HoverMode.Hover : HoverMode.Static;
    }
}
