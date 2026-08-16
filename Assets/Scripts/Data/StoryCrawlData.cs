using System.Collections.Generic;

namespace Scripts.Data
{
    /// <summary>
    /// STORYCRAWLDATA - The barebones plot, one crawl per campaign theme (US-131 / GG-A5).
    ///
    /// <para>PURPOSE: Data-driven text for the skippable Star-Wars-style crawl shown on first
    /// entry into each theme. Keyed by <c>CampaignTheme.Id</c>. Prose only — no dialog system,
    /// no branching (those stay cut per §27/GG-A5). Writers edit THIS file; nothing else.</para>
    ///
    /// <para>LORE FRAME: the light-bearing invaders descend into the Undearth — a sunless world —
    /// chasing the stolen dawn, from the pastoral edge (Green Valley) down to the drowned city
    /// where the Vampire Lord holds the spire (Veshker Ruins).</para>
    ///
    /// <para>RELATED FILES: StoryCrawlManager.cs, StoryCrawlBuilder.cs, CampaignStages.cs.</para>
    /// </summary>
    public static class StoryCrawlData
    {
        private static readonly Dictionary<string, string[]> ByThemeId = new Dictionary<string, string[]>
        {
            ["GreenValley"] = new[]
            {
                "The sun did not set. It was taken.",
                "You are the Lightbearers — the last to carry embers of the stolen dawn. Below the " +
                "world's skin lies the UNDEARTH, sunless and vast, and somewhere in its deepest dark " +
                "the thief waits on a drowned throne.",
                "Your descent begins gently: the GREEN VALLEY, where the last pastures cling to the " +
                "cavern mouths. The creatures here are small, and hungry, and only the first of many.",
            },
            ["Desert"] = new[]
            {
                "Beyond the valley the ceiling rises and the ground forgets water.",
                "The SANDSEA REACHES: an arid ocean of dunes under stone sky, raider country where " +
                "nothing moves in the open — until it does. The locals learned long ago that light " +
                "draws teeth. You carry light.",
                "Cross the dunes. Watch for movement.",
            },
            ["Swamp"] = new[]
            {
                "The Reaches drain into the fens, and the fens remember everything they swallow.",
                "MIREHOLT: knee-deep water black as ink, reeds that whisper without wind. Lurkers " +
                "hunt below the surface, and the hags of the mist trade in stolen voices.",
                "Keep your embers dry. Keep each other close. The pincer is your only edge here.",
            },
            ["Cave"] = new[]
            {
                "Below the fens the water turns to ice, and the dark turns honest — it stops " +
                "pretending to be anything but a throat.",
                "The FROSTMAW CAVERNS: ice-cracked stone where heavy things move slow and hit like " +
                "falling ceilings. The cold saps the timeline itself; every second you hesitate " +
                "belongs to them.",
                "The spire is close now. The dark is thickest just before the throne.",
            },
            ["CityRuins"] = new[]
            {
                "At the bottom of the world lies a city that drowned standing up.",
                "VESHKER: towers under black water, streets that remember festivals. The stolen dawn " +
                "burns at the top of the spire — and the VAMPIRE LORD who took it has had a very " +
                "long time to prepare for your arrival.",
                "This is the last descent. Bring back the sun.",
            },
        };

        /// <summary>Crawl paragraphs for a theme, or null when the theme has no crawl.</summary>
        public static string[] Get(string themeId)
            => !string.IsNullOrEmpty(themeId) && ByThemeId.TryGetValue(themeId, out var text) ? text : null;
    }
}
