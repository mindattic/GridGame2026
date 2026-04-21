using Scripts.Helpers;
using Scripts.Libraries;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
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
    /// PROJECTILEFACTORY - Creates the transform node used by ProjectileManager.
    /// <para>PURPOSE: Single programmatic entry point for a projectile's root
    /// GameObject ("ProjectileNode"). Trail/impact VFX and motion are attached by
    /// the caller — this factory only produces the bare transform carrier.</para>
    /// <para>CALLED BY: ProjectileManager.SpawnProjectileRoutine.</para>
    /// <para>RELATED FILES: ProjectileManager.cs, ProjectileNode.cs</para>
    /// </summary>
    public static class ProjectileFactory
    {
        /// <summary>Creates a ProjectileNode GameObject at the given world position, parented to the given transform.</summary>
        public static GameObject Create(Transform parent, Vector3 worldPosition)
        {
            var go = new GameObject("ProjectileNode");
            go.transform.position = worldPosition;
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }
    }
}
