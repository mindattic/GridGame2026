using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// OVERWORLDSCAFFOLD - Minimal bootstrap scaffold for the Overworld scene.
/// <para>PURPOSE: Creates the empty stage — cameras, canvases, and root containers —
/// without populating the procedural map, NPCs, or encounters (those are
/// generated at runtime). Once the scene is populated and tuned in the editor,
/// run Tools › Scenes › Overworld › Save to overwrite this file with a full
/// deep-clone of the current scene state.</para>
/// <para>HIERARCHY (bootstrap):
/// Main Camera .......... Orthographic Mode7-style camera rig
/// EventSystem .......... EventSystem + StandaloneInputModule
/// Map .................. Empty parent: Terrain / Surface / Canopy / Heroes / Caravan
/// BattleTransition ..... Empty parent for the transition effect
/// Canvas ............... ScreenSpaceOverlay (UI: VirtualJoystick, buttons, DayNightCycle)
/// </para>
/// <para>RELATED FILES: GameObjectHelper.Overworld, OverworldManager.cs, DayNightCycle.cs</para>
/// </summary>
public static class OverworldScaffold
{
    private const string SceneName = "Overworld";

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);

        var map = SceneScaffoldHelper.EnsureEmptyGameObject("Map", ref created, ref found);
        EnsureChild(map, "Terrain", ref created, ref found);
        EnsureChild(map, "Surface", ref created, ref found);
        EnsureChild(map, "Canopy", ref created, ref found);
        EnsureChild(map, "Heroes", ref created, ref found);
        EnsureChild(map, "Caravan", ref created, ref found);

        SceneScaffoldHelper.EnsureEmptyGameObject("BattleTransition", ref created, ref found);

        SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    private static void EnsureChild(GameObject parent, string name, ref int created, ref int found)
    {
        if (parent == null) return;
        var existing = parent.transform.Find(name);
        if (existing != null) { found++; return; }

        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Overworld/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Overworld scene and recreate the bootstrap skeleton?\n\n" +
            "This is a MINIMAL scaffold — procedural map data will still generate at runtime, " +
            "but detailed UI and NPCs will be gone until you run Save to snapshot them.\n\n" +
            "Any unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }

    //[MenuItem("Tools/Scenes/Overworld/Create Scaffolding")]
    //private static void Menu_Create() => CreateScaffolding();

    //[MenuItem("Tools/Scenes/Overworld/Clear Scene")]
    //private static void Menu_Clear() => ClearScene();
}
