# GridGame2026

> *We ate through the world like acid.*
> *Seeping into the Undearth, trickling ever downward,*
> *consuming all we encountered; Never sated, never still.*
>
> *Until we met resistance. Dwellers in the dark.*
> *A people who had never known war or light.*
>
> *We were interlopers. Invaders from above —*
> *a light-bearing race corrupting everything in our wake.*

**A tactical grid RPG of pincer strikes, sliding bodies, and tipping scales.**

You command a band of light-bearing invaders descending into the Undearth — a sunless world of dwellers who have never known war. Every encounter is a puzzle of position: drag your heroes across the grid, shove enemies and allies aside, and trap your foes between two attackers to deliver the only damage that matters.

**Why GridGame2026:**

- **Position is the only weapon.** Movement deals zero damage. Damage is dealt by the *pincer* the new position completes. Every drag is a question: does this finish the line?
- **The slide.** Drag a hero onto an occupied tile and the displaced actor — friend or foe — slides into the tile you just left. Use the shove to set up flanks, eject allies from danger, or feed an enemy into a kill zone.
- **The Timeline is a clock you can control.** Enemies "load" left-to-right along a strip at the top of the screen. The rightmost stretch is the **Pushback Zone** — strike a foe whose icon sits inside it and their turn is shoved back toward spawn. Time your pincers to delay the heaviest hitters indefinitely.
- **Code-only Unity.** Every scene is the rebuilt output of a `*Builder.cs` file. No prefab dragging, no inspector wiring, no `[SerializeField]`. Builders edit, the editor rebuilds, the scene catches up — all without opening Unity's UI.
- **Guardrails enforced at the pre-push hook.** `SerializedFieldBan`, `ResourcesLoadBan`, `InstantiateBan`, and `BuilderDriftChecker` block regressions automatically. The codebase stays code-driven on purpose.

---

## Table of Contents

- [What the game is](#what-the-game-is)
- [Slide. Pincer. Pushback.](#slide-pincer-pushback)
- [Casts, Interrupts, and Clutch Moments](#casts-interrupts-and-clutch-moments)
- [Beyond the Battlefield](#beyond-the-battlefield)
- [Stack](#stack)
- [Getting started (opening the project)](#getting-started-opening-the-project)
- [Repository layout](#repository-layout)
- [Assets/ directory reference](#assets-directory-reference)
- [Helper scripts](#helper-scripts)
- [Testing](#testing)
- [Documentation index](#documentation-index)
- [Code-only workflow (builders)](#code-only-workflow-builders)
- [Guardrails](#guardrails)
- [Known issues / notes](#known-issues--notes)
- [Status](#status)

---

## What the game is

GridGame2026 is a **single-player, turn-structured, grid-position tactical RPG** built in Unity. Combat plays out on a single board (a grid of tiles rendered as 2D sprites on a 3D board via URP). There is no "attack" button in the conventional sense — the entire combat system is built around **where actors end up**, not what button you press:

- The player **drags** a hero from tile to tile, one cardinal step at a time (`ActorMovement.TowardDestinationRoutine()` — diagonals do not exist).
- Dragging a hero onto an occupied tile **displaces** whatever was there (ally or enemy) into the tile the hero just vacated (`ActorMovement.CheckLocationChanged()` → `HandleOverlap()`). This is "the slide."
- Damage is never dealt by moving onto an enemy. Damage is dealt when a drag completes a **pincer**: two heroes sharing a row or column with an unbroken line of enemies between them (`PincerAttackManager.Check()`).
- Turn order is driven by a horizontal **Timeline** strip (a Grandia-style "IP gauge") where enemy icons load left-to-right; reaching the right edge queues that enemy's turn (`TimelineBarInstance`, `TurnManager.IsHeroTurn` / `HasQueuedEnemyAfterHero`).
- Striking an enemy whose icon has already entered the rightmost **Pushback Zone** shoves it back toward spawn instead, delaying its turn (`TimelineBarInstance.PushbackOnAttack()`).
- Spellcasting shares a 12-orb mana bank and resolves as its own icon riding the same Timeline; getting hit mid-cast can Fail, get Pushed back, or (rarely) Clutch — snap-resolve on the spot (`ManaPoolManager`, `CastingState`, `CastInterruptResolver`).

Outside of combat, the game has a hub/vendor meta-loop: a stage-select hub connects themed campaigns of hand-built battle stages, and dedicated vendor scenes (Alchemist, Blacksmith, Equip, Party, Abilities) handle crafting, gear, and party management. Progress persists through a `Profile` → `SaveState` JSON save system.

The full, authoritative combat-rules writeup (with exact class/method names, verified against the code) lives in [`CLAUDE.md`](CLAUDE.md) under **Core Game Loop**, and the canonical design/rules source of truth is [`docs/BIBLE.md`](docs/BIBLE.md).

## Slide. Pincer. Pushback.

- **Pincer combat.** Damage is never dealt by movement — it's dealt by *position*. Line up two heroes on the same row or column with an unbroken file of enemies between them and the pincer fires. Chain pairs together for cascading volleys that clear entire ranks in a single drag.
- **The slide.** Drag a hero onto an occupied tile and the displaced actor — friend or foe — slides into the tile you just left. Use the shove to set up flanks, eject allies from danger, or feed an enemy into a kill zone.
- **The Timeline.** Enemies "load" left-to-right along a strip at the top of the screen. The rightmost stretch is the **Pushback Zone** — strike a foe whose icon sits inside it and their turn is shoved back toward spawn. Time your pincers to delay the heaviest hitters indefinitely.
- **Supporters.** Allies adjacent to either end of a pincer pile on bonus damage. Stack your formation to turn a routine attack into a cleave.

## Casts, Interrupts, and Clutch Moments

Mana is a shared 12-orb bank harvested from pincers; spend it on abilities that travel their own icon down the timeline strip. Take a hit mid-cast and roll one of three outcomes — **Fail**, **Pushback**, or, when your luck holds, **Clutch!** — where the spell snaps to the trigger and resolves in the same instant the caster crumples. A dying healer can still let off one last miracle.

## Beyond the Battlefield

- **Themed campaigns** of hand-built stages connected by a stage-select hub
- **Vendor scenes** for every craft — Alchemist, Blacksmith, Equip, Party, Abilities
- **Weapon durability** with shatter rebound, dual-damage on break, and decaying repair caps
- **Original lore** and self-authored world building set in the corrupted depths of the Undearth
- **Custom ShaderLab** effects driving the 2D-on-3D presentation

---

## Stack

| Layer | Technology |
|---|---|
| **Engine** | Unity **6000.4.3f1** (Unity 6) |
| **Scripting** | C# 9, targeting .NET Standard 2.1 |
| **Root namespace** | `Scripts.*` (project `RootNamespace` is intentionally left empty in the csproj) |
| **Rendering** | 2D sprites on a 3D board, URP 17.4.0, custom ShaderLab effects |
| **Asset loading** | Addressables 2.9.1 only — no `Resources.Load` in new code |
| **Persistence** | `Profile` → `SaveState` JSON; XP stored as `TotalXP`, derived at runtime |
| **Testing** | Unity Test Framework 1.6.0 — Edit Mode + Play Mode (NUnit under the hood) |
| **Build target** | `win-x64` — `StandaloneWindows64` |

## Getting started (opening the project)

1. **Install the exact Unity editor version.** `ProjectSettings/ProjectVersion.txt` pins this project to `6000.4.3f1` (revision `39d1a88d4dd1`). Install it via Unity Hub. Opening the project with a different installed editor version will trigger a re-import and can corrupt the `Library/` package cache — use the pinned version.
2. **Clone the repository** (or let `GridGame.Console.ps1` option **4 — Setup** do it for you; see [Helper scripts](#helper-scripts) below):
   ```
   git clone https://github.com/mindattic/GridGame2026.git
   ```
3. **Open via Unity Hub**, pointing it at the cloned folder. First import will be slow (asset database rebuild); subsequent opens are fast.
4. **Activate the pre-push git hook** so the guardrails run automatically:
   ```
   git config core.hooksPath .githooks
   ```
   (`GridGame.Console.ps1` option 4 does this for you and is idempotent — safe to re-run.)
5. **Press Play** in the Unity Editor, or use `GridGame.Console.ps1` option 1 to launch the editor directly. The scene that loads on Play is controlled by `Assets/Scripts/Data/Config/StartSceneConfig.cs` — see option 6 below to change it.
6. To build a standalone Windows player without opening the editor UI, either run `GridGame.Console.ps1` option 5, or run `Run.bat` at the repo root, which builds on first run if `Build\Windows\GridGame.exe` doesn't exist yet, then launches it.

There is no separate package-restore step beyond Unity's own import — `Packages/` is a standard Unity Package Manager manifest, resolved automatically on first open.

## Repository layout

```
GridGame2026/
├── Assets/
│   ├── Scripts/                   # Game code (root namespace: Scripts.*)
│   │   ├── Data/                  # Static data definitions (ItemData_*, ActorData_*, SkillData_*)
│   │   ├── Models/                # Data structures, enums, Singleton<T>
│   │   ├── Managers/              # Singleton game systems (TurnManager, PincerAttackManager, ...)
│   │   ├── Instances/              # Runtime MonoBehaviours (ActorInstance, ActorMovement, ...)
│   │   ├── Sequences/               # Async combat/UI event queue
│   │   ├── Canvas/                 # In-game HUD (TimelineBar, TimelineIcon, ...)
│   │   ├── Hub/                    # Shared vendor-UI utilities (HubTheme, HubToast)
│   │   ├── Factories/               # Object instantiation (only place Instantiate() is allowed)
│   │   ├── Libraries/                # Lazy-loaded registries (ItemLibrary, ActorLibrary, ...)
│   │   ├── Services/                # Pure-logic helpers (EnemyPlanner, PincerDetector, CastInterruptResolver, ...)
│   │   ├── Helpers/                 # GameHelper (the global accessor — `using g = ...`)
│   │   ├── Abilities/                # Ability scene UI and logic
│   │   ├── Alchemist/               # Alchemist vendor scene
│   │   ├── Blacksmith/               # Blacksmith vendor scene
│   │   ├── Equip/                    # Equipment vendor scene
│   │   ├── Inventory/                # Inventory and equipment models
│   │   ├── Party/                    # Party management scene
│   │   ├── Vendor/                   # Shared vendor utilities
│   │   ├── Overworld/                # Top-down exploration
│   │   ├── Effects/                  # Screen-space visual effects
│   │   ├── Serialization/             # Save/load helpers
│   │   └── Utilities/                 # Formulas.cs, RNG.cs, Extensions.cs, Geometry.cs, AspectGuard.cs
│   ├── Editor/
│   │   ├── Builders/               # *Builder.cs — the source of truth for every scene
│   │   ├── BuilderDriftChecker.cs  # Guardrail: scene YAML vs. builder output
│   │   ├── CliEntryPoints.cs       # Batchmode entry points (BuildStandaloneWindows, guardrails, ...)
│   │   ├── InstantiateBan.cs       # Guardrail: Instantiate() outside *Factory.cs
│   │   ├── ResourcesLoadBan.cs     # Guardrail: Resources.Load* call-sites
│   │   ├── SerializedFieldBan.cs   # Guardrail: new [SerializeField] in Scripts/
│   │   ├── DebugWindow.*.cs        # In-editor debug/dev-tools window (11 partial-class files)
│   │   └── *Allowlist.txt          # Curated exceptions to the four guardrails
│   ├── Scenes/                     # 21 .unity scenes (see Assets/ directory reference below)
│   └── Tests/PlayMode/             # Play Mode test fixtures (Tests.PlayMode assembly)
├── Documentation/                  # Human/LLM-readable technical docs (builder snapshots, scene hierarchies, style guide)
├── docs/                           # Codex canon — BIBLE.md, AMENDMENTS.md, USER_STORIES.md, data/, rfc/
├── Tools/                          # codex.ps1 (doctor/digest), ParseScene.ps1, build-readme.ps1
├── GridGame.Console.ps1            # Top-level operator console (see Helper scripts)
├── GridGame.Console.bat            # Launches the console above in a titled PowerShell tab
├── Run.bat                         # Build-if-missing + launch the standalone player
├── Backup.ps1                      # Legacy backup script (stale — see Known issues)
├── Export.ps1                      # Dumps all .cs sources into one text bundle for LLM context
├── COMMIT.cmd                      # Empty/dead file (see Known issues)
└── README.md                       # <- you are here
```

## Assets/ directory reference

Top-level folders under `Assets/` (folders only): `Adaptive Performance/, AddressableAssetsData/, Animations/, Animator/, Devices/, Documentation/, Editor/, Fonts/, Lights/, Maps/, Materials/, Mesh/, MusicTracks/, Others/, Particles/, Plugins/, PostProcessing/, Prefabs/, Resources/, Scenes/, Scripts/, Settings/, Shader/, Shared/, SoundEffects/, Sprites/, Synergy/, Tests/, TextMesh Pro/, Textures/, URP/, VisualEffects/`.

`Assets/Scripts/` (699 `.cs` files total under `Assets/`) matches the [Repository layout](#repository-layout) tree above exactly — see [`CLAUDE.md`](CLAUDE.md) → *Architecture* for the full per-folder purpose breakdown, including the Global Access Pattern (`using g = Scripts.Helpers.GameHelper;`), the Actor system, the Save/Persistence layer, and the Scene Builder system.

`Assets/Editor/` holds every editor-only tool: the four guardrail scripts + their allowlists, `CliEntryPoints.cs` (batchmode entry points), the `Builders/` folder (one `*Builder.cs` per scene — see [`Documentation/Builders/README.md`](Documentation/Builders/README.md)), a family of analyzers (`AddressablesAnalyzer`, `AnimatorAnalyzer`, `PrefabAnalyzer`, `ProjectSettingsAnalyzer`, `SceneAnalyzer`, `ScriptableObjectAnalyzer`), `StartSceneAuthority.cs`, `SpriteAssetAuthor.cs`, `VfxPrefabAuthor.cs`, `EightWayAnimatorGenerator.cs`, `AltTesterStripper.cs` (see [`Documentation/AltTester-Setup.md`](Documentation/AltTester-Setup.md)), and the 11-file `DebugWindow.*.cs` in-editor debug window.

`Assets/Scenes/` contains 21 `.unity` files: `SplashScreen, TitleScreen, ProfileSelect, ProfileCreate, SaveFileSelect, StageSelect, LoadingScreen, Hub, PostBattleScreen, Settings, Credits, Party, Abilities, Alchemist, Blacksmith, Equip, Vendor, Bestiary, Game, Overworld` (plus a `Game/` subfolder). Every one of these except `Game` and `Overworld` is fully reproducible from its matching `Assets/Editor/Builders/*Builder.cs` — see [Code-only workflow (builders)](#code-only-workflow-builders).

## Helper scripts

| Script | What it does |
|---|---|
| **`GridGame.Console.ps1`** | The operator console — an interactive PowerShell menu with 6 numbered operations (below). This is the primary way a human drives the repo day-to-day. |
| **`GridGame.Console.bat`** | Trivial launcher: opens a titled (`Main Menu`), `-NoExit` PowerShell window running `GridGame.Console.ps1`. |
| **`Run.bat`** | Checks for `Build\Windows\GridGame.exe`. If it's missing, builds it via a headless Unity batchmode call to `CliEntryPoints.BuildStandaloneWindows` (hardcoded to Unity `6000.3.2f1` — see [Known issues](#known-issues--notes)), then launches the exe either way. |
| **`Backup.ps1`** | **Legacy / stale** — its default parameters (`Source = D:\Projects\Unity\GridGame2025`, `BackupBase = R:\Backup\SnowCrash`) point at a *prior* project (GridGame2025 / SnowCrash), not this repo. The actual "Create Backup" menu option runs different, inline logic in `GridGame.Console.ps1` (see below) — this file is not wired to it. |
| **`Export.ps1`** | Standalone dev utility (not called by the console menu). Recursively scans the repo for `.cs` files (skipping `Library/Temp/Logs/obj/.git/.vs/Build/Builds/Packages`), computes a SHA256 hash + line count + GUID (from the `.meta` file) per source file, and writes one big `ExportedScripts.txt` bundle (JSON manifest + full file contents) — used to hand the whole codebase to an LLM in a single file. |
| **`COMMIT.cmd`** | **Empty (0 bytes) — dead file.** Superseded by `GridGame.Console.ps1` option 2 (Commit and Sync). |

**`GridGame.Console.ps1` menu:**

| # | Operation | Notes |
|---|---|---|
| 1 | Run Application | Launches the Unity editor (`Unity.exe -projectPath .`). |
| 2 | Commit and Sync | `git add -A`, prompts for a commit message, `git commit`, `git push`. Aborts cleanly on an empty message or a failed commit/push. |
| 3 | Create Backup | Copies the whole repo (excluding `Library`, `Temp`, `obj`, `Logs`) to `R:\Backup\GridGame\yyyy-MM-dd`, with a letter-suffix (`a`, `b`, `c`, …) appended on same-day collisions. |
| 4 | Setup | One-time, idempotent: clone-or-pull the repo, set `git config core.hooksPath .githooks` to activate the pre-push guardrail hook, and launch Unity for the initial project import. Safe to re-run. |
| 5 | Build Player (headless) | Runs `CliEntryPoints.BuildStandaloneWindows` via Unity batchmode, logging to `Logs/cli-<timestamp>.log`. |
| 6 | Set Start Scene | Lists every `.unity` file under `Assets/Scenes`, lets you pick one, and rewrites the `StartScene` constant in `Assets/Scripts/Data/Config/StartSceneConfig.cs` via regex replace. `StartSceneAuthority` (an `[InitializeOnLoad]` watcher) then applies the change to `playModeStartScene` and `EditorBuildSettings.scenes[0]` on the next domain reload. |

Headless invocations from an automated agent / CI:

```powershell
Unity -batchmode -nographics -projectPath . `
  -executeMethod CliEntryPoints.<Method> -quit -logFile -
```

Exit code `0` = success, `1` = failure.

## Testing

- **Framework:** Unity Test Framework 1.6.0, NUnit underneath.
- **Assemblies:** the solution (`GridGame2026.slnx`) references exactly three C# projects — `Assembly-CSharp.csproj` (game code), `Assembly-CSharp-Editor.csproj` (editor tooling), and `Tests.PlayMode.csproj` (Play Mode tests, `RootNamespace = Scripts.Tests.PlayMode`, assembly name `Tests.PlayMode`).
- **Current test coverage:** `Tests.PlayMode.csproj` currently compiles a single Play Mode test file, `Assets/Tests/PlayMode/PincerScenarioTest.cs` (assembly definition: `Assets/Tests/PlayMode/Tests.PlayMode.asmdef`).
- **Run from the Editor:** Window → General → Test Runner, PlayMode tab.
- **Run headless:** wire a batchmode call through `CliEntryPoints` (see `CliEntryPoints.CheckAllGuardrails` / `RunEditTests` referenced in `CLAUDE.md`'s batchmode duties table) the same way as the build/guardrail entry points above.

## Documentation index

Documentation is split across two top-level folders with distinct roles — don't confuse them:

| Folder | Role |
|---|---|
| **`docs/`** | The **MindAttic Codex canon** — the layered source of truth for what the game *is* and the rules governing it. `BIBLE.md` (L0, the Laws, `{#GG-LAW-n}`), `AMENDMENTS.md` (L1, append-only, wins over the bible), `USER_STORIES.md` (L2, the dependency-ordered build board, `US-NNN`), `data/*.json` (L5, canon-as-data for spells/buffs/classes/enemy archetypes/item rarities, schema-validated), `rfc/` (design notes awaiting graduation), `BIBLE.digest.md` (generated — never hand-edit). |
| **`Documentation/`** | Human/LLM-readable *technical* docs — not Codex canon. `DOCUMENTATION_STYLE_GUIDE.md`, `Addressables.md`, `AltTester-Setup.md`, `ProjectSettings.md`, `Builders/README.md` + `Builders/SceneHierarchies.txt` (parsed scene-hierarchy dump, authoritative for scene structure) + `Builders/Drift/*.snapshot.txt` (guardrail snapshots), `Scenes/*_Hierarchy.md` (one file per scene). |

Legacy root pointer files `game_bible.md` and `user_stories.md` are 1-line "Moved." redirects to `docs/` — kept only so old links don't 404.

For the exhaustive, code-verified combat-mechanics writeup (exact class and method names), see [`CLAUDE.md`](CLAUDE.md) → *Core Game Loop* and *Architecture*.

## Code-only workflow (builders)

The project is authored to run **without opening Unity's editor UI**. Every `.unity` scene is the regenerated output of a corresponding `Assets/Editor/Builders/*Builder.cs`. The builder is the source of truth; the `.unity` is the build artifact.

- **New GameObjects** → add to the scene's builder. `BuilderAutoRebuild` regenerates the `.unity` after the next domain reload.
- **New UI** → extend the existing factory pattern (`ActorFactory`, `HubItemRowFactory`, …). Do not create new `.prefab` files.
- **New assets** (sprite, font, audio) → register an Addressable address and load via `AssetHelper.LoadAssetAsync<T>(address)`. Do not add inspector drag-drop references.
- **Avoid new `[SerializeField]`.** Initialize from data-layer statics (`ItemData_*`, `SkillData_*`, `ActorData_*`) or factory parameters.

**Builder → Scene auto-rebuild.** `BuilderAutoRebuild.cs` is an `[InitializeOnLoad]` watcher. After every domain reload it diffs each `*Builder.cs` mtime against `Library/BuilderMTimes.json` and rebuilds the matching `Assets/Scenes/{Name}.unity`. Manual escape hatch: **Tools › Scenes › Rebuild All** or `CliEntryPoints.BuilderAllScenes` in batchmode.

**The reverse direction (`.unity` → builder) is intentionally absent.** A scene file is YAML; a builder is C#; the mapping requires judgment. Hand-edited scenes get caught by `BuilderDriftChecker` so the discrepancy can't sneak past pre-push.

## Guardrails

Auto-enforced pre-push via `.githooks/pre-push` (activated by Setup option 4):

| Guardrail | What it blocks | Allowlist |
|---|---|---|
| `SerializedFieldBan` | new `[SerializeField]` fields in `Scripts/` | `Assets/Editor/SerializedFieldAllowlist.txt` |
| `ResourcesLoadBan` | new `Resources.Load*` call-sites | `Assets/Editor/ResourcesLoadAllowlist.txt` |
| `InstantiateBan` | `Instantiate(` outside `*Factory.cs` | `Assets/Editor/InstantiateAllowlist.txt` |
| `BuilderDriftChecker` | scene YAML drifting from its builder's output | `Documentation/Builders/Drift/*.snapshot.txt` |

`CliEntryPoints.CheckAllGuardrails` runs all four in one batchmode session — run it before handing work back. Bypass for hotfixes with `git push --no-verify`.

## Known issues / notes

Verified discrepancies worth flagging (documented here rather than silently fixed):

- **Unity editor version mismatch.** `GridGame.Console.ps1` and `Run.bat` both hardcode the Unity editor path to `6000.3.2f1`, but `ProjectSettings/ProjectVersion.txt` — the authoritative version per `CLAUDE.md` — is `6000.4.3f1`. If your Hub install is `6000.4.3f1` only, options 1, 4, and 5 in the console (and `Run.bat`'s auto-build path) will report "Unity editor not found."
- **`Backup.ps1` is a leftover from a prior project.** Its default parameters point at `D:\Projects\Unity\GridGame2025` / `R:\Backup\SnowCrash`. The real "Create Backup" logic lives inline in `GridGame.Console.ps1` (option 3), which correctly targets this repo and `R:\Backup\GridGame`.
- **`COMMIT.cmd` is an empty, dead file.**
- **A second copy of `SceneHierarchies.txt`** exists at `Tools/SceneHierarchies.txt`, same byte size as the authoritative `Documentation/Builders/SceneHierarchies.txt` — appears to be a stray duplicate rather than a second source of truth.
- **An orphaned Node.js-based landing-page build** is described by the root `package.json` (`gridgame2026-landing`, scripts pointing at `scripts/cli/build-html.js` and `scripts/cli/deploy.ps1`), but neither script file exists and `scripts/cli/` is empty. This appears superseded by the shared `codex-standard/build-readme.ps1` engine used by `Tools/build-readme.ps1` (see below).

## Status

Active development. Single-developer project. The combat loop (slide / pincer / supporters / pushback / buffs / mana economy), the cast-as-timeline-icon system, the Fail/Pushback/Clutch cast-stagger interrupt resolver, enemy charge casts + interrupt→orb mint, boss scripted phases, and the full battle↔vendor macro loop are all implemented and play-tested. See `CLAUDE.md` and `docs/BIBLE.md` for the current verified state. Active frontier: Epic G (UI polish / accessibility) and Epic H (performance hardening).
