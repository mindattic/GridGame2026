using UnityEngine;
using System.Collections;

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

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.position.SetZ(0f);

        isVisible = spriteRenderer != null && spriteRenderer.isVisible;

        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    private void OnEnable()
    {
        TryCacheHero();
        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    private void OnBecameVisible()
    {
        isVisible = true;
        if (followHeroSorting) YSortUtility.ApplyFromBottom(spriteRenderer);
    }

    private void OnBecameInvisible()
    {
        isVisible = false;
    }

    private void Update()
    {
        if (followHeroSorting)
            YSortUtility.ApplyFromBottom(spriteRenderer);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep order correct in editor when moving objects
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (followHeroSorting && spriteRenderer != null)
            YSortUtility.ApplyFromBottom(spriteRenderer);
    }
#endif

    private static void TryCacheHero()
    {
        if (hero == null) hero = Object.FindObjectOfType<OverworldHero>();
        if (hero != null && (heroSR == null || heroSR.gameObject != hero.gameObject))
            heroSR = hero.GetComponent<SpriteRenderer>();
    }
}
