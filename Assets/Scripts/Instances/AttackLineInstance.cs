using Scripts.Helpers;
using System;
using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
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

namespace Scripts.Instances
{
    /// <summary>
    /// ATTACKLINEINSTANCE - Visual connection line during pincer attacks.
    /// 
    /// PURPOSE:
    /// Renders a glowing line connecting two heroes during a pincer attack,
    /// showing the attack formation visually.
    /// 
    /// VISUAL EFFECT:
    /// ```
    /// [Hero A] ════════════════ [Hero B]
    ///              ↑
    ///        attack line (glowing)
    /// ```
    /// 
    /// LINE RENDERING:
    /// Uses LineRenderer with calculated corner points based on axis.
    /// Width based on tile size for proper scaling.
    /// 
    /// ANIMATION:
    /// - Fades in when spawned
    /// - Fades out when attack completes
    /// - fadeDuration controls animation speed
    /// 
    /// RELATED FILES:
    /// - AttackLineFactory.cs: Creates line GameObjects
    /// - AttackLineManager.cs: Manages all lines
    /// - PincerAttackSequence.cs: Triggers lines
    /// </summary>
    public class AttackLineInstance : MonoBehaviour
    {
        public Transform parent { get => gameObject.transform.parent; set => gameObject.transform.SetParent(value, true); }
        public Vector3 position { get => gameObject.transform.position; set => gameObject.transform.position = value; }
        public int sortingOrder { get => lineRenderer.sortingOrder; set => lineRenderer.sortingOrder = value; }

        // Fields
        public float alpha;

        [SerializeField] private float fadeDuration = 0.5f;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private float thickness;
        private float maxAlpha;
        private Color baseColor;
        private Color color;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            thickness = g.TileSize * 0.02f;
            alpha = 0f;
            maxAlpha = 1f;
            baseColor = ColorHelper.RGBA(100, 195, 200, 0);
            lineRenderer = gameObject.GetComponent<LineRenderer>();
        }

        private void Start()
        {
            lineRenderer.startWidth = thickness;
            lineRenderer.endWidth = thickness;
        }

        public void Spawn(ActorPair actorPair)
        {
            parent = g.Board.transform;
            name = $"AttackLine_{Guid.NewGuid():N}";

            startPosition = actorPair.startActor.Position;
            endPosition = actorPair.endActor.Position;

            Vector3 ul, ur, lr, ll;
            float offset = g.TileSize / 2;
            Vector3[] points = { };

            if (actorPair.axis == Axis.Vertical)
            {
                ul = new Vector3(endPosition.x - offset, endPosition.y + offset, 0);
                ur = new Vector3(endPosition.x + offset, endPosition.y + offset, 0);
                lr = new Vector3(startPosition.x + offset, startPosition.y - offset, 0);
                ll = new Vector3(startPosition.x - offset, startPosition.y - offset, 0);
                points = new Vector3[] { ul, ur, lr, ll, ul };
            }
            else if (actorPair.axis == Axis.Horizontal)
            {
                ul = new Vector3(startPosition.x - offset, startPosition.y - offset, 0);
                ur = new Vector3(endPosition.x + offset, endPosition.y - offset, 0);
                lr = new Vector3(endPosition.x + offset, endPosition.y + offset, 0);
                ll = new Vector3(startPosition.x - offset, startPosition.y + offset, 0);
                points = new Vector3[] { ul, ur, lr, ll, ul };
            }

            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);

            StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            float startAlpha = 0f;
            float targetAlpha = maxAlpha;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
                SetAlpha(alpha);
                yield return Wait.None();
            }

            alpha = maxAlpha;
            SetAlpha(alpha);
        }

        public void Despawn()
        {
            StartCoroutine(DespawnRoutine());
        }

        public IEnumerator DespawnRoutine()
        {
            //Before:
            float startAlpha = maxAlpha;
            float targetAlpha = 0f;
            float elapsedTime = 0f;

            //During:
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
                SetAlpha(alpha);
                yield return Wait.None();
            }

            //After:
            alpha = 0f;
            SetAlpha(alpha);
            StopAllCoroutines();
        }

        private void SetAlpha(float a)
        {
            color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
