using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// ITEMPICKUPMANAGER - Spawns on-map crafting-material pickup visuals.
///
/// PURPOSE:
/// Visualizes loot drops when enemies die. The drops themselves are booked
/// into <see cref="LootTracker"/> at the death site (ActorInstance.DieRoutine);
/// this manager's job is only the celebratory pickup animation, tinted by
/// rarity so the player can read the drop quality at a glance.
///
/// BURST PATTERN:
/// ```
/// [Enemy dies]
///   /-- rarity-tinted pickups burst out
///   \->  [Coin Counter] (pickups fly toward collect endpoint)
/// ```
///
/// RARITY COLORS (ItemRarityColors.Get):
/// - Junk      gray   | Common   white | Uncommon green
/// - Rare      blue   | Epic   purple  | Legendary orange
///
/// RELATED FILES:
/// - ItemPickupFactory.cs: Creates pickup GameObjects
/// - ItemPickupInstance.cs: Per-pickup animation behavior
/// - CoinManager.cs: Sister system (currency burst)
/// - LootTracker.cs: Where drops are actually booked for PostBattle
///
/// ACCESS: g.ItemPickupManager
/// </summary>
public class ItemPickupManager : MonoBehaviour
{
    private const float PerItemStagger = 0.05f;

    /// <summary>Spawns a single pickup visual for <paramref name="def"/> at <paramref name="position"/>.</summary>
    public void Spawn(Vector3 position, ItemDefinition def)
    {
        if (def == null) return;
        var go = ItemPickupFactory.Create();
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<ItemPickupInstance>();
        instance.name = $"Pickup_{def.Id}_{Guid.NewGuid():N}";
        instance.Spawn(position, def);
    }

    /// <summary>
    /// Spawns one pickup per item per drop (staggered). Drops must already have
    /// been booked into <see cref="LootTracker"/>; this call is purely visual.
    /// </summary>
    public void SpawnBurst(Vector3 worldPosition, List<DropResult> drops)
    {
        if (drops == null || drops.Count == 0) return;
        StartCoroutine(SpawnBurstRoutine(worldPosition, drops));
    }

    private IEnumerator SpawnBurstRoutine(Vector3 worldPosition, List<DropResult> drops)
    {
        foreach (var drop in drops)
        {
            if (drop == null || string.IsNullOrEmpty(drop.ItemId) || drop.Count <= 0) continue;
            var def = ItemLibrary.Get(drop.ItemId);
            if (def == null) continue;

            int count = Mathf.Clamp(drop.Count, 1, 8); // cap visual burst so stacks of 10+ don't swamp the board
            for (int i = 0; i < count; i++)
            {
                Spawn(worldPosition, def);
                yield return new WaitForSeconds(PerItemStagger);
            }
        }
    }
}
}
