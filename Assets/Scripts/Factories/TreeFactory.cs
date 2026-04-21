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
    /// TREEFACTORY - Creates an Overworld Tree GameObject.
    /// <para>PURPOSE: Single programmatic entry point for an overworld decoration
    /// tree (TreeInstance behaviour on a bare GameObject). The caller positions
    /// and orients each tree after construction.</para>
    /// <para>CALLED BY: OverworldScaffold (edit-time) and any future runtime
    /// overworld builder.</para>
    /// <para>RELATED FILES: TreeInstance.cs, OverworldScaffold.cs</para>
    /// </summary>
    public static class TreeFactory
    {
        /// <summary>Creates a new Tree GameObject with its TreeInstance component, optionally parented.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var go = new GameObject("Tree");
            go.AddComponent<Scripts.Overworld.TreeInstance>();
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }
    }
}
