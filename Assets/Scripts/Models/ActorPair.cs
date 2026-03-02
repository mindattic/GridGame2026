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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
/// <summary>
/// ACTORPAIR - Two actors with axis relationship.
/// 
/// PURPOSE:
/// Stores two actors (e.g., supporter + attacker) with their
/// relative axis (horizontal or vertical alignment).
/// 
/// AXIS DETECTION:
/// Automatically determines if actors are aligned
/// horizontally or vertically based on position.
/// 
/// ACCESSORS:
/// - startActor: Top (vertical) or right (horizontal) actor
/// - endActor: Bottom (vertical) or left (horizontal) actor
/// 
/// USAGE:
/// Used for support line calculations and pincer detection.
/// 
/// RELATED FILES:
/// - SupportLineManager.cs: Creates support lines
/// - PincerAttackManager.cs: Pincer detection
/// </summary>
public class ActorPair
{
    public ActorInstance actor1;
    public ActorInstance actor2;
    public Axis axis = Axis.Horizontal;

    public ActorPair(ActorInstance actor1, ActorInstance actor2)
    {
        this.actor1 = actor1;
        this.actor2 = actor2;

        float diffX = Mathf.Abs(actor1.location.x - actor2.location.x);
        float diffY = Mathf.Abs(actor1.location.y - actor2.location.y);
        this.axis = diffX > diffY ? Axis.Horizontal : Axis.Vertical;
    }

    public ActorPair(ActorInstance actor1, ActorInstance actor2, Axis axis)
    {
        this.actor1 = actor1;
        this.actor2 = actor2;
        this.axis = axis;
    }

    /// <summary>Returns top (vertical) or right (horizontal) actor.</summary>
    public ActorInstance startActor
    {
        get
        {
            if (axis == Axis.Vertical)
            {
                return (actor1.location.y > actor2.location.y) ? actor1 : actor2;
            }
            else
            {
                return (actor1.location.x > actor2.location.x) ? actor1 : actor2;
            }
        }
    }

    /// <summary>
    /// Returns whichever actor is "bottom" (if vertical) or "left" (if horizontal).
    /// </summary>
    public ActorInstance endActor
    {
        get
        {
            if (axis == Axis.Vertical)
            {
                return (actor1.location.y < actor2.location.y) ? actor1 : actor2;
            }
            else // horizontal
            {
                return (actor1.location.x < actor2.location.x) ? actor1 : actor2;
            }
        }
    }

    /// <summary>
    /// Returns true if the two actors match this pair's supporter and attacker in either order.
    /// </summary>
    public bool Matches(ActorInstance a1, ActorInstance a2)
    {
        return (actor1 == a1 && actor2 == a2) || (actor1 == a2 && actor2 == a1);
    }
}

}
