using Scripts.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// TARGETLINEMANAGER - Owns persistent FFXII-style targeting arcs keyed by caller string.
///
/// <para>PURPOSE: Callers (SelectionManager enemy-select, AbilityManager cast arcs, etc.) show
/// a named arc between two <see cref="TargetPoint"/> endpoints and hide it by the same key.
/// Arcs follow moving actors / canvas UI each frame until hidden.</para>
///
/// <para>USAGE:
/// <code>
/// g.TargetLineManager.Show("enemy-select",
///     TargetPoint.Canvas(icon.Rect),
///     TargetPoint.Actor(enemy),
///     Color.red);
/// g.TargetLineManager.Hide("enemy-select");
/// </code>
/// </para>
///
/// <para>RELATED FILES: TargetLineFactory.cs, TargetLineInstance.cs, TargetPoint.cs</para>
///
/// <para>ACCESS: g.TargetLineManager</para>
/// </summary>
public class TargetLineManager : MonoBehaviour
{
    #region Fields

    // Persistent named arcs. Keyed by caller-chosen string so the same caller
    // can show/hide without owning the instance.
    private readonly Dictionary<string, TargetLineInstance> persistentArcs = new Dictionary<string, TargetLineInstance>();

    #endregion

    #region Persistent Arc API

    /// <summary>
    /// Shows a persistent arc between two endpoints, tinted <paramref name="color"/>, keyed by
    /// <paramref name="key"/>. If an arc with this key already exists it is updated in place
    /// (endpoints + color) rather than re-spawned, so callers can refresh every frame cheaply.
    /// Endpoints follow moving actors / UI automatically until <see cref="Hide"/> is called.
    /// </summary>
    public void Show(string key, TargetPoint from, TargetPoint to, Color color)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (!persistentArcs.TryGetValue(key, out var arc) || arc == null)
        {
            var go = TargetLineFactory.Create();
            arc = go.GetComponent<TargetLineInstance>();
            arc.name = $"TargetLine_{key}";
            arc.parent = g.Board != null ? g.Board.transform : null;
            persistentArcs[key] = arc;
        }

        arc.SetColor(color);
        arc.BindEndpoints(from, to);
    }

    /// <summary>
    /// Hides and despawns the persistent arc registered under <paramref name="key"/>. No-op if
    /// no arc exists for that key. Safe to call during cast resolution / interrupt paths.
    /// </summary>
    public void Hide(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!persistentArcs.TryGetValue(key, out var arc)) return;

        persistentArcs.Remove(key);
        if (arc != null)
        {
            arc.UnbindEndpoints();
            arc.Despawn();
        }
    }

    /// <summary>True if a persistent arc is currently registered under <paramref name="key"/>.</summary>
    public bool IsShowing(string key)
        => !string.IsNullOrEmpty(key)
           && persistentArcs.TryGetValue(key, out var arc)
           && arc != null;

    #endregion
}

}
