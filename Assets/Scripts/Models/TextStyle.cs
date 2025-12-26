using TMPro;
using UnityEngine;

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
