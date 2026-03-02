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
    public static class RedMage
    {
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "RedMage",
                CharacterClass = CharacterClass.RedMage, // If this enum does not exist, replace accordingly.
                Tags = Tag.Hero | Tag.Humanoid | Tag.Magic,

                Description = "A ranged spellcaster that shapes the fight with arcane control and damage.",
                Expectations = "Midline control. Uses [debuff], [aoe], and [curse]. Keep safely behind the front.",
                Lore = "Disciplines of ink and flame taught that power begins in focus.",
                Card = "Arcane [controller][debuffer]. Shapes fights with [aoe][curse].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 7f,
                    Vitality = 8f,
                    Agility = 11f,

                    Speed = 12f,

                    Stamina = 10f,
                    Intelligence = 20f,
                    Wisdom = 18f,
                    Luck = 12f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.35f,
                    Vitality = 0.45f,
                    Agility = 0.55f,

                    Speed = 0.60f,

                    Stamina = 0.55f,
                    Intelligence = 1.20f,
                    Wisdom = 1.00f,
                    Luck = 0.50f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.12f, Vitality = 0.16f, Agility = 0.19f, Speed = 0.21f, Stamina = 0.19f, Intelligence = 0.42f, Wisdom = 0.35f, Luck = 0.17f } },
                    { 10, new StatGrowth { Strength = 0.16f, Vitality = 0.2f, Agility = 0.25f, Speed = 0.27f, Stamina = 0.25f, Intelligence = 0.54f, Wisdom = 0.45f, Luck = 0.23f } },
                    { 20, new StatGrowth { Strength = 0.21f, Vitality = 0.27f, Agility = 0.33f, Speed = 0.36f, Stamina = 0.33f, Intelligence = 0.72f, Wisdom = 0.6f, Luck = 0.3f } },
                    { 40, new StatGrowth { Strength = 0.28f, Vitality = 0.36f, Agility = 0.44f, Speed = 0.48f, Stamina = 0.44f, Intelligence = 0.96f, Wisdom = 0.8f, Luck = 0.4f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.RedMage}")
            };
        }
    }

}
