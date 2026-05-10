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
    /// Biome identifiers. Used both by the campaign (CampaignStages.Themes) and by parked
    /// Places/Bounty code from the legacy Hub. The first six values are the legacy set;
    /// GreenValley / Desert / Swamp / CityRuins were added in slice 9 for the themed campaign.
    /// </summary>
    public enum Biome
    {
        None = 0,
        Field,        // legacy — open grassland
        Forest,       // legacy — wooded
        Ruins,        // legacy — crumbling structures
        Cave,         // dark tunnels — cyclops, trolls, lurkers, yetis (used by both legacy + campaign)
        Boss,         // bespoke boss stages
        // Campaign themes added 2026-05-10. Each is a distinct theme spanning 3 campaign stages.
        GreenValley,  // tutorial pastures — slimes, wolves, bats
        Desert,       // arid raider country — scorpions, vultures, soldiers
        Swamp,        // marshy fens — lurkers, frogs, hags, marsh shamblers
        CityRuins,    // urban ruins — undead, ghosts, vampires, phantoms
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

                // ============================================================
                // CAMPAIGN — 5 themes × 3 stages, sawtooth difficulty curve.
                // Each new theme starts a notch easier than the previous theme's peak,
                // so the player can read new enemy compositions before the climb resumes.
                // Difficulty score = total enemy count across all waves.
                // ============================================================

                // ── Theme 1: Green Valley ─────────────────────────── (2 → 3 → 4 enemies)
                { "GreenValley-01", new Stage
                    {
                        Name = "GreenValley-01",
                        Description = "A quiet meadow — but slimes are gathering. Clear the field.",
                        Biome = Biome.GreenValley,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Slime00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Slime01, Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "GreenValley-02", new Stage
                    {
                        Name = "GreenValley-02",
                        Description = "Wolves circle the trail. Three of them, hungry.",
                        Biome = Biome.GreenValley,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Wolf00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Wolf01, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Bat00,  Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "GreenValley-03", new Stage
                    {
                        Name = "GreenValley-03",
                        Description = "Two waves at the woodland edge. Slimes flank, wolves close.",
                        Biome = Biome.GreenValley,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Slime02, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Slime03, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Wolf02, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Wolf03, Team = Team.Enemy },
                            }},
                        }
                    }
                },

                // ── Theme 2: Sandsea Reaches (Desert) ─────────────── DIP → 3 → 4 → 5
                { "Desert-01", new Stage
                    {
                        Name = "Desert-01",
                        Description = "Dunes ripple. Scorpions and a vulture wait at the well.",
                        Biome = Biome.Desert,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Scorpion, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Scorpion, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Vulture,  Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "Desert-02", new Stage
                    {
                        Name = "Desert-02",
                        Description = "Raider patrol. Two waves — scout, then the muscle.",
                        Biome = Biome.Desert,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Vulture,   Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Soldier00, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.SandMaw,   Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Soldier01, Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "Desert-03", new Stage
                    {
                        Name = "Desert-03",
                        Description = "A captain has rallied the dune-runners. Cut the snake's head.",
                        Biome = Biome.Desert,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Soldier02, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Soldier03, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Soldier00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Soldier01, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Captain,   Team = Team.Enemy },
                            }},
                        }
                    }
                },

                // ── Theme 3: Mireholt Fens (Swamp) ────────────────── DIP → 4 → 5 → 6
                { "Swamp-01", new Stage
                    {
                        Name = "Swamp-01",
                        Description = "Reeds rustle. Lurkers slide between bog islands.",
                        Biome = Biome.Swamp,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Lurker00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Frog00,   Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.MarshShambler00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Toad00,          Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "Swamp-02", new Stage
                    {
                        Name = "Swamp-02",
                        Description = "Hag laughter rises through the fog. Don't follow it.",
                        Biome = Biome.Swamp,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Lurker01,        Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.MarshShambler01, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Hag00,    Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Naga00,   Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Lurker02, Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "Swamp-03", new Stage
                    {
                        Name = "Swamp-03",
                        Description = "The mistress of the marsh stands among her servants. Three waves.",
                        Biome = Biome.Swamp,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Frog01, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Toad00, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Hag01,           Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.MarshShambler03, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 3, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.SwampMistress00, Level = 5, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Lurker02,        Team = Team.Enemy },
                            }},
                        }
                    }
                },

                // ── Theme 4: Frostmaw Caverns (Cave) ──────────────── DIP → 5 → 6 → 7
                { "Cave-01", new Stage
                    {
                        Name = "Cave-01",
                        Description = "Goblins in the entrance shaft. Cyclops ahead.",
                        Biome = Biome.Cave,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.GoblinThug00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.GoblinThug00, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Cyclops00,     Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.MountainTroll, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Skelepede00,   Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "Cave-02", new Stage
                    {
                        Name = "Cave-02",
                        Description = "Ice cracks underfoot. Yetis descend.",
                        Biome = Biome.Cave,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Cyclops01,    Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.GoblinThug00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Skelepede01,  Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Yeti,          Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.IceMauler,     Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.MountainTroll, Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "Cave-03", new Stage
                    {
                        Name = "Cave-03",
                        Description = "Three waves into the deeps. The mountain itself fights back.",
                        Biome = Biome.Cave,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.GoblinThug00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Skelepede00,  Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Cyclops02,    Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.MountainTroll, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 3, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Yeti,       Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.IceMauler,  Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Cyclops00,  Team = Team.Enemy },
                            }},
                        }
                    }
                },

                // ── Theme 5: Veshker Ruins (CityRuins) ─────────────── DIP → 6 → 7 → 8 (boss)
                { "CityRuins-01", new Stage
                    {
                        Name = "CityRuins-01",
                        Description = "Toppled walls, restless dead. Two waves through the plaza.",
                        Biome = Biome.CityRuins,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Undead00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Undead01, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Ghost,    Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Phantom,  Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Bat00,    Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Undead02, Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "CityRuins-02", new Stage
                    {
                        Name = "CityRuins-02",
                        Description = "A reaper has joined the dead. Three waves; do not fall.",
                        Biome = Biome.CityRuins,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Undead00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Undead01, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Ghost,   Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Phantom, Team = Team.Enemy },
                            }},
                            new StageWave { WaveID = 3, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Reaper,   Level = 6, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Undead04, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Bat01,    Team = Team.Enemy },
                            }},
                        }
                    }
                },
                { "CityRuins-03", new Stage
                    {
                        Name = "CityRuins-03",
                        Description = "The Vampire Lord holds the spire. End it.",
                        Biome = Biome.Boss,
                        CompletionCondition = "DefeatAllEnemies",
                        CompletionValue = 0,
                        Waves = new List<StageWave>
                        {
                            // Warmup — restless dead.
                            new StageWave { WaveID = 1, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Undead00, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Undead01, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Ghost,    Team = Team.Enemy },
                            }},
                            // Mid-fight — the crypt swarms.
                            new StageWave { WaveID = 2, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Bat00,        Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Bat01,        Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Phantom,     Team = Team.Enemy },
                            }},
                            // Final wave — the Vampire Lord with two escorts.
                            new StageWave { WaveID = 3, Actors = new List<StageActor>
                            {
                                new StageActor { CharacterClass = CharacterClass.Vampire,  Level = 10, Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Reaper,   Level = 6,  Team = Team.Enemy },
                                new StageActor { CharacterClass = CharacterClass.Undead04, Team = Team.Enemy },
                            }},
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
