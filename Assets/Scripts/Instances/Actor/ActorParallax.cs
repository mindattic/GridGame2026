using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Instances.Actor
{
    public class ActorParallax
    {

        //Fields
        private ActorInstance instance;
        public float maxFocus = 0f;
        public float targetX = 0f;
        public float targetY = 0f;
        public Direction attackerDirection = Direction.None;
        public float transitionDuration = 2f;

        public void Initialize(ActorInstance parentInstance)
        {
            this.instance = parentInstance;

            maxFocus = g.TileSize * 10f;
        }

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
