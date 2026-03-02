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

        private static void Load()
        {
            if (isLoaded) return;

            stages = new Dictionary<string, Stage>
            {

                { $"{Map.GreenValley}-00", new Stage
                    {
                        Name = $"{Map.GreenValley}-00",
                        Description = "DefeatAllEnemies",
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
                        Description = "DefeatAllEnemies",
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
                { $"{Map.Test}-00", new Stage
                    {
                        Name = $"{Map.Test}-00",
                        Description = "Intro Battle",
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
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = GenerateWaves(4, new List<CharacterClass> { CharacterClass.Slime00, CharacterClass.Scorpion, CharacterClass.Bat00 })
                    }
                },
            };

            isLoaded = true;
        }

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
