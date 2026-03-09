# Scene Scaffold System

## Overview

Every scene in the game (except Game and Overworld) can be **fully recreated from code** using
Editor scaffold scripts under `Assets/Editor/Scaffolds/`. This means scene `.unity` files are
reproducible artifacts — if a scene gets corrupted or needs resetting, run the scaffold.

## Menu Structure

All scaffolds live under **Tools › Scenes › {SceneName}** with three options:

| Menu Item          | Description |
|--------------------|-------------|
| **Create Scaffolding** | Idempotent — creates missing objects, skips existing ones |
| **Clear Scene**        | Destroys all root objects (confirmation dialog, Ctrl+Z undoable) |
| **Clear & Recreate**   | Wipes + rebuilds in one step (no confirmation, undoable) |

Every menu item **auto-switches** to the correct `.unity` scene file first. If the scene doesn't
exist, a new empty one is created at `Assets/Scenes/{SceneName}.unity`.

## Files

| File | Scene |
|------|-------|
| `SceneScaffoldHelper.cs` | Shared helper (EnsureCamera, EnsureCanvas, EnsureButton, etc.) |
| `SplashScreenScaffold.cs` | SplashScreen.unity |
| `TitleScreenScaffold.cs` | TitleScreen.unity |
| `ProfileSelectScaffold.cs` | ProfileSelect.unity |
| `ProfileCreateScaffold.cs` | ProfileCreate.unity |
| `SaveFileSelectScaffold.cs` | SaveFileSelect.unity |
| `StageSelectScaffold.cs` | StageSelect.unity |
| `LoadingScreenScaffold.cs` | LoadingScreen.unity |
| `HubScaffold.cs` | Hub.unity |
| `PostBattleScreenScaffold.cs` | PostBattleScreen.unity |
| `SettingsScaffold.cs` | Settings.unity |
| `CreditsScaffold.cs` | Credits.unity |
| `PartyManagerScaffold.cs` | PartyManager.unity |

## How to Add New Objects to a Scene

1. Open the scaffold `.cs` file for the target scene
2. Add `SceneScaffoldHelper.Ensure*()` calls in `CreateScaffolding()`
3. Run **Tools › Scenes › {Scene} › Create Scaffolding** — new objects appear, existing ones untouched
4. Save the scene

## Shared Helper Methods (`SceneScaffoldHelper`)

### Root-Level
- `EnsureCamera(name)` — Orthographic camera, depth -1, black background, AudioListener
- `EnsureEventSystem()` — EventSystem + StandaloneInputModule
- `EnsureEmptyGameObject(name)` — Plain GO for manager scripts

### Canvas
- `EnsureCanvas(name)` — ScreenSpaceOverlay, CanvasScaler (1920×1080, match 0.5), GraphicRaycaster, CanvasRenderer, Image (background)

### Patterns
- `EnsureFadeOverlay(canvas)` — Full-screen black Image, last sibling, FadeOverlayInstance
- `EnsureCutoutOverlay(canvas)` — CutoutOverlay + Top (LeftPane/CenterPane/RightPane) + Bottom
- `EnsureScrollView(parent)` — Full ScrollRect with Viewport/Content, vertical/horizontal scrollbars
- `EnsureTitle(parent, text)` — TMP title label anchored to top-center
- `EnsureBackButton(parent)` — Button anchored top-left with Label child

### Primitives
- `EnsureRectChild(parent, name)` — RectTransform-only child (stretch-fill)
- `EnsureImage(parent, name, stretch)` — CanvasRenderer + Image
- `EnsureButton(parent, name, label)` — Image + Button + Label (TMP child)
- `EnsureLabel(parent, name, text)` — CanvasRenderer + TMP label
- `EnsureNineSliceFrame(parent)` — 9 border Images (Background, Top, Bottom, Left, Right, corners)

### Scene Management
- `OpenScene(sceneName)` — Auto-switches to scene, prompts save, creates if missing
- `ClearAllRootObjects()` — Destroys all with confirmation dialog
- `ClearAllRootObjectsSilent()` — Destroys all without dialog (for Clear & Recreate)

## Common Scene Patterns

Most scenes share this structure:
```
Main Camera ............ Camera (ortho, depth -1) + AudioListener
EventSystem ............ EventSystem + StandaloneInputModule
{SceneName}Manager ..... MonoBehaviour controller
Canvas ................. Canvas (Overlay) + CanvasScaler + GraphicRaycaster + Image
  ├── CutoutOverlay .... Decorative frame (Top/Bottom with LeftPane/CenterPane/RightPane)
  ├── Title ............ TMP heading
  ├── ScrollView ....... ScrollRect + Viewport/Content + Scrollbars
  ├── BackButton ....... Button + Label
  └── FadeOverlay ...... Black Image (last child, for scene transitions)
```

## Scene Hierarchies (Authoritative Source)

See `Documentation/Scaffolds/SceneHierarchies.txt` for the complete parsed output of every scene
file, including exact RectTransform anchoring, component lists, and child ordering.

## Regenerating SceneHierarchies.txt

Run from project root:
```powershell
$scenes = @('SplashScreen','TitleScreen','ProfileSelect','ProfileCreate','SaveFileSelect','StageSelect','LoadingScreen','Hub','PostBattleScreen','Settings','Credits','PartyManager')
foreach ($s in $scenes) {
    powershell -ExecutionPolicy Bypass -File "Tools\ParseScene.ps1" -ScenePath "Assets\Scenes\$s.unity"
}
```
