using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Behaviors.Actor
{
    /// <summary>
    /// ACTORVFX - Visual effect references for an actor.
    /// 
    /// PURPOSE:
    /// Holds references to visual effect assets used by
    /// an actor for attacks and abilities.
    /// 
    /// PROPERTIES:
    /// - Attack: VFX played when actor attacks
    /// 
    /// RELATED FILES:
    /// - VisualEffectAsset.cs: VFX data
    /// - VisualEffectManager.cs: VFX spawning
    /// - ActorInstance.cs: Owns VFX data
    /// </summary>
    public class ActorVFX
    {
        public VisualEffectAsset Attack;
    }
}
