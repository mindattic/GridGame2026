using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ItemDefinition describes a single item or equipment piece. Compatible with static data style used for actors.
/// Stat modifiers (when equipment) must appear in the required order: Strength, Vitality, Agility, Stamina, Intelligence, Wisdom, Luck.
/// </summary>
[System.Serializable]
public class ItemDefinition
{
    public string Id;
public string DisplayName;
    public string Description;
    public ItemType Type;
    public int BaseCost;
    public int MaxStack = 99;
    public int Durability; // equipment only; 0 means no durability tracking
    public int BaseHealing; // consumable effect value (healing or similar); 0 if none

    // Stat modifiers for equipment (order enforced by project rules).
    public float Strength;
    public float Vitality;
    public float Agility;
    public float Stamina;
    public float Intelligence;
    public float Wisdom;
    public float Luck;

    // Tag constraints (e.g. requires Healer tag to equip).
    public List<ActorTag> RequiredTags = new List<ActorTag>();
}

/// <summary>
/// ItemType classification for items.
/// </summary>
public enum ItemType
{
    Consumable,
Equipment,
    CraftingMaterial,
    QuestItem
}
