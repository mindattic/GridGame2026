using System;

[Serializable]
public class ActorStats : BaseStats
{
    public int Level = 1; 
    public int CurrentXP;
    public int TotalXP;

    public float PreviousHP;
    public float HP;
    public float MaxHP;

    public float PreviousAP;
    public float AP;
    public float MaxAP;

    public ActorStats() { }

    public ActorStats(ActorStats other)
    {
        if (other == null) return;

        Level = other.Level;
        CurrentXP = other.CurrentXP;
        TotalXP = other.TotalXP;

        PreviousHP = other.HP;
        HP = other.HP;
        MaxHP = other.MaxHP;

        PreviousAP = 0f;
        AP = 0f;
        MaxAP = 100f;

        Strength = other.Strength;
        Vitality = other.Vitality;
        Speed = other.Speed;
        Stamina = other.Stamina;
        Intelligence = other.Intelligence;
        Wisdom = other.Wisdom;
        Luck = other.Luck;
    }
}
