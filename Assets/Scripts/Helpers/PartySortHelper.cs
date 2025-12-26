using System.Collections.Generic;
using UnityEngine;

public static class PartySortHelper
{
    private static OverworldHero hero;
    private static SpriteRenderer heroSR;
    private static readonly List<SpriteRenderer> party = new List<SpriteRenderer>(16);
    private static float nextRefresh;
    private const float refreshInterval = 0.5f;

    // Global scale used to map world Y to sorting order. Higher = more separation between rows.
    public static int GlobalScale = 1000;

    // Compute a sorting order from a world-space Y. North (higher Y) => lower order; South => higher order.
    public static int ComputeOrderFromY(float y, int scale)
    {
        // Negative so higher Y gets smaller order
        return Mathf.RoundToInt(-y * Mathf.Max(1, scale));
    }

    // Get hero's sorting layer ID if available
    public static int GetHeroSortingLayerId()
    {
        var hero = Object.FindFirstObjectByType<OverworldHero>();
        var sr = hero != null ? hero.GetComponent<SpriteRenderer>() : null;
        return sr != null ? sr.sortingLayerID : 0;
    }

    // Apply Y-sorting to an actor (hero/follower) each frame.
    public static void ApplyActorYSort(SpriteRenderer sr, int scale, int? forceLayerId = null)
    {
        if (sr == null) return;
        int order = ComputeOrderFromY(sr.transform.position.y, scale);
        if (sr.sortingOrder != order)
            sr.sortingOrder = order;
        int layerId = forceLayerId ?? GetHeroSortingLayerId();
        if (layerId != 0 && sr.sortingLayerID != layerId)
            sr.sortingLayerID = layerId;
    }

    private static void RefreshIfNeeded()
    {
        if (Time.unscaledTime < nextRefresh && party.Count > 0) return;
        party.Clear();

        if (hero == null) hero = Object.FindFirstObjectByType<OverworldHero>();
        heroSR = hero != null ? hero.GetComponent<SpriteRenderer>() : null;

        var heroes = Object.FindObjectsOfType<OverworldHero>();
        for (int i = 0; i < heroes.Length; i++)
        {
            var h = heroes[i]; if (h == null) continue;
            var sr = h.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                party.Add(sr);
                if (heroSR == null && h == hero) heroSR = sr;
            }
        }

        nextRefresh = Time.unscaledTime + refreshInterval;
    }

    public static SpriteRenderer GetClosestToY(float y)
    {
        RefreshIfNeeded();
        SpriteRenderer best = null;
        float bestAbs = float.PositiveInfinity;
        for (int i = 0; i < party.Count; i++)
        {
            var sr = party[i];
            if (sr == null) continue;
            float dy = Mathf.Abs(sr.transform.position.y - y);
            if (dy < bestAbs) { bestAbs = dy; best = sr; }
        }
        return best;
    }

    public static SpriteRenderer GetLowestY()
    {
        RefreshIfNeeded();
        SpriteRenderer best = null;
        float bestY = float.PositiveInfinity;
        for (int i = 0; i < party.Count; i++)
        {
            var sr = party[i];
            if (sr == null) continue;
            float y = sr.transform.position.y;
            if (y < bestY) { bestY = y; best = sr; }
        }
        return best;
    }

    public static SpriteRenderer GetHighestY()
    {
        RefreshIfNeeded();
        SpriteRenderer best = null;
        float bestY = float.NegativeInfinity;
        for (int i = 0; i < party.Count; i++)
        {
            var sr = party[i];
            if (sr == null) continue;
            float y = sr.transform.position.y;
            if (y > bestY) { bestY = y; best = sr; }
        }
        return best;
    }

    public static SpriteRenderer GetClosestBelow(float y)
    {
        RefreshIfNeeded();
        SpriteRenderer best = null;
        float bestY = float.NegativeInfinity;
        for (int i = 0; i < party.Count; i++)
        {
            var sr = party[i]; if (sr == null) continue;
            float py = sr.transform.position.y;
            if (py < y && py > bestY) { bestY = py; best = sr; }
        }
        return best;
    }

    public static SpriteRenderer GetClosestAbove(float y)
    {
        RefreshIfNeeded();
        SpriteRenderer best = null;
        float bestY = float.PositiveInfinity;
        for (int i = 0; i < party.Count; i++)
        {
            var sr = party[i]; if (sr == null) continue;
            float py = sr.transform.position.y;
            if (py >= y && py < bestY) { bestY = py; best = sr; }
        }
        return best;
    }

    /// <summary>
    /// Compute a robust sorting order for an object at the given Y so that:
    /// - Any party member with Y greater than the object (north/above) renders behind it
    /// - Any party member with Y less than the object (south/below) renders in front of it
    /// Uses existing party member sorting orders and chooses an order between the closest below and above if possible.
    /// Falls back to +/- 1 from the nearest anchor when only one side exists.
    /// </summary>
    public static bool TryComputeOrderForObject(float objectY, out int sortingLayerId, out int desiredOrder)
    {
        RefreshIfNeeded();
        sortingLayerId = heroSR != null ? heroSR.sortingLayerID : 0;
        desiredOrder = 0;
        if (party.Count == 0) return false;

        // Choose layer from hero if available; otherwise from any present SR
        if (sortingLayerId == 0)
        {
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != null) { sortingLayerId = party[i].sortingLayerID; break; }
            }
        }

        SpriteRenderer below = null, above = null;
        int belowMaxOrder = int.MinValue, aboveMinOrder = int.MaxValue;
        float belowClosestDy = float.PositiveInfinity, aboveClosestDy = float.PositiveInfinity;

        for (int i = 0; i < party.Count; i++)
        {
            var sr = party[i]; if (sr == null) continue;
            float py = sr.transform.position.y;
            int po = sr.sortingOrder;
            if (py < objectY)
            {
                // Below group: keep the one closest in Y and track max order
                float dy = objectY - py;
                if (dy < belowClosestDy) { belowClosestDy = dy; below = sr; }
                if (po > belowMaxOrder) belowMaxOrder = po;
            }
            else
            {
                // Above group: keep the one closest in Y and track min order
                float dy = py - objectY;
                if (dy < aboveClosestDy) { aboveClosestDy = dy; above = sr; }
                if (po < aboveMinOrder) aboveMinOrder = po;
            }
        }

        if (below != null && above != null)
        {
            // Prefer to sit between the closest below and above in order space if there is room
            int lowerBound = below.sortingOrder + 1;
            int upperBound = above.sortingOrder - 1;
            if (lowerBound <= upperBound)
            {
                desiredOrder = Mathf.Clamp((below.sortingOrder + above.sortingOrder) / 2, lowerBound, upperBound);
                sortingLayerId = below.sortingLayerID; // assume same layer across party
                return true;
            }
            // No gap: bias toward the nearer side in Y
            if (belowClosestDy <= aboveClosestDy)
            {
                desiredOrder = below.sortingOrder + 1;
                sortingLayerId = below.sortingLayerID;
                return true;
            }
            else
            {
                desiredOrder = above.sortingOrder - 1;
                sortingLayerId = above.sortingLayerID;
                return true;
            }
        }
        else if (below != null)
        {
            // Object is north of all party -> behind them
            desiredOrder = below.sortingOrder - 1; // behind the closest-below in implicit convention
            sortingLayerId = below.sortingLayerID;
            return true;
        }
        else if (above != null)
        {
            // Object is south of all party -> in front of them
            desiredOrder = above.sortingOrder + 1; // in front of the closest-above in implicit convention
            sortingLayerId = above.sortingLayerID;
            return true;
        }

        return false;
    }
}