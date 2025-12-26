using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Behaviors.Actor
{
    public class ActorFlags
    {
        public bool IsMoving;
        public bool IsSwapping;
        public bool IsAttacking;
        public bool IsDefending;
        public bool IsSupporting;
        public bool isGainingAP;
        public bool IsRedirecting;
        public bool HasSpawned;

        // Number of turns the actor is rooted (cannot move). Decrement after their turn starts.
        public int RootedTurnsRemaining;

        // Name of the looping VFX instance (if any) applied while rooted, so we can despawn when root ends.
        public string RootedVfxInstanceName;
    }
}
