using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class RockInstance : MonoBehaviour
{
    [Header("Sorting")]
    [Tooltip("Match hero's sorting layer and go behind when hero is below, in front when above.")]
    public bool followHeroSorting = true;

    private SpriteRenderer spriteRenderer;

    // Cache hero and its SpriteRenderer once for all rocks
    private static OverworldHero hero;
    private static SpriteRenderer heroSR;

    private bool isVisible;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.position.SetZ(0f); // ensure on Z=0 plane

        isVisible = spriteRenderer != null && spriteRenderer.isVisible;

        // Apply initial sort from the bottom of the sprite so base controls the order
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
        // Always apply Y-sort so rocks don't get stuck with a static order (e.g., 30)
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
