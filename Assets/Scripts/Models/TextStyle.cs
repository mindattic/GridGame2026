using TMPro;
using UnityEngine;

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
