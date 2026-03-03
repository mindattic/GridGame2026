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

    /// <summary>Set.</summary>
    public void Set(Vector3 top, Vector3 right, Vector3 bottom, Vector3 left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }
}

}
