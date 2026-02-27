using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using System;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class CombatTextManager : MonoBehaviour
{
    /// <summary>
    /// Spawns a floating text using a profile key (e.g., "Damage", "Healing", etc.)
    /// </summary>
    public void Spawn(string text, Vector3 position, string styleKey = "Damage")
    {
        var textStyle = TextStyleLibrary.Get(styleKey);
        if (textStyle == null)
        {
            Debug.LogError($"Text style '{styleKey}' not found. Falling back to default profile.");
            textStyle = TextStyleLibrary.Get("Damage"); // fallback
            if (textStyle == null) return;
        }

        // Use factory instead of Instantiate(prefab)
        var go = CombatTextFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<CombatTextInstance>();
        instance.name = $"DamageText_{Guid.NewGuid():N}";
        instance.parent = g.Canvas3D.transform;
        instance.Spawn(text, position, textStyle);
    }

    /// <summary>
    /// Clears all combat text objects from the scene without using tags.
    /// </summary>
    public void Clear()
    {
        var instances = GameObject.FindObjectsByType<CombatTextInstance>(FindObjectsSortMode.None);
        foreach (var instance in instances)
        {
            Destroy(instance.gameObject);
        }
    }
}
