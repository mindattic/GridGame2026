using System;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Overworld
{
/// <summary>
/// CARAVANINSTANCE - Caravan world object in overworld.
///
/// PURPOSE:
/// Stationary sprite with a circular trigger collider. When the
/// OverworldHero enters the radius, fires OnHeroNearby(true);
/// when the hero exits, fires OnHeroNearby(false).
/// OverworldManager listens to this event to show/hide the
/// "Enter Hub" button on the canvas.
///
/// SCENE SETUP:
/// Place under Map/ as "Caravan". Requires:
/// - SpriteRenderer (caravan art)
/// - CircleCollider2D (isTrigger = true, radius = proximity range)
/// - CaravanInstance (this script)
///
/// POSITION PERSISTENCE:
/// Position is saved/restored via OverworldSaveData.CaravanX/Y.
///
/// RELATED FILES:
/// - OverworldManager.cs: Subscribes to OnHeroNearby
/// - OverworldHero.cs: Has Rigidbody2D that triggers callbacks
/// - GameObjectHelper.Overworld.Map.Caravan: Path constant
/// - OverworldSaveData: CaravanX, CaravanY fields
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class CaravanInstance : MonoBehaviour
{
    /// <summary>Fired when the hero enters (true) or exits (false) the proximity trigger.</summary>
    public event Action<bool> OnHeroNearby;

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    /// <summary>Called when another collider enters the trigger zone.</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.GetComponentInParent<OverworldHero>() != null)
            OnHeroNearby?.Invoke(true);
    }

    /// <summary>Called when another collider exits the trigger zone.</summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        if (other.GetComponentInParent<OverworldHero>() != null)
            OnHeroNearby?.Invoke(false);
    }
}

}
