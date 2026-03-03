#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using scene = Scripts.Helpers.SceneHelper;
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
using Scripts.Utilities;

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
    /// <summary>Handles the scene loaded event.</summary>
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

    /// <summary>Wait and open debug window.</summary>
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

    /// <summary>Handles the active scene changed event.</summary>
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

    /// <summary>Handles the play mode state changed event.</summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            CancelWait();
        }
    }

    /// <summary>Begin wait.</summary>
    private static void BeginWait()
    {
        isWaiting = true;
        elapsedTime = 0f;
        EnsureUpdateSubscription();
    }

    /// <summary>Returns whether the cancel wait condition is met.</summary>
    private static void CancelWait()
    {
        isWaiting = false;
        elapsedTime = 0f;
        RemoveUpdateSubscription();
    }

    /// <summary>Safe open window.</summary>
    private static void SafeOpenWindow()
    {
        try
        {
            if (Application.isPlaying && IsActiveSceneGame())
                DebugWindow.ShowWindow();
        }
        catch { }
    }

    /// <summary>Returns whether the is active scene game condition is met.</summary>
    private static bool IsActiveSceneGame()
    {
        var activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() && activeScene.name == scene.Game;
    }

    /// <summary>Returns whether the is game condition is met.</summary>
    private static bool IsGame(Scene activeScene)
    {
        return activeScene.IsValid() && activeScene.name == scene.Game;
    }

    /// <summary>Ensure update subscription.</summary>
    private static void EnsureUpdateSubscription()
    {
        if (subscribedUpdate) return;
        EditorApplication.update += WaitAndOpenDebugWindow;
        subscribedUpdate = true;
    }

    /// <summary>Remove update subscription.</summary>
    private static void RemoveUpdateSubscription()
    {
        if (!subscribedUpdate) return;
        EditorApplication.update -= WaitAndOpenDebugWindow;
        subscribedUpdate = false;
    }

    /// <summary>Ensure scene subscription.</summary>
    private static void EnsureSceneSubscription()
    {
        if (subscribedScene) return;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        subscribedScene = true;
    }

    /// <summary>Ensure play mode subscription.</summary>
    private static void EnsurePlayModeSubscription()
    {
        if (subscribedPlayMode) return;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        subscribedPlayMode = true;
    }
}
#endif
