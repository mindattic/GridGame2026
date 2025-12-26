using Assets.Helpers;
using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Tag = ActorTag;

namespace Assets.Data.Actor
{
    public static class Ronin
    {
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "Ronin",
                CharacterClass = CharacterClass.Ronin, // If this enum does not exist, replace accordingly.
                Tags = Tag.Hero | Tag.Humanoid,

                Description = "A close-quarters specialist that thrives in direct engagements.",
                Expectations = "Brawler. Uses [cleave], [stagger], and [execute]. Press close advantage.",
                Lore = "Every scar is a lesson. Every lesson becomes an opening.",
                Card = "Close-range [brawler][cleaver]. Wins trades with [stagger].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 16f,
                    Vitality = 14f,
                    Agility = 13f,

                    Speed = 13f,

                    Stamina = 15f,
                    Intelligence = 9f,
                    Wisdom = 9f,
                    Luck = 11f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.95f,
                    Vitality = 0.80f,
                    Agility = 0.70f,

                    Speed = 0.70f,

                    Stamina = 0.85f,
                    Intelligence = 0.45f,
                    Wisdom = 0.45f,
                    Luck = 0.50f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.33f, Vitality = 0.28f, Agility = 0.24f, Speed = 0.24f, Stamina = 0.3f, Intelligence = 0.16f, Wisdom = 0.16f, Luck = 0.17f } },
                    { 10, new StatGrowth { Strength = 0.43f, Vitality = 0.36f, Agility = 0.32f, Speed = 0.32f, Stamina = 0.38f, Intelligence = 0.2f, Wisdom = 0.2f, Luck = 0.23f } },
                    { 20, new StatGrowth { Strength = 0.57f, Vitality = 0.48f, Agility = 0.42f, Speed = 0.42f, Stamina = 0.51f, Intelligence = 0.27f, Wisdom = 0.27f, Luck = 0.3f } },
                    { 40, new StatGrowth { Strength = 0.76f, Vitality = 0.64f, Agility = 0.56f, Speed = 0.56f, Stamina = 0.68f, Intelligence = 0.36f, Wisdom = 0.36f, Luck = 0.4f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.Ronin}")
            };
        }
    }

}
