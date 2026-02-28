using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using System;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// COMBATTEXTMANAGER - Spawns floating combat text (damage numbers, status text).
/// 
/// PURPOSE:
/// Creates and manages floating text that appears during combat to show
/// damage dealt, healing received, status effects, etc.
/// 
/// VISUAL APPEARANCE:
/// ```
///     -42   ← Red damage number
///      ↑    ← Rises and fades
///   [Enemy]
///   
///    +15   ← Green heal number
///      ↑
///   [Hero]
/// ```
/// 
/// SPAWN FLOW:
/// 1. Spawn() called with text, position, and style key
/// 2. TextStyleLibrary provides font, color, size
/// 3. CombatTextFactory creates GameObject
/// 4. CombatTextInstance animates rise and fade
/// 5. Auto-destroys when animation complete
/// 
/// STYLE KEYS (from TextStyleLibrary):
/// - "Damage": Red text for damage dealt
/// - "Heal": Green text for healing
/// - "Miss": Gray text for missed attacks
/// - "Critical": Large yellow text for crits
/// - "GainExperience": XP gain notification
/// 
/// USAGE:
/// ```csharp
/// g.CombatTextManager.Spawn("-42", enemy.Position, "Damage");
/// g.CombatTextManager.Spawn("+15", hero.Position, "Heal");
/// g.CombatTextManager.Spawn("MISS", target.Position, "Miss");
/// ```
/// 
/// RELATED FILES:
/// - CombatTextFactory.cs: Creates text GameObjects
/// - CombatTextInstance.cs: Animation component
/// - TextStyleLibrary.cs: Text style definitions
/// - AttackHelper.cs: Calls Spawn after damage
/// 
/// ACCESS: g.CombatTextManager
/// </summary>
public class CombatTextManager : MonoBehaviour
{
    /// <summary>
    /// Spawns floating text with the specified style.
    /// </summary>
    /// <param name="text">Text to display (e.g., "-42", "+15", "MISS")</param>
    /// <param name="position">World position to spawn at</param>
    /// <param name="styleKey">Style key from TextStyleLibrary (default: "Damage")</param>
    public void Spawn(string text, Vector3 position, string styleKey = "Damage")
    {
        var textStyle = TextStyleLibrary.Get(styleKey);
        if (textStyle == null)
        {
            Debug.LogError($"Text style '{styleKey}' not found. Falling back to default profile.");
            textStyle = TextStyleLibrary.Get("Damage");
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
    /// Destroys all combat text objects in the scene.
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
