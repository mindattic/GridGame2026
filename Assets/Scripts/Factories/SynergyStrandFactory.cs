using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for SynergyStrand - replaces SynergyStrandPrefab.prefab
    /// </summary>
    public static class SynergyStrandFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("SynergyStrand");
            root.layer = LayerMask.NameToLayer("Default"); // Layer 0

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
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 200;

            // Material - use default sprites material (can be overridden)
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Line settings
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0, 0, 1));
            lineRenderer.widthMultiplier = 0.045f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCornerVertices = 3;
            lineRenderer.numCapVertices = 3;

            // Width curve (constant width)
            lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);

            // Color gradient (white to white)
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            lineRenderer.colorGradient = gradient;

            // SynergyLineStrand (custom component)
            root.AddComponent<SynergyLineStrand>();

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
