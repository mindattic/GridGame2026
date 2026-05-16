using UnityEngine;

namespace Scripts.Helpers
{
    /// <summary>
    /// ALTTESTERGUARD - Runtime gate that strips AltTester instrumentation from release builds.
    /// <para>PURPOSE: <see cref="GameBuilder"/> adds an AltRunner GameObject to Game.unity when
    /// the AltTester SDK is installed. The runner opens a WebSocket on :13000 and would happily
    /// ship with a production build — bad. This guard sits alongside AltRunner; on Awake it
    /// checks <see cref="Debug.isDebugBuild"/> and destroys the entire GameObject (AltRunner +
    /// itself) the moment a Release / non-Development build loads the scene.</para>
    /// <para>Why a runtime guard instead of an editor-time strip: Build() runs in the Editor,
    /// where <c>Debug.isDebugBuild</c> is always true. We can't decide at scene-build time
    /// whether the eventual player build will be Dev or Release — so we defer the decision to
    /// the player's first frame.</para>
    /// <para>If you want stricter hygiene (e.g. don't even ship the GameObject), strip it via
    /// an <c>IPreprocessBuildWithReport</c> instead of (or in addition to) this guard.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class AltTesterGuard : MonoBehaviour
    {
        private void Awake()
        {
            // Development builds + Editor Play Mode keep the runner alive.
            if (Debug.isDebugBuild || Application.isEditor) return;

            // Release build: pull the whole instrumentation rig out of the scene.
            Debug.Log("[AltTesterGuard] Release build detected — destroying AltRunner GameObject.");
            Destroy(gameObject);
        }
    }
}
