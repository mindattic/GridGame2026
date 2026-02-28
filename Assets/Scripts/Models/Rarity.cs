using UnityEngine;

/// <summary>
/// RARITY - Item/character rarity level.
/// 
/// PURPOSE:
/// Defines a rarity tier with associated name and
/// color for UI display.
/// 
/// COMMON RARITIES:
/// - Common (Gray)
/// - Uncommon (Green)
/// - Rare (Blue)
/// - Epic (Purple)
/// - Legendary (Orange)
/// 
/// USAGE:
/// ```csharp
/// var rare = new Rarity("Rare", Color.blue);
/// itemNameText.color = item.Rarity.Color;
/// ```
/// </summary>
public class Rarity
{
    public string Name;
    public Color Color;

    public Rarity() { }

    public Rarity(string name, Color color)
    {
        Name = name;
        Color = color;
    }
}