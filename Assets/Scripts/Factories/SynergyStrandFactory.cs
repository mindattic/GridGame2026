using UnityEngine;
using UnityEngine.Rendering;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
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

namespace Scripts.Factories
{
    /// <summary>
    /// SYNERGYSTRANDFACTORY - Creates individual synergy strand GameObjects.
    /// 
    /// PURPOSE:
    /// Creates a single animated strand line that's part of a synergy
    /// connection between allies. Multiple strands create a wispy effect.
    /// 
    /// SYNERGY LINE COMPOSITION:
    /// ```
    /// [Hero A] ════════════ [Hero B]
    ///          ↑↑↑↑↑↑↑↑↑↑↑
    ///        multiple strands
    /// 
    /// Each strand:
    /// ───~──~───~──~───  ← Animated wave pattern
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// SynergyStrand (root)
    /// ├── Transform
    /// ├── LineRenderer (wavy line)
    /// └── SynergyStrand (behavior - added by caller)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - SortingOrder: 200 (above actors)
    /// - Width: 0.045 units
    /// - Corner/Cap vertices: 3 (smooth curves)
    /// - Material: Sprites/Default
    /// 
    /// ANIMATION:
    /// SynergyStrand component animates the line positions
    /// to create flowing wave patterns.
    /// 
    /// CALLED BY:
    /// - SynergyLineInstance.SpawnStrands()
    /// 
    /// RELATED FILES:
    /// - SynergyStrand.cs: Strand animation behavior
    /// - SynergyLineInstance.cs: Manages multiple strands
    /// - SynergyLineFactory.cs: Creates parent container
    /// </summary>
    public static class SynergyStrandFactory
    {
        /// <summary>Creates a new synergy strand line.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("SynergyStrand");
            root.layer = LayerMask.NameToLayer("Default");

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

            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0, 0, 1));
            lineRenderer.widthMultiplier = 0.045f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCornerVertices = 3;
            lineRenderer.numCapVertices = 3;

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
