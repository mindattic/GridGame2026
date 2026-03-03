using Scripts.Helpers;
using Scripts.Models;
using System.Collections;
using UnityEngine;
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
/// ACTORHEALTHBAR - Manages actor HP bar visualization.
/// 
/// PURPOSE:
/// Controls the health bar UI element for an actor, handling
/// fill levels, drain animations, and HP text display.
/// 
/// VISUAL COMPONENTS:
/// - healthBarBack: Background bar (full size)
/// - healthBarFill: Current HP (green/blue)
/// - healthBarDrain: Damage animation (red, delayed shrink)
/// - healthBarText: "HP/MaxHP" display
/// 
/// DRAIN ANIMATION:
/// When damage is taken, the drain bar shows previous HP,
/// then smoothly shrinks to match current HP.
/// 
/// RELATED FILES:
/// - ActorRenderers.cs: Provides bar renderers
/// - ActorStats.cs: HP/MaxHP values
/// </summary>
public class ActorHealthBar
{
    private ActorInstance instance;
    public bool isDraining;
    protected ActorRenderers render => instance.Render;
    protected ActorStats stats => instance.Stats;

    public bool isEmpty => !isDraining && stats.PreviousHP < 1;

    /// <summary>Initializes initialize.</summary>
    public void Initialize(ActorInstance parentInstance)
    {
        this.instance = parentInstance;
    }

    private Vector3 initialScale => render.healthBarBack.transform.localScale;

    /// <summary>Gets the scale.</summary>
    private Vector3 GetScale(float value)
    {
        var x = Mathf.Clamp(initialScale.x * (value / stats.MaxHP), 0f, initialScale.x);
        return new Vector3(x, initialScale.y, initialScale.z);
    }

    /// <summary>Runs per-frame update logic.</summary>
    public void Update()
    {
        render.healthBarDrain.transform.localScale = GetScale(stats.PreviousHP);
        render.healthBarFill.transform.localScale = GetScale(stats.HP);
        render.healthBarText.text = $@"{stats.HP}/{stats.MaxHP}";

        if (instance.IsActive)
            instance.StartCoroutine(DrainRoutine());
    }

    /// <summary>Coroutine that executes the drain sequence.</summary>
    private IEnumerator DrainRoutine()
    {
        Vector3 scale;
        isDraining = true;

        yield return Wait.For(Intermission.Before.HealthBar.Drain);
        while (stats.HP < stats.PreviousHP)
        {
            stats.PreviousHP -= Increment.HealthBar.Drain;
            stats.PreviousHP = Mathf.Clamp(stats.PreviousHP, stats.HP, stats.MaxHP);
            scale = GetScale(stats.PreviousHP);
            render.healthBarDrain.transform.localScale = scale;
            yield return Wait.None();
        }

        //After:
        stats.PreviousHP = stats.HP;
        scale = GetScale(stats.PreviousHP);
        render.healthBarDrain.transform.localScale = scale;
        isDraining = false;
    }


}

}
