using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    /// <summary>
    /// ACTORMANAGER - Manages all actor instances on the battlefield.
    /// 
    /// PURPOSE:
    /// Central manager for actor lifecycle and utility operations.
    /// Provides helper methods for actor management.
    /// 
    /// ACTOR COLLECTIONS (via g.Actors.*):
    /// - All: All ActorInstance objects
    /// - Heroes: Actors with team == Team.Hero
    /// - Enemies: Actors with team == Team.Enemy
    /// - SelectedActor: Currently selected hero
    /// - MovingHero: Hero being dragged
    /// 
    /// KEY METHODS:
    /// - Clear(): Destroys all actors (scene cleanup)
    /// - CheckEnemyAP(): Fills AP for enemies ready to act
    /// 
    /// SNAP THRESHOLD:
    /// Used for position snapping during drag operations.
    /// Calculated as 12.5% of tile size.
    /// 
    /// RELATED FILES:
    /// - ActorInstance.cs: Individual actor component
    /// - HeroManager.cs: Hero-specific filtering
    /// - EnemyManager.cs: Enemy-specific filtering
    /// - StageManager.cs: Actor spawning
    /// - SelectionManager.cs: Actor selection
    /// 
    /// ACCESS: g.ActorManager
    /// COLLECTIONS: g.Actors.All, g.Actors.Heroes, g.Actors.Enemies
    /// </summary>
    public class ActorManager : MonoBehaviour
    {
        /// <summary>Distance threshold for position snapping during drag.</summary>
        public float snapTheshold;

        /// <summary>Initializes component references and state.</summary>
        private void Awake()
        {
            snapTheshold = g.TileSize * 0.125f * 1.01f;
        }

        /// <summary>
        /// Fills AP for all playing enemies that don't have max AP.
        /// Called at turn transitions to charge enemy abilities.
        /// </summary>
        public void CheckEnemyAP()
        {
            var enemies = g.Actors.Enemies.Where(x => x.IsPlaying && !x.HasMaxAP).ToList();
            enemies.ForEach(x => x.ActionBar.Fill());
        }

        /// <summary>
        /// Destroys all actors and clears the collection.
        /// Used during scene cleanup and stage restart.
        /// </summary>
        public void Clear()
        {
            if (g.Actors.All != null && g.Actors.All.Count > 0)
            {
                foreach (var actor in g.Actors.All)
                {
                    Destroy(actor.gameObject);
                }
                g.Actors.All.Clear();
            }
        }
    }
}
