# Scene Builder System

## Overview

Every scene in the game is **fully recreated from code** using Editor builder scripts under `Assets/Editor/Builders/`. Scene `.unity` files are reproducible artifacts — if a scene gets corrupted or needs resetting, run the builder. The builder is the source of truth; the `.unity` is the build artifact.

## Menu Structure

All builders live under **Tools › Scenes › {SceneName}** with three options:

| Menu Item | Description |
|---|---|
| **Create Building** | Idempotent — creates missing objects, skips existing ones |
| **Clear Scene** | Destroys all root objects (confirmation dialog, Ctrl+Z undoable) |
| **Clear & Recreate** | Wipes + rebuilds in one step (no confirmation, undoable) |

Every menu item **auto-switches** to the correct `.unity` scene file first. If the scene does not exist, a new empty one is created at `Assets/Scenes/{SceneName}.unity`.

## Builder Files

| File | Scene |
|---|---|
| `SceneBuilderHelper.cs` | Shared helper (EnsureCamera, EnsureCanvas, EnsureButton, etc.) |
| `UiKit.cs` | Shared UI primitives used by builders |
| `VendorNavBarBuilder.cs` | VendorNavBar (floating hamburger — injected into vendor scenes) |
| `SplashScreenBuilder.cs` | SplashScreen.unity |
| `TitleScreenBuilder.cs` | TitleScreen.unity |
| `ProfileSelectBuilder.cs` | ProfileSelect.unity |
| `ProfileCreateBuilder.cs` | ProfileCreate.unity |
| `SaveFileSelectBuilder.cs` | SaveFileSelect.unity |
| `StageSelectBuilder.cs` | StageSelect.unity |
| `LoadingScreenBuilder.cs` | LoadingScreen.unity |
| `HubBuilder.cs` | Hub.unity |
| `PostBattleScreenBuilder.cs` | PostBattleScreen.unity |
| `SettingsBuilder.cs` | Settings.unity |
| `CreditsBuilder.cs` | Credits.unity |
| `PartyBuilder.cs` | Party.unity |
| `AbilitiesBuilder.cs` | Abilities.unity |
| `AlchemistBuilder.cs` | Alchemist.unity |
| `BlacksmithBuilder.cs` | Blacksmith.unity |
| `EquipBuilder.cs` | Equip.unity |
| `VendorBuilder.cs` | Vendor.unity |
| `BestiaryBuilder.cs` | Bestiary.unity |
| `GameBuilder.cs` | Game.unity |
| `OverworldBuilder.cs` | Overworld.unity |

## Auto-Rebuild

`BuilderAutoRebuild.cs` (`[InitializeOnLoad]`) runs after every domain reload. It diffs each `*Builder.cs` mtime against `Library/BuilderMTimes.json` and rebuilds the matching `Assets/Scenes/{Name}.unity` automatically.

- If the rebuilt scene is currently loaded in the Editor, it is reloaded in place (in-editor edits are lost — builders win).
- Rebuilds are deferred while in Play Mode and resume on exit.
- First launch with no cache records mtimes silently — no rebuild on a fresh checkout.
- Manual escape hatch: **Tools › Scenes › Rebuild All** or `CliEntryPoints.BuilderAllScenes` in batchmode.

**The reverse direction (`.unity` → builder) is intentionally absent.** A `.unity` is YAML; a builder is C#. Translating one to the other requires judgment. Hand-edited scenes are caught by `BuilderDriftChecker` at pre-push.

## How to Add New Objects to a Scene

1. Open the builder `.cs` file for the target scene
2. Add `SceneBuilderHelper.Ensure*()` or `UiKit.*()` calls in `Build()`
3. Run **Tools › Scenes › {Scene} › Create Building** — new objects appear, existing ones untouched
4. Save the scene

## Shared Helper Methods (`SceneBuilderHelper`)

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

## Common Scene Pattern

Most non-game scenes share this structure:
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

See `Documentation/Builders/SceneHierarchies.txt` for the complete parsed output of every scene file, including exact RectTransform anchoring, component lists, and child ordering.

## Drift Snapshots

`Documentation/Builders/Drift/` contains `*.snapshot.txt` files used by `BuilderDriftChecker`. Each snapshot is the expected YAML fingerprint of a builder's output. If a scene's live YAML diverges from its snapshot, the pre-push guardrail blocks the push.

Regenerate snapshots after an intentional builder change:
```powershell
Unity -batchmode -nographics -projectPath . `
  -executeMethod CliEntryPoints.RegenerateBuilderSnapshots -quit -logFile -
```
