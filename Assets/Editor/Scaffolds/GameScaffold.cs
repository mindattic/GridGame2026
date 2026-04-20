using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// GAMESCAFFOLD - Minimal bootstrap scaffold for the Game scene.
/// <para>PURPOSE: Creates the empty stage — cameras, canvases, and manager host
/// GameObjects — without populating the board or actors (those are spawned at
/// runtime by TileMap, ActorFactory, and the wave system). This is a starting
/// point: once the scene is populated and tuned in the editor, run
/// Tools › Scenes › Game › Save to overwrite this file with a full deep-clone
/// of the current scene state.</para>
/// <para>HIERARCHY (bootstrap):
/// Main Camera .......... Orthographic, depth=-1
/// Overlay Camera ....... Orthographic, depth=0 (for UI/FX layer)
/// EventSystem .......... EventSystem + StandaloneInputModule
/// Background ........... Empty parent for parallax
/// Board ................ Empty parent for tiles/actors (populated by TileMap)
/// ManaPoolManager ...... Empty parent — ManaPoolManager script attached by user
/// Canvas ............... ScreenSpaceOverlay (UI root)
/// Canvas3D ............. WorldSpace canvas (3D portraits, announcements)
/// </para>
/// <para>RELATED FILES: GameObjectHelper.Game, TileMap.cs, BoardInstance.cs, ManaPoolManager.cs</para>
/// </summary>
public static class GameScaffold
{
    private const string SceneName = "Game";

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        EnsureOverlayCamera(ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);

        SceneScaffoldHelper.EnsureEmptyGameObject("Background", ref created, ref found);
        SceneScaffoldHelper.EnsureEmptyGameObject("Board", ref created, ref found);
        SceneScaffoldHelper.EnsureEmptyGameObject("ManaPoolManager", ref created, ref found);

        SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        EnsureCanvas3D(ref created, ref found);

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    private static void EnsureOverlayCamera(ref int created, ref int found)
    {
        var existing = GameObject.Find("Overlay Camera");
        if (existing != null) { found++; return; }

        var go = new GameObject("Overlay Camera");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.depth = 0f;
        cam.clearFlags = CameraClearFlags.Depth;
        Undo.RegisterCreatedObjectUndo(go, "Create Overlay Camera");
        created++;
    }

    private static void EnsureCanvas3D(ref int created, ref int found)
    {
        var existing = GameObject.Find("Canvas3D");
        if (existing != null) { found++; return; }

        var go = new GameObject("Canvas3D");
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas3D");
        created++;
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Game/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Game scene and recreate the bootstrap skeleton?\n\n" +
            "This is a MINIMAL scaffold — runtime board/actor spawning will still work, " +
            "but detailed UI and managers will be gone until you run Save to snapshot them.\n\n" +
            "Any unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }

    //[MenuItem("Tools/Scenes/Game/Create Scaffolding")]
    //private static void Menu_Create() => CreateScaffolding();

    //[MenuItem("Tools/Scenes/Game/Clear Scene")]
    //private static void Menu_Clear() => ClearScene();
}
