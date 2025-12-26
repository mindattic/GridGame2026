using UnityEngine;

/// <summary>
/// RectVector3 stores four directional Vector3 points: Top, Right, Bottom, and Left.
/// </summary>
[System.Serializable]
public class RectVector3
{
    public Vector3 Top;
    public Vector3 Right;
    public Vector3 Bottom;
    public Vector3 Left;

    public RectVector3() { }

    public RectVector3(Vector3 top, Vector3 right, Vector3 bottom, Vector3 left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public void Set(Vector3 top, Vector3 right, Vector3 bottom, Vector3 left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }
}
