using Assets.Helpers;
using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Tag = ActorTag;

namespace Assets.Data.Actor
{
    public static class CyberZombie02
    {
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "CyberZombie02",
                CharacterClass = CharacterClass.CyberZombie02, // If this enum does not exist, replace accordingly.
                Tags = Tag.Mechanical | Tag.Undead,

                Description = "An unnatural entity that endures through grim resilience and dread.",
                Expectations = "Attrition fighter. Uses [drain], [fear], and [resist] to grind opponents down.",
                Lore = "Life ended. Purpose remained. The rest is hunger and memory.",
                Card = "Grim [attrition][drain]. Pressures with [fear] and [rot].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 14f,
                    Vitality = 15f,
                    Agility = 10f,

                    Speed = 10f,

                    Stamina = 16f,
                    Intelligence = 10f,
                    Wisdom = 12f,
                    Luck = 9f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.85f,
                    Vitality = 0.90f,
                    Agility = 0.50f,

                    Speed = 0.50f,

                    Stamina = 0.95f,
                    Intelligence = 0.55f,
                    Wisdom = 0.70f,
                    Luck = 0.40f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.3f, Vitality = 0.32f, Agility = 0.17f, Speed = 0.17f, Stamina = 0.33f, Intelligence = 0.19f, Wisdom = 0.24f, Luck = 0.14f } },
                    { 10, new StatGrowth { Strength = 0.38f, Vitality = 0.41f, Agility = 0.23f, Speed = 0.23f, Stamina = 0.43f, Intelligence = 0.25f, Wisdom = 0.32f, Luck = 0.18f } },
                    { 20, new StatGrowth { Strength = 0.51f, Vitality = 0.54f, Agility = 0.3f, Speed = 0.3f, Stamina = 0.57f, Intelligence = 0.33f, Wisdom = 0.42f, Luck = 0.24f } },
                    { 40, new StatGrowth { Strength = 0.68f, Vitality = 0.72f, Agility = 0.4f, Speed = 0.4f, Stamina = 0.76f, Intelligence = 0.44f, Wisdom = 0.56f, Luck = 0.32f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.CyberZombie02}")
            };
        }
    }

}
