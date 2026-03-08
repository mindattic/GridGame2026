using UnityEngine;
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
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
/// <summary>
/// DAYNIGHTSTATE - Static cross-scene day/night cycle state.
///
/// PURPOSE:
/// Holds the current cycle position and evaluated overlay color so
/// non-Overworld scenes (Hub, PostBattle) can apply the correct
/// time-of-day tint without running a full DayNightCycle instance.
///
/// LIFECYCLE:
/// 1. OverworldManager.SaveState() writes T01 to save data
/// 2. DayNightCycle.Update() writes snapshot here each frame
/// 3. HubManager reads snapshot on Awake to tint the Hub frozen
/// 4. On return to Overworld, cycle resumes from save data
///
/// RELATED FILES:
/// - DayNightCycle.cs: Writes snapshots here every frame
/// - HubManager.cs: Reads snapshot to apply frozen tint
/// - OverworldManager.cs: Restores cycle on scene load
/// - OverworldSaveData: Persists T01 across sessions
/// </summary>
public static class DayNightState
{
    /// <summary>Normalized cycle position (0..1) at the moment the Overworld was left.</summary>
    public static float T01 { get; set; }

    /// <summary>Evaluated overlay color at the moment the Overworld was left.</summary>
    public static Color OverlayColor { get; set; } = new Color(1f, 1f, 1f, 0f);

    /// <summary>True once the Overworld has written at least one snapshot.</summary>
    public static bool HasSnapshot { get; set; }

    // ===================== Sleep Transition =====================

    /// <summary>
    /// Speed multiplier for the sleep time-lapse animation.
    /// Higher values = faster passage of time during sleep.
    /// Default 30x means a 1024s cycle plays in ~34 real seconds.
    /// </summary>
    public static float SleepSpeedMultiplier { get; set; } = 30f;

    /// <summary>
    /// HP regenerated per hero per real second of sleep.
    /// Base rate is slow; premium tier can increase this.
    /// </summary>
    public static float BaseHPRegenPerSecond { get; set; } = 2f;

    /// <summary>
    /// Premium HP regen multiplier applied on top of BaseHPRegenPerSecond.
    /// 1.0 = normal, 3.0 = triple speed healing, etc.
    /// This is the monetization hook — purchasable "Soft Bed" or similar.
    /// </summary>
    public static float PremiumRegenMultiplier { get; set; } = 1f;

    /// <summary>Effective HP regen per real second (base × premium).</summary>
    public static float EffectiveHPRegenPerSecond => BaseHPRegenPerSecond * PremiumRegenMultiplier;
}

}
