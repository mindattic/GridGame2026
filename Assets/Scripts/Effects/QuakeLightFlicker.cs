using UnityEngine;

namespace Scripts.Effects
{
    /// <summary>
    /// QUAKELIGHTFLICKER - id Software's classic Quake (1996) light style patterns.
    /// <para>PURPOSE: Provides the original 11 light flicker pattern strings used by Quake's
    /// dynamic lighting system, plus samplers that turn them into a brightness curve over time.
    /// Each character maps to a brightness level: 'a'=0 (off), 'm'=1 (normal), 'z'=2.083 (max).</para>
    /// <para>RELATED FILES: ActorPanel.cs (enemy backdrop flicker)</para>
    /// </summary>
    public static class QuakeLightFlicker
    {
        public const string Normal              = "m";
        public const string FlickerA            = "mmnmmommommnonmmonqnmmo";
        public const string SlowPulse           = "abcdefghijklmnopqrstuvwxyzyxwvutsrqponmlkjihgfedcba";
        public const string CandleA             = "mmmmmaaaaammmmmaaaaaabcdefgabcdefg";
        public const string FastStrobe          = "mamamamamama";
        public const string GentlePulse         = "jklmnopqrstuvwxyzyxwvutsrqponmlkj";
        public const string FlickerB            = "nmonqnmomnmomomno";
        public const string CandleB             = "mmmaaaabcdefgmmmmaaaammmaamm";
        public const string CandleC             = "mmmaaammmaaammmabcdefaaaammmmabcdefmmmaaaa";
        public const string SlowStrobe          = "aaaaaaaazzzzzzzz";
        public const string FluorescentFlicker  = "mmamammmmammamamaaamammma";

        public const float CharsPerSecond = 10f;

        /// <summary>Sample the pattern at <paramref name="time"/>, returning brightness in [0..2.083].</summary>
        public static float Sample(string pattern, float time)
        {
            if (string.IsNullOrEmpty(pattern)) return 1f;
            int len = pattern.Length;
            int idx = Mathf.FloorToInt(time * CharsPerSecond);
            char c = pattern[((idx % len) + len) % len];
            return (c - 'a') / (float)('m' - 'a');
        }

        /// <summary>Sample with linear interpolation between adjacent frames for a smoother curve.</summary>
        public static float SampleSmooth(string pattern, float time)
        {
            if (string.IsNullOrEmpty(pattern)) return 1f;
            int len = pattern.Length;
            float pos = time * CharsPerSecond;
            int idx = Mathf.FloorToInt(pos);
            float frac = pos - idx;
            char a = pattern[((idx % len) + len) % len];
            char b = pattern[(((idx + 1) % len) + len) % len];
            float va = (a - 'a') / (float)('m' - 'a');
            float vb = (b - 'a') / (float)('m' - 'a');
            return Mathf.Lerp(va, vb, frac);
        }
    }
}
