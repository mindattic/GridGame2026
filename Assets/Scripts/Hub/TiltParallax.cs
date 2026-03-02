using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

namespace Scripts.Hub
{
/// <summary>
/// Applies a parallax/scroll effect to a panel based on device tilt.
/// Editor emulation: hold LeftControl and move the mouse (screen position maps to tilt -1..1).
/// Hold Shift + LeftControl to double amplitude.
/// </summary>
[DisallowMultipleComponent]
public class TiltParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    public float amplitude =64f;
    public float smoothing =10f;
    public float deadzone =0.01f;

    [Header("Debug Output")]
    public bool writeToOutput = true;

    private RectTransform rect;
    private Vector2 basePos;
    private Vector2 currentOffset;
    private bool layoutDetected;

    private static TextMeshProUGUI sharedOutput; // single output label
    private static TiltParallax primary; // instance responsible for writing

    private bool usingMouseEmulation; // editor flag
    private Vector2 lastMouseNormalized; // for output

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
            basePos = rect.anchoredPosition;
        DetectLayoutLocking();
        AcquireOutputLabel();
        if (primary == null) primary = this; // first instance becomes primary
    }

    private void OnEnable() => ResetPosition();
    private void OnDisable() => ResetPosition();

    private void ResetPosition()
    {
        if (rect == null) return;
        rect.anchoredPosition = basePos;
        currentOffset = Vector2.zero;
    }

    private void Update()
    {
        if (rect == null) return;
        Vector2 input = ReadTilt();
        if (input.sqrMagnitude < deadzone * deadzone) input = Vector2.zero;

        // If emulating, allow more direct feel (less smoothing slowdown)
        float lerpFactor = usingMouseEmulation ?1f : Mathf.Clamp01(smoothing * Time.deltaTime);
        Vector2 target = new Vector2(-input.x, input.y) * amplitude; // invert X for parallax feel

        // Shift modifier boosts amplitude when emulating
        if (usingMouseEmulation && Input.GetKey(KeyCode.LeftShift))
            target *=2f;

        currentOffset = Vector2.Lerp(currentOffset, target, lerpFactor);

        // Apply immediately (also apply again in LateUpdate in case layout adjusts later)
        rect.anchoredPosition = basePos + currentOffset;

        if (writeToOutput && this == primary) WriteStatus(input);
    }

    private void LateUpdate()
    {
        if (rect != null)
            rect.anchoredPosition = basePos + currentOffset;
    }

    private void DetectLayoutLocking()
    {
        var p = transform.parent;
        while (p != null)
        {
            if (p.GetComponent<HorizontalOrVerticalLayoutGroup>() != null ||
                p.GetComponent<LayoutGroup>() != null ||
                p.GetComponent<ContentSizeFitter>() != null)
            {
                layoutDetected = true;
                return;
            }
            p = p.parent;
        }
    }

    private void AcquireOutputLabel()
    {
        if (sharedOutput != null) return;
        var go = GameObject.Find("Canvas/Output"); // expected path
        if (go == null) go = GameObject.Find("Output"); // fallback name
        if (go != null)
            sharedOutput = go.GetComponent<TextMeshProUGUI>();
    }

    private void WriteStatus(Vector2 input2D)
    {
        if (sharedOutput == null) return;
        Vector3 acc = SystemInfo.supportsAccelerometer && !Application.isEditor ? Input.acceleration : Vector3.zero;
#if UNITY_EDITOR
        Vector2 rawMouse = (Vector2)Input.mousePosition;
#endif
        sharedOutput.text =
        $"Tilt Monitor\n" +
        $"Accelerometer Supported: {SystemInfo.supportsAccelerometer}\n" +
        $"Accel Raw XYZ: {acc.x:F3} {acc.y:F3} {acc.z:F3}\n" +
        $"Input2D (used): {input2D.x:F3} {input2D.y:F3}\n" +
        $"Offset XY: {currentOffset.x:F1} {currentOffset.y:F1}\n" +
        $"LayoutLocked: {layoutDetected}\n" +
#if UNITY_EDITOR
        $"Editor Emulation: {(usingMouseEmulation ? (Input.GetKey(KeyCode.LeftShift) ? "Mouse (Ctrl+Shift)" : "Mouse (Ctrl)") : "Idle")}\n" +
        $"Mouse Norm XY: {lastMouseNormalized.x:F3} {lastMouseNormalized.y:F3}\n" +
        $"Mouse Raw XY: {rawMouse.x:F0} {rawMouse.y:F0}\n" +
#endif
        "";
    }

    private Vector2 ReadTilt()
    {
#if UNITY_EDITOR
        // Always prefer mouse emulation in editor when LeftControl is held (ignore accelerometer availability)
        if (Input.GetKey(KeyCode.LeftControl))
        {
            usingMouseEmulation = true;
            Vector2 mp = Input.mousePosition;
            float nx = (mp.x / Mathf.Max(1f, Screen.width) -0.5f) *2f;
            float ny = (mp.y / Mathf.Max(1f, Screen.height) -0.5f) *2f;
            var v = new Vector2(Mathf.Clamp(nx, -1f,1f), Mathf.Clamp(ny, -1f,1f));
            v = Vector2.ClampMagnitude(v,1f);
            lastMouseNormalized = v;
            return v;
        }
        usingMouseEmulation = false;
        lastMouseNormalized = Vector2.zero;
        return Vector2.zero;
#else
        if (SystemInfo.supportsAccelerometer)
        {
            Vector3 a = Input.acceleration;
            return new Vector2(Mathf.Clamp(a.x, -1f,1f), Mathf.Clamp(a.y, -1f,1f));
        }
        return Vector2.zero;
#endif
    }
}

}
