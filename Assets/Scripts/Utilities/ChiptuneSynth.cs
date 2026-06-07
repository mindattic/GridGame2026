using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Utilities
{
    /// <summary>
    /// CHIPTUNESYNTH - Procedural 8-bit audio generator (no art assets needed).
    ///
    /// <para>PURPOSE: the project's audio mandate ([[feedback_chiptune_audio]]) is that NOTHING is
    /// silent — every event gets an automated chiptune sound, and battle/vendor scenes get chiptune
    /// background music. This synthesizes <see cref="AudioClip"/>s in code: single tones (with ADSR +
    /// optional pitch slide), multi-note jingles, and tileable looping music beds. Callers cache the
    /// results (see <c>ChiptuneBank</c>) — generation is cheap but not free.</para>
    /// </summary>
    public static class ChiptuneSynth
    {
        public enum Wave { Square, Pulse25, Triangle, Saw, Noise }

        public const int SampleRate = 22050;

        /// <summary>A single tone: <paramref name="seconds"/> long, pitch sliding linearly from
        /// <paramref name="startHz"/> to <paramref name="endHz"/> (equal = steady), shaped by a quick
        /// attack + exponential decay so it reads as a chiptune blip.</summary>
        public static AudioClip Tone(string name, float startHz, float endHz, float seconds, Wave wave, float volume = 0.5f)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * Mathf.Max(0.01f, seconds)));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;                       // 0..1 across the clip
                float freq = Mathf.Lerp(startHz, endHz, t);
                phase += freq / SampleRate;
                if (phase >= 1f) phase -= 1f;
                data[i] = Sample(wave, phase) * Envelope(t) * volume;
            }
            return FromSamples(name, data);
        }

        /// <summary>A sequence of (frequencyHz, durationSeconds) notes rendered back-to-back into one
        /// clip — for jingles and characterful SFX (victory fanfare, level-up arpeggio, etc.).</summary>
        public static AudioClip Sequence(string name, (float hz, float dur)[] notes, Wave wave, float volume = 0.5f)
        {
            var data = new List<float>();
            foreach (var (hz, dur) in notes)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * Mathf.Max(0.01f, dur)));
                float phase = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / n;
                    phase += hz / SampleRate;
                    if (phase >= 1f) phase -= 1f;
                    data.Add(Sample(wave, phase) * Envelope(t) * volume);
                }
            }
            return FromSamples(name, data.ToArray());
        }

        /// <summary>A seamlessly-tileable music bed: a square-wave arpeggio over the given
        /// <paramref name="scaleHz"/> plus a triangle bassline, <paramref name="beats"/> long at
        /// <paramref name="bpm"/>. Looping it (AudioSource.loop) gives endless chiptune background.</summary>
        public static AudioClip MusicLoop(string name, float[] scaleHz, float bpm, int beats, float volume = 0.22f)
        {
            if (scaleHz == null || scaleHz.Length == 0) scaleHz = new[] { 440f };
            float beatSeconds = 60f / Mathf.Max(1f, bpm);
            int beatSamples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * beatSeconds));
            int total = beatSamples * Mathf.Max(1, beats);
            var data = new float[total];

            float leadPhase = 0f, bassPhase = 0f;
            for (int b = 0; b < beats; b++)
            {
                // Lead: step up the scale each beat (arpeggio); Bass: root an octave down, every 2 beats.
                float lead = scaleHz[b % scaleHz.Length];
                float bass = scaleHz[(b / 2) % scaleHz.Length] * 0.5f;
                for (int i = 0; i < beatSamples; i++)
                {
                    int idx = b * beatSamples + i;
                    float t = (float)i / beatSamples;        // 0..1 within the beat (for note envelope)
                    leadPhase += lead / SampleRate; if (leadPhase >= 1f) leadPhase -= 1f;
                    bassPhase += bass / SampleRate; if (bassPhase >= 1f) bassPhase -= 1f;
                    float leadV = Sample(Wave.Square, leadPhase) * NoteEnv(t) * 0.6f;
                    float bassV = Sample(Wave.Triangle, bassPhase) * 0.7f;
                    data[idx] = (leadV + bassV) * volume;
                }
            }
            return FromSamples(name, data);
        }

        // ── helpers ──

        private static float Sample(Wave wave, float p)
        {
            switch (wave)
            {
                case Wave.Square:   return p < 0.5f ? 1f : -1f;
                case Wave.Pulse25:  return p < 0.25f ? 1f : -1f;
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
                case Wave.Saw:      return 2f * p - 1f;
                case Wave.Noise:    return Random.value * 2f - 1f;
                default:            return 0f;
            }
        }

        /// <summary>Whole-clip envelope: fast attack, gentle exponential decay, short release.</summary>
        private static float Envelope(float t)
        {
            const float attack = 0.04f, release = 0.10f;
            float a = t < attack ? t / attack : 1f;
            float r = t > 1f - release ? (1f - t) / release : 1f;
            float decay = Mathf.Lerp(1f, 0.5f, t); // slight fade so blips don't sound flat
            return a * r * decay;
        }

        /// <summary>Per-note envelope inside a music beat: pluck attack + decay, silent tail so notes
        /// stay distinct and the loop seam is quiet.</summary>
        private static float NoteEnv(float t)
        {
            const float attack = 0.02f;
            float a = t < attack ? t / attack : 1f;
            float d = Mathf.Exp(-3f * t);   // exponential pluck decay
            return a * d;
        }

        private static AudioClip FromSamples(string name, float[] data)
        {
            // Soft-clip to avoid harsh overflow when layers stack.
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i], -0.95f, 0.95f);
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
