namespace Scripts.Data.Config
{
    /// <summary>
    /// CYLINDERMANAGERCONFIG - Static tuning values for CylinderManager.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// CylinderManager with compile-time defaults. CylinderManager mutates
    /// Ceiling / Floor / Focus at runtime (RNG-driven), so it seeds public
    /// instance fields with these values in Awake rather than referencing
    /// the constants directly.</para>
    /// <para>USAGE: Referenced from CylinderManager.Awake.</para>
    /// <para>RELATED FILES: CylinderManager.cs</para>
    /// </summary>
    public static class CylinderManagerConfig
    {
        // Initial upper bound of the oscillation in local Y units.
        public const float DefaultCeiling = 1f;

        // Initial lower bound of the oscillation in local Y units.
        public const float DefaultFloor = -1f;

        // Initial movement speed toward Ceiling/Floor (units per second).
        public const float DefaultFocus = 0.05f;
    }
}
