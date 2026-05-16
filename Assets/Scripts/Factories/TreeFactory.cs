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
    /// tree. Adds SpriteRenderer (Tree00 sprite, sortingOrder=30), static
    /// Rigidbody2D, and a CircleCollider2D matching the original prefab. The
    /// caller positions and orients each tree after construction.</para>
    /// <para>CALLED BY: OverworldBuilder (edit-time) and any future runtime
    /// overworld builder.</para>
    /// <para>RELATED FILES: TreeInstance.cs, OverworldBuilder.cs</para>
    /// </summary>
    public static class TreeFactory
    {
        /// <summary>Creates a new Tree GameObject matching the original Tree prefab, optionally parented.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var go = new GameObject("Tree");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetHelper.LoadAsset<Sprite>("Maps/Test/Tree");
            sr.sortingOrder = 30;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;

            go.AddComponent<Scripts.Overworld.TreeInstance>();

            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }
    }
}
