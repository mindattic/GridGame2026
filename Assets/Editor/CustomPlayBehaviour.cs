using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

[InitializeOnLoad]
public class CustomPlayBehaviour
{
    private static PlayStateProcess playStateProcess;

    static CustomPlayBehaviour()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanging;
        EditorApplication.update += OnUpdate;
    }

    /// <summary>Handles the update event.</summary>
    private static void OnUpdate()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (playStateProcess == PlayStateProcess.PreStarting)
            {
                float startTime = EditorPrefs.GetFloat("startTime");
                if (EditorApplication.timeSinceStartup > startTime)
                {
                    playStateProcess = PlayStateProcess.Starting;
                    OnAboutToEnterPlaymode();

                    playStateProcess = PlayStateProcess.Ready;
                    EditorApplication.isPlaying = true;
                }
            }
            else
            {
                playStateProcess = PlayStateProcess.Editing;
            }
        }
    }

    /// <summary>Handles the play mode state changing event.</summary>
    private static void OnPlayModeStateChanging(PlayModeStateChange playModeStateChange)
    {
        if (playModeStateChange == PlayModeStateChange.ExitingEditMode)
        {
            if (playStateProcess != PlayStateProcess.Ready)
            {
                EditorApplication.isPlaying = false;
            }

            if (playStateProcess == PlayStateProcess.Editing)
            {
                playStateProcess = PlayStateProcess.PreStarting;
                EditorPrefs.SetFloat("startTime", (float)EditorApplication.timeSinceStartup + 0.15f);
            }
        }
        else if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
        {
            if (playStateProcess == PlayStateProcess.Ready)
            {
                playStateProcess = PlayStateProcess.Editing;
            }
        }
    }

    /// <summary>Handles the about to enter playmode event.</summary>
    private static void OnAboutToEnterPlaymode()
    {
        //this will run just before play mode
    }

}
