using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Overworld
{
/// <summary>
/// OFFSCREENARROWINDICATOR - Points to offscreen targets.
/// 
/// PURPOSE:
/// Shows a screen-edge arrow pointing toward a world-space
/// target when that target is outside the camera view.
/// 
/// VISUAL EFFECT:
/// ```
///       [Target offscreen to the right]
///                              →
///    ┌──────────────────────┐ ← Arrow at edge
///    │                      │
///    │    [Viewport]        │
///    │                      │
///    └──────────────────────┘
/// ```
/// 
/// FEATURES:
/// - Positions arrow at screen edge
/// - Rotates to point toward target
/// - Fades in/out smoothly
/// - Configurable margin from edge
/// 
/// RELATED FILES:
/// - OverworldManager.cs: Overworld scene
/// - OverworldHero.cs: Potential target
/// </summary>
public sealed class OffscreenArrowIndicator : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float margin = 40f;
    [SerializeField] private float fadeSpeed = 10f;

    private RectTransform arrowRect;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;
    private Graphic arrowGraphic;

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

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        arrowRect = GetComponent<RectTransform>();
        if (arrowRect == null)
            Debug.LogWarning("OffscreenArrowIndicator requires a RectTransform.");

        var canvas = GetComponentInParent<UnityEngine.Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>Runs per-frame logic after all Update calls.</summary>
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

    /// <summary>Applies the alpha.</summary>
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

}
