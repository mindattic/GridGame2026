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
    public static class Operative
    {
        public static ActorData Data()
        {
            return new ActorData
            {
                CharacterName = "Operative",
                CharacterClass = CharacterClass.Operative, // If this enum does not exist, replace accordingly.
                Tags = Tag.Hero | Tag.Humanoid,

                Description = "A gadgeteer who leverages devices, traps, and tactical tools.",
                Expectations = "Utility controller. Uses [trap], [turret], and [gadget] effects.",
                Lore = "Blueprints and boldness turned problems into levers and switches.",
                Card = "Tactical [engineer][controller]. Deploys [traps][turrets][gadgets].",

                BaseStats = new ActorStats
                {
                    Level = 1,

                    Strength = 10f,
                    Vitality = 11f,
                    Agility = 12f,

                    Speed = 12f,

                    Stamina = 12f,
                    Intelligence = 16f,
                    Wisdom = 14f,
                    Luck = 12f
                },

                StatGrowth = new StatGrowth
                {
                    Strength = 0.55f,
                    Vitality = 0.60f,
                    Agility = 0.70f,

                    Speed = 0.70f,

                    Stamina = 0.70f,
                    Intelligence = 0.95f,
                    Wisdom = 0.85f,
                    Luck = 0.60f
                },

                MilestoneStatGrowth = new Dictionary<int, StatGrowth>
                {
                    { 5,  new StatGrowth { Strength = 0.19f, Vitality = 0.21f, Agility = 0.24f, Speed = 0.24f, Stamina = 0.24f, Intelligence = 0.33f, Wisdom = 0.3f, Luck = 0.21f } },
                    { 10, new StatGrowth { Strength = 0.25f, Vitality = 0.27f, Agility = 0.32f, Speed = 0.32f, Stamina = 0.32f, Intelligence = 0.43f, Wisdom = 0.38f, Luck = 0.27f } },
                    { 20, new StatGrowth { Strength = 0.33f, Vitality = 0.36f, Agility = 0.42f, Speed = 0.42f, Stamina = 0.42f, Intelligence = 0.57f, Wisdom = 0.51f, Luck = 0.36f } },
                    { 40, new StatGrowth { Strength = 0.44f, Vitality = 0.48f, Agility = 0.56f, Speed = 0.56f, Stamina = 0.56f, Intelligence = 0.76f, Wisdom = 0.68f, Luck = 0.48f } }
                },

                Stats = new ActorStats(),

                ThumbnailSettings = new ThumbnailSettings
                {
                    PixelPosition = new Vector2Int(512, 140),
                    Scale = new Vector3(1f, 1f, 0f)
                },

                CanvasThumbnailSettings = CanvasThumbnailSettings.SetDefault(),

                Portrait = AssetHelper.LoadAsset<Sprite>($"{CharacterClass.Operative}")
            };
        }
    }

}
