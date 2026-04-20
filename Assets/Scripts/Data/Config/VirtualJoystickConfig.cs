namespace Scripts.Data.Config
{
    /// <summary>
    /// VIRTUALJOYSTICKCONFIG - Static tuning values for VirtualJoystick.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// VirtualJoystick with compile-time constants. The handle RectTransform
    /// is now resolved at runtime via transform.Find("Handle").</para>
    /// <para>USAGE: Referenced from VirtualJoystick.OnDrag.</para>
    /// <para>RELATED FILES: VirtualJoystick.cs, OverworldHero.cs</para>
    /// </summary>
    public static class VirtualJoystickConfig
    {
        // Pixels from joystick center to edge — clamps handle displacement.
        public const float MaxRadius = 60f;

        // Normalized (0..1) magnitude below which input is treated as zero.
        public const float DeadZone = 0.1f;
    }
}
