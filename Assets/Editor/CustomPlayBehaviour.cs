using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class CustomPlayBehaviour
{
    private static PlayStateProcess playStateProcess;

    static CustomPlayBehaviour()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanging;
        EditorApplication.update += OnUpdate;
    }

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

    private static void OnAboutToEnterPlaymode()
    {
        //this will run just before play mode
    }

}
