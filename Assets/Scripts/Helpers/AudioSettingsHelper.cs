using UnityEngine;
using Scripts.Managers;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Helpers
{
    /// <summary>
    /// AUDIOSETTINGSHELPER - US-096. Reads the active profile's audio preferences
    /// (<c>MusicVolume</c> / <c>SfxVolume</c> / <c>MuteMusic</c> / <c>MuteSfx</c>) and pushes the
    /// effective (mute-folded) volumes to every audio output:
    /// <list type="bullet">
    ///   <item>the cross-scene <see cref="Jukebox"/> — music bed + vendor-scene SFX;</item>
    ///   <item>the battle <c>SoundSource</c> — in-battle SFX (when a battle scene is loaded).</item>
    /// </list>
    /// <para>Call <see cref="Apply"/> at startup and whenever a volume/mute setting changes. Cheap and
    /// idempotent — safe to call every time a slider moves.</para>
    /// <para>RELATED FILES: Jukebox.cs, AudioManager.cs, SettingsManager.cs, Models/Profile.cs.</para>
    /// </summary>
    public static class AudioSettingsHelper
    {
        // Fallbacks match ProfileHelper.DefaultSettings, used when no profile is loaded yet.
        private const float DefaultMusic = 0.6f;
        private const float DefaultSfx = 0.85f;

        /// <summary>Push the current profile's effective audio volumes to all outputs.</summary>
        public static void Apply()
        {
            var s = ProfileHelper.CurrentProfile?.Settings;

            float music = s != null ? (s.MuteMusic ? 0f : Mathf.Clamp01(s.MusicVolume)) : DefaultMusic;
            float sfx   = s != null ? (s.MuteSfx   ? 0f : Mathf.Clamp01(s.SfxVolume))   : DefaultSfx;

            // Music + vendor SFX live on the cross-scene Jukebox.
            Jukebox.SetVolumes(music, sfx);

            // Battle SFX route through the battle SoundSource (see AudioManager.Play).
            var battleSfx = g.SoundSource;
            if (battleSfx != null) battleSfx.volume = sfx;
        }
    }
}
