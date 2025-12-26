using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Helpers;
using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public static class EndlessWaveGenerator
    {
        // Config
        private const int BaseBudget = 30;          // initial points
        private const int BudgetPerWave = 12;       // growth per wave
        private const int MinInitialSpawns = 2;     // immediate enemies at wave start
        private const int MaxInitialSpawns = 4;
        private const int TrickleEveryTurns = 3;    // how many turns between trickle spawns
        private const int TrickleBatchMin = 1;
        private const int TrickleBatchMax = 2;

        public static StageWave Generate(int waveNumber, ActorTag tags)
        {
            int budget = BaseBudget + (waveNumber - 1) * BudgetPerWave;
            int enemyLevel = 1 + Mathf.FloorToInt((waveNumber - 1) * 0.5f); // scale slowly

            var candidates = GetCandidatesByTags(tags);
            var picked = PickByBudget(candidates, enemyLevel, budget);

            var wave = new StageWave
            {
                WaveID = waveNumber,
                Actors = new List<StageActor>(),
                DottedLines = new List<StageDottedLine>()
            };

            int initialCount = Mathf.Clamp(picked.Count / 2, MinInitialSpawns, MaxInitialSpawns);

            for (int i = 0; i < picked.Count; i++)
            {
                var characterClass = picked[i];
                int spawnTurn = i < initialCount
                    ? 0
                    : ((i - initialCount) / Mathf.Max(1, (TrickleBatchMin + TrickleBatchMax) / 2) + 1) * TrickleEveryTurns;

                wave.Actors.Add(new StageActor
                {
                    CharacterClass = characterClass,
                    Team = Team.Enemy,
                    Level = enemyLevel,
                    SpawnTurn = spawnTurn,
                });
            }

            return wave;
        }

        private static List<CharacterClass> GetCandidatesByTags(ActorTag tags)
        {
            if (tags == ActorTag.None)
            {
                return ActorLibrary.Actors.Keys.ToList();
            }

            var result = new List<CharacterClass>();
            foreach (var kv in ActorLibrary.Actors)
            {
                var data = kv.Value;
                if (data == null) continue;

                if ((data.Tags & tags) != ActorTag.None)
                {
                    result.Add(kv.Key);
                }
            }

            if (result.Count == 0)
                result = ActorLibrary.Actors.Keys.ToList();

            return result;
        }

        private static List<CharacterClass> PickByBudget(List<CharacterClass> candidates, int level, int budget)
        {
            var scored = candidates
                .Select(id => (id, score: Mathf.Max(1, ScoreFor(id, level))))
                .OrderBy(s => s.score)
                .ToList();

            var result = new List<CharacterClass>();
            int remaining = budget;

            while (remaining > 0 && result.Count < 32 && scored.Count > 0)
            {
                int window = Mathf.Max(1, scored.Count / 3);
                int index = RNG.Int(0, window - 1);
                var pick = scored[index];
                if (pick.score <= remaining)
                {
                    result.Add(pick.id);
                    remaining -= pick.score;
                }
                else
                {
                    var cheaper = scored.FirstOrDefault(s => s.score <= remaining);
                    if (cheaper.id != null)
                    {
                        result.Add(cheaper.id);
                        remaining -= cheaper.score;
                    }
                    else
                    {
                        break;
                    }
                }

                scored.RemoveAt(index);
            }

            if (result.Count == 0)
            {
                var pick = scored.FirstOrDefault();
                if (pick.id != null) result.Add(pick.id);
            }

            return result;
        }

        private static int ScoreFor(CharacterClass characterClass, int level)
        {
            var data = ActorLibrary.Get(characterClass);
            if (data == null) return 1;
            var stats = data.GetStats(level);

            float physPower = Formulas.Offense(stats, 0f) + Formulas.Defense(stats, 0f);
            float magicPower = Formulas.MagicOffense(stats) + Formulas.MagicResistance(stats);
            float hpScore = Mathf.Max(stats.MaxHP, stats.HP);
            float levelScore = Mathf.Max(1f, stats.Level);

            float powerScore =
                physPower * 0.06f +
                magicPower * 0.05f +
                hpScore * 0.02f +
                levelScore * 1.5f;

            int score = Mathf.Max(1, Mathf.RoundToInt(powerScore + data.BonusXP * 0.5f));
            return score;
        }
    }
}
