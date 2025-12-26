using Assets.Helper;
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Instances.Actor
{
    public class ActorGlow
    {
        protected ActorRenderers Render => instance.Render;
        private bool IsPlayer => instance.IsHero;
        private bool IsEnemy => instance.IsEnemy;
        protected AnimationCurve glowCurve => instance.glowCurve;

        private ActorInstance instance;
        private Vector3 baseScale;
        private float maxScale;   // 1.1f target
        private float speed;
        private Coroutine glowRoutineRef;

        public void Initialize(ActorInstance parentInstance)
        {
            this.instance = parentInstance;
            baseScale = g.TileScale;
            maxScale = 1.25f;
            speed = 2.0f;
        }

        public bool IsGlowing = false;
        //private bool IsGlowing =>
        //    instance.IsPlaying && ((g.TurnManager.IsHeroTurn && IsPlayer) || (g.TurnManager.IsEnemyTurn && IsEnemy));

        public void Play()
        {
            if (!instance.IsActive) return;
            if (glowRoutineRef != null) instance.StopCoroutine(glowRoutineRef);
            IsGlowing = true;
            glowRoutineRef = instance.StartCoroutine(GlowRoutine());
        }

        public void Stop()
        {
            IsGlowing = false;
        }

        public IEnumerator GlowRoutine()
        {
            // Ensure starting scale at 1
            Render.SetGlowScale(baseScale);

            // Warm up to 1.1
            float warm = 0.15f;
            float t = 0f;
            while (t < warm)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / warm);
                float s = Mathf.Lerp(1f, maxScale, k);
                Render.SetGlowScale(new Vector3(s, s, 1f));
                yield return Wait.OneTick();
            }

            // Pulse while glowing
            while (IsGlowing)
            {
                float curve = glowCurve != null && glowCurve.length > 0 ? glowCurve.Evaluate(Time.time * speed % glowCurve.length) : Mathf.Sin(Time.time * speed) * 0.05f;
                float s = maxScale + curve * 0.05f; // subtle +/- around 1.1
                s = Mathf.Clamp(s, 1f, maxScale);
                Render.SetGlowScale(new Vector3(s, s, 1f));
                yield return Wait.OneTick();
            }

            // Cooldown back to 1.0
            float cool = 0.15f; t = 0f;
            while (t < cool)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / cool);
                float s = Mathf.Lerp(maxScale, 1f, k);
                Render.SetGlowScale(new Vector3(s, s, 1f));
                yield return Wait.OneTick();
            }

            Render.SetGlowScale(baseScale);
            glowRoutineRef = null;
        }
    }
}
