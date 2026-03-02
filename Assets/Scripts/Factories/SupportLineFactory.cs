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
    /// SUPPORTLINEFACTORY - Creates support connection line GameObjects.
    /// 
    /// PURPOSE:
    /// Creates visual lines connecting a supporter to an attacker during
    /// pincer attacks. Shows which allies are providing support bonus damage.
    /// 
    /// SUPPORT MECHANIC:
    /// ```
    /// [Supporter] ??????? [Attacker]
    ///      ?                  ?
    ///  provides bonus    executing attack
    /// ```
    /// 
    /// Adjacent heroes to pincer attackers become supporters and add
    /// bonus damage. This line visualizes that connection.
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// SupportLine (root)
    /// ??? LineRenderer (line visual)
    /// ??? SupportLineInstance (behavior)
    /// ??? SortingGroup (render order)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Tag: "SupportLine"
    /// - SortingLayer: Lines
    /// - Material: Sprites/Default
    /// - Width: 0.515 constant
    /// 
    /// CALLED BY:
    /// - SupportLineManager.Spawn()
    /// 
    /// RELATED FILES:
    /// - SupportLineInstance.cs: Line behavior
    /// - SupportLineManager.cs: Manages support lines
    /// - PincerAttackManager.cs: FindSupporters() method
    /// </summary>
    public static class SupportLineFactory
    {
        /// <summary>Creates a new support line GameObject.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("SupportLine");
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

            root.AddComponent<SupportLineInstance>();

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
