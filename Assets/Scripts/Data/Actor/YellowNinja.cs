using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Tag = Scripts.Models.ActorTag;
using Scripts.Canvas;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Data.Actor
{
    public static class YellowNinja
    {
        /// <summary>Data.</summary>
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "YellowNinja",
                CharacterClass = CharacterClass.YellowNinja, // If this enum does not exist, replace accordingly.
                Tags = Tag.Hero | Tag.Humanoid,

                Description = "A swift combatant who wins through speed, angles, and precision strikes.",
                Expectations = "High mobility. Uses [flank], [crit], and [reposition]. Avoid extended trades.",
                Lore = "Years of quiet training forged a will that moves faster than thought.",
                Card = "Mobile [striker][flanker]. Leans on [crit] and [backstab] windows.",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 12f,
                    Vitality = 10f,
                    Agility = 18f,

                    Speed = 18f,

                    Stamina = 11f,
                    Intelligence = 12f,
                    Wisdom = 10f,
                    Luck = 14f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.70f,
                    Vitality = 0.55f,
                    Agility = 1.10f,

                    Speed = 1.05f,

                    Stamina = 0.65f,
                    Intelligence = 0.55f,
                    Wisdom = 0.50f,
                    Luck = 0.75f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.24f, Vitality = 0.19f, Agility = 0.39f, Speed = 0.37f, Stamina = 0.23f, Intelligence = 0.19f, Wisdom = 0.17f, Luck = 0.26f } },
                    { 10, new StatGrowth { Strength = 0.32f, Vitality = 0.25f, Agility = 0.5f, Speed = 0.47f, Stamina = 0.29f, Intelligence = 0.25f, Wisdom = 0.23f, Luck = 0.34f } },
                    { 20, new StatGrowth { Strength = 0.42f, Vitality = 0.33f, Agility = 0.66f, Speed = 0.63f, Stamina = 0.39f, Intelligence = 0.33f, Wisdom = 0.3f, Luck = 0.45f } },
                    { 40, new StatGrowth { Strength = 0.56f, Vitality = 0.44f, Agility = 0.88f, Speed = 0.84f, Stamina = 0.52f, Intelligence = 0.44f, Wisdom = 0.4f, Luck = 0.6f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.YellowNinja}")
            };
        }
    }

}
