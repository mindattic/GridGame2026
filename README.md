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

- **Position is the only weapon.** Movement deals zero damage. Damage is dealt by the *pincer* the new position completes. Every drop is a question: does this finish the line?
- **The slide.** Drag a hero onto an occupied tile and the displaced actor — friend or foe — slides into the tile you just left. Use the shove to set up flanks, eject allies from danger, or feed an enemy into a kill zone.
- **The Timeline is a clock you can control.** Enemies "load" left-to-right along a strip at the top of the screen. The rightmost stretch is the **Pushback Zone** — strike a foe whose icon sits inside it and their turn is shoved back toward spawn. Time your pincers to delay the heaviest hitters indefinitely.
- **Code-only Unity.** Every scene is the rebuilt output of a `*Builder.cs` file. No prefab dragging, no inspector wiring, no `[SerializeField]`. Builders edit, the editor rebuilds, the scene catches up — all without opening Unity's UI.
- **Guardrails enforced at the pre-push hook.** `SerializedFieldBan`, `ResourcesLoadBan`, `InstantiateBan`, and `BuilderDriftChecker` block regressions automatically. The codebase stays code-driven on purpose.

---

## Table of Contents

- [Slide. Pincer. Pushback.](#slide-pincer-pushback)
- [Casts, Interrupts, and Clutch Moments](#casts-interrupts-and-clutch-moments)
- [Beyond the Battlefield](#beyond-the-battlefield)
- [Stack](#stack)
- [Repository layout](#repository-layout)
- [Running the project](#running-the-project)
- [Code-only workflow (builders)](#code-only-workflow-builders)
- [Guardrails](#guardrails)
- [Status](#status)

---

## Slide. Pincer. Pushback.

- **Pincer combat.** Damage is never dealt by movement — it's dealt by *position*. Line up two heroes on the same row or column with an unbroken file of enemies between them and the pincer fires. Chain pairs together for cascading volleys that clear entire ranks in a single drop.
- **The slide.** Drag a hero onto an occupied tile and the displaced actor — friend or foe — slides into the tile you just left. Use the shove to set up flanks, eject allies from danger, or feed an enemy into a kill zone.
- **The Timeline.** Enemies "load" left-to-right along a strip at the top of the screen. The rightmost stretch is the **Pushback Zone** — strike a foe whose icon sits inside it and their turn is shoved back toward spawn. Time your pincers to delay the heaviest hitters indefinitely.
- **Supporters.** Allies adjacent to either end of a pincer pile on bonus damage. Stack your formation to turn a routine attack into a cleave.

## Casts, Interrupts, and Clutch Moments

Mana ticks in real time while the timeline advances; bank it for burst, or spend it on spells that travel their own icon down the strip. Take a hit mid-cast and roll one of three outcomes — **Fail**, **Pushback**, or, when your luck holds, **Clutch!** — where the spell snaps to the trigger and resolves in the same instant the caster crumples. A dying healer can still let off one last miracle.

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
| **Engine** | Unity **6000.3.2f1** (Unity 6) |
| **Scripting** | C# 9, targeting .NET Standard 2.1 |
| **Root namespace** | `Scripts.*` |
| **Rendering** | 2D sprites on a 3D board, custom ShaderLab effects |
| **Persistence** | `Profile` → `SaveState` JSON; XP stored as `TotalXP`, derived at runtime |
| **Asset loading** | Addressables only — no `Resources.Load` in new code |
| **Testing** | Unity Test Framework — Edit Mode + Play Mode |

## Repository layout

```
GridGame2026/
├── Assets/
│   ├── Scripts/                   # Game code (root namespace: Scripts.*)
│   │   ├── Data/                  # Static data definitions (ItemData_*, ActorData_*, SkillData_*)
│   │   ├── Models/                # Data structures, enums, Singleton<T>
│   │   ├── Managers/              # Singleton game systems (TurnManager, PincerAttackManager, ...)
│   │   ├── Instances/             # Runtime MonoBehaviours (ActorInstance, ActorMovement, ...)
│   │   ├── Sequences/             # Async combat/UI event queue
│   │   ├── Canvas/                # In-game HUD (TimelineBar, TimelineIcon, ...)
│   │   ├── Hub/                   # Shared vendor-UI utilities (HubTheme, HubToast)
│   │   ├── Factories/             # Object instantiation (only place Instantiate() is allowed)
│   │   ├── Helpers/               # GameHelper (the global accessor — `using g = ...`)
│   │   └── Utilities/             # Formulas.cs, RNG.cs, Extensions.cs, Geometry.cs
│   └── Editor/
│       ├── Builders/              # *Builder.cs — the source of truth for every scene
│       ├── BuilderAutoRebuild.cs  # [InitializeOnLoad] watcher — rebuilds scenes on builder edit
│       ├── CliEntryPoints.cs      # Batchmode entry points (BuildStandaloneWindows, guardrails, ...)
│       └── *Allowlist.txt         # Curated exceptions to the four guardrails
├── Documentation/                 # Builder snapshots, scene hierarchies, design docs
├── Tests/                         # Edit Mode + Play Mode test fixtures
├── GridGame.Console.ps1           # Top-level operator console
└── README.md                      # ← you are here
```

## Running the project

`GridGame.Console.ps1` is the operator console — six numbered operations covering everything a human runs by hand:

| # | Operation | Notes |
|---|---|---|
| 1 | Run Application | Launches the Unity editor; `/run` triggers Play Mode |
| 2 | Commit and Sync | `git add -A`, commit, push |
| 3 | Create Backup | Copies the project to `R:\Backup\GridGame` with date-stamped folders |
| 4 | Setup | One-time idempotent: clone/pull, activate pre-push hook, launch Unity for initial import |
| 5 | Build Player (headless) | `CliEntryPoints.BuildStandaloneWindows` in batchmode |
| 6 | Set Start Scene | Rewrites `StartSceneConfig.StartScene` and propagates to `playModeStartScene` |

Headless invocations from Claude / CI / scripts:

```powershell
Unity -batchmode -nographics -projectPath . `
  -executeMethod CliEntryPoints.<Method> -quit -logFile -
```

Exit code `0` = success, `1` = failure.

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

## Status

Active development. Single-developer project. The combat loop (slide / pincer / supporters / pushback) is implemented and playable; the cast-as-timeline-icon system and the Fail/Pushback/Clutch interrupt resolver are partially implemented — see `CLAUDE.md` for the design intent and current state.
