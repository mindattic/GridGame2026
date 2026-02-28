using Assets.Helpers;
using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Tag = ActorTag;

namespace Assets.Data.Actor
{
    public static class CeramicKnight02
    {
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "Ceramicnight02",
                CharacterClass = CharacterClass.CeramicKnight02, // If this enum does not exist, replace accordingly.
                Tags = Tag.Enemy | Tag.Humanoid | Tag.Construct,

                Description = "A stalwart defender that anchors the line and absorbs pressure.",
                Expectations = "Frontline anchor. Uses [guard], [taunt], and [disrupt]. Protects allies.",
                Lore = "Built on vows and iron patience, they stand where others fall back.",
                Card = "Frontline [guardian][bruiser]. Absorbs pressure and [taunts].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 17f,
                    Vitality = 18f,
                    Agility = 10f,

                    Speed = 11f,

                    Stamina = 18f,
                    Intelligence = 8f,
                    Wisdom = 9f,
                    Luck = 11f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 1.05f,
                    Vitality = 1.00f,
                    Agility = 0.55f,

                    Speed = 0.55f,

                    Stamina = 1.00f,
                    Intelligence = 0.40f,
                    Wisdom = 0.45f,
                    Luck = 0.45f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.37f, Vitality = 0.35f, Agility = 0.19f, Speed = 0.19f, Stamina = 0.35f, Intelligence = 0.14f, Wisdom = 0.16f, Luck = 0.16f } },
                    { 10, new StatGrowth { Strength = 0.47f, Vitality = 0.45f, Agility = 0.25f, Speed = 0.25f, Stamina = 0.45f, Intelligence = 0.18f, Wisdom = 0.2f, Luck = 0.2f } },
                    { 20, new StatGrowth { Strength = 0.63f, Vitality = 0.6f, Agility = 0.33f, Speed = 0.33f, Stamina = 0.6f, Intelligence = 0.24f, Wisdom = 0.27f, Luck = 0.27f } },
                    { 40, new StatGrowth { Strength = 0.84f, Vitality = 0.8f, Agility = 0.44f, Speed = 0.44f, Stamina = 0.8f, Intelligence = 0.32f, Wisdom = 0.36f, Luck = 0.36f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.CeramicKnight02}")
            };
        }
    }

}
