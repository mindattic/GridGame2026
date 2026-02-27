using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for SupportLine - replaces SupportLinePrefab.prefab
    /// </summary>
    public static class SupportLineFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("SupportLine");
            root.layer = 0; // Default layer
            root.tag = "SupportLine";

            // Transform
            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // LineRenderer
            var lineRenderer = root.AddComponent<LineRenderer>();
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            lineRenderer.lightProbeUsage = LightProbeUsage.Off;
            lineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            lineRenderer.sortingLayerName = "Lines"; // SortingLayer 4
            lineRenderer.sortingOrder = 0;

            // Material - use default Line material
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Line settings
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0, 0, 1));
            lineRenderer.widthMultiplier = 1f;
            lineRenderer.useWorldSpace = true;

            // Width curve (constant width)
            lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 0.515152f);

            // SupportLineInstance (custom component)
            root.AddComponent<SupportLineInstance>();

            // SortingGroup
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "Lines";
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
