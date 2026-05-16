using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ALTTESTERSTRIPPER - Build preprocessor that strips the AltRunner GameObject from every
/// scene being baked into a Release / non-Development build.
/// <para>WHY: <see cref="GameBuilder.TryAddAltRunner"/> adds an AltRunner GameObject to
/// Game.unity whenever the AltTester SDK is installed, so the PlayMode test harness can
/// connect via WebSocket. The runtime-side <c>AltTesterGuard</c> already destroys the
/// GameObject on the first frame of a Release player. This stripper goes one step further
/// — it removes the GameObject before serialization so the runner isn't even *briefly*
/// present in the shipped build (no WebSocket listener can ever bind, even for a frame).
/// Belt-and-suspenders with the runtime guard.</para>
/// <para>WHEN IT FIRES: <see cref="IProcessSceneWithReport.OnProcessScene"/> is called by
/// Unity for every scene during the build pipeline. We early-return when:
/// <list type="bullet">
/// <item><paramref name="report"/> is null — that's Editor Play Mode entering the callback;
/// the runner SHOULD stay for tests.</item>
/// <item>The build has the <see cref="BuildOptions.Development"/> flag set — Development
/// builds keep AltTester instrumentation by design.</item>
/// </list></para>
/// <para>RELATED FILES: GameBuilder.cs (adds the runner), AltTesterGuard.cs (runtime
/// fallback strip), Documentation/AltTester-Setup.md</para>
/// </summary>
public class AltTesterStripper : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // Editor Play Mode also routes through this callback with a null report — leave the
        // runner alone there so PlayMode tests still find it.
        if (report == null) return;

        // Development builds get to keep the runner. Only strip from Release.
        if ((report.summary.options & BuildOptions.Development) != 0) return;

        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null && root.name == "AltRunner")
            {
                Object.DestroyImmediate(root);
                removed++;
            }
        }

        if (removed > 0)
            Debug.Log($"[AltTesterStripper] Removed {removed} AltRunner GameObject(s) from '{scene.name}' for Release build.");
    }
}
