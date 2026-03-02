using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
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

namespace Scripts.Instances.Actor
{
    /// <summary>
    /// ACTORFLAGS - Boolean state flags for an actor.
    /// 
    /// PURPOSE:
    /// Tracks transient state flags that indicate what action
    /// an actor is currently performing or what conditions apply.
    /// 
    /// STATE FLAGS:
    /// ```
    /// Movement States:
    /// - IsMoving: Actor is being dragged/moved
    /// - IsSwapping: Actor is exchanging position with another
    /// 
    /// Combat States:
    /// - IsAttacking: Actor is executing an attack
    /// - IsDefending: Actor is in defensive stance
    /// - IsSupporting: Actor is providing support in pincer
    /// 
    /// Other States:
    /// - IsGainingAP: Action bar is filling
    /// - IsRedirecting: Movement is being redirected
    /// - HasSpawned: Actor has completed spawn animation
    /// ```
    /// 
    /// STATUS EFFECTS:
    /// - RootedTurnsRemaining: Turns until root effect expires
    /// - RootedVfxInstanceName: VFX to despawn when root ends
    /// 
    /// USAGE:
    /// ```csharp
    /// if (actor.Flags.IsMoving) { /* handle movement */ }
    /// actor.Flags.IsAttacking = true;
    /// ```
    /// 
    /// RELATED FILES:
    /// - ActorInstance.cs: Owns the Flags component
    /// - InputManager.cs: Sets movement flags
    /// - PincerAttackSequence.cs: Sets combat flags
    /// </summary>
    public class ActorFlags
    {
        #region Movement Flags

        /// <summary>Actor is currently being moved/dragged.</summary>
        public bool IsMoving;

        /// <summary>Actor is swapping positions with another actor.</summary>
        public bool IsSwapping;

        #endregion

        #region Combat Flags

        /// <summary>Actor is executing an attack.</summary>
        public bool IsAttacking;

        /// <summary>Actor is in defensive stance.</summary>
        public bool IsDefending;

        /// <summary>Actor is providing support in a pincer attack.</summary>
        public bool IsSupporting;

        #endregion

        #region Other Flags

        /// <summary>Actor's action bar is currently filling.</summary>
        public bool isGainingAP;

        /// <summary>Actor's movement is being redirected.</summary>
        public bool IsRedirecting;

        /// <summary>Actor has completed spawn animation.</summary>
        public bool HasSpawned;

        #endregion

        #region Status Effects

        /// <summary>Number of turns the actor is rooted (cannot move).</summary>
        public int RootedTurnsRemaining;

        /// <summary>VFX instance name for root effect (for cleanup).</summary>
        public string RootedVfxInstanceName;

        #endregion
    }
}
