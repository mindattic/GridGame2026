using UnityEngine;
using UnityEngine.SceneManagement;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
/// <summary>
/// SINGLETON<T> - Generic MonoBehaviour singleton base class.
/// 
/// PURPOSE:
/// Ensures only one instance of type T exists and provides
/// global access to it. Thread-safe implementation.
/// 
/// FEATURES:
/// - Auto-creates instance if none exists
/// - Optional persistence across scene loads
/// - Destroys duplicate instances
/// - Handles application quit gracefully
/// 
/// USAGE:
/// ```csharp
/// public class MyManager : Singleton<MyManager>
/// {
///     public void DoSomething() { ... }
/// }
/// 
/// // Access anywhere:
/// MyManager.instance.DoSomething();
/// ```
/// 
/// RELATED FILES:
/// - GameManager.cs: Example singleton usage
/// </summary>
public abstract class Singleton<T> : Singleton where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object objLock = new object();
    private bool isPersistent = false;

    /// <summary>
    /// Public property to access the singleton instance.
    /// Creates or finds instance if needed.
    /// </summary>
    public static T instance
    {
        get
        {
            if (isQuitting)
            {
                Debug.LogWarning($"[{nameof(Singleton)}<{typeof(T)}>] instance will not be returned because the application is quitting.");
                return null;
            }

            lock (objLock)
            {
                if (_instance != null)
                    return _instance;

                var instances = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
                if (instances.Length > 0)
                {
                    if (instances.Length > 1)
                    {
                        for (int i = 1; i < instances.Length; i++)
                            Destroy(instances[i]);
                    }
                    // Use the first found actors.
                    return _instance = instances[0];
                }

                // If no actors exists and the active scene is not "Game", do not create one.
                if (SceneManager.GetActiveScene().name != "Game")
                {
                    return null;
                }

                // In the "Game" scene, create a new GameObject and add the singleton component to it.
                // Debug.Log($"[{nameof(Singleton)}<{typeof(T)}>] Creating new singleton in Game scene.");
                return _instance = new GameObject($"({nameof(Singleton)}){typeof(T)}").AddComponent<T>();
            }
        }
    }

    /// <summary>
    /// Awake is called when the script actors is being loaded.
    /// If isPersistent is true, this GameObject will not be destroyed on scene load.
    /// </summary>
    private void Awake()
    {
        if (isPersistent)
            DontDestroyOnLoad(gameObject);

        // Call a virtual method for any subclass-specific initialization.
        OnAwake();
    }

    /// <summary>
    /// Virtual method called during Awake. Subclasses can override this for additional initialization.
    /// </summary>
    protected virtual void OnAwake() { }
}

/// <summary>
/// Base non-generic singleton class that tracks whether the application is quitting.
/// This is used by Singleton<T> to prevent new actors creation during shutdown.
/// </summary>
public abstract class Singleton : MonoBehaviour
{
    /// <summary>
    /// Indicates whether the application is in the process of quitting.
    /// </summary>
    public static bool isQuitting { get; private set; }

    /// <summary>
    /// OnApplicationQuit is called when the application is about to exit.
    /// Sets the isQuitting flag to true.
    /// </summary>
    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}

}
