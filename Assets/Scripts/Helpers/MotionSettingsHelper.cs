using Scripts.Managers;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// MOTIONSETTINGSHELPER - US-095 reduce-motion. Reads the active profile's <c>ReduceMotion</c>
    /// preference and pushes it to the two motion sources:
    /// <list type="bullet">
    ///   <item><see cref="VisualEffectManager.IntensityScale"/> → 0 suppresses particle VFX;</item>
    ///   <item><see cref="ProjectileMotionEval.ReduceMotion"/> → true collapses projectile arcs to
    ///   a straight glide.</item>
    /// </list>
    /// <para>Call <see cref="Apply"/> at startup and whenever the toggle changes. Cheap + idempotent.</para>
    /// <para>RELATED FILES: VisualEffectManager.cs, ProjectileMotion.cs, SettingsManager.cs, Models/Profile.cs.</para>
    /// </summary>
    public static class MotionSettingsHelper
    {
        public static void Apply()
        {
            var s = ProfileHelper.CurrentProfile?.Settings;
            bool reduce = s != null && s.ReduceMotion;

            VisualEffectManager.IntensityScale = reduce ? 0f : 1f;
            ProjectileMotionEval.ReduceMotion = reduce;
        }
    }
}
