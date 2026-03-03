using Scripts.Libraries;
using TMPro;
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
    /// COMBATTEXTFACTORY - Creates floating combat text (damage numbers).
    /// 
    /// PURPOSE:
    /// Replaces CombatTextPrefab.prefab with code-driven creation.
    /// Creates floating text that rises and fades when damage/healing occurs.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    ///     -42   ? Damage number rises
    ///      ?    ? Animated upward
    ///   [Enemy] ? Spawns above target
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// CombatText (GameObject)
    /// ??? RectTransform (positioning)
    /// ??? TextMeshPro (3D text)
    /// ??? MeshRenderer (rendering)
    /// ??? CombatTextInstance (animation logic)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Font: FontLibrary.Fonts["Attic"]
    /// - Scale: 0.1x (world-space sizing)
    /// - SortingLayer: "DamageText" (renders above actors and VFX)
    /// 
    /// ANIMATION:
    /// Uses AnimationCurve (riseCurve) to control:
    /// - Upward movement
    /// - Alpha fade
    /// - Scale changes
    /// 
    /// CALLED BY:
    /// - CombatTextManager.Spawn() during damage/heal events
    /// 
    /// RELATED FILES:
    /// - CombatTextInstance.cs: Animation component
    /// - CombatTextManager.cs: Spawns and manages text
    /// - FontLibrary.cs: Provides Attic font
    /// - PincerAttackSequence.cs: Triggers damage display
    /// </summary>
    public static class CombatTextFactory
    {
        /// <summary>
        /// Creates a new combat text GameObject.
        /// </summary>
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("CombatText");
            root.layer = 0; // Default layer

            // RectTransform
            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = new Vector3(0.1f, 0.1f, 1f);

            // TextMeshPro (3D text component)
            var textMesh = root.AddComponent<TextMeshPro>();
            textMesh.font = FontLibrary.Fonts["Attic"];
            textMesh.text = "Miss";
            textMesh.fontSize = 32;
            textMesh.color = Color.white;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.enableWordWrapping = false;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.richText = true;
            textMesh.enableKerning = true;
            textMesh.raycastTarget = true;

            // Configure MeshRenderer (added by TextMeshPro)
            var meshRenderer = root.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.sortingLayerName = "DamageText";
                meshRenderer.sortingOrder = 0;
            }

            // CombatTextInstance (animation component)
            var combatTextInstance = root.AddComponent<CombatTextInstance>();
            SetRiseCurve(combatTextInstance);

            // Parent if specified
            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }

        /// <summary>Sets the rise curve.</summary>
        private static void SetRiseCurve(CombatTextInstance instance)
        {
            // Access the riseCurve field via reflection since it's SerializeField
            var riseCurveField = typeof(CombatTextInstance).GetField("riseCurve", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (riseCurveField != null)
            {
                // Create the arc curve: starts at -1, peaks at 1 at midpoint, returns to -1
                var riseCurve = new AnimationCurve(
                    new Keyframe(0f, -1f, 0f, 0f),
                    new Keyframe(0.5f, 1f, 0f, 0f) { inWeight = 0.33333334f, outWeight = 0.24990547f },
                    new Keyframe(1f, -1f, 0f, 0f) { inWeight = 0.112100124f, outWeight = 0f }
                );
                riseCurveField.SetValue(instance, riseCurve);
            }
        }
    }
}
