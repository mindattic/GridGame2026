using System.Collections.Generic;
using Scripts.Helpers;

namespace Scripts.Libraries
{
    /// <summary>
    /// CAMPAIGNTHEME - Ordered group of campaign stages sharing a biome / aesthetic.
    /// </summary>
    public class CampaignTheme
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public Biome Biome;
        public IReadOnlyList<string> StageNames;
    }

    /// <summary>
    /// CAMPAIGNSTAGES - Themed campaign progression. Five themes × three stages each.
    /// <para>STRUCTURE: Each theme groups stages with a shared biome and tightening
    /// difficulty curve. Stage 1 of a new theme is intentionally a notch easier than
    /// stage 3 of the previous theme — a transition dip — so the player can read the
    /// new biome's enemy compositions before facing peak intensity.</para>
    /// <para>UNLOCK GATING: <see cref="Order"/> is the flattened global stage list.
    /// Stage at global index N becomes selectable when the save's
    /// <see cref="Scripts.Models.StageSaveData.HighestClearedStageIndex"/> ≥ N - 1.</para>
    /// <para>RELATED FILES: StageSelectManager.cs, StageLibrary.cs, EnemyBiomeMap.cs, StageManager.cs</para>
    /// </summary>
    public static class CampaignStages
    {
        public static readonly IReadOnlyList<CampaignTheme> Themes = new List<CampaignTheme>
        {
            new CampaignTheme
            {
                Id = "GreenValley",
                DisplayName = "Green Valley",
                Description = "Pastures and the woodland edge. Small game, light packs.",
                Biome = Biome.GreenValley,
                StageNames = new List<string> { "GreenValley-01", "GreenValley-02", "GreenValley-03" },
            },
            new CampaignTheme
            {
                Id = "Desert",
                DisplayName = "Sandsea Reaches",
                Description = "Arid raider country. Watch the dunes for movement.",
                Biome = Biome.Desert,
                StageNames = new List<string> { "Desert-01", "Desert-02", "Desert-03" },
            },
            new CampaignTheme
            {
                Id = "Swamp",
                DisplayName = "Mireholt Fens",
                Description = "Knee-deep marsh. Lurkers in the reeds, hags in the mist.",
                Biome = Biome.Swamp,
                StageNames = new List<string> { "Swamp-01", "Swamp-02", "Swamp-03" },
            },
            new CampaignTheme
            {
                Id = "Cave",
                DisplayName = "Frostmaw Caverns",
                Description = "Ice-cracked stone. Heavy hitters in the dark.",
                Biome = Biome.Cave,
                StageNames = new List<string> { "Cave-01", "Cave-02", "Cave-03" },
            },
            new CampaignTheme
            {
                Id = "CityRuins",
                DisplayName = "Veshker Ruins",
                Description = "A drowned city, restless. The Vampire Lord holds the spire.",
                Biome = Biome.CityRuins,
                StageNames = new List<string> { "CityRuins-01", "CityRuins-02", "CityRuins-03" },
            },
        };

        private static IReadOnlyList<string> _order;

        /// <summary>Flattened stage names across every theme, in campaign order.</summary>
        public static IReadOnlyList<string> Order
        {
            get
            {
                if (_order != null) return _order;
                var list = new List<string>();
                foreach (var theme in Themes)
                    foreach (var stage in theme.StageNames)
                        list.Add(stage);
                _order = list;
                return _order;
            }
        }

        /// <summary>Returns the (theme, indexInTheme) coordinates for the given global stage index.</summary>
        public static (CampaignTheme theme, int indexInTheme) Locate(int globalIndex)
        {
            int seen = 0;
            foreach (var theme in Themes)
            {
                if (globalIndex < seen + theme.StageNames.Count)
                    return (theme, globalIndex - seen);
                seen += theme.StageNames.Count;
            }
            return (null, -1);
        }

        /// <summary>Player-facing label for the stage at the given global index, e.g. "Stage 1-2".</summary>
        public static string LabelFor(int globalIndex)
        {
            var (theme, indexInTheme) = Locate(globalIndex);
            if (theme == null) return "?";
            int themeIndex = Themes.IndexOf(theme) + 1;
            return $"Stage {themeIndex}-{indexInTheme + 1}";
        }

        /// <summary>Returns the campaign index of the given stage name, or -1 if not in the campaign.</summary>
        public static int IndexOf(string stageName)
        {
            for (int i = 0; i < Order.Count; i++)
                if (Order[i] == stageName) return i;
            return -1;
        }

        /// <summary>True when the stage at <paramref name="globalIndex"/> is selectable in StageSelect.</summary>
        public static bool IsUnlocked(int globalIndex, int highestClearedStageIndex)
        {
            if (globalIndex <= 0) return true;
            return highestClearedStageIndex >= globalIndex - 1;
        }

        /// <summary>US-135: the campaign difficulty curve — recommended enemy (and party) level
        /// for a stage: stage 1 → level 1 … stage 15 → level 15. Non-campaign stages
        /// (Test-*, Endless) return 1 (their authored levels stand alone). Enemy spawns floor
        /// to this (StageManager.SpawnActor); StageSelect shows it on the detail panel.</summary>
        public static int RecommendedLevel(string stageName)
        {
            int index = IndexOf(stageName);
            return index < 0 ? 1 : index + 1;
        }

        /// <summary>Updates the save's HighestClearedStageIndex if the cleared stage is higher than current.
        /// Call from StageManager when victory is detected.</summary>
        public static void MarkCleared(int globalIndex)
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return;
            if (globalIndex < 0 || globalIndex >= Order.Count) return;
            if (globalIndex > save.Stage.HighestClearedStageIndex)
            {
                save.Stage.HighestClearedStageIndex = globalIndex;
                ProfileHelper.Save(overwrite: true);
            }
        }

        /// <summary>Convenience overload — looks up the stage's campaign index and marks it cleared.</summary>
        public static void MarkCleared(string stageName) => MarkCleared(IndexOf(stageName));

        private static int IndexOf(this IReadOnlyList<CampaignTheme> themes, CampaignTheme target)
        {
            for (int i = 0; i < themes.Count; i++)
                if (themes[i] == target) return i;
            return -1;
        }
    }
}
