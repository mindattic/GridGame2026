/// <summary>
/// Static healing and consumable item definitions.
/// </summary>
public static class ItemData_Healing
{
    public static readonly ItemDefinition BasicHealingPotion = new ItemDefinition
    {
  Id = "healing_potion_basic",
   DisplayName = "Healing Potion",
   Description = "Restores a modest amount of health.",
        Type = ItemType.Consumable,
    BaseCost = 25,
        MaxStack = 10,
    BaseHealing = 50,
    Strength = 0,
        Vitality = 0,
    Agility = 0,
        Stamina = 0,
    Intelligence = 0,
  Wisdom = 0,
    Luck = 0
    };

    public static readonly ItemDefinition ManaPotion = new ItemDefinition
    {
        Id = "mana_potion_basic",
    DisplayName = "Mana Potion",
  Description = "Restores a small amount of mana.",
  Type = ItemType.Consumable,
    BaseCost = 30,
  MaxStack = 10,
   BaseHealing = 0, // TODO: adapt if AP/Mana system needs separate field.
  Strength = 0,
   Vitality = 0,
        Agility = 0,
   Stamina = 0,
  Intelligence = 0,
        Wisdom = 0,
    Luck = 0
};
}
