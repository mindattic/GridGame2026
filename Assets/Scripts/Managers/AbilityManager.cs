using System.Collections.Generic;
using UnityEngine;
using Scripts.Helpers;
using g = Scripts.Helpers.GameHelper;
using Scripts.Sequences;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    /// <summary>
    /// ABILITYMANAGER - Shared magic-effect lookup for the legacy <see cref="Ability"/> model.
    ///
    /// <para>PURPOSE: <see cref="TryGetMagicEffect"/> maps an offensive-magic
    /// <see cref="AbilityEffect"/> to its damage element and impact VFX key. Enemy charge-casts
    /// (US-026, <c>EnemyChargeSequence</c>) resolve through it. Hero abilities are cast from the
    /// Row-13 <see cref="Scripts.Canvas.AbilityBar"/> via <c>TargetingMode</c> and
    /// <c>SpellEffectDispatcher</c>, not through this component.</para>
    ///
    /// ACCESS: g.AbilityManager (component on the Game root); the lookup is static.
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        /// <summary>Maps an offensive-magic <see cref="AbilityEffect"/> to its damage element + impact
        /// VFX key. Returns false for effects that are not direct-damage magic.</summary>
        public static bool TryGetMagicEffect(AbilityEffect effect, out ElementalDamageType element, out string vfxKey)
        {
            switch (effect)
            {
                case AbilityEffect.Fire:     element = ElementalDamageType.Fire;      vfxKey = "Flame";              return true;
                case AbilityEffect.Fireball: element = ElementalDamageType.Fire;      vfxKey = "Fireball";           return true;
                case AbilityEffect.Fira:     element = ElementalDamageType.Fire;      vfxKey = "FireRain";           return true;
                case AbilityEffect.Ice:      element = ElementalDamageType.Ice;       vfxKey = "IceSparkle";         return true;
                case AbilityEffect.Thunder:  element = ElementalDamageType.Lightning; vfxKey = "LightningExplosion"; return true;
                case AbilityEffect.Smite:    element = ElementalDamageType.Light;     vfxKey = "GodRays";            return true;
                default:                     element = ElementalDamageType.Arcane;    vfxKey = null;                 return false;
            }
        }
    }
}
