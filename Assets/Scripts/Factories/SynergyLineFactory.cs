using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
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
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Factories
{
    /// <summary>
    /// SYNERGYLINEFACTORY - Creates synergy connection line GameObjects.
    /// 
    /// PURPOSE:
    /// Creates the root container for synergy lines that connect
    /// allied actors with synergy bonuses.
    /// 
    /// SYNERGY LINES:
    /// Visual representation of team synergy bonuses between allies.
    /// Animated lines with particle strands flowing between actors.
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// SynergyLine (root)
    /// ??? SynergyLineInstance (behavior)
    ///     ??? [Strands added dynamically via SynergyStrandFactory]
    /// ```
    /// 
    /// NOTE:
    /// This factory creates the root container. The SynergyLineInstance
    /// component then spawns SynergyStrand children for the animated
    /// particle effect.
    /// 
    /// CALLED BY:
    /// - SynergyLineManager.Spawn()
    /// 
    /// RELATED FILES:
    /// - SynergyLineInstance.cs: Line behavior component
    /// - SynergyStrandFactory.cs: Creates strand particles
    /// - SynergyLineManager.cs: Manages synergy lines
    /// </summary>
    public static class SynergyLineFactory
    {
        /// <summary>Creates a new synergy line container.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("SynergyLine");
            root.layer = LayerMask.NameToLayer("Default");

            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            root.AddComponent<SynergyLineInstance>();

            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
