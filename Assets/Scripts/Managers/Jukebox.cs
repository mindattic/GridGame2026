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

        private static void Ensure()
        {
            if (music != null) return;
            var go = new GameObject("ChiptuneJukebox");
            Object.DontDestroyOnLoad(go);
            music = go.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.volume = 0.45f;
            sfx = go.AddComponent<AudioSource>();
            sfx.loop = false; sfx.playOnAwake = false; sfx.volume = 0.7f;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.activeSceneChanged += (from, to) => Apply(to.name);
            Apply(SceneManager.GetActiveScene().name);
        }

        private static void Apply(string sceneName)
        {
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
                    return "Vendor";
                default:
                    return null; // title / splash / loading → quiet
            }
        }
    }
}
