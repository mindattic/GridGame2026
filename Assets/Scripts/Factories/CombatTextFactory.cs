using Assets.Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for CombatText - replaces CombatTextPrefab.prefab
    /// </summary>
    public static class CombatTextFactory
    {
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

            // MeshRenderer (added automatically by TextMeshPro, but we configure it)
            // TextMeshPro will add its own MeshRenderer

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
                meshRenderer.sortingLayerName = "Default"; // Use Default sorting layer
                meshRenderer.sortingOrder = 1000; // High value to render on top of everything
            }

            // CombatTextInstance (custom component)
            var combatTextInstance = root.AddComponent<CombatTextInstance>();
            // Note: textMesh is assigned in Awake(), speed is set there too
            // riseCurve is serialized, so we set it here
            SetRiseCurve(combatTextInstance);

            // Parent if specified
            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return root;
        }

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
