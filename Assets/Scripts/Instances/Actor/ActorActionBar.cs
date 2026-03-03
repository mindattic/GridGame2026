using Scripts.Helpers;
using Scripts.Instances.Actor;
using Scripts.Models;
using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances.Actor
{
/// <summary>
/// ACTORACTIONBAR - Manages actor AP (action points) bar visualization.
/// 
/// PURPOSE:
/// Controls the action bar UI element for an actor, handling
/// fill levels, drain animations, and AP text display.
/// 
/// VISUAL COMPONENTS:
/// - actionBarBack: Background bar (full size)
/// - actionBarFill: Current AP (yellow)
/// - actionBarDrain: AP spend animation
/// - actionBarText: "AP/MaxAP" display
/// 
/// AP USAGE:
/// AP is spent when actors take actions. Bar updates to reflect
/// remaining action points for the turn.
/// 
/// RELATED FILES:
/// - ActorRenderers.cs: Provides bar renderers
/// - ActorStats.cs: AP/MaxAP values
/// </summary>
public class ActorActionBar
{
    protected ActorFlags flags => instance.Flags;
    protected ActorRenderers render => instance.Render;
    protected ActorStats stats => instance.Stats;

    private Vector3 initialScale => render.actionBarBack.transform.localScale;
    private ActorInstance instance;

    /// <summary>Initializes initialize.</summary>
    public void Initialize(ActorInstance parentInstance)
    {
        this.instance = parentInstance;
    }

    /// <summary>Gets the scale.</summary>
    private Vector3 GetScale(float value)
    {
        return new Vector3(
            Mathf.Clamp(initialScale.x * (value / stats.MaxAP), 0, initialScale.x),
            initialScale.y,
            initialScale.z);
    }

    /// <summary>Runs per-frame update logic.</summary>
    public void Update()
    {
        render.actionBarDrain.transform.localScale = GetScale(stats.PreviousAP);
        render.actionBarFill.transform.localScale = GetScale(stats.AP);
        render.actionBarText.text = $@"{stats.AP}/{stats.MaxAP}";

        Drain();
    }

    // Drain starts the drain coroutine to Animation the reduction of the drain fill,
    // but only if the actor actors is active.
    /// <summary>Drain.</summary>
    private void Drain()
    {
        if (instance.IsActive)
            instance.StartCoroutine(DrainRoutine());
    }

    // DrainRoutine is a coroutine that gradually reduces the displayed AP on the drain fill until it matches the CurrentProfile AP.
    // It waits for a brief interval before starting, then decreases Stats.PreviousAP in increments,
    // updating the scale of the drain fill each tick.
    /// <summary>Coroutine that executes the drain sequence.</summary>
    private IEnumerator DrainRoutine()
    {
        // Abort if no drain is required (i.e., CurrentProfile AP equals previous AP).
        if (stats.PreviousAP == stats.AP)
            yield break;

        // Local variable to hold the computed scale.
        Vector3 scale;

        // Wait for a pre-defined delay before beginning the drain Animation.
        yield return Wait.For(Intermission.Before.ActionBar.Drain);

        // Gradually decrease PreviousAP until it matches the CurrentProfile AP.
        while (stats.AP < stats.PreviousAP)
        {
            stats.PreviousAP -= Increment.ActionBar.Drain;
            scale = GetScale(stats.PreviousAP);
            render.actionBarDrain.transform.localScale = scale;
            yield return Wait.OneTick();
        }

        // After draining, synchronize PreviousAP with the CurrentProfile AP and update the health fill drain element.
        stats.PreviousAP = stats.AP;
        scale = GetScale(stats.PreviousAP);
        render.healthBarDrain.transform.localScale = scale;
    }

    // Fill starts the coroutine that fills the Animation fill (increasing AP) if conditions are met.
    /// <summary>Fill.</summary>
    public void Fill()
    {
        if (instance.IsActive)
            instance.StartCoroutine(FillRoutine());
    }

    // FillRoutine is a coroutine that incrementally increases the actor's AP based on its Intelligence stat.
    // It continues to increase AP until the actor reaches max AP or one of the abort conditions occurs.
    /// <summary>Coroutine that executes the fill sequence.</summary>
    private IEnumerator FillRoutine()
    {
        // Abort the fill process if:
        // - The attacker is stunned,
        // - No hero is selected,
        // - The actor is not an attacker,
        // - The actor is not playing,
        // - The actor already has max AP, or
        // - The actor is currently gaining AP.
        if (g.DebugManager.isEnemyStunned || !g.Actors.HasMovingHero|| !instance.IsEnemy || !instance.IsPlaying || instance.HasMaxAP || flags.isGainingAP)
            yield break;

        // Before starting, mark that the actor is gaining AP and calculate the increment amount.
        flags.isGainingAP = true;
        float amount = stats.Intelligence * 0.1f;

        // During: Gradually increase AP until max AP is reached.
        while (g.Actors.HasMovingHero && instance.IsEnemy && instance.IsPlaying && !instance.HasMaxAP)
        {
            stats.AP += amount;
            stats.AP = Mathf.Clamp(stats.AP, 0, stats.MaxAP);
            stats.PreviousAP = stats.AP;
            Update();
            yield return Wait.OneTick();
        }

        // After: Finalize the AP values and update the UI.
        stats.PreviousAP = stats.AP;
        Update();
        flags.isGainingAP = false;
    }

    // Reset sets the actor's AP values to zero and refreshes the Animation fill UI.
    /// <summary>Resets component to default values (editor only).</summary>
    public void Reset()
    {
        stats.AP = 0;
        stats.PreviousAP = 0;
        Update();
    }

    // AddInitiative provides a small initial AP value based on the actor's Intelligence stat.
    // This is used to seed the initiative system, allowing for a randomized start.
    /// <summary>Add initiative.</summary>
    public void AddInitiative()
    {
        // TODO: Consider incorporating Stats.Luck for more nuanced randomization.
        float amount = stats.Intelligence * 0.01f;
        stats.AP = amount;
        stats.PreviousAP = amount;
        Update();
    }
}

}
