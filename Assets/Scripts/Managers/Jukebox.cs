using UnityEngine;
using UnityEngine.SceneManagement;
using Scripts.Libraries;

namespace Scripts.Managers
{
    /// <summary>
    /// JUKEBOX - Scene-independent chiptune audio output ([[feedback_chiptune_audio]]).
    ///
    /// <para>Owns its own persistent <see cref="AudioSource"/>s (a looping music source + a one-shot
    /// SFX source) on a DontDestroyOnLoad object, so background music and fallback SFX work in EVERY
    /// scene — including the vendor scenes, which have no battle GameManager/SoundSource. Clips come
    /// from <see cref="ChiptuneBank"/> (generated + cached).</para>
    /// </summary>
    public static class Jukebox
    {
        private static AudioSource music;
        private static AudioSource sfx;
        private static string currentTrack;

        // US-096: source volumes, driven by ProfileSettings via AudioSettingsHelper.Apply().
        // Defaults match ProfileHelper.DefaultSettings so playback before settings load is sane.
        private static float musicVolume = 0.6f;
        private static float sfxVolume = 0.85f;

        private static void Ensure()
        {
            if (music != null) return;
            var go = new GameObject("ChiptuneJukebox");
            Object.DontDestroyOnLoad(go);
            music = go.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.volume = musicVolume;
            sfx = go.AddComponent<AudioSource>();
            sfx.loop = false; sfx.playOnAwake = false; sfx.volume = sfxVolume;
        }

        /// <summary>US-096: set the Jukebox's music + sfx source volumes (already mute-folded by the
        /// caller — pass 0 to mute). Applies live to the running sources.</summary>
        public static void SetVolumes(float musicVol, float sfxVol)
        {
            Ensure();
            musicVolume = Mathf.Clamp01(musicVol);
            sfxVolume = Mathf.Clamp01(sfxVol);
            music.volume = musicVolume;
            sfx.volume = sfxVolume;
        }

        /// <summary>Start (or switch to) a looping music bed by key ("Battle"/"Vendor"). No-op if the
        /// same track is already playing; null/empty stops the music.</summary>
        public static void PlayMusic(string trackKey)
        {
            Ensure();
            if (string.IsNullOrEmpty(trackKey)) { music.Stop(); currentTrack = null; return; }
            if (currentTrack == trackKey && music.isPlaying) return;
            currentTrack = trackKey;
            music.clip = ChiptuneBank.Music(trackKey);
            music.Play();
        }

        public static void StopMusic()
        {
            if (music != null) music.Stop();
            currentTrack = null;
        }

        /// <summary>One-shot SFX on the persistent source — used as the cross-scene fallback when no
        /// battle SoundSource exists (e.g. vendor scenes).</summary>
        public static void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            Ensure();
            sfx.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// MUSICDIRECTOR - Picks the chiptune track per scene with zero per-scene wiring. Subscribes to
    /// scene changes at launch and maps scene name → "Battle" / "Vendor" / silence.
    /// </summary>
    public static class MusicDirector
    {
        /// <summary>One-shot music override for the next PostBattleScreen load, set by
        /// BattleWonSequence / BattleLostSequence ("Victory" / "Defeat"). PostBattleScreen is a
        /// single scene for both outcomes, so the scene name alone can't pick the bed — the
        /// outcome-aware sequence stashes it here and TrackFor consumes it once.</summary>
        public static string PendingPostBattleTrack;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.activeSceneChanged += (from, to) => Apply(to.name);
            Apply(SceneManager.GetActiveScene().name);
        }

        private static void Apply(string sceneName)
        {
            // US-096/US-095: re-apply audio + motion settings on every scene change so a profile
            // loaded after launch (or a change made in another scene) is honored.
            Scripts.Helpers.AudioSettingsHelper.Apply();
            Scripts.Helpers.MotionSettingsHelper.Apply();

            var track = TrackFor(sceneName);
            if (string.IsNullOrEmpty(track)) Jukebox.StopMusic();
            else Jukebox.PlayMusic(track);
        }

        private static string TrackFor(string scene)
        {
            if (scene == "Game") return "Battle";
            switch (scene)
            {
                case "Vendor":
                case "Alchemist":
                case "Blacksmith":
                case "Equip":
                case "Party":
                case "Abilities":
                case "StageSelect":
                case "Hub":       // vendor launcher — same shop-district bed
                    return "Vendor";
                case "Bestiary":  // codex screen reached from Title — keep the title bed
                    return "Title";
                case "Overworld":
                    return "Overworld";
                case "TitleScreen":
                case "SplashScreen":
                    return "Title";
                case "PostBattleScreen":
                {
                    // One scene serves both win and loss — use the outcome the won/lost sequence
                    // stashed (consumed once). Falls back to quiet if entered some other way.
                    var t = PendingPostBattleTrack;
                    PendingPostBattleTrack = null;
                    return t; // "Victory" / "Defeat" / null
                }
                default:
                    // loading / credits / profile / save-select / settings → quiet.
                    return null;
            }
        }
    }
}
