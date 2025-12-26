using Assets.Scripts.Models;
using System.Collections.Generic;
using System;
using UnityEngine;
using Assets.Helpers;

[Serializable]
public class ActorData
{

    public string CharacterName;
    public int Level = 1;
    public CharacterClass CharacterClass; // now enum for internal consistency


    // Base XP awarded when this actor is defeated. Can be overridden per actor Data().
    public int BonusXP = 10;

    public ActorTag Tags { get; set; }


    public ActorStats BaseStats;
    public ActorStats Stats;

    public StatGrowth StatGrowth = new StatGrowth();
    public Dictionary<int, StatGrowth> MilestoneStatGrowth = new Dictionary<int, StatGrowth>();

    public ThumbnailSettings ThumbnailSettings;
    public CanvasThumbnailSettings CanvasThumbnailSettings; // New: Canvas-specific cropping for timeline blocks
    public Sprite Portrait;


    public List<Ability> Abilities = new List<Ability>();

    private const int DefaultMilestoneWindow = 5;

    public string Description;
    public string Expectations;
    public string Lore;
    public string Card;
    public List<string> Trivia = new List<string>();

    public ActorData() { }

    public ActorData(ActorData other)
    {
        if (other == null) return;

        Level = other.Level;
        CharacterClass = other.CharacterClass;
        Description = other.Description;
        Expectations = other.Expectations;
        Lore = other.Lore;

        BonusXP = other.BonusXP;

        BaseStats = other.BaseStats != null ? new ActorStats(other.BaseStats) : new ActorStats();
        StatGrowth = other.StatGrowth != null ? new StatGrowth(other.StatGrowth) : new StatGrowth();

        MilestoneStatGrowth = other.MilestoneStatGrowth != null
            ? new Dictionary<int, StatGrowth>(other.MilestoneStatGrowth)
            : new Dictionary<int, StatGrowth>();

        ThumbnailSettings = other.ThumbnailSettings != null
            ? new ThumbnailSettings(other.ThumbnailSettings)
            : new ThumbnailSettings();

        CanvasThumbnailSettings = other.CanvasThumbnailSettings != null
            ? new CanvasThumbnailSettings(other.CanvasThumbnailSettings)
            : new CanvasThumbnailSettings();

        Portrait = other.Portrait;

        Stats = GetStats(Level);
    }

    public void RecalculateStats()
    {
        Stats = GetStats(Level);
    }

    public ActorStats GetStats(int level)
    {
        return GetStatsWithOptions(level, DefaultMilestoneWindow, true);
    }

    public ActorStats GetStatsWithOptions(int level, int milestoneWindow, bool distributeMilestones)
    {
        if (level < 1) level = 1;
        if (milestoneWindow < 1) milestoneWindow = 1;

        var stats = new ActorStats
        {
            Level = 1,
            Strength = BaseStats != null ? BaseStats.Strength : 0,
            Vitality = BaseStats != null ? BaseStats.Vitality : 0,
            Agility = BaseStats != null ? BaseStats.Agility : 0,
            Speed = BaseStats != null ? BaseStats.Speed : 0,
            Stamina = BaseStats != null ? BaseStats.Stamina : 0,
            Intelligence = BaseStats != null ? BaseStats.Intelligence : 0,
            Wisdom = BaseStats != null ? BaseStats.Wisdom : 0,
            Luck = BaseStats != null ? BaseStats.Luck : 0,
            PreviousHP = 0,
            HP = 0,
            MaxHP = 0,
            PreviousAP = 0,
            AP = 0,
            MaxAP = 100
        };

        var distributed = new List<(int start, int end, StatGrowth perLevel)>();
        if (distributeMilestones && MilestoneStatGrowth != null)
        {
            foreach (var kvp in MilestoneStatGrowth)
            {
                var bonus = kvp.Value ?? new StatGrowth();
                int start = kvp.Key;
                int end = start + milestoneWindow - 1;

                var per = new StatGrowth
                {
                    Strength = bonus.Strength / milestoneWindow,
                    Vitality = bonus.Vitality / milestoneWindow,
                    Agility = bonus.Agility / milestoneWindow,
                    Speed = bonus.Speed / milestoneWindow,
                    Stamina = bonus.Stamina / milestoneWindow,
                    Intelligence = bonus.Intelligence / milestoneWindow,
                    Wisdom = bonus.Wisdom / milestoneWindow,
                    Luck = bonus.Luck / milestoneWindow
                };

                distributed.Add((start, end, per));
            }
        }

        for (int L = 2; L <= level; L++)
        {
            stats.Level = L;

            stats.Strength += StatGrowth != null ? StatGrowth.Strength : 0;
            stats.Vitality += StatGrowth != null ? StatGrowth.Vitality : 0;
            stats.Agility += StatGrowth != null ? StatGrowth.Agility : 0;
            stats.Speed += StatGrowth != null ? StatGrowth.Speed : 0;
            stats.Stamina += StatGrowth != null ? StatGrowth.Stamina : 0;
            stats.Intelligence += StatGrowth != null ? StatGrowth.Intelligence : 0;
            stats.Wisdom += StatGrowth != null ? StatGrowth.Wisdom : 0;
            stats.Luck += StatGrowth != null ? StatGrowth.Luck : 0;

            if (distributeMilestones)
            {
                foreach (var slice in distributed)
                {
                    if (L >= slice.start && L <= slice.end)
                    {
                        stats.Strength += slice.perLevel.Strength;
                        stats.Vitality += slice.perLevel.Vitality;
                        stats.Agility += slice.perLevel.Agility;
                        stats.Speed += slice.perLevel.Speed;
                        stats.Stamina += slice.perLevel.Stamina;
                        stats.Intelligence += slice.perLevel.Intelligence;
                        stats.Wisdom += slice.perLevel.Wisdom;
                        stats.Luck += slice.perLevel.Luck;
                    }
                }
            }
            else if (MilestoneStatGrowth != null && MilestoneStatGrowth.TryGetValue(L, out var instant))
            {
                stats.Strength += instant.Strength;
                stats.Vitality += instant.Vitality;
                stats.Agility += instant.Agility;
                stats.Speed += instant.Speed;
                stats.Stamina += instant.Stamina;
                stats.Intelligence += instant.Intelligence;
                stats.Wisdom += instant.Wisdom;
                stats.Luck += instant.Luck;
            }
        }

        stats.Strength = Mathf.Floor(stats.Strength);
        stats.Vitality = Mathf.Floor(stats.Vitality);
        stats.Agility = Mathf.Floor(stats.Agility);
        stats.Speed = Mathf.Floor(stats.Speed);
        stats.Stamina = Mathf.Floor(stats.Stamina);
        stats.Intelligence = Mathf.Floor(stats.Intelligence);
        stats.Wisdom = Mathf.Floor(stats.Wisdom);
        stats.Luck = Mathf.Floor(stats.Luck);

        stats.HP = Formulas.Health(stats);
        stats.MaxHP = stats.HP;

        if (stats.HP < 1f) stats.HP = 0;

        return stats;
    }

    public bool InGroups(ActorTag mask) => (Tags & mask) == mask;
    public void AddGroups(ActorTag groups) => Tags |= groups;
    public void RemoveGroups(ActorTag groups) => Tags &= ~groups;

}