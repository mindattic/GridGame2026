using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ResidenceSectionController represents the player's caravan housing where trophies and decorations
/// can be placed. Stores layout state so it persists across visits.
/// </summary>
public class ResidenceSectionController : MonoBehaviour
{
    private HubManager hub;

    /// <summary>
    /// Simple decoration slot model.
    /// </summary>
public class DecorationPlacement
    {
    public string DecorationId;
  public Vector2Int Slot; // conceptual grid position
    }

    /// <summary>
    /// Available unlocked decoration ids.
    /// </summary>
    public List<string> unlockedDecorations = new List<string>();

    /// <summary>
    /// Current placements in the residence.
    /// </summary>
    public List<DecorationPlacement> placements = new List<DecorationPlacement>();

    /// <summary>
    /// Initializes the section.
    /// </summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
   // TODO: Load unlockedDecorations and placements from save system.
    }

    /// <summary>
    /// Called when activated. Refresh layout UI.
    /// </summary>
    public void OnActivated()
    {
        // TODO: Rebuild placement visuals.
    }

/// <summary>
    /// Places a decoration into a slot, replacing existing decoration in that slot if present.
    /// </summary>
    public bool Place(string decorationId, Vector2Int slot)
    {
  if (string.IsNullOrEmpty(decorationId)) return false;
    if (!unlockedDecorations.Contains(decorationId)) return false;
  var existing = placements.Find(p => p.Slot == slot);
    if (existing != null) existing.DecorationId = decorationId;
        else placements.Add(new DecorationPlacement { DecorationId = decorationId, Slot = slot });
    return true;
    }
}
