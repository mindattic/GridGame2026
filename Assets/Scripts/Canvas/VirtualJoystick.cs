using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
/// VIRTUALJOYSTICK - On-screen analog joystick input.
/// 
/// PURPOSE:
/// Provides touch-based analog joystick control for mobile devices.
/// Outputs a normalized direction vector for movement input.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌─────────────┐
/// │      ○      │ ← Handle (draggable)
/// │    ╱   ╲    │
/// │   ○     ○   │ ← Background bounds
/// │    ╲   ╱    │
/// │      ○      │
/// └─────────────┘
/// ```
/// 
/// OUTPUT:
/// - Direction: Vector2 from -1 to 1 for both axes
/// - Magnitude preserved for analog speed control
/// - Dead zone filters small movements
/// 
/// CONFIGURATION:
/// - maxRadius: Pixels from center to edge (default 60)
/// - deadZone: Normalized threshold for ignoring input (default 0.1)
/// 
/// USAGE:
/// ```csharp
/// Vector2 input = virtualJoystick.Direction;
/// hero.Move(input * speed);
/// ```
/// 
/// RELATED FILES:
/// - OverworldHero.cs: Uses for movement input
/// - InputManager.cs: Alternative input source
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private float maxRadius = 60f;     // pixels from center to edge
    [SerializeField] private float deadZone = 0.1f;     // normalized (0..1)

    private RectTransform rect;
    private Vector2 pointerLocal;                       // last local pointer pos
    private Vector2 output;                             // -1..1 vector

    public Vector2 Direction => output;                 // Consumers read this

    /// <summary>Caches the RectTransform and resolves the handle child, centering it.</summary>
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

    /// <summary>Begins drag tracking when the joystick area is touched.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    /// <summary>Moves the handle toward the pointer position, clamped within maxRadius, and outputs normalized direction.</summary>
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

    /// <summary>Resets the handle to center and clears the output direction on release.</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        output = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    /// <summary>Externally resets the joystick output and handle position (e.g., on encounter start).</summary>
    public void ResetOutput()
    {
        output = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
}
