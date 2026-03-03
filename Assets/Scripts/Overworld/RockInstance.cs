using UnityEngine;
using System.Collections;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Overworld
{
/// <summary>
/// ROCKINSTANCE - Rock prop in overworld.
/// 
/// PURPOSE:
/// Static decorative rock sprite with Y-based sorting
/// relative to the hero.
/// 
/// FEATURES:
/// - Y-sorting from sprite bottom (base controls order)
/// - No animation (static object)
/// 
/// RELATED FILES:
/// - TreeInstance.cs: Similar prop
/// - OverworldManager.cs: Overworld scene
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RockInstance : MonoBehaviour
{
    [Header("Sorting")]
    [Tooltip("Match hero's sorting layer and sort by Y position.")]
    public bool followHeroSorting = true;

    private SpriteRenderer spriteRenderer;

    private static OverworldHero hero;
    private static SpriteRenderer heroSR;

    private bool isVisible;

    /// <summary>Initializes component references and state.</summary>
    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.position.SetZ(0f);

        isVisible = spriteRenderer != null && spriteRenderer.isVisible;

        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Called when the component becomes enabled and active.</summary>
    private void OnEnable()
    {
        TryCacheHero();
        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Called when the renderer becomes visible by any camera.</summary>
    private void OnBecameVisible()
    {
        isVisible = true;
        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    /// <summary>Called when the renderer is no longer visible by any camera.</summary>
    private void OnBecameInvisible()
    {
        isVisible = false;
    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        if (followHeroSorting)
            YSortUtility.ApplyFromBottom(spriteRenderer);
    }

#if UNITY_EDITOR
    /// <summary>Editor callback when inspector values change.</summary>
    private void OnValidate()
    {
        // Keep order correct in editor when moving objects
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (followHeroSorting && spriteRenderer != null)
            YSortUtility.ApplyFromBottom(spriteRenderer);
    }
#endif

    /// <summary>Try cache hero.</summary>
    private static void TryCacheHero()
    {
        if (hero == null) hero = Object.FindObjectOfType<OverworldHero>();
        if (hero != null && (heroSR == null || heroSR.gameObject != hero.gameObject))
            heroSR = hero.GetComponent<SpriteRenderer>();
    }
}

}
