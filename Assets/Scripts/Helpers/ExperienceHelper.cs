using Assets.Scripts.Libraries;
using Assets.Scripts.Managers;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Helpers
{
    /// <summary>
    /// EXPERIENCEHELPER - XP calculation and distribution.
    /// 
    /// PURPOSE:
    /// Calculates XP rewards for defeating enemies and
    /// handles XP gain with level-up processing.
    /// 
    /// XP FORMULA:
    /// Based on defeated enemy's stats:
    /// - Physical power (offense + defense)
    /// - Magic power (magic offense + resistance)
    /// - HP score
    /// - Level score
    /// - Bonus XP from ActorData
    /// 
    /// LEVEL THRESHOLDS:
    /// NextLevel(level) = 50 + (level²) * 10
    /// 
    /// USAGE:
    /// ```csharp
    /// int xp = ExperienceHelper.Calculate(defeatedEnemy);
    /// ExperienceHelper.Gain(hero, xp);
    /// ```
    /// 
    /// RELATED FILES:
    /// - ExperienceTracker.cs: Tracks pending XP
    /// - PostBattleManager.cs: Awards XP after battle
    /// - LevelHelper.cs: Level-up stat growth
    /// </summary>
    public static class ExperienceHelper
    {
        /// <summary>XP required to reach next level.</summary>
        public static int NextLevel(int level)
        {
            level = Mathf.Max(1, level);
            return 50 + (level * level) * 10;
        }

        /// <summary>Calculate XP reward for defeating an actor.</summary>
        public static int Calculate(ActorInstance defeated)
        {
            if (defeated == null || defeated.characterClass == CharacterClass.None)
                return 0;

            var actorData = ActorLibrary.Get(defeated.characterClass);
            if (actorData == null)
                return 0;

            var stats = defeated.Stats;

            float physPower = Formulas.Offense(stats, 0f) + Formulas.Defense(stats, 0f);
            float magicPower = Formulas.MagicOffense(stats) + Formulas.MagicResistance(stats);
            float hpScore = Mathf.Max(stats.MaxHP, stats.HP);
            float levelScore = Mathf.Max(1f, stats.Level);

            float powerScore =
                physPower * 0.10f +
                magicPower * 0.08f +
                hpScore * 0.05f +
                levelScore * 2.0f;

            int reward = Mathf.Max(1, Mathf.RoundToInt(powerScore + actorData.BonusXP));
            return reward;
        }

        /// <summary>Add XP to an actor and handle level-ups.</summary>
        public static void Gain(ActorInstance actor, int amount)
        {
            if (actor == null || amount <= 0) return;

            actor.Stats.TotalXP = Mathf.Max(0, actor.Stats.TotalXP + amount);

            // Derive target level and currentXP from TotalXP and rebuild stats accordingly.
            int prevLevel = Mathf.Max(1, actor.Stats.Level);
            int prevCur = Mathf.Max(0, actor.Stats.CurrentXP);
            var (level, currentXP) = DeriveFromTotalXP(actor.Stats.TotalXP);

            // Apply any level ups by rebuilding stats at the new level
            if (level != prevLevel)
            {
                var data = ActorLibrary.Get(actor.characterClass);
                if (data != null)
                {
                    var next = data.GetStats(level);
                    actor.Stats = new ActorStats(next)
                    {
                        CurrentXP = currentXP,
                        TotalXP = actor.Stats.TotalXP,
                        HP = next.MaxHP,
                        PreviousHP = next.MaxHP
                    };

                    actor.HealthBar.Update();

                    g.CombatTextManager?.Spawn("Level Up!", actor.Position, "Heal");
                    if (VisualEffectLibrary.VisualEffects.TryGetValue("LevelUp", out var vfx))
                        g.VisualEffectManager?.Spawn(vfx, actor.Position);
                }
                else
                {
                    // Fallback if no data -- still set derived level/xp
                    actor.Stats.Level = level;
                    actor.Stats.CurrentXP = currentXP;
                }
            }
            else
            {
                // Same level, just update the CurrentXP field to reflect TotalXP remainder
                actor.Stats.Level = level;
                actor.Stats.CurrentXP = currentXP;
            }

            // Persist hero progress unless Endless mode (Campaign only)
            if (actor.IsHero && !GameModeHelper.IsEndless)
                SaveHeroProgress(actor);
        }

        private static void SaveHeroProgress(ActorInstance actor)
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null) return;

            var entry = party.FirstOrDefault(m => m != null && m.CharacterClass == actor.characterClass);
            if (entry == null) return;

            // Persist only TotalXP. Level and CurrentXP are derived at runtime.
            entry.TotalXP = Mathf.Max(0, actor.Stats.TotalXP);

            //ProfileHelper.Save(true);
        }

        // Derive current level and current (post-level) XP from lifetime TotalXP.
        public static (int level, int currentXP) DeriveFromTotalXP(int totalXP)
        {
            int level = 1;
            int cur = Mathf.Max(0, totalXP);

            while (cur >= NextLevel(level))
            {
                cur -= NextLevel(level);
                level++;
            }

            return (level, cur);
        }

        // Convenience: normalize a pair to match TotalXP (useful when you want to sync save fields).
        public static void NormalizeFromTotal(ref int level, ref int currentXP, int totalXP)
        {
            (level, currentXP) = DeriveFromTotalXP(totalXP);
        }
    }
}
