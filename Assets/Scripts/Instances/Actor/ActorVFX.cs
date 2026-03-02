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
