# AltTester Setup — UI-driven Pincer Test for GridGame2026

> **CURRENTLY DISABLED.** The `com.alttester.sdk` package has been removed from `Packages/manifest.json`. The integration code (`AltTesterGuard.cs`, `AltTesterStripper.cs`, `GameBuilder.TryAddAltRunner`, `PincerScenarioTest.cs`'s `#if ALTTESTER` block) is intact and dormant — it gracefully no-ops when the package is absent, so nothing else needs to change.
>
> **To re-enable**, restore the three lines that were removed:
> 1. `Packages/manifest.json` → add back `"com.alttester.sdk": "https://github.com/alttester/AltTester-Unity-SDK.git?path=Assets/AltTester",` to `dependencies`.
> 2. `Assets/Tests/PlayMode/Tests.PlayMode.asmdef` → add `"AltTester.AltTesterUnitySDK.Driver"` to `references`.
> 3. `Packages/packages-lock.json` regenerates automatically on next Editor launch.
>
> After re-enabling, run `Tools › Scenes › Rebuild All` so `GameBuilder.TryAddAltRunner()` re-adds the `AltRunner` GameObject to `Game.unity`.

This project ships a PlayMode test that drives the Game scene through Unity's Test Framework. With the AltTester SDK installed, the same test can additionally exercise the pincer-attack flow end-to-end through an external WebSocket driver (similar to Cypress for a web page).

## Files in this scaffold

```
Assets/Tests/PlayMode/
├── Tests.PlayMode.asmdef          // PlayMode assembly definition; ALTTESTER define-gated
└── PincerScenarioTest.cs          // Smoke test (always runs) + AltDriver pincer scenario (#if ALTTESTER)
```

The asmdef already has a `versionDefines` entry that defines `ALTTESTER` automatically when the `com.alttester.sdk` package is present. No manual scripting-define change required.

## Two run modes

| Mode | What runs | Setup |
|---|---|---|
| **Smoke only** | `Game_scene_boots_with_core_managers` — loads Game scene, asserts `GameManager` exists | None beyond Test Framework |
| **Full pincer scenario** | `Pincer_drop_damages_flanked_enemy` — connects via AltDriver, drops a hero to complete a pincer, asserts enemy HP dropped | Requires AltTester SDK + AltRunner in scene |

The full scenario currently calls `TryFindPincerSetup` which is a stub that returns false — the test marks itself **Inconclusive** until you replace the stub with deterministic board-setup logic (or wire a `TestHooks` helper that places actors at fixed grid positions before each test).

## Installing AltTester

The AltTester SDK is open source on GitHub: <https://github.com/alttester/AltTester-Unity-SDK>. Install it through Unity Package Manager:

1. Open **Window → Package Manager**.
2. Click the **+** dropdown → **Add package from git URL...**.
3. Paste: `https://github.com/alttester/AltTester-Unity-SDK.git?path=Assets/AltTester`
4. Wait for Unity to import. The `ALTTESTER` symbol now appears in PlayMode test compilation (via the asmdef `versionDefines`).

Confirmed coordinates (as of repo `development` branch, package version 2.3.1-hotfix.1):
- **Package name**: `com.alttester.sdk`
- **Subpath in repo**: `Assets/AltTester` (no leading slash, no `AltTesterUnitySDK` suffix)

Alternative: use OpenUPM if you prefer a versioned registry. Refer to the SDK README for the current OpenUPM coordinates.

## Wiring AltRunner into the Game scene

AltTester needs an `AltRunner` MonoBehaviour somewhere in the scene to start the WebSocket server. Three options, ranked by build-hygiene:

### Option A — Reflection-based detection in `GameBuilder.cs` (recommended — already wired)

`GameBuilder.Build()` calls `TryAddAltRunner()` at the end. That helper looks up
`AltTester.AltTesterUnitySDK.Commands.AltRunner` via reflection: if the SDK is installed the
type is found and a runner GameObject is added to Game.unity; if not, the method silently
no-ops. No `#if` directives, no project-wide define management, no compile-time package
dependency on `GameBuilder` — the file compiles whether AltTester is installed or not.

After installing the SDK, run **Tools → Scenes → Game → Checkout** to regenerate `Game.unity`
with the runner. The Console will log `[GameBuilder] AltTester detected — AltRunner added…`.

**Don't ship to production with it — already handled:** the AltRunner opens a WebSocket on
`:13000`. Whenever `TryAddAltRunner()` adds the runner GameObject it also adds a sibling
`Scripts.Helpers.AltTesterGuard` component. On `Awake()` that guard checks
`Debug.isDebugBuild`; if false (i.e. a Release / non-Development player build), it calls
`Destroy(gameObject)` and the AltRunner never starts its WebSocket. Editor Play Mode and
Development builds keep the runner alive.

*(Why not just wrap `TryAddAltRunner()` in `Debug.isDebugBuild`? `Build()` runs at Editor
time, where `Debug.isDebugBuild` is always true — so an Editor-side check can't predict
which build profile you'll ship. The runtime guard defers the decision to the player's
first frame, where it's always answerable.)*

**Belt-and-suspenders strip — already wired:** `Assets/Editor/AltTesterStripper.cs`
implements `IProcessSceneWithReport` and runs for every scene during the build pipeline.
For Release builds (no `BuildOptions.Development` flag) it walks each scene's root objects
and `DestroyImmediate`s any GameObject named `"AltRunner"` *before* the scene is serialized
into the player. Development builds and Editor Play Mode are untouched (PlayMode tests
still find the runner). So the protection is layered:

| Build path | What removes AltRunner |
|---|---|
| Editor Play Mode | nothing — runner is live for tests |
| Development player build | nothing — runner is live for AltDriver tests against the running player |
| Release player build | **scene processor** strips it pre-serialization; if any slipped through, **runtime guard** destroys it on first `Awake()` |

If you ever need to bypass the stripper temporarily (e.g. ad-hoc test of the runner in a
non-Dev build), open Build Settings and enable Development Build before building — same flag
the stripper checks.

### Option B — Drag the shipped prefab

The SDK ships `AltRunnerPrefab` under `Assets/AltTester/AltTesterUnitySDK/Prefabs/`. Drag it into the Game scene root and save. **Caveat:** this couples your scene file to a development-only dependency. If you go this route, exclude AltRunner from Release builds via a script-execution filter.

### Option C — Per-test instrumentation

Call `AltTester.AltTesterUnitySDK.AltRunner.StartInstrumentation()` from `[UnitySetUp]` and `StopInstrumentation()` from `[TearDown]`. Cleanest but requires every PlayMode test to opt in.

## Running the test suite

1. **Window → General → Test Runner**
2. Switch to the **PlayMode** tab
3. Select `Tests.PlayMode` → click **Run All**

For headless / CI execution from a shell:

```
Unity -batchmode -projectPath D:\Projects\MindAttic\GridGame2026 \
      -runTests -testPlatform PlayMode -testResults playmode-results.xml -quit
```

(Note: PlayMode tests *cannot* run with `-nographics` — Unity needs a display context, even a virtual one. On Windows CI, use a real or virtual desktop session.)

## Replacing the `TryFindPincerSetup` stub

The stub is the only thing standing between you and a green pincer scenario. Two paths:

### Quickest — deterministic test scene

Create `Assets/Scenes/PincerTest.unity` with a tiny `PincerTestBuilder` that places:
- Hero A at `(2, 5)`
- Hero B at `(2, 7)` (free for the test to move)
- Enemy at `(2, 6)` (already between A's column and the spot B needs to land on)

Then `TryFindPincerSetup` simply returns those known objects + `pincerTile = (2, 7)` — no search logic needed.

### Cleanest — add a `TestHooks` static helper to the game

```csharp
// Assets/Scripts/Helpers/TestHooks.cs
namespace Scripts.Helpers
{
    public static class TestHooks
    {
        public static void PlaceActorAt(string characterClass, Vector2Int location) { ... }
        public static int  GetActorHp(string characterClass) { ... }
        public static bool SequenceManagerIsExecuting() => g.SequenceManager.IsExecuting;
    }
}
```

Then AltDriver calls `Scripts.Helpers.TestHooks.PlaceActorAt("Paladin", new Vector2Int(2, 5))` to set up any board state before driving the test.

## What this gives you long-term

- **Regression coverage for the pincer mechanic** — every change to `PincerAttackManager.Check`, `SelectionManager.Drop`, or actor displacement runs against this scenario in CI.
- **A foundation for additional gameplay tests** — once the AltDriver bridge is wired, copy the pattern: enemy-turn AI, weapon shatter, ability casting, equipment swap. Each scenario is ~50 lines of test code.
- **Recording the contract between game systems** — the test calls `g.SequenceManager.IsExecuting` and `Stats.HP`. If those rename, the test breaks early — surfacing the integration boundary before runtime players hit it.
