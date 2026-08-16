using System.Text;

namespace Scripts.Data
{
    /// <summary>
    /// AUDIOCREDITS - Every sourced audio asset with its full attribution (US-137 / GG-A5).
    ///
    /// <para>RULE (owner directive 2026-08-15): all sound and music comes from royalty-free
    /// sources and EVERYTHING is attributed in the Credits scene — CC0/no-attribution-required
    /// included. Add a row here whenever an audio asset lands in the project;
    /// <see cref="BuildCreditsSection"/> renders the block the Credits scroll shows.</para>
    ///
    /// <para>Kevin MacLeod tracks use his required attribution format (CC BY 4.0).</para>
    ///
    /// <para>RELATED FILES: MusicTrackLibrary.cs, SoundEffectLibrary.cs, CreditsManager.cs.</para>
    /// </summary>
    public static class AudioCredits
    {
        public readonly struct Entry
        {
            public readonly string Title;
            public readonly string Author;
            public readonly string License;
            public readonly string Url;
            public readonly string UsedFor;

            public Entry(string title, string author, string license, string url, string usedFor)
            {
                Title = title; Author = author; License = license; Url = url; UsedFor = usedFor;
            }
        }

        public static readonly Entry[] Music =
        {
            new Entry("Teller of the Tales", "Kevin MacLeod (incompetech.com)",
                "Creative Commons: By Attribution 4.0", "http://creativecommons.org/licenses/by/4.0/",
                "Title theme"),
            new Entry("Minstrel Guild", "Kevin MacLeod (incompetech.com)",
                "Creative Commons: By Attribution 4.0", "http://creativecommons.org/licenses/by/4.0/",
                "Vendor & campaign music"),
            new Entry("Crusade", "Kevin MacLeod (incompetech.com)",
                "Creative Commons: By Attribution 4.0", "http://creativecommons.org/licenses/by/4.0/",
                "Battle music"),
            new Entry("Triumph (Instrumental RPG Adventure)", "Pixabay contributor (asset 135451)",
                "Pixabay Content License", "https://pixabay.com/service/license-summary/",
                "Victory music"),
            new Entry("Melancholy Lull", "bundled track — origin untracked; verify/replace before commercial release",
                "unverified", "",
                "Defeat music"),
        };

        public static readonly Entry[] SoundEffects =
        {
            new Entry("Battle & UI sound-effect pack (SFX_*, Click, Move, Slash, …)",
                "bundled asset-pack audio — origin untracked; verify/replace before commercial release",
                "unverified", "",
                "Combat and interface SFX"),
            new Entry("Procedural chiptune synth (fallback SFX + music beds)",
                "MindAttic (generated in code — ChiptuneSynth.cs)",
                "original work", "",
                "Every event that has no authored clip"),
        };

        /// <summary>The full Audio block for the Credits scroll (TMP rich text).</summary>
        public static string BuildCreditsSection(string nl)
        {
            var sb = new StringBuilder();
            sb.Append($"<size=80%>Music</size>{nl}");
            foreach (var e in Music) AppendEntry(sb, e, nl);
            sb.Append($"{nl}<size=80%>Sound Effects</size>{nl}");
            foreach (var e in SoundEffects) AppendEntry(sb, e, nl);
            return sb.ToString();
        }

        private static void AppendEntry(StringBuilder sb, Entry e, string nl)
        {
            sb.Append($"<size=150%>\"{e.Title}\"</size>{nl}");
            sb.Append($"<size=60%>{e.Author} — {e.License} — {e.UsedFor}</size>{nl}");
            if (!string.IsNullOrEmpty(e.Url))
                sb.Append($"<size=10%>{e.Url}</size>{nl}");
            sb.Append(nl);
        }
    }
}
