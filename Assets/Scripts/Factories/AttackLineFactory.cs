using Game.Instances;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for AttackLine - replaces AttackLinePrefab.prefab
    /// </summary>
    public static class AttackLineFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("AttackLine");
            root.layer = 0; // Default layer
            root.tag = "AttackLine";

            // Transform
            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // LineRenderer
            var lineRenderer = root.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.allowOcclusionWhenDynamic = true;
            lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            lineRenderer.lightProbeUsage = LightProbeUsage.Off;
            lineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // Material - Unity's built-in Default-Line material
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Sorting
            lineRenderer.sortingLayerName = "Default"; // Use Default sorting layer
            lineRenderer.sortingOrder = 500; // VFX layer (above Actors, below CombatText)

            // Positions
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { Vector3.zero, new Vector3(0, 0, 1) });

            // Width
            lineRenderer.widthMultiplier = 1f;
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.515152f)
            );

            // Color gradient - cyan-ish with low alpha
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.39215687f, 0.76409096f, 0.78431374f), 0f),
                    new GradientColorKey(new Color(0.39215687f, 0.7647059f, 0.78431374f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.2509804f, 0f),
                    new GradientAlphaKey(0.2509804f, 1f)
                }
            );
            lineRenderer.colorGradient = gradient;

            // Other LineRenderer settings
            lineRenderer.numCornerVertices = 0;
            lineRenderer.numCapVertices = 0;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.textureScale = Vector2.one;
            lineRenderer.shadowBias = 0.5f;
            lineRenderer.generateLightingData = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;

            // AttackLineInstance (custom component)
            var attackLine = root.AddComponent<AttackLineInstance>();
            attackLine.alpha = 0f;

            // SortingGroup
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "Board"; // SortingLayer 2
            sortingGroup.sortingOrder = 0;

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
