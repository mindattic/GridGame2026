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
}
