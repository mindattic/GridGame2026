using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// TARGETLINEFACTORY - Creates ability targeting line GameObjects.
    /// 
    /// PURPOSE:
    /// Creates a visual line connecting an ability button to the
    /// cursor/target during ability targeting mode.
    /// 
    /// TARGETING FLOW:
    /// ```
    /// [Ability Button] ?????????????? [Cursor/Target]
    ///                      ?
    ///              targeting line
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// TargetLine (root)
    /// ??? LineRenderer (line visual)
    /// ??? TargetLineInstance (behavior)
    /// ??? SortingGroup (render order)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Tag: "SupportLine" (shared tag)
    /// - SortingLayer: Lines
    /// - Width: 0.515 constant
    /// - Material: Sprites/Default
    /// 
    /// CALLED BY:
    /// - TargetLineManager.BeginTargeting()
    /// 
    /// RELATED FILES:
    /// - TargetLineInstance.cs: Line behavior
    /// - TargetLineManager.cs: Manages targeting lines
    /// - AbilityManager.cs: Ability targeting flow
    /// </summary>
    public static class TargetLineFactory
    {
        /// <summary>Creates a new targeting line.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("TargetLine");
            root.layer = 0;
            root.tag = "SupportLine";

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
            lineRenderer.sortingLayerName = "Lines";
            lineRenderer.sortingOrder = 0;

            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0, 0, 1));
            lineRenderer.widthMultiplier = 1f;
            lineRenderer.useWorldSpace = true;

            lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 0.515152f);

            root.AddComponent<TargetLineInstance>();

            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "Lines";
            sortingGroup.sortingOrder = 0;

            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
