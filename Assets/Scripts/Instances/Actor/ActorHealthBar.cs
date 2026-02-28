using Assets.Helper;
using Assets.Scripts.Models;
using System.Collections;
using UnityEngine;

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

    public void Initialize(ActorInstance parentInstance)
    {
        this.instance = parentInstance;
    }

    private Vector3 initialScale => render.healthBarBack.transform.localScale;

    private Vector3 GetScale(float value)
    {
        var x = Mathf.Clamp(initialScale.x * (value / stats.MaxHP), 0f, initialScale.x);
        return new Vector3(x, initialScale.y, initialScale.z);
    }

    public void Update()
    {
        render.healthBarDrain.transform.localScale = GetScale(stats.PreviousHP);
        render.healthBarFill.transform.localScale = GetScale(stats.HP);
        render.healthBarText.text = $@"{stats.HP}/{stats.MaxHP}";

        if (instance.IsActive)
            instance.StartCoroutine(DrainRoutine());
    }

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
