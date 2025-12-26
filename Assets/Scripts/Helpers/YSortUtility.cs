using UnityEngine;

public static class YSortUtility
{
    // Larger scale = bigger gaps between rows, keeps ordering stable
    public static int GlobalScale = 1000;

    public static int ComputeOrderFromY(float y)
    {
        return Mathf.RoundToInt(-y * Mathf.Max(1, GlobalScale));
    }

    // Use transform pivot Y (good for characters whose pivot is at the feet)
    public static void Apply(SpriteRenderer sr)
    {
        if (sr == null) return;
        int order = ComputeOrderFromY(sr.transform.position.y);
        if (sr.sortingOrder != order) sr.sortingOrder = order;

        int heroLayerId = PartySortHelper.GetHeroSortingLayerId();
        if (heroLayerId != 0 && sr.sortingLayerID != heroLayerId)
            sr.sortingLayerID = heroLayerId;
    }

    // Use the bottom of the sprite bounds (good for props whose base is visually at the bottom of the sprite)
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
