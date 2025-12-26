using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A generic singleton base class for MonoBehaviour-derived classes.
/// This class ensures that only one actors of T exists and provides global access to it.
/// </summary>
/// <typeparam name="T">The type of the singleton, which must derive from MonoBehaviour.</typeparam>
public abstract class Singleton<T> : Singleton where T : MonoBehaviour
{
    // The singleton actors of type T.
    private static T _instance;

    // Lock object to ensure thread safety when accessing actors.
    private static readonly object objLock = new object();

    // Flag to determine if this singleton should persist between scene loads.
    private bool isPersistent = false;

    /// <summary>
    /// Public property to access the singleton actors.
    /// If no actors exists, it attempts to find one or creates a new one if the active scene is "Game".
    /// </summary>
    public static T instance
    {
        get
        {
            // If the application is quitting, do not return the actors.
            if (isQuitting)
            {
                Debug.LogWarning($"[{nameof(Singleton)}<{typeof(T)}>] instance will not be returned because the application is quitting.");
                return null;
            }

            // Lock to ensure that only one thread can access this block at a time.
            lock (objLock)
            {
                // If an actors already exists, return it.
                if (_instance != null)
                    return _instance;

                // Try to find any existing instances of type T in the scene.
                var instances = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
                if (instances.Length > 0)
                {
                    // If more than one actors exists, log a warning and destroy extras.
                    if (instances.Length > 1)
                    {
                        // Debug.LogWarning($"[{nameof(Singleton)}<{typeof(T)}>] More than one actors found. Destroying extras.");
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
