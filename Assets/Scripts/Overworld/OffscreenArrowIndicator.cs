using UnityEngine;
using UnityEngine.UI;

// Shows a screen-edge arrow pointing toward a world-space target when the target is offscreen.
// Attach this to Canvas/OffscreenArrow. The GameObject remains active; we fade its alpha in/out.
public sealed class OffscreenArrowIndicator : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float margin = 40f; // pixels inside screen edge
    [SerializeField] private float fadeSpeed = 10f;   // alpha units per second

    private RectTransform arrowRect;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup; // preferred for fading entire UI
    private Graphic arrowGraphic;    // fallback if no CanvasGroup

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public Camera WorldCamera
    {
        get => worldCamera;
        set => worldCamera = value;
    }

    public float Margin
    {
        get => margin;
        set => margin = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        arrowRect = GetComponent<RectTransform>();
        if (arrowRect == null)
            Debug.LogWarning("OffscreenArrowIndicator requires a RectTransform.");

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void LateUpdate()
    {
        float targetAlpha = 0f;

        if (arrowRect == null || worldCamera == null || target == null)
        {
            // No target/camera/rect -> fade out
            ApplyAlpha(targetAlpha);
            return;
        }

        Vector3 sp = worldCamera.WorldToScreenPoint(target.position);
        bool visible = sp.z > 0f && sp.x >= 0f && sp.x <= Screen.width && sp.y >= 0f && sp.y <= Screen.height;

        if (!visible)
        {
            // Determine point on screen edge pointing toward target
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 toTarget = new Vector2(sp.x, sp.y) - center;
            if (toTarget.sqrMagnitude > 1e-6f)
            {
                Vector2 dir = toTarget.normalized;
                float halfW = Screen.width * 0.5f - margin;
                float halfH = Screen.height * 0.5f - margin;

                float vx, vy;
                if (Mathf.Abs(dir.x) > 1e-4f)
                {
                    float sx = dir.x > 0 ? halfW : -halfW;
                    float sy = dir.y / Mathf.Abs(dir.x) * Mathf.Abs(sx);
                    if (Mathf.Abs(sy) <= halfH)
                    {
                        vx = center.x + sx;
                        vy = center.y + sy;
                    }
                    else
                    {
                        float sy2 = dir.y > 0 ? halfH : -halfH;
                        float sx2 = (dir.x / Mathf.Abs(dir.y)) * Mathf.Abs(sy2);
                        vx = center.x + sx2;
                        vy = center.y + sy2;
                    }
                }
                else
                {
                    vx = center.x;
                    vy = center.y + (dir.y > 0 ? halfH : -halfH);
                }

                Vector2 edge = new Vector2(vx, vy);
                if (canvasRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, edge, null, out var localPos))
                    {
                        arrowRect.anchoredPosition = localPos;
                    }
                }

                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                arrowRect.localEulerAngles = new Vector3(0f, 0f, ang);
            }

            targetAlpha = 1f; // offscreen -> fade in
        }
        else
        {
            targetAlpha = 0f; // onscreen -> fade out
        }

        ApplyAlpha(targetAlpha);
    }

    private void ApplyAlpha(float targetAlpha)
    {
        float dt = Application.isPlaying ? Time.deltaTime : 0f;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * dt);
        }
        else if (arrowGraphic != null)
        {
            var c = arrowGraphic.color;
            float a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * dt);
            arrowGraphic.color = new Color(c.r, c.g, c.b, a);
        }
    }
}
