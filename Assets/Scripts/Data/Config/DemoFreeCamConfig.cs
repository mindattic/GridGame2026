using UnityEngine;

namespace Scripts.Data.Config
{
    /// <summary>
    /// DEMOFREECAMCONFIG - Static tuning values for the Demo_FreeCam debug camera.
    /// <para>PURPOSE: Demo_FreeCam is a development-only free-flying camera attached
    /// to a scene camera for debugging. All 18 Inspector fields are authoring defaults
    /// — no runtime tuning. Moved to config for the [SerializeField] divorce.</para>
    /// <para>USAGE: Demo_FreeCam reads these constants directly.</para>
    /// <para>RELATED FILES: Demo_FreeCam.cs</para>
    /// </summary>
    public static class DemoFreeCamConfig
    {
        // ── Focus Object ─────────────────────────────────────────────────────
        public const bool  DoFocus          = false;
        public const float FocusLimit       = 100f;
        public const float MinFocusDistance = 5.0f;

        // ── Undo Focus keys ──────────────────────────────────────────────────
        public const KeyCode FirstUndoKey  = KeyCode.LeftControl;
        public const KeyCode SecondUndoKey = KeyCode.Z;

        // ── Movement speeds ──────────────────────────────────────────────────
        public const float MoveSpeed     = 1.0f;
        public const float RotationSpeed = 10.0f;
        public const float ZoomSpeed     = 10.0f;

        // ── Axis names (Unity Input Manager) ─────────────────────────────────
        public const string MouseY   = "Mouse Y";
        public const string MouseX   = "Mouse X";
        public const string ZoomAxis = "Mouse ScrollWheel";

        // ── Move keys ────────────────────────────────────────────────────────
        public const KeyCode ForwardKey = KeyCode.W;
        public const KeyCode BackKey    = KeyCode.S;
        public const KeyCode LeftKey    = KeyCode.A;
        public const KeyCode RightKey   = KeyCode.D;

        // ── Modifier keys ────────────────────────────────────────────────────
        public const KeyCode FlatMoveKey       = KeyCode.LeftShift;
        public const KeyCode AnchoredMoveKey   = KeyCode.Mouse2;
        public const KeyCode AnchoredRotateKey = KeyCode.Mouse1;
    }
}
