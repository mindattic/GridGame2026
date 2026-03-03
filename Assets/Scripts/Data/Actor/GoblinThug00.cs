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
    public static class GoblinThug00
    {
        /// <summary>Data.</summary>
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "GoblinThug00",
                CharacterClass = CharacterClass.GoblinThug00, // If this enum does not exist, replace accordingly.
                Tags = Tag.Hero | Tag.Humanoid,

                Description = "A flexible fighter with no single defining specialty.",
                Expectations = "Adaptable combatant. Uses [combo] tools across multiple ranges.",
                Lore = "Walks many roads, never lost for lack of a single path.",
                Card = "Flexible [fighter]. Converts [openings] into [combo finishes].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 12f,
                    Vitality = 12f,
                    Agility = 12f,

                    Speed = 12f,

                    Stamina = 12f,
                    Intelligence = 12f,
                    Wisdom = 12f,
                    Luck = 12f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.70f,
                    Vitality = 0.70f,
                    Agility = 0.70f,

                    Speed = 0.70f,

                    Stamina = 0.70f,
                    Intelligence = 0.70f,
                    Wisdom = 0.70f,
                    Luck = 0.70f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.24f, Vitality = 0.24f, Agility = 0.24f, Speed = 0.24f, Stamina = 0.24f, Intelligence = 0.24f, Wisdom = 0.24f, Luck = 0.24f } },
                    { 10, new StatGrowth { Strength = 0.32f, Vitality = 0.32f, Agility = 0.32f, Speed = 0.32f, Stamina = 0.32f, Intelligence = 0.32f, Wisdom = 0.32f, Luck = 0.32f } },
                    { 20, new StatGrowth { Strength = 0.42f, Vitality = 0.42f, Agility = 0.42f, Speed = 0.42f, Stamina = 0.42f, Intelligence = 0.42f, Wisdom = 0.42f, Luck = 0.42f } },
                    { 40, new StatGrowth { Strength = 0.56f, Vitality = 0.56f, Agility = 0.56f, Speed = 0.56f, Stamina = 0.56f, Intelligence = 0.56f, Wisdom = 0.56f, Luck = 0.56f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.GoblinThug00}")
            };
        }
    }

}
