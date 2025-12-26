using UnityEngine;
/// <summary>
/// A minimal class storing two actors (supporter, attacker) plus an axis (Vertical or Horizontal).
/// Optionally retains startActor / endActor for convenience, as well as Matches().
/// All older "in-between" logic and lists have been removed.
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

    /// <summary>
    /// Returns whichever actor is "top" (if vertical) or "right" (if horizontal).
    /// This logic is often used to figure out start->end in row/column order.
    /// </summary>
    public ActorInstance startActor
    {
        get
        {
            if (axis == Axis.Vertical)
            {
                return (actor1.location.y > actor2.location.y) ? actor1 : actor2;
            }
            else // horizontal
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
