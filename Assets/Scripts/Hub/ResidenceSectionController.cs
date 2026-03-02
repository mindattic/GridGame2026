using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

namespace Scripts.Hub
{
/// <summary>
/// RESIDENCESECTIONCONTROLLER - Hub residence/housing section.
/// 
/// PURPOSE:
/// Manages the player's caravan housing where trophies and
/// decorations can be placed for cosmetic customization.
/// 
/// FEATURES:
/// - Place/remove decorations in grid slots
/// - Unlock new decorations through gameplay
/// - Persist layout across visits
/// 
/// DATA STRUCTURES:
/// - DecorationPlacement: ID + grid position
/// - unlockedDecorations: Available decoration IDs
/// - placements: Current decoration layout
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// </summary>
public class ResidenceSectionController : MonoBehaviour
{
    private HubManager hub;

    /// <summary>Decoration placement data.</summary>
    public class DecorationPlacement
    {
        public string DecorationId;
        public Vector2Int Slot;
    }

    /// <summary>Available unlocked decoration IDs.</summary>
    public List<string> unlockedDecorations = new List<string>();

    /// <summary>Current placements in residence.</summary>
    public List<DecorationPlacement> placements = new List<DecorationPlacement>();

    /// <summary>Initializes the section.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
    }

    /// <summary>Called when activated.</summary>
    public void OnActivated()
    {
        // TODO: Rebuild placement visuals
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

}
