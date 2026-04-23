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
/// Arcs follow moving actors / canvas UI each frame until hidden. Two flavors are available:
/// <see cref="Show2D"/> builds the arc under the main ScreenSpaceOverlay Canvas so it draws
/// on top of the mana bar / HUD; <see cref="Show3D"/> builds a world-space arc on the "VFX"
/// sorting layer (always beneath overlay UI).</para>
///
/// <para>USAGE:
/// <code>
/// // Canvas-space arc (draws over HUD) — preferred for selection/target feedback:
/// g.TargetLineManager.Show2D("enemy-select",
///     TargetPoint.Canvas(icon.Rect),
///     TargetPoint.Actor(enemy),
///     Color.red);
///
/// // World-space arc (behind HUD) — for effects anchored to the board:
/// g.TargetLineManager.Show3D("projectile-path",
///     TargetPoint.Actor(caster),
///     TargetPoint.Actor(target),
///     Color.white);
///
/// g.TargetLineManager.Hide("enemy-select"); // works for either flavor
/// </code>
/// </para>
///
/// <para>RELATED FILES: TargetLine2DFactory.cs, TargetLine2DInstance.cs,
/// TargetLine3DFactory.cs, TargetLine3DInstance.cs, TargetPoint.cs</para>
///
/// <para>ACCESS: g.TargetLineManager</para>
/// </summary>
public class TargetLineManager : MonoBehaviour
{
    #region Fields

    // Keyed by caller-chosen string. Both dictionaries share the key namespace, so a
    // single Hide(key) can despawn whichever flavor was used to Show.
    private readonly Dictionary<string, TargetLine2DInstance> persistentArcs2D = new Dictionary<string, TargetLine2DInstance>();
    private readonly Dictionary<string, TargetLine3DInstance> persistentArcs3D = new Dictionary<string, TargetLine3DInstance>();

    #endregion

    #region Canvas (2D) API

    /// <summary>
    /// Shows a canvas-space arc under <paramref name="key"/>, rendered on top of the HUD so it
    /// reads as a targeting indicator rather than a world effect. If a 3D arc was previously
    /// shown under the same key it is despawned first. Re-calling with the same key updates
    /// endpoints/color in place.
    /// </summary>
    public void Show2D(string key, TargetPoint from, TargetPoint to, Color color)
    {
        if (string.IsNullOrEmpty(key)) return;

        // Flavor swap: tear down the other variant if the caller is switching.
        if (persistentArcs3D.TryGetValue(key, out var old3D) && old3D != null)
        {
            persistentArcs3D.Remove(key);
            old3D.UnbindEndpoints();
            old3D.Despawn();
        }

        if (!persistentArcs2D.TryGetValue(key, out var arc) || arc == null)
        {
            var go = TargetLine2DFactory.Create();
            arc = go.GetComponent<TargetLine2DInstance>();
            arc.name = $"TargetLine2D_{key}";
            persistentArcs2D[key] = arc;
        }

        arc.SetColor(color);
        arc.BindEndpoints(from, to);
    }

    #endregion

    #region World (3D) API

    /// <summary>
    /// Shows a world-space arc under <paramref name="key"/> on the "VFX" sorting layer. Always
    /// beneath ScreenSpaceOverlay Canvas UI (mana bar, timeline bar). If a 2D arc was previously
    /// shown under the same key it is despawned first.
    /// </summary>
    public void Show3D(string key, TargetPoint from, TargetPoint to, Color color)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (persistentArcs2D.TryGetValue(key, out var old2D) && old2D != null)
        {
            persistentArcs2D.Remove(key);
            old2D.UnbindEndpoints();
            old2D.Despawn();
        }

        if (!persistentArcs3D.TryGetValue(key, out var arc) || arc == null)
        {
            var go = TargetLine3DFactory.Create();
            arc = go.GetComponent<TargetLine3DInstance>();
            arc.name = $"TargetLine3D_{key}";
            arc.parent = g.Board != null ? g.Board.transform : null;
            persistentArcs3D[key] = arc;
        }

        arc.SetColor(color);
        arc.BindEndpoints(from, to);
    }

    #endregion

    #region Common API

    /// <summary>
    /// Hides and despawns the persistent arc registered under <paramref name="key"/>, whether
    /// it's a 2D or 3D instance. No-op if no arc exists. Safe to call during cast resolution /
    /// interrupt paths.
    /// </summary>
    public void Hide(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (persistentArcs2D.TryGetValue(key, out var arc2D))
        {
            persistentArcs2D.Remove(key);
            if (arc2D != null)
            {
                arc2D.UnbindEndpoints();
                arc2D.Despawn();
            }
        }
        if (persistentArcs3D.TryGetValue(key, out var arc3D))
        {
            persistentArcs3D.Remove(key);
            if (arc3D != null)
            {
                arc3D.UnbindEndpoints();
                arc3D.Despawn();
            }
        }
    }

    /// <summary>True if a persistent arc (2D or 3D) is currently registered under <paramref name="key"/>.</summary>
    public bool IsShowing(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (persistentArcs2D.TryGetValue(key, out var a2D) && a2D != null) return true;
        if (persistentArcs3D.TryGetValue(key, out var a3D) && a3D != null) return true;
        return false;
    }

    #endregion
}

}
