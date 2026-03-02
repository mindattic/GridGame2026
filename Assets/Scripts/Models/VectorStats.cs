using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
// Summary:
//   Seven-component stat vector in this order:
//   Strength, Vitality, Speed, Stamina, Intelligence, Wisdom, Luck.
[System.Serializable]
public struct VectorStats
{
    public float Strength;
    public float Vitality;
    public float Agility;
    public float Speed;
    public float Stamina;
    public float Intelligence;
    public float Wisdom;
    public float Luck;

    public VectorStats(float str, float vit, float agi, float spd, float sta, float intel, float wis, float lck)
    {
        this.Strength = str;
        this.Vitality = vit;
        this.Agility = agi;
        this.Speed = spd;
        this.Stamina = sta;
        this.Intelligence = intel;
        this.Wisdom = wis;
        this.Luck = lck;
    }

    public static VectorStats operator +(VectorStats a, VectorStats b)
    {
        return new VectorStats(
            a.Strength + b.Strength,
            a.Vitality + b.Vitality,
            a.Agility + b.Agility,
            a.Speed + b.Speed,
            a.Stamina + b.Stamina,
            a.Intelligence + b.Intelligence,
            a.Wisdom + b.Wisdom,
            a.Luck + b.Luck
        );
    }

    public static VectorStats operator *(VectorStats a, float m)
    {
        return new VectorStats(
            a.Strength * m,
            a.Vitality * m,
            a.Agility * m,
            a.Speed * m,
            a.Stamina * m,
            a.Intelligence * m,
            a.Wisdom * m,
            a.Luck * m
        );
    }
}

}
