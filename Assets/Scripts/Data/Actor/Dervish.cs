using Assets.Helpers;
using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Tag = ActorTag;

namespace Assets.Data.Actor
{
    public static class Dervish
    {
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "Dervish",
                CharacterClass = CharacterClass.Dervish, // If this enum does not exist, replace accordingly.
                Tags = Tag.Hero | Tag.Humanoid,

                Description = "A battlefield support that sustains allies and manipulates tempo.",
                Expectations = "Backline support. Uses [heal], [cleanse], and [buff] to stabilize fights.",
                Lore = "Knows every reagent, every breath, and when to spend both.",
                Card = "Backline [healer][buffer]. Restores tempo with [cleanse] and [aid].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 10f,
                    Vitality = 11f,
                    Agility = 11f,

                    Speed = 12f,

                    Stamina = 12f,
                    Intelligence = 16f,
                    Wisdom = 15f,
                    Luck = 12f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.55f,
                    Vitality = 0.60f,
                    Agility = 0.60f,

                    Speed = 0.65f,

                    Stamina = 0.65f,
                    Intelligence = 0.95f,
                    Wisdom = 0.90f,
                    Luck = 0.60f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.19f, Vitality = 0.21f, Agility = 0.21f, Speed = 0.23f, Stamina = 0.23f, Intelligence = 0.33f, Wisdom = 0.32f, Luck = 0.21f } },
                    { 10, new StatGrowth { Strength = 0.25f, Vitality = 0.27f, Agility = 0.27f, Speed = 0.29f, Stamina = 0.29f, Intelligence = 0.43f, Wisdom = 0.41f, Luck = 0.27f } },
                    { 20, new StatGrowth { Strength = 0.33f, Vitality = 0.36f, Agility = 0.36f, Speed = 0.39f, Stamina = 0.39f, Intelligence = 0.57f, Wisdom = 0.54f, Luck = 0.36f } },
                    { 40, new StatGrowth { Strength = 0.44f, Vitality = 0.48f, Agility = 0.48f, Speed = 0.52f, Stamina = 0.52f, Intelligence = 0.76f, Wisdom = 0.72f, Luck = 0.48f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.Dervish}")
            };
        }
    }

}
