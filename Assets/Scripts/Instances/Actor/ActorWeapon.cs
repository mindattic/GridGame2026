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
    /// ACTORWEAPON - Equipped weapon data for an actor.
    /// 
    /// PURPOSE:
    /// Holds weapon information for an actor including type,
    /// stats, and display information.
    /// 
    /// PROPERTIES:
    /// - Type: Weapon category (WeaponType enum)
    /// - Name: Display name
    /// - Description: Tooltip text
    /// - Attack: Damage bonus
    /// - Defense: Defensive bonus
    /// 
    /// RELATED FILES:
    /// - WeaponType.cs: Weapon category enum
    /// - ActorInstance.cs: Owns weapon data
    /// - ItemDefinition.cs: Equipment item data
    /// </summary>
    public class ActorWeapon
    {
        public WeaponType Type;
        public string Name; 
        public string Description;
        public float Attack;
        public float Defense;
    }
}
