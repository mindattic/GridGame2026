using System;
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
/// DebugWindow
/// Purpose:
///   Editor window that can stay open across scenes. It only renders content while in the Game activeScene.
///   Rendering is guarded to avoid null references and IMGUI imbalance during activeScene switches.
/// </summary>
public partial class DebugWindow : EditorWindow
{
    public static DebugWindow instance;
    public static bool isOpen = false;

    private Vector2 scrollPosition;
    private DateTime lastUpdateTime;
    private float updateInterval = 1.0f;

    private GameSpeedOption selectedGameFocus = GameSpeedOption.Normal;
    private DebugOptions selectedOption = DebugOptions.None;
    private VFX selectedVfx = VFX.None;

    static DebugWindow()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Window/Debug Window")]
    /// <summary>Show windows this component.</summary>
    public static void ShowWindow()
    {
        instance = GetWindow<DebugWindow>("Debug Window");
        isOpen = true;
    }

    /// <summary>Close window.</summary>
    public static void CloseWindow()
    {
        if (instance == null)
            return;

        instance.Close();
        instance = null;
        isOpen = false;
    }

    /// <summary>Handles the play mode state changed event.</summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
#if UNITY_EDITOR_WIN
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.delayCall += WaitForGameScene;
        }
#endif
    }

#if UNITY_EDITOR_WIN
    /// <summary>Wait for game scene.</summary>
    private static void WaitForGameScene()
    {
        EditorApplication.update += CheckSceneLoad;
    }

    /// <summary>Check scene load.</summary>
    private static void CheckSceneLoad()
    {
        if (!Application.isPlaying)
        {
            EditorApplication.update -= CheckSceneLoad;
            return;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.name == scene.Game)
        {
            ShowWindow();
            EditorApplication.update -= CheckSceneLoad;
        }
    }
#endif

    /// <summary>Called when the component becomes enabled and active.</summary>
    private void OnEnable()
    {
        DelayCall(Initialize);
    }

    /// <summary>Delay call.</summary>
    private static void DelayCall(Action action)
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying)
                action();
        };
    }

    /// <summary>Initializes initialize.</summary>
    private void Initialize()
    {
        instance = this;
        isOpen = true;
        lastUpdateTime = DateTime.Now;

        EditorApplication.update += OnEditorUpdate;
    }

    /// <summary>Called when the component becomes disabled.</summary>
    private void OnDisable()
    {
        isOpen = false;
        instance = null;

        EditorApplication.update -= OnEditorUpdate;
        // Do not null out EditorApplication.delayCall. That would clear all editor listeners.
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    /// <summary>Handles the editor update event.</summary>
    private void OnEditorUpdate()
    {
        if ((DateTime.Now - lastUpdateTime).TotalSeconds >= updateInterval)
        {
            lastUpdateTime = DateTime.Now;
            Repaint();
        }
    }

    /// <summary>Render button row.</summary>
    private void RenderButtonRow(params (string label, System.Action onClick)[] buttons)
    {
        GUILayout.BeginHorizontal();

        int count = buttons.Length;
        for (int i = 0; i < count; i++)
        {
            var b = buttons[i];
            bool clicked = GUILayout.Button(b.label, GUILayout.Width(Screen.width * Increment.Percent25));
            if (clicked)
                b.onClick?.Invoke();
        }

        // Fill remaining cells to keep the row width consistent when fewer than four buttons are provided.
        for (int i = count; i < 4; i++)
        {
            GUILayout.Label(string.Empty, GUILayout.Width(Screen.width * Increment.Percent25));
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    /// <summary>Draws immediate-mode GUI elements.</summary>
    private void OnGUI()
    {
        // Only draw content while playing and while the active activeScene is Game.
        if (!Application.isPlaying || !IsActiveSceneGame())
        {
            // Render nothing. Window stays open.
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Debug Window is only available in `Game` activeScene.");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return;
        }

        try
        {
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                false,
                false,
                GUILayout.Width(position.width),
                GUILayout.Height(position.height)
            );

            GUILayout.BeginVertical();
            try
            {
                RenderGameStats();
                RenderThumbnailSettings();
                RenderCanvasThumbnailSettings();
                RenderProjectileOptions();
                RenderGameSpeedOptions();
                RenderDebugOptions();
                RenderVfxOptions();
                RenderKeyboard();
                RenderToggleOptions();
                RenderLevels();
                RenderScenes();
                RenderSpawnOptions();
                RenderActorStats();
                RenderTextStyles(); // NEW section for experimenting with floating text styles
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
        }
        catch (Exception ex)
        {
            // Swallow to keep IMGUI stable; do not close or reopen.
            Debug.LogError(ex);
        }
    }

    /// <summary>Returns whether the is active scene game condition is met.</summary>
    private static bool IsActiveSceneGame()
    {
        var activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() && activeScene.name == scene.Game;
    }
}
