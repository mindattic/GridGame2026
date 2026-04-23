using Scripts.Helpers;
using Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
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

namespace Scripts.Libraries
{
    /// <summary>
    /// Map identifiers for stage grouping.
    /// </summary>
    public enum Map
    {
        Test,
        GreenValley,
    }

    /// <summary>
    /// Biome identifiers for the Places hub selection.
    /// Each biome hunts a themed enemy pool that drops biome-specific materials.
    /// </summary>
    public enum Biome
    {
        None = 0,
        Field,   // open grassland — slimes, frogs, scorpions
        Forest,  // wooded — wolves, werewolves, tree golems
        Ruins,   // crumbling structures — undead, ceramic knights, ghosts
        Cave,    // dark tunnels — cyclops, trolls, lurkers, yetis
        Boss,    // bespoke boss stages
    }

    /// <summary>
    /// STAGELIBRARY - Registry of all game stages/levels.
    /// 
    /// PURPOSE:
    /// Defines all stages with their enemy waves, completion
    /// conditions, and difficulty scaling.
    /// 
    /// STAGE STRUCTURE:
    /// - Name: "MapName-##" format
    /// - Waves: List of enemy spawns per wave
    /// - CompletionCondition: Win condition type
    /// 
    /// USAGE:
    /// ```csharp
    /// var stage = StageLibrary.Stages["GreenValley-01"];
    /// ```
    /// 
    /// RELATED FILES:
    /// - Stage.cs: Stage data structure
    /// - StageManager.cs: Stage execution
    /// - StageSelectManager.cs: Stage selection UI
    /// </summary>
    public static class StageLibrary
    {
        private static Dictionary<string, Stage> stages;
        private static bool isLoaded = false;

        public static Dictionary<string, Stage> Stages
        {
            get
            {
                if (!isLoaded)
                    Load();
                return stages;
            }
        }

        /// <summary>Load.</summary>
        private static void Load()
        {
            if (isLoaded) return;

            stages = new Dictionary<string, Stage>
            {

                { $"{Map.GreenValley}-00", new Stage
                    {
                        Name = $"{Map.GreenValley}-00",
                        Description = "Clear the grassland of slimes.",
                        Biome = Biome.Field,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = GenerateWaves(1, new List<CharacterClass> {
                            CharacterClass.Slime00,
                            CharacterClass.Slime01,
                            CharacterClass.Slime02,
                            CharacterClass.Slime03,
                        })
                    }
                },
                { $"{Map.GreenValley}-01", new Stage
                    {
                        Name = $"{Map.GreenValley}-01",
                        Description = "Hunt the wolf pack stalking the tree line.",
                        Biome = Biome.Forest,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = GenerateWaves(1, new List<CharacterClass> {
                            CharacterClass.Wolf00,
                            CharacterClass.Wolf01,
                            CharacterClass.Wolf02,
                            CharacterClass.Wolf03,
                        })
                    }
                },
                { $"{Map.GreenValley}-02", new Stage
                    {
                        Name = $"{Map.GreenValley}-02",
                        Description = "Something stirs among the broken stones.",
                        Biome = Biome.Ruins,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = GenerateWaves(1, new List<CharacterClass> {
                            CharacterClass.Undead00,
                            CharacterClass.Undead01,
                            CharacterClass.Undead02,
                            CharacterClass.Skelepede00,
                            CharacterClass.Ghost,
                        })
                    }
                },
                { $"{Map.GreenValley}-03", new Stage
                    {
                        Name = $"{Map.GreenValley}-03",
                        Description = "The cave mouth yawns open. Torches lit.",
                        Biome = Biome.Cave,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = GenerateWaves(1, new List<CharacterClass> {
                            CharacterClass.Lurker00,
                            CharacterClass.Cyclops00,
                            CharacterClass.MountainTroll,
                            CharacterClass.Yeti,
                        })
                    }
                },
                { $"{Map.GreenValley}-Boss", new Stage
                    {
                        Name = $"{Map.GreenValley}-Boss",
                        Description = "The Vampire Lord awaits in the deepest crypt.",
                        Biome = Biome.Boss,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave
                            {
                                WaveID = 1,
                                Actors = new List<StageActor>
                                {
                                    new StageActor { CharacterClass = CharacterClass.Undead00, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Undead01, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Ghost, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Vampire, Level = 10, Team = Team.Enemy },
                                }
                            }
                        }
                    }
                },
                { $"{Map.Test}-00", new Stage
                    {
                        Name = $"{Map.Test}-00",
                        Description = "Intro Battle",
                        Biome = Biome.Field,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave
                            {
                                Actors = new List<StageActor>
                                {
                                    new StageActor { CharacterClass = CharacterClass.Soldier00, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier01, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier02, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier03, Team = Team.Enemy }
                                }
                            },
                            new StageWave
                            {
                                Actors = new List<StageActor>
                                {
                                    new StageActor { CharacterClass = CharacterClass.Soldier00, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier01, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier02, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier03, Team = Team.Enemy }
                                }
                            },
                            new StageWave
                            {
                                Actors = new List<StageActor>
                                {
                                    new StageActor { CharacterClass = CharacterClass.Soldier00, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier01, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier02, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Soldier03, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Captain, Team = Team.Enemy },
                                }
                            },
                            new StageWave
                            {
                                Actors = new List<StageActor>
                                {
                                    new StageActor { CharacterClass = CharacterClass.Slime00, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Slime01, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Slime02, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Slime03, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Slime00, Team = Team.Enemy, SpawnTurn = 4 },
                                    new StageActor { CharacterClass = CharacterClass.Slime01, Team = Team.Enemy, SpawnTurn = 4 },
                                    new StageActor { CharacterClass = CharacterClass.Slime02, Team = Team.Enemy, SpawnTurn = 4 },
                                    new StageActor { CharacterClass = CharacterClass.Slime03, Team = Team.Enemy, SpawnTurn = 4 },
                                    new StageActor { CharacterClass = CharacterClass.Slime00, Team = Team.Enemy, SpawnTurn = 8 },
                                    new StageActor { CharacterClass = CharacterClass.Slime01, Team = Team.Enemy, SpawnTurn = 8 },
                                    new StageActor { CharacterClass = CharacterClass.Slime02, Team = Team.Enemy, SpawnTurn = 10 },
                                    new StageActor { CharacterClass = CharacterClass.Slime03, Team = Team.Enemy, SpawnTurn = 10 },
                                    new StageActor { CharacterClass = CharacterClass.Slime00, Team = Team.Enemy, SpawnTurn = 12 },
                                    new StageActor { CharacterClass = CharacterClass.Slime01, Team = Team.Enemy, SpawnTurn = 12 },
                                    new StageActor { CharacterClass = CharacterClass.Slime02, Team = Team.Enemy, SpawnTurn = 14 },
                                    new StageActor { CharacterClass = CharacterClass.Slime03, Team = Team.Enemy, SpawnTurn = 14 },
                                    new StageActor { CharacterClass = CharacterClass.Scorpion, Level = 10, Team = Team.Enemy, SpawnTurn = 16 },

                                }
                            },
                            new StageWave
                            {
                                Actors = new List<StageActor>
                                {
                                    new StageActor { CharacterClass = CharacterClass.Yeti, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Scorpion, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Captain, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Bat00, Team = Team.Enemy },
                                    new StageActor { CharacterClass = CharacterClass.Bat01, Team = Team.Enemy },
                                }
                            },


                        }
                    }
                },
                { $"{Map.Test}-01", new Stage
                    {
                        Name = $"{Map.Test}-01",
                        Description = "DefeatAllEnemies",
                        Biome = Biome.Field,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = GenerateWaves(4, new List<CharacterClass> { CharacterClass.Slime00, CharacterClass.Scorpion, CharacterClass.Bat00 })
                    }
                },
            };

            isLoaded = true;
        }

        /// <summary>Get.</summary>
        public static Stage Get(string name)
        {
            if (!isLoaded) Load();
            if (!stages.ContainsKey(name))
            {
                Debug.LogError($"Unable to retrieve stage for `{name}`");
                return null;
            }
            return new Stage(stages[name]);
        }

        /// <summary>Returns all stages tagged with the given biome (fresh copies).</summary>
        public static List<Stage> GetByBiome(Biome biome)
        {
            if (!isLoaded) Load();
            var list = new List<Stage>();
            foreach (var kv in stages)
                if (kv.Value.Biome == biome)
                    list.Add(new Stage(kv.Value));
            return list;
        }

        /// <summary>Returns the first stage tagged with the given biome, or null.</summary>
        public static Stage GetFirstByBiome(Biome biome)
        {
            if (!isLoaded) Load();
            foreach (var kv in stages)
                if (kv.Value.Biome == biome)
                    return new Stage(kv.Value);
            return null;
        }

        /// <summary>Generate waves.</summary>
        private static List<StageWave> GenerateWaves(int waveCount, List<CharacterClass> possibleEnemies)
        {
            List<StageWave> waves = new List<StageWave>();
            System.Random rng = new System.Random();

            for (int i = 0; i < waveCount; i++)
            {
                StageWave wave = new StageWave
                {
                    WaveID = i + 1,
                    Actors = new List<StageActor>(),
                    DottedLines = new List<StageDottedLine>()
                };

                int enemyCount = rng.Next(2, 6);
                for (int j = 0; j < enemyCount; j++)
                {
                    CharacterClass randomEnemy = possibleEnemies[rng.Next(possibleEnemies.Count)];
                    wave.Actors.Add(new StageActor
                    {
                        CharacterClass = randomEnemy,
                        Team = Team.Enemy
                    });
                }

                waves.Add(wave);
            }

            return waves;
        }
    }
}
