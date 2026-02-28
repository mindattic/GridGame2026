/// <summary>
/// ITEMDATA_EQUIPMENT - Equipment item definitions.
/// 
/// PURPOSE:
/// Static definitions for weapons, armor, and accessories.
/// 
/// ITEMS:
/// - RustySword: +2 Strength weapon
/// - LeatherArmor: +2 Vitality, +1 Stamina armor
/// 
/// STAT ORDER:
/// Strength, Vitality, Agility, Stamina, Intelligence, Wisdom, Luck
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Registers these items
/// - ItemDefinition.cs: Item data structure
/// </summary>
public static class ItemData_Equipment
{
    public static readonly ItemDefinition RustySword = new ItemDefinition
    {
        Id = "eq_sword_rusty",
        DisplayName = "Rusty Sword",
        Description = "A worn blade offering minimal power.",
        Type = ItemType.Equipment,
        BaseCost = 40,
        MaxStack = 1,
        Durability = 100,
        Strength = 2,
        Vitality = 0,
        Agility = 0,
        Stamina = 0,
        Intelligence = 0,
        Wisdom = 0,
        Luck = 0
    };

    public static readonly ItemDefinition LeatherArmor = new ItemDefinition
    {
        Id = "eq_armor_leather",
        DisplayName = "Leather Armor",
        Description = "Basic protective gear made of hardened leather.",
        Type = ItemType.Equipment,
        BaseCost = 55,
        MaxStack = 1,
        Durability = 120,
        Strength = 0,
        Vitality = 2,
        Agility = 0,
        Stamina = 1,
        Intelligence = 0,
        Wisdom = 0,
        Luck = 0
    };
}
