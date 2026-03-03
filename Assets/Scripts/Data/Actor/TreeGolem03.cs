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
    public static class TreeGolem03
    {

        /// <summary>Data.</summary>
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "TreeGolem03",
                CharacterClass = CharacterClass.TreeGolem03, // If this enum does not exist, replace accordingly.
                Tags = Tag.PlantBased | Tag.Mechanical,

                Description = "A feral threat that overwhelms foes with instinct and power.",
                Expectations = "Pouncer. Uses [maul], [pounce], and [roar] to break formations.",
                Lore = "Born to the deep places, it runs by instinct and strikes without doubt.",
                Card = "Ferocious [pouncer][mauler]. Disrupts with [roar] and [knockdown].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 15f,
                    Vitality = 12f,
                    Agility = 15f,

                    Speed = 14f,

                    Stamina = 14f,
                    Intelligence = 8f,
                    Wisdom = 8f,
                    Luck = 11f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.95f,
                    Vitality = 0.75f,
                    Agility = 0.90f,

                    Speed = 0.85f,

                    Stamina = 0.85f,
                    Intelligence = 0.40f,
                    Wisdom = 0.40f,
                    Luck = 0.55f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.33f, Vitality = 0.26f, Agility = 0.32f, Speed = 0.3f, Stamina = 0.3f, Intelligence = 0.14f, Wisdom = 0.14f, Luck = 0.19f } },
                    { 10, new StatGrowth { Strength = 0.43f, Vitality = 0.34f, Agility = 0.41f, Speed = 0.38f, Stamina = 0.38f, Intelligence = 0.18f, Wisdom = 0.18f, Luck = 0.25f } },
                    { 20, new StatGrowth { Strength = 0.57f, Vitality = 0.45f, Agility = 0.54f, Speed = 0.51f, Stamina = 0.51f, Intelligence = 0.24f, Wisdom = 0.24f, Luck = 0.33f } },
                    { 40, new StatGrowth { Strength = 0.76f, Vitality = 0.6f, Agility = 0.72f, Speed = 0.68f, Stamina = 0.68f, Intelligence = 0.32f, Wisdom = 0.32f, Luck = 0.44f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.TreeGolem03}")
            };
        }
    }

}
