using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
/// <summary>
/// YSORTUTILITY - Y-axis sprite sorting utility.
/// 
/// PURPOSE:
/// Computes and applies sorting order for sprites based on their
/// Y position, enabling proper depth sorting in 2D/2.5D games.
/// 
/// SORTING PRINCIPLE:
/// ```
/// Lower Y = Closer to camera = Higher sort order
/// Higher Y = Further from camera = Lower sort order
/// ```
/// 
/// METHODS:
/// - Apply(sr): Sort from transform pivot Y
/// - ApplyFromBottom(sr): Sort from sprite bottom bounds
/// 
/// USE CASES:
/// - Characters: Use Apply() (pivot at feet)
/// - Props: Use ApplyFromBottom() (base of sprite)
/// 
/// RELATED FILES:
/// - TreeInstance.cs, RockInstance.cs: Use for props
/// - OverworldHero.cs: Uses for character sorting
/// - PartySortHelper.cs: Provides layer ID
/// </summary>
public static class YSortUtility
{
    /// <summary>Scale factor for stable ordering between rows.</summary>
    public static int GlobalScale = 1000;

    /// <summary>Computes sorting order from Y position.</summary>
    public static int ComputeOrderFromY(float y)
    {
        return Mathf.RoundToInt(-y * Mathf.Max(1, GlobalScale));
    }

    /// <summary>Applies sorting from transform pivot Y (good for characters).</summary>
    public static void Apply(SpriteRenderer sr)
    {
        if (sr == null) return;
        int order = ComputeOrderFromY(sr.transform.position.y);
        if (sr.sortingOrder != order) sr.sortingOrder = order;

        int heroLayerId = PartySortHelper.GetHeroSortingLayerId();
        if (heroLayerId != 0 && sr.sortingLayerID != heroLayerId)
            sr.sortingLayerID = heroLayerId;
    }

    /// <summary>Applies sorting from sprite bottom bounds (good for props).</summary>
    public static void ApplyFromBottom(SpriteRenderer sr)
    {
        if (sr == null) return;
        float bottomY = sr.bounds.min.y;
        int order = ComputeOrderFromY(bottomY);
        if (sr.sortingOrder != order) sr.sortingOrder = order;

        int heroLayerId = PartySortHelper.GetHeroSortingLayerId();
        if (heroLayerId != 0 && sr.sortingLayerID != heroLayerId)
            sr.sortingLayerID = heroLayerId;
    }
}

}
