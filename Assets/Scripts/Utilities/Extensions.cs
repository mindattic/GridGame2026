using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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

namespace Scripts.Utilities
{
/// <summary>
/// EXTENSIONS - Extension methods for Unity and C# types.
/// 
/// PURPOSE:
/// Provides utility extension methods that add functionality to
/// built-in Unity and .NET types for cleaner, more readable code.
/// 
/// CATEGORIES:
/// - GameObject: Child finding, path traversal
/// - Transform: Component search, hierarchy navigation
/// - Vector2/Vector3: SetX/SetY/SetZ individual axis setters
/// - Color: Alpha manipulation
/// - IEnumerable: Shuffle, random selection
/// - String: Formatting utilities
/// 
/// USAGE:
/// ```csharp
/// // Vector extensions
/// position = position.SetY(5f);
/// 
/// // Color extensions
/// color = color.WithAlpha(0.5f);
/// 
/// // Collection extensions
/// var shuffled = list.Shuffle();
/// var random = list.Random();
/// 
/// // GameObject extensions
/// var child = gameObject.GetChildByName("Thumbnail");
/// ```
/// 
/// RELATED FILES:
/// - Used throughout the codebase
/// - Geometry.cs: Additional vector utilities
/// </summary>

#region GameObject Extensions

public static class GameObjectExtensions
{
    /// <summary>Gets a direct child by name (not recursive).</summary>
    public static GameObject GetChildByName(this GameObject parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
            return null;

        var childTransform = parent.transform.Find(childName);
        return childTransform != null ? childTransform.gameObject : null;
    }

    /// <summary>Finds a descendant GameObject by slash-delimited path.</summary>
    public static GameObject FindByPath(this GameObject root, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || root == null)
            return null;

        string[] names = path.Split('/');
        GameObject current = root.name == names[0] ? root : GameObject.Find(names[0]);
        if (current == null)
            return null;

        for (int i = (current == root ? 1 : 1); i < names.Length; i++)
        {
            Transform child = current.transform.Find(names[i]);
            if (child == null)
                return null;

            current = child.gameObject;
        }

        return current;
    }

    /// <summary>
    /// Recursively collects components of type T where the owning GameObject's name matches exactly.
    /// </summary>
    public static IEnumerable<T> GetComponentsInChildrenByName<T>(this GameObject root, string name, bool includeInactive = true) where T : UnityEngine.Component
    {
        if (root == null || string.IsNullOrEmpty(name)) yield break;
        foreach (var comp in root.transform.GetComponentsInChildrenByName<T>(name, includeInactive))
            yield return comp;
    }

    /// <summary>
    /// Returns the first component of type T whose GameObject name matches, or null if not found.
    /// </summary>
    public static T GetComponentInChildrenByName<T>(this GameObject root, string name, bool includeInactive = true) where T : UnityEngine.Component
    {
        return root == null ? null : root.transform.GetComponentsInChildrenByName<T>(name, includeInactive).FirstOrDefault();
    }
}

public static class StringExtensions
{
    /// <summary>Sanitize file name.</summary>
    public static string SanitizeFileName(this string value)
    {
        //Trim and replace spaces
        value = value.Trim().Replace(" ", "-");

        //Remove illegal characters for Windows, iOS, and Android
        System.IO.Path.GetInvalidFileNameChars().ToList().ForEach(c => value = value.Replace(c.ToString(), ""));

        return value;
    }

    /// <summary>To pascal case.</summary>
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var separators = new[] { ' ', '\t', '\n', '\r', '_', '-', '.', '/', ':' };
        var parts = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        var sb = new StringBuilder(parts.Length * 4);
        foreach (var part in parts)
        {
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1) sb.Append(part.Substring(1));
        }

        return sb.ToString();
    }

}

public static class TransformExtensions
{
    /// <summary>Gets the child.</summary>
    public static Transform GetChild(this Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            //Recursively search in the child hierarchy
            Transform found = child.GetChild(childName);
            if (found != null)
                return found;
        }
        return null; //Return null if no matching child is found
    }

    /// <summary>
    /// Recursively collects components of type T where the owning GameObject's name matches exactly.
    /// </summary>
    public static IEnumerable<T> GetComponentsInChildrenByName<T>(this Transform root, string name, bool includeInactive = true) where T : UnityEngine.Component
    {
        if (root == null || string.IsNullOrEmpty(name)) yield break;

        void Collect(Transform t, List<T> list)
        {
            if (t == null) return;
            if (includeInactive || (t.gameObject.activeInHierarchy && t.gameObject.activeSelf))
            {
                var comp = t.GetComponent<T>();
                if (comp != null && t.gameObject.name == name)
                    list.Add(comp);
            }

            foreach (Transform child in t)
                Collect(child, list);
        }

        var results = new List<T>();
        Collect(root, results);
        foreach (var r in results)
            yield return r;
    }

    /// <summary>
    /// Returns the first component of type T whose GameObject name matches, or null if not found.
    /// </summary>
    public static T GetComponentInChildrenByName<T>(this Transform root, string name, bool includeInactive = true) where T : UnityEngine.Component
    {
        return root == null ? null : root.GetComponentsInChildrenByName<T>(name, includeInactive).FirstOrDefault();
    }
}

public static class EnumExtensions
{
    public static T Next<T>(this T src) where T : struct
    {
        var values = (T[])Enum.GetValues(src.GetType());
        int index = Array.IndexOf<T>(values, src) + 1;
        return (values.Length == index) ? values[0] : values[index];
    }

    /// <summary>Gets the description.</summary>
    public static string GetDescription(this Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString());
        var attribute = field.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return attribute == null ? value.ToString() : attribute.Description;
    }

    /// <summary>To name.</summary>
    public static string ToName(this Enum value)
    {
        return Enum.GetName(value.GetType(), value);
    }

    public static T ToEnum<T>(this string value, T defaultValue = default) where T : struct, Enum
    {
        return Enum.TryParse(value, true, out T result) ? result : defaultValue;
    }

    // Convert enum to int
    /// <summary>To int.</summary>
    public static int ToInt(this Enum value)
    {
        return Convert.ToInt32(value);
    }

    // Convert enum to float
    /// <summary>To float.</summary>
    public static float ToFloat(this Enum value)
    {
        return Convert.ToSingle(value);
    }
}



public static class TileInstanceExtensions
{
    /// <summary>Exists.</summary>
    public static bool Exists(this TileInstance tile)
        => tile != null;

}

/// <summary>
/// Extension methods for ActorInstance to simplify common property checks.
/// </summary>
public static class ActorInstanceExtensions
{
    /// <summary>
    /// Determines if this actor belongs to the hero's team.
    /// </summary>
    public static bool IsHero(this ActorInstance actor)
        => actor != null && actor.team == Team.Hero;

    /// <summary>
    /// Determines if this actor belongs to the attacker's team.
    /// </summary>
    public static bool IsEnemy(this ActorInstance actor)
        => actor != null && actor.team == Team.Enemy;

    /// <summary>
    /// Checks if the GameObject is active and enabled.
    /// </summary>
    public static bool IsActive(this ActorInstance actor)
        => actor != null && actor.isActiveAndEnabled;

    /// <summary>
    /// Actor is alive if HP is above zero.
    /// </summary>
    public static bool IsAlive(this ActorInstance actor)
        => actor != null && actor.Stats.HP > 0;

    /// <summary>
    /// Actor is active in the game (alive and enabled).
    /// </summary>
    public static bool IsPlaying(this ActorInstance actor)
        => actor.IsActive() && actor.IsAlive();

    /// <summary>
    /// Actor is in the process of dying (active but HP below 1).
    /// </summary>
    public static bool IsDying(this ActorInstance actor)
        => actor.IsActive() && actor.Stats.HP < 1;

    /// <summary>
    /// Actor is dead when not active and HP is zero.
    /// </summary>
    public static bool IsDead(this ActorInstance actor)
        => !actor.IsActive() && !actor.IsAlive();

    /// <summary>
    /// Actor can spawn if not already spawned and the spawn turn has arrived.
    /// </summary>
    public static bool IsSpawnable(this ActorInstance actor)
        => actor != null && !actor.Flags.HasSpawned
           && actor.spawnTurn <= g.TurnManager.CurrentTurn;

    /// <summary>
    /// Actor has maximum Animation points.
    /// </summary>
    public static bool HasMaxAP(this ActorInstance actor)
        => actor != null && actor.Stats.AP == actor.Stats.MaxAP;

    /// <summary>
    /// None-safe existence check for the actor.
    /// </summary>
    public static bool Exists(this ActorInstance actor)
        => actor != null;
}


public static class ListExtensions
{
    public static bool IsNullOrEmpty<T>(this IList<T> list)
    {
        return list == null || list.Count == 0;
    }

    public static List<T> Shuffle<T>(this List<T> list)
    {
        return list.OrderBy(x => Guid.NewGuid()).ToList();
    }

    public static T ShuffleFirst<T>(this List<T> list)
    {
        return list.OrderBy(x => Guid.NewGuid()).First();
    }

    public static void Set<T>(this List<T> list, T item)
    {
        list.Clear();
        list.Add(item);
    }

    public static void SetRange<T>(this List<T> list, IEnumerable<T> items)
    {
        list.Clear();
        list.AddRange(items);
    }

    public static void SetRange<T>(this List<T> list, params T[] items)
    {
        list.Clear();
        list.AddRange(items);
    }
}


public static class IEnumerableExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
    {
        return source == null || !source.Any();
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
    {
        return list.OrderBy(x => Guid.NewGuid());
    }
}

public static class ArrayExtensions
{
    public static T[] Shuffle<T>(this T[] array)
    {
        return array.OrderBy(x => Guid.NewGuid()).ToArray();
    }

    public static T ShuffleFirst<T>(this T[] array)
    {
        return array.OrderBy(x => Guid.NewGuid()).First();
    }
}
/// <summary>
/// Extension methods for Vector2Int to simplify common grid location checks.
/// </summary>
public static class Vector2IntExtensions
{
    /// <summary>
    /// Determines whether the vector represents a valid board location.
    /// Returns true if s and y are within the board's column and row bounds (inclusive).
    /// </summary>
    public static bool Exists(this Vector2Int v)
    {
        return v.x >= 1 && v.x <= g.Board.columnCount
            && v.y >= 1 && v.y <= g.Board.rowCount;
    }

    /// <summary>Shift.</summary>
    public static void Shift(this ref Vector2Int v, int x, int y)
    {
        v.x += x;
        v.y += y;
    }

    /// <summary>Sets the x.</summary>
    public static Vector2Int SetX(this Vector2Int v, int x)
    {
        return new Vector2Int(x, v.y);
    }

    /// <summary>Add x.</summary>
    public static Vector2Int AddX(this Vector2Int v, int x)
    {
        return new Vector2Int(v.x + x, v.y);
    }

    /// <summary>Sets the y.</summary>
    public static Vector2Int SetY(this Vector2Int v, int y)
    {
        return new Vector2Int(v.x, y);
    }

    /// <summary>Add y.</summary>
    public static Vector2Int AddY(this Vector2Int v, int y)
    {
        return new Vector2Int(v.x, v.y + y);
    }
}


public static class Vector2Extensions
{
    /// <summary>Sets the x.</summary>
    public static Vector2 SetX(this Vector2 v, float x)
    {
        return new Vector2(x, v.y);
    }

    /// <summary>Add x.</summary>
    public static Vector2 AddX(this Vector2 v, float x)
    {
        return new Vector2(v.x + x, v.y);
    }

    /// <summary>Sets the y.</summary>
    public static Vector2 SetY(this Vector2 v, float y)
    {
        return new Vector2(v.x, y);
    }

    /// <summary>Add y.</summary>
    public static Vector2 AddY(this Vector2 v, float y)
    {
        return new Vector2(v.x, v.y + y);
    }
}

public static class Vector3Extensions
{
    /// <summary>Sets the x.</summary>
    public static Vector3 SetX(this Vector3 v, float x)
    {
        return new Vector3(x, v.y, v.z);
    }

    /// <summary>Add x.</summary>
    public static Vector3 AddX(this Vector3 v, float x)
    {
        return new Vector3(v.x + x, v.y, v.z);
    }

    /// <summary>Sets the y.</summary>
    public static Vector3 SetY(this Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    /// <summary>Add y.</summary>
    public static Vector3 AddY(this Vector3 v, float y)
    {
        return new Vector3(v.x, v.y + y, v.z);
    }

    /// <summary>Sets the z.</summary>
    public static Vector3 SetZ(this Vector3 v, float z)
    {
        return new Vector3(v.x, v.y, z);
    }

    /// <summary>Add z.</summary>
    public static Vector3 AddZ(this Vector3 v, float z)
    {
        return new Vector3(v.x, v.y, v.z + z);
    }

    /// <summary>Returns whether the has na n condition is met.</summary>
    public static bool HasNaN(this Vector3 v)
    {
        return v.x == float.NaN || v.y == float.NaN || v.z == float.NaN;
    }

    /// <summary>Randomize offset.</summary>
    public static Vector3 RandomizeOffset(this Vector3 v, float amount)
    {
        return new Vector3(v.x + RNG.Float(-amount, amount), v.y + RNG.Float(-amount, amount), v.z);
    }

    /// <summary>From string.</summary>
    public static Vector3 FromString(string v)
    {
        //Remove parentheses and split by commas
        v = v.Trim('(', ')');
        string[] split = v.Split(',');

        if (split.Length != 3)
            return Vector3.zero;

        //Parse each component to float and return the Vector3
        float x = float.Parse(split[0].Trim());
        float y = float.Parse(split[1].Trim());
        float z = float.Parse(split[2].Trim());
        return new Vector3(x, y, z);
    }

    /// <summary>Multiply by.</summary>
    public static Vector3 MultiplyBy(this Vector3 v, Vector2 other)
    {
        return new Vector3(v.x * other.x, v.y * other.y, v.z);
    }

    /// <summary>Multiply by.</summary>
    public static Vector3 MultiplyBy(this Vector3 v, Vector3 other)
    {
        return new Vector3(v.x * other.x, v.y * other.y, v.z * other.z);
    }

    /// <summary>
    /// Clamps the X and Y of this Vector3 to the board bounds from g.Board.
    /// Preserves the original Z.
    /// </summary>
    /// <param name="position">The position to clamp.</param>
    /// <returns>Position clamped within the board bounds.</returns>
    public static Vector3 ClampToBoard(this Vector3 position)
    {
        // Preserve Z before clamping
        float z = position.z;

        position.x = Mathf.Clamp(position.x, g.Board.bounds.Left, g.Board.bounds.Right);
        position.y = Mathf.Clamp(position.y, g.Board.bounds.Bottom, g.Board.bounds.Top);
        position.z = z;

        return position;
    }
}

public static class ColorExtensions
{

    /// <summary>Sets the a.</summary>
    public static Color SetA(this Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    /// <summary>Add a.</summary>
    public static Color AddA(this Color c, float a)
    {
        return new Color(c.r, c.g, c.b, c.a + a);
    }
}






public static class IntExtensions
{
    /// <summary>To float.</summary>
    public static float ToFloat(this int i)
    {
        return (float)i;
    }
}

public static class FloatExtensions
{
    /// <summary>To int.</summary>
    public static int ToInt(this float f)
    {
        return (int)f;
    }
}

//public static class BooleanExtensions
//{
//   public static bool IsNot(this bool b, bool value)
//   {
//       return b != value;
//   }

//   public static bool Toggle(this bool b, bool value)
//   {
//       if (b == value)
//           return false;

//       b = value;
//       return true;
//   }

//}

static class StopwatchExtensions
{
    ///<summary>
    ///Gets estimated time on compleation. 
    ///</summary>
    ///<param value="sw"></param>
    ///<param value="counter"></param>
    ///<param value="counterGoal"></param>
    ///<returns></returns>
    /// <summary>Gets the eta.</summary>
    public static TimeSpan GetEta(this Stopwatch sw, int counter, int counterGoal)
    {
        /* this is based off of:
         * (TimeTaken / linesProcessed) * linesLeft=timeLeft
         * so we have
         * (10/100) * 200 = 20 Seconds now 10 seconds go past
         * (20/100) * 200 = 40 Seconds left now 10 more seconds and we process 100 more supportLines'
         * (30/200) * 100 = 15 Seconds and now we all see why the copy file dialog jumps from 3 hours to 30 minutes :-)
         * 
         * pulled from http://stackoverflow.com/questions/473355/calculate-time-remaining/473369#473369
         */
        if (counter == 0) return TimeSpan.Zero;
        float elapsedMin = ((float)sw.ElapsedMilliseconds / 1000) / 60;
        float minLeft = (elapsedMin / counter) * (counterGoal - counter); //see comment a
        TimeSpan ret = TimeSpan.FromMinutes(minLeft);
        return ret;
    }
}

public static class DirectionExtensions
{
    /// <summary>Opposite.</summary>
    public static Direction Opposite(this Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.South => Direction.North,
            Direction.West => Direction.East,
            _ => Direction.None
        };
    }
}



public static class Texture2DExtensions
{
    /// <summary>To sprite.</summary>
    public static Sprite ToSprite(this Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}


public static class CharactersExtensions
{
    /// <summary>Returns a string representation of this object.</summary>
    public static string ToString(this Characters character)
    {
        return character.ToString();
    }

    /// <summary>From string.</summary>
    public static Characters FromString(string value)
    {
        if (Enum.TryParse(value, true, out Characters result))
        {
            return result;
        }
        throw new ArgumentException($"Invalid character value: {value}");
    }



}


public static class ActorInstanceTimelineExtensions
{
    /// <summary>
    /// Estimate total seconds this actor will spend to reach target and perform its action.
    /// Uses simple heuristics so the UI stays synced without editing core actor logic.
    /// </summary>
    public static float EstimateTurnSeconds(this ActorInstance actor)
    {
        // Baseline time for thinking and windup
        float baseSeconds = 0.6f;

        // Movement estimate: distance in tiles times a per-tile travel time
        // If you already select a target elsewhere, plug that distance in here.
        float tilesToMove = 1f; // Safe default when unknown
        float perTileSeconds = 0.35f;

        // Attack or cast time heuristic
        float attackSeconds = 0.7f;

        // Simple first pass
        float total = baseSeconds + tilesToMove * perTileSeconds + attackSeconds;

        // Clamp to a sane range
        return Mathf.Clamp(total, 0.6f, 4.0f);
    }
}

#endregion
}
