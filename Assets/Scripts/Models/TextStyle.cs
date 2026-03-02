using TMPro;
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
/// TEXTSTYLE - Combat text visual configuration.
/// 
/// PURPOSE:
/// Defines the visual style for floating combat text
/// including font, size, color, and animation type.
/// 
/// PROPERTIES:
/// - Name: Style identifier
/// - Font: TextMeshPro font asset
/// - Size: Font size
/// - Color: Text color
/// - Motion: Animation type (Float, Bounce, etc.)
/// 
/// RELATED FILES:
/// - TextStyleLibrary.cs: Style registry
/// - CombatTextInstance.cs: Text animation
/// </summary>
public class TextStyle
{
    public string Name { get; }
    public TMP_FontAsset Font { get; }
    public int Size { get; }
    public Color Color { get; }
    public TextMotion Motion { get; }

    public TextStyle(string name, TMP_FontAsset font, int size, Color color, TextMotion motion)
    {
        Name = name;
        Font = font;
        Size = size;
        Color = color;
        Motion = motion;
    }
}

}
