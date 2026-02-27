using UnityEngine;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for SynergyLine - replaces SynergyLinePrefab.prefab
    /// </summary>
    public static class SynergyLineFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("SynergyLine");
            root.layer = LayerMask.NameToLayer("Default"); // Layer 0

            // Transform
            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // SynergyLineInstance (custom component)
            root.AddComponent<SynergyLineInstance>();

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
