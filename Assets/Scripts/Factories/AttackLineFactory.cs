using Scripts.Instances;
using UnityEngine;
using UnityEngine.Rendering;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
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

namespace Scripts.Factories
{
    /// <summary>
    /// ATTACKLINEFACTORY - Creates attack indicator line GameObjects.
    /// 
    /// PURPOSE:
    /// Creates visual lines showing attack connections between attackers
    /// and their targets during combat sequences.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// [Attacker] ????????????? [Target]
    ///              ?
    ///         attack line
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// AttackLine (GameObject)
    /// ??? LineRenderer (line visual)
    /// ??? AttackLineInstance (behavior)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Tag: "AttackLine"
    /// - Color: Cyan (0.39, 0.76, 0.78)
    /// - SortingOrder: 500 (above actors, below combat text)
    /// - Material: Sprites/Default shader
    /// 
    /// USAGE:
    /// ```csharp
    /// var line = AttackLineFactory.Create(parent);
    /// var instance = line.GetComponent<AttackLineInstance>();
    /// instance.Spawn(attacker, target);
    /// ```
    /// 
    /// CALLED BY:
    /// - AttackLineManager.Spawn()
    /// 
    /// RELATED FILES:
    /// - AttackLineInstance.cs: Line behavior component
    /// - AttackLineManager.cs: Manages attack lines
    /// - PincerAttackSequence.cs: Uses attack lines
    /// </summary>
    public static class AttackLineFactory
    {
        /// <summary>Creates a new attack line GameObject.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("AttackLine");
            root.layer = 0;
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

            // Material
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Sorting
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 500;

            // Positions
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { Vector3.zero, new Vector3(0, 0, 1) });

            // Width
            lineRenderer.widthMultiplier = 1f;
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.515152f)
            );

            // Color gradient - cyan with low alpha
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
