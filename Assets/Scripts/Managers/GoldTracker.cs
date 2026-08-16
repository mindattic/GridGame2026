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
/// GOLDTRACKER - Books the coins collected during one battle into spendable gold.
///
/// PURPOSE:
/// Coin pickups increment the lifetime ticker (GameHelper.TotalCoins ->
/// save.Global.TotalCoins) which vendors never read; vendors spend only
/// save.Inventory.Gold. This tracker bridges the two per battle session: it
/// snapshots TotalCoins at battle start, exposes the delta as Collected, and
/// commits that delta into Inventory.Gold at the PostBattle loot phase — so the
/// coins the player watched the CoinCounter rack up during the fight become the
/// gold they can actually spend. The lifetime ticker itself is left untouched.
///
/// SESSION FLOW:
/// 1. StartSession() snapshots the lifetime coin total (StageManager.Initialize,
///    beside ExperienceTracker/LootTracker session starts)
/// 2. CoinInstance.Despawn() keeps incrementing GameHelper.TotalCoins as usual
/// 3. Collected = lifetime total - snapshot (coins earned THIS battle)
/// 4. CommitToInventory() adds Collected to save.Inventory.Gold (PostBattleManager)
/// 5. Clear() resets the session so consecutive battles never double-count
///
/// RELATED FILES: LootTracker.cs (pattern source), ExperienceTracker.cs,
/// StageManager.cs, PostBattleManager.cs, CoinInstance.cs, GameHelper.cs
/// </summary>
public static class GoldTracker
{
    private static int sessionStartCoins;
    private static bool sessionActive;

    /// <summary>Snapshots the lifetime coin total at battle start.</summary>
    public static void StartSession()
    {
        sessionStartCoins = g.TotalCoins;
        sessionActive = true;
    }

    /// <summary>Coins collected this battle (never negative; 0 outside a session).</summary>
    public static int Collected =>
        sessionActive ? Mathf.Max(0, g.TotalCoins - sessionStartCoins) : 0;

    /// <summary>
    /// Adds the session's collected coins to the save's spendable gold.
    /// Call once at the PostBattle reward commit, then Clear().
    /// </summary>
    public static void CommitToInventory()
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save == null || Collected <= 0) return;

        var inv = new PlayerInventory();
        if (save.Inventory != null)
            inv.LoadFromSaveData(save.Inventory);

        inv.Gold += Collected;
        save.Inventory = inv.ToSaveData();
    }

    /// <summary>Ends the session; consecutive battles never double-count.</summary>
    public static void Clear()
    {
        sessionStartCoins = 0;
        sessionActive = false;
    }
}

}
