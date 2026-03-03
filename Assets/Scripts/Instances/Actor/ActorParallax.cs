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
    /// ACTORPARALLAX - Pseudo-3D depth effect for actor sprites.
    /// 
    /// PURPOSE:
    /// Creates a parallax layering effect that gives actors
    /// a sense of depth, especially when being attacked.
    /// 
    /// EFFECT:
    /// When an attacker approaches from a direction, the parallax
    /// layer shifts to create an illusion of depth/dimension.
    /// 
    /// DIRECTION MAPPING:
    /// - North: Shift up (attacker above)
    /// - East: Shift right (attacker to right)
    /// - South: Shift down (attacker below)
    /// - West: Shift left (attacker to left)
    /// 
    /// RELATED FILES:
    /// - ActorRenderers.cs: Applies parallax transform
    /// - MaterialLibrary.cs: Parallax shader materials
    /// </summary>
    public class ActorParallax
    {
        private ActorInstance instance;
        public float maxFocus = 0f;
        public float targetX = 0f;
        public float targetY = 0f;
        public Direction attackerDirection = Direction.None;
        public float transitionDuration = 2f;

        /// <summary>Initializes initialize.</summary>
        public void Initialize(ActorInstance parentInstance)
        {
            this.instance = parentInstance;
            maxFocus = g.TileSize * 10f;
        }

        /// <summary>Assign.</summary>
        public void Assign(Direction attackerDirection)
        {
            this.attackerDirection = attackerDirection;

            switch (attackerDirection)
            {
                case Direction.North:
                    targetX = 0f;
                    targetY = maxFocus;
                    break;
                case Direction.East:
                    targetX = maxFocus;
                    targetY = 0f;
                    break;
                case Direction.South:
                    targetX = 0f;
                    targetY = -maxFocus;
                    break;
                case Direction.West:
                    targetX = -maxFocus;
                    targetY = 0f;
                    break;
                case Direction.None:
                    targetX = 1f;
                    targetY = 1f;
                    break;
            }

            instance.Render.parallax.material.SetFloat("_XScroll", targetX);
            instance.Render.parallax.material.SetFloat("_YScroll", targetY);
        }



    }

}
