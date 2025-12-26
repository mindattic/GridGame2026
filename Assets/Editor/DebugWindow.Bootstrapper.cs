#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using scene = Assets.Helpers.SceneHelper;

/// <summary>
/// DebugWindowBootstrapper
/// Purpose:
///   Starts a short delayed open when the Game scene becomes active.
///   Does not close the window when leaving Game. Rendering is gated inside DebugWindow.
/// </summary>
public static class DebugWindowBootstrapper
{
    private static float delayTime = 3f;
    private static float elapsedTime = 0f;
    private static bool isWaiting = false;
    private static bool subscribedUpdate = false;
    private static bool subscribedScene = false;
    private static bool subscribedPlayMode = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        EnsureSceneSubscription();
        EnsurePlayModeSubscription();

        if (!Application.isPlaying)
        {
            CancelWait();
            return;
        }

        if (IsActiveSceneGame())
        {
            BeginWait();
        }
        else
        {
            CancelWait();
        }
    }

    private static void WaitAndOpenDebugWindow()
    {
        if (!isWaiting)
            return;

        if (!Application.isPlaying)
        {
            CancelWait();
            return;
        }

        if (!IsActiveSceneGame())
        {
            // Stay open if it already is; just stop waiting.
            CancelWait();
            return;
        }

        elapsedTime += Time.unscaledDeltaTime;

        if (elapsedTime >= delayTime)
        {
            CancelWait();
            SafeOpenWindow();
        }
    }

    private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (!Application.isPlaying)
        {
            CancelWait();
            return;
        }

        if (IsGame(newScene))
        {
            BeginWait();
        }
        else
        {
            // Leaving Game. Do not close the window. Just cancel any pending delayed open.
            CancelWait();
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            CancelWait();
        }
    }

    private static void BeginWait()
    {
        isWaiting = true;
        elapsedTime = 0f;
        EnsureUpdateSubscription();
    }

    private static void CancelWait()
    {
        isWaiting = false;
        elapsedTime = 0f;
        RemoveUpdateSubscription();
    }

    private static void SafeOpenWindow()
    {
        try
        {
            if (Application.isPlaying && IsActiveSceneGame())
                DebugWindow.ShowWindow();
        }
        catch { }
    }

    private static bool IsActiveSceneGame()
    {
        var activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() && activeScene.name == scene.Game;
    }

    private static bool IsGame(Scene activeScene)
    {
        return activeScene.IsValid() && activeScene.name == scene.Game;
    }

    private static void EnsureUpdateSubscription()
    {
        if (subscribedUpdate) return;
        EditorApplication.update += WaitAndOpenDebugWindow;
        subscribedUpdate = true;
    }

    private static void RemoveUpdateSubscription()
    {
        if (!subscribedUpdate) return;
        EditorApplication.update -= WaitAndOpenDebugWindow;
        subscribedUpdate = false;
    }

    private static void EnsureSceneSubscription()
    {
        if (subscribedScene) return;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        subscribedScene = true;
    }

    private static void EnsurePlayModeSubscription()
    {
        if (subscribedPlayMode) return;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        subscribedPlayMode = true;
    }
}
#endif
