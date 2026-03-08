using Scripts.Canvas;
using Scripts.Data.Actor;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Data.Items
{
/// <summary>Equipment slot classification for equippable items.</summary>
public enum EquipmentSlot
{
    None,
    Weapon,
    Armor,
    Relic1,
    Relic2,
    Relic3
}

/// <summary>Helper methods for EquipmentSlot.</summary>
public static class EquipmentSlotHelper
{
    /// <summary>True if the slot is any of the three relic slots.</summary>
    public static bool IsRelicSlot(EquipmentSlot slot)
        => slot == EquipmentSlot.Relic1 || slot == EquipmentSlot.Relic2 || slot == EquipmentSlot.Relic3;

    /// <summary>Display name for a slot.</summary>
    public static string DisplayName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return "Weapon";
            case EquipmentSlot.Armor: return "Armor";
            case EquipmentSlot.Relic1: return "Relic 1";
            case EquipmentSlot.Relic2: return "Relic 2";
            case EquipmentSlot.Relic3: return "Relic 3";
            default: return "None";
        }
    }
}
}
