using UnityEngine;
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
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
/// <summary>
/// MAPICON - Hovering/bobbing map marker effect.
/// 
/// PURPOSE:
/// Creates a gentle hovering animation for map icons and markers,
/// supporting both UI (RectTransform) and world-space transforms.
/// 
/// MODES:
/// - Static: Icon remains at base position
/// - Hover: Icon bobs up and down continuously
/// 
/// ANIMATION:
/// ```
///    ↑ amplitude
///    ○  ← icon bobs
///    ↓
/// ```
/// Uses sine wave for smooth oscillation.
/// 
/// CONFIGURATION:
/// - amplitude: Vertical movement range (pixels/units)
/// - speed: Oscillation frequency (cycles per second)
/// - phaseOffset: Starting phase (radians)
/// - useUnscaledTime: Ignore Time.timeScale
/// 
/// RELATED FILES:
/// - WorldMapInstance.cs: Map display
/// - StageButtonInstance.cs: Stage markers
/// </summary>
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

    /// <summary>Caches the RectTransform and determines whether this is a UI element.</summary>
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        isUI = rect != null;
    }

    /// <summary>Records the initial position as the base for hover oscillation.</summary>
    void Start()
    {
        if (isUI)
            baseAnchoredPos = rect.anchoredPosition;
        else
            baseLocalPos = transform.localPosition;
    }

    /// <summary>Applies a sine-wave vertical offset for the hover bob effect each frame.</summary>
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

    /// <summary>Enables or disables the hover bob effect at runtime.</summary>
    public void SetHoverEnabled(bool enabled)
    {
        mode = enabled ? HoverMode.Hover : HoverMode.Static;
    }
}

}
