using System.Collections.Generic;
using UnityEngine;
using Scripts.Utilities;
using W = Scripts.Utilities.ChiptuneSynth.Wave;

namespace Scripts.Libraries
{
    /// <summary>
    /// CHIPTUNEBANK - Cached procedural chiptune SFX + music ([[feedback_chiptune_audio]]).
    ///
    /// <para><see cref="Sfx"/> maps a semantic event key to a generated <see cref="AudioClip"/> (cached
    /// on first use). UNKNOWN keys still resolve — they get a deterministic fallback blip pitched from
    /// the key's hash — so no event is ever silent and there is no "sound not found" error spam.
    /// <see cref="Music"/> returns a looping battle/vendor bed.</para>
    /// </summary>
    public static class ChiptuneBank
    {
        private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        /// <summary>A cached chiptune clip for <paramref name="key"/> (generated on first request).</summary>
        public static AudioClip Sfx(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "blip";
            if (cache.TryGetValue(key, out var c) && c != null) return c;
            var clip = Build(key);
            cache[key] = clip;
            return clip;
        }

        /// <summary>A cached looping music bed: "Battle" (driving minor pentatonic) or "Vendor"
        /// (gentle major); any other key falls back to the vendor bed.</summary>
        public static AudioClip Music(string key)
        {
            string ck = "music:" + key;
            if (cache.TryGetValue(ck, out var c) && c != null) return c;
            AudioClip clip = (key == "Battle")
                // A-minor pentatonic, driving.
                ? ChiptuneSynth.MusicLoop("music_battle", new[] { 220f, 261.63f, 293.66f, 329.63f, 392f, 329.63f, 293.66f, 261.63f }, 140f, 16)
                // C-major, gentle shopping tune.
                : ChiptuneSynth.MusicLoop("music_vendor", new[] { 261.63f, 329.63f, 392f, 329.63f, 349.23f, 293.66f, 261.63f, 293.66f }, 92f, 16, volume: 0.18f);
            cache[ck] = clip;
            return clip;
        }

        private static AudioClip T(string n, float a, float b, float s, W w, float v = 0.45f) => ChiptuneSynth.Tone(n, a, b, s, w, v);

        private static AudioClip Build(string key)
        {
            switch (key)
            {
                // UI
                case "Click":    return T("sfx_click", 880, 880, 0.05f, W.Square, 0.30f);
                case "Select":   return T("sfx_select", 620, 990, 0.08f, W.Square);
                case "Back":     return T("sfx_back", 660, 440, 0.08f, W.Square);
                // Movement
                case "Slide":
                case "Move":     return T("sfx_move", 300, 420, 0.07f, W.Triangle, 0.35f);
                // Physical hits
                case "Slash":
                case "Hit":      return T("sfx_hit", 520, 120, 0.10f, W.Noise, 0.5f);
                case "Bump":     return T("sfx_bump", 180, 90, 0.09f, W.Square, 0.5f);
                // Magic / casts
                case "Cast":     return ChiptuneSynth.Sequence("sfx_cast", new[] { (523.25f, 0.05f), (659.25f, 0.05f), (783.99f, 0.07f) }, W.Square, 0.4f);
                case "Charge":   return T("sfx_charge", 200, 700, 0.35f, W.Saw, 0.4f);   // rising telegraph
                case "Fire":     return T("sfx_fire", 700, 200, 0.18f, W.Noise, 0.5f);
                case "Ice":      return T("sfx_ice", 1200, 1500, 0.16f, W.Pulse25, 0.4f);
                case "Thunder":  return T("sfx_thunder", 1500, 100, 0.16f, W.Noise, 0.55f);
                // Heals / buffs
                case "Heal":     return ChiptuneSynth.Sequence("sfx_heal", new[] { (523.25f, 0.06f), (659.25f, 0.06f), (783.99f, 0.06f), (1046.5f, 0.09f) }, W.Triangle, 0.4f);
                case "Quicken":  return T("sfx_quicken", 600, 1200, 0.14f, W.Square, 0.4f);
                // Economy / pincer
                case "Pincer":   return ChiptuneSynth.Sequence("sfx_pincer", new[] { (392f, 0.05f), (523.25f, 0.05f), (659.25f, 0.08f) }, W.Square, 0.45f);
                case "Orb":      return T("sfx_orb", 900, 1400, 0.10f, W.Pulse25, 0.4f);
                case "Crit":     return ChiptuneSynth.Sequence("sfx_crit", new[] { (659.25f, 0.04f), (988f, 0.04f), (1318.5f, 0.08f) }, W.Square, 0.5f);
                case "Pushback": return T("sfx_pushback", 400, 120, 0.12f, W.Saw, 0.45f);
                // Status / drama
                case "Debuff":   return T("sfx_debuff", 330, 160, 0.16f, W.Pulse25, 0.4f);
                case "Enrage":   return ChiptuneSynth.Sequence("sfx_enrage", new[] { (110f, 0.10f), (146.83f, 0.10f), (220f, 0.16f) }, W.Saw, 0.55f);
                case "Clutch":   return ChiptuneSynth.Sequence("sfx_clutch", new[] { (784f, 0.05f), (1046.5f, 0.05f), (1318.5f, 0.05f), (1568f, 0.12f) }, W.Square, 0.5f);
                case "Announce": return ChiptuneSynth.Sequence("sfx_announce", new[] { (784f, 0.05f), (1046.5f, 0.07f) }, W.Square, 0.4f);
                // Life cycle
                case "Death":    return T("sfx_death", 300, 60, 0.30f, W.Saw, 0.5f);
                case "Victory":  return ChiptuneSynth.Sequence("sfx_victory", new[] { (523.25f, 0.10f), (659.25f, 0.10f), (783.99f, 0.10f), (1046.5f, 0.22f) }, W.Square, 0.5f);
                case "Defeat":   return ChiptuneSynth.Sequence("sfx_defeat", new[] { (440f, 0.12f), (349.23f, 0.12f), (261.63f, 0.26f) }, W.Triangle, 0.5f);
                default:         return Fallback(key);
            }
        }

        /// <summary>Deterministic blip for an unregistered key — pitched from the key's hash so distinct
        /// events sound distinct (and consistent across runs). Guarantees nothing is ever silent.</summary>
        private static AudioClip Fallback(string key)
        {
            int h = Mathf.Abs(key.GetHashCode());
            float hz = 320f + (h % 14) * 55f;
            return ChiptuneSynth.Tone("sfx_" + key, hz, hz, 0.06f, W.Square, 0.32f);
        }
    }
}
