using System;
using System.Collections.Generic;
using Scripts.Helpers;
using Scripts.Models;
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
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>Types of sorting requests.</summary>
public enum SortEventType
{
    Default,
    Focus,
    Drag,
    LocationChanged,
    Drop,
    ActorMoving,
    Overlap,
    PincerAttack,
    Bump
}

/// <summary>
/// SORTEVENT - Context for sorting request.
/// 
/// Carries all data needed by actors to decide their sorting order.
/// </summary>
public class SortEvent
{
    public SortEventType Type;
    public ActorInstance Initiator;
    public ActorInstance Target;
    public Vector2Int? NewLocation;
    public PincerAttackParticipants Participants;
}

/// <summary>
/// SORTINGMANAGER - Global actor sprite sorting system.
/// 
/// PURPOSE:
/// Manages z-ordering and sorting layers for actors. Uses an event-based
/// system where actors subscribe and handle their own sorting updates.
/// 
/// EVENT TYPES:
/// - Default: Reset to standard sorting
/// - Focus: Bring actor to front (selected)
/// - Drag: Actor being dragged
/// - LocationChanged: Actor moved to new tile
/// - Drop: Actor dropped after drag
/// - ActorMoving: Actor in motion
/// - Overlap: Actors overlapping positions
/// - PincerAttack: Pincer attack in progress
/// - Bump: Actor bumping animation
/// 
/// USAGE:
/// ```csharp
/// g.SortingManager.OnActorFocus(hero);
/// g.SortingManager.OnActorDrop(hero);
/// ```
/// 
/// SUBSCRIPTION:
/// Actors subscribe to OnSortRequested in Awake and handle sorting in callback.
/// 
/// RELATED FILES:
/// - ActorRenderers.cs: Handles sorting for individual actors
/// - SelectionManager.cs: Triggers focus/drag/drop events
/// 
/// ACCESS: g.SortingManager
/// </summary>
public class SortingManager : MonoBehaviour
{
    /// <summary>Global event actors subscribe to for sorting updates.</summary>
    public static event Action<SortEvent> OnSortRequested;

    /// <summary>Invokes the sorting event.</summary>
    private void Invoke(SortEvent e)
    {
        OnSortRequested?.Invoke(e);
    }

    /// <summary>Handles the actor focus event.</summary>
    public void OnActorFocus()
    {
        if (!g.Actors.HasSelectedActor) return;
        Invoke(new SortEvent
        {
            Type = SortEventType.Focus,
            Initiator = g.Actors.SelectedActor
        });
    }

    /// <summary>Handles the hero drag event.</summary>
    public void OnHeroDrag()
    {
        if (!g.Actors.HasSelectedActor) return;
        Invoke(new SortEvent
        {
            Type = SortEventType.Drag,
            Initiator = g.Actors.SelectedActor
        });
    }

    /// <summary>Handles the selected hero location changed event.</summary>
    public void OnSelectedHeroLocationChanged(Vector2Int newLocation)
    {
        if (!g.Actors.HasSelectedActor) return;
        Invoke(new SortEvent
        {
            Type = SortEventType.LocationChanged,
            Initiator = g.Actors.SelectedActor,
            NewLocation = newLocation
        });
    }

    /// <summary>Handles the selected hero drop event.</summary>
    public void OnSelectedHeroDrop()
    {
        if (!g.Actors.HasSelectedActor) return;
        Invoke(new SortEvent
        {
            Type = SortEventType.Drop,
            Initiator = g.Actors.SelectedActor
        });
    }

    /// <summary>Handles the actor moving event.</summary>
    public void OnActorMoving(ActorInstance actor)
    {
        Invoke(new SortEvent
        {
            Type = SortEventType.ActorMoving,
            Initiator = actor
        });
    }

    /// <summary>Handles the actor overlap event.</summary>
    public void OnActorOverlap(ActorInstance initiator, ActorInstance target)
    {
        Invoke(new SortEvent
        {
            Type = SortEventType.Overlap,
            Initiator = initiator,
            Target = target
        });
    }

    /// <summary>Handles the pincer attack event.</summary>
    public void OnPincerAttack(PincerAttackParticipants participants)
    {
        Invoke(new SortEvent
        {
            Type = SortEventType.PincerAttack,
            Participants = participants
        });
    }

    /// <summary>Handles the bump event.</summary>
    public void OnBump(ActorInstance initiator, ActorInstance target)
    {
        Invoke(new SortEvent
        {
            Type = SortEventType.Bump,
            Initiator = initiator,
            Target = target
        });
    }

    // These two rely on existing direct layering logic:
    /// <summary>Handles the support line spawn event.</summary>
    public void OnSupportLineSpawn(SupportLineInstance supportLine)
    {
        var isAbove = supportLine.supporter.SortingGroup.sortingLayerName == SortingHelper.Layer.ActorAbove;
        supportLine.SetSorting(isAbove ? SortingHelper.Layer.SupportLineAbove : SortingHelper.Layer.SupportLineBelow);
    }

    /// <summary>
    /// Ensure SynergyLine uses the correct layer relative to actors at spawn.
    /// </summary>
    public void OnSynergyLineSpawn(SynergyLineInstance synergyLineInstance)
    {
        var supporterLayer = synergyLineInstance.supporter != null && synergyLineInstance.supporter.SortingGroup != null
            ? synergyLineInstance.supporter.SortingGroup.sortingLayerName
            : string.Empty;
        var attackerLayer = synergyLineInstance.attacker != null && synergyLineInstance.attacker.SortingGroup != null
            ? synergyLineInstance.attacker.SortingGroup.sortingLayerName
            : string.Empty;

        bool anyBelow = supporterLayer == SortingHelper.Layer.ActorBelow || attackerLayer == SortingHelper.Layer.ActorBelow;
        var layer = anyBelow ? SortingHelper.Layer.SupportLineBelow : SortingHelper.Layer.SupportLineAbove;
        synergyLineInstance.SetSorting(layer);
    }

    /// <summary>Handles the portrait pop in event.</summary>
    public void OnPortraitPopIn(PortraitInstance portrait)
    {
        portrait.SetSorting(SortingHelper.Layer.PortraitPopIn, SortingHelper.Order.Max);
    }
}

}
