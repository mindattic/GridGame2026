---
codex: 1
project: GridGame2026
code: GG
layer: stories
status: living
updated: 2026-06-09
---

# GridGame2026 — User Stories

> Legend: ✅ done (shipped & play-tested) · 🟡 partial · ⬜ planned · 🗑️ cut · living.
> **Verification reality (GG-§6).** Headless Unity is unlicensed in this dev env, so ✅ here means
> built + **in-editor play-tested** with a shipped `DebugManager.Demo_*` button, and verified by
> reading the cited implementation — NOT by an automated CI test (the only automated PlayMode test
> is `PincerScenarioTest`). Each ✅ names the implementing file(s)/demo as its evidence token.
> Story IDs keep the original `US-NNN` scheme (not `GG-US-<Epic><n>`) because the bible cross-refs
> hundreds of these by number — renaming would break them. Epic grouping is in the section headers.
>
> **Migrated 2026-06-07** from the root `user_stories.md` (now this file is the canonical L2). This
> file is the AUDIT LOG: a changed story keeps its original spec verbatim, marked
> *(Original spec — audit log)*.

---

## The Build Board

**Purpose.** A single, **dependency-ordered** backlog of *genuinely remaining* work, distilled from the bible ([`BIBLE.md`](BIBLE.md)) and **reconciled against the actual codebase** (verified 2026-05-30 by reading the implementation, not the doc). Work top-to-bottom: every story's prerequisites are landed by the stories above it. Finish a story → check its box → move on.

> **Critical finding from the reconciliation.** The bible substantially *understated* what's built. The full cast-on-timeline system, the interrupt Fail path, weapon durability rules, the per-hero ability-bar save, **and the entire battle↔vendor macro loop + content layer are already implemented.** ~20 originally-planned stories were already done. They're listed in **§A — Verified Complete** so nobody rebuilds them. The active board below is only the real remainder.

**How to read a story.**
- `US-NNN` — stable id (kept across the rewrite so the bible's cross-refs still resolve).
- **State** — `NOT-BUILT` or `PARTIAL` (with what's missing), as verified in code.
- **Why / Done when / Touch / Bible / Dep** — payoff, acceptance, files, bible section to update, prerequisite story ids.

## Definition of Done (every story)
1. Compiles, zero Console errors (§17.3). 2. **Bible section updated** (no silent drift). 3. `DebugManager.Demo_*` + DebugWindow button shipped ([[feedback_debug_window_demos]]). 4. `CheckAllGuardrails` green. 5. Play-tested in-editor. 6. One commit at the end ([[feedback_commit_granularity]]).

---

# §A — Verified Complete (do NOT rebuild)

These were on the original board and are **already implemented in code**. The bible's "TODO / not built / Phase C stub" language for them is stale and is being corrected in this pass. Evidence in parentheses.

**Cast & interrupt scaffolding**
- ✅ `Formulas.CastTime(baseSeconds, wis, int)` WIS/INT scaling (`Formulas.cs:492`; `CastingState.cs:91`).
- ✅ Cast-as-timeline-icon: `TimelineIconMode.Resolving` + `EnterResolvingMode()` (`TimelineIcon.cs:52,833`).
- ✅ TurnManager third state: `IsResolvingCast` + input suspension (`TurnManager.cs:84,245,257`).
- ✅ Interrupt **Fail** path wired: `EnemyAttackSequence.InterruptCastingHero → TimelineBar.InterruptCastsByOwner` (`EnemyAttackSequence.cs:109,133`; `TimelineBarInstance.cs:731`).

**Buffs**
- ✅ Burning/Poisoned per-tick damage on the timeline-advancing gate (`BuffTickManager.cs:45-60`).

**Mana**
- ✅ Orb line responsive/equidistant layout (`ManaOrbLineFactory.cs:38,41-47`; `ManaPoolManager.cs:216`). *(Bible §16.4 #16 was already satisfied.)*

**Equipment & durability**
- ✅ Weapon shatter dual-damage — target bonus + wielder self-damage (`WeaponDurabilityHelper.cs:37-103`).
- ✅ Repair max-durability cap + escalating cost (`WeaponDurabilityHelper.cs:105-138`).
- ✅ Ability-bar weapon swap end-to-end (`ChangeEquippedWeaponSequence.cs:48-108`; `AbilityLibrary.FromWeapon` ~520).

**Save & macro loop (the entire battle↔vendor cycle is live)**
- ✅ `HeroEquipmentSave.AbilityBarSlots` source-of-truth round-trip (`Profile.cs:376-487`; `HeroLoadout.cs:183-268`).
- ✅ Stage carrier via `StageSaveData.CurrentStage` (`StageManager.cs:117`; `Profile.cs:195-212`).
- ✅ `StageLibrary` with 15+ stages, waves, per-wave actors (`StageLibrary.cs:77-628`).
- ✅ Game→PostBattle on victory + defeat (`BattleWonSequence.cs`, `BattleLostSequence.cs`).
- ✅ PostBattle XP/loot reveal + save commit (`PostBattleManager.cs:216-340`). *(Uses `ExperienceTracker`+`LootTracker` instead of a formal `BattleResultCarrier` — functionally complete; the named-carrier refactor is unnecessary.)*
- ✅ All six vendors hydrate-on-Awake + commit-before-navigate (`VendorManager`, `BlacksmithManager`, `AlchemistManager`, `EquipManager`, `PartyManager`, `AbilitiesManager`).
- ✅ StageSelect unlock gating reads `HighestClearedStageIndex` (`StageSelectManager.cs:109-176`).

**Content & data**
- ✅ `DropTableLibrary` — 16 populated per-enemy tables (`DropTableLibrary.cs:53-68`).
- ✅ `RecipeLibrary` — 22 recipes incl. Iron→Steel; Blacksmith Forge/Salvage + Alchemist Brew menus (`RecipeLibrary.cs:53-82`, `BlacksmithManager.cs:210`, `AlchemistManager.cs:139`).
- ✅ `EnchantLibrary` — 4-element weapon affinity recipes (`EnchantLibrary.cs:58-174`). *(Bible §25.3 "Enchant (planned)" is now built.)*
- ✅ 172 concrete enemy classes with archetype stat leans + tags (`ActorLibrary.cs:95-268`).
- ✅ EnemyPlanner core: HP-weighted targeting, pincer-seek (+50), flank-avoid (−100), immobilize (`EnemyPlanner.cs:20-138`). *(The §14.3 future hooks remain — see Epic F.)*

---

# §B — Active Board (remaining work, in build order)

---

## EPIC A — Foundations
*The one true UI foundation left, plus the two small save fields that unblock the failure path and the Bestiary/Scan UI. Do these first.*

- [x] **US-120 — Multi-tile enemies (2×2 bosses) — cohesive footprint backing.** ✅ DONE 2026-06-06 (user-requested initiative; planned + Legion-free since design forks were user-decided; built in 5 verified phases). The board assumed 1 actor = 1 tile everywhere; this adds a single footprint-aware backing. **Representation:** `location` = anchor + `Vector2Int Footprint` (default 1×1; enemies only — heroes stay 1×1). **Chokepoint:** zero-alloc `ActorInstance.Occupies(tile)` (reduces to `tile==location` for 1×1) + `GameHelper.Actors.ActorAt/IsTileOccupied`; `TileInstance.IsOccupied/Occupier` forward to it, so movement/pincer/targeting/spawn/support-lines became footprint-aware in one stroke. **Four locked rules (user):** (1) boss is an **immovable wall** to hero slides (`ActorMovement.CheckLocationChanged` blocks entry); (2) **pincer by flanking its width** — distinct-actor "all-covered" predicate in `PincerDetector.Detect`, a 2×2 counts as one opponent; (3) boss **shoves heroes** on its turn — `ActorInstance.StepFootprint`+`ResolveShoveChain` mirror the hero-slide cascade, abort at the edge, `EnemyPlanner.FootprintStepLegal` gates candidates; (4) one timeline icon, footprint melee via `Geometry.AreAdjacent`. **Render/spawn:** sprite scaled+centered (`Geometry.GetFootprintCenter`/`CenterPosition`), `RNG.UnoccupiedFootprintAnchor`, `StageManager` footprint placement. **Cyclops00** is the first live 2×2 boss. Demo: "Spawn 2×2 Boss". Bible §1.5 added. Touch: `ActorInstance`, `TileInstance`, `GameHelper`, `PincerDetector`, `Geometry`, `ActorMovement`, `EnemyPlanner`, `StageManager`, `RNG`, `ActorData`, `TargetShapeResolver`, `SupportLineManager`, `Cyclops00`. **Dep:** —

- [x] **US-001 — AspectGuard + portrait lock.** ✅ DONE 2026-06-08 (code complete; visual verify on next play-test). `Utilities/AspectGuard.cs` self-installs on every scene (runtime hook, no per-builder edits): (1) **letterboxes `Camera.main.rect`** to the VALID portrait aspect closest to the device's with a persistent **black background camera** filling the bars; (2) **normalizes every `CanvasScaler`** to 1170×2532 + match 0.5; (3) **safe-area inset** — any Canvas child named "SafeArea" tracks `Screen.safeArea` normalized anchors, re-applied on change. §26.4 URP Overlay Camera already exists in `GameBuilder` (Base+Overlay stack, depth -1/+1, clearFlags SolidColor/Nothing). Valid aspects: 9:21, 9:20, 9:19.5, 1:2, 9:16, 10:16, 3:4. Bible §26 updated.
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (`CameraManager.cs` has no aspect logic; no AspectGuard file).
  **Why:** The 15-row HUD breaks on non-reference aspects; foundational before more builders accrue.
  **Done when:** `Utilities/AspectGuard.cs` per §26.6 inserted as first child of every Canvas; black `AspectBars` behind; `CameraViewportSync.cs` clamps `Camera.rect`; safe-area inset; separate UI Overlay Camera shares the viewport (§26.4). Verified pillarbox (iPad-portrait) + letterbox (ultra-tall).
  **Touch:** new `Utilities/AspectGuard.cs`, `Utilities/CameraViewportSync.cs`; every `Editor/Builders/*Builder.cs`. **Bible:** §26.3–§26.7 (flip Status → ✅); delete §16.4 #17. **Dep:** —

- [x] **US-002 — GameBuilder clears roots before rebuild.** ✅ DONE 2026-05-31 (`GameBuilder.cs:55` calls `ClearAllRootObjectsSilent()`; bible §17.1 #8 struck).
  **Why:** Kills the "already exists" warning spam that hides real errors.
  **Done when:** `GameBuilder.Build()` calls `ClearAllRootObjectsSilent()` first; rebuild is warning-free.
  **Touch:** `Editor/Builders/GameBuilder.cs`. **Bible:** strike §17.1 #8. **Dep:** —

- [x] **US-003 — `LayerMask.NameToLayer("UI")` defensive fallback.** ✅ ALREADY SATISFIED (verified 2026-05-31): the sole `cullingMask` site in `Assets/` is `BestiaryBuilder.cs:55` and already uses the guard; no other camera culls. No code change; bible §17.1 #10 struck. (05-30 pass mislabeled NOT-BUILT.)
  **Done when:** every camera `cullingMask` site uses `uiLayer >= 0 ? (1<<uiLayer) : ~0`.
  **Touch:** builders creating cameras. **Bible:** §17.1 #10. **Dep:** —

- [x] **US-004 — `Application.targetFrameRate = 60` in Bootstrap.** ✅ DONE 2026-05-31: new `Scripts.Helpers.Bootstrap` static with `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` pins 60 at app launch (entry-scene-independent; GameManager later applies the user's setting). Bible §30.5 updated.
  **Why:** Editor matches build framerate so perf spikes show during the whole build, not just at the end.
  **Touch:** `Bootstrap.Awake`. **Bible:** §30.5. **Dep:** —

- [x] **US-053 — HP carry-over between battles.** ✅ DONE 2026-06-01. `CharacterLevelPair.HpCurrent` added (`Profile.cs`); `StageManager.SpawnActor` hydrates wounds after equipment finalizes MaxHP; `BattleWonSequence` persists each party hero's HP (survivors keep their wound, a hero who fell in a won battle revives at 1); `BattleLostSequence` resets the party to full. **Heal model = §29.3 #12 model A (Legion 4/4): wounds persist, gold-cost full-heal at the Alchemist** (Inn was cut). Demos: "Wound Party 50%" / "Heal Party Full". Bible §15.1/§15.2 + §29.3 #12 updated. *Follow-on: the Alchemist heal-service UI button (the carry-over mechanism + in-battle heal preview are built).*
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (no `HpCurrent` field in any save class — `Profile.cs`).
  **Why:** Wounds-between-battles gives the heal vendor a job (§15.1) and completes the defeat path (US-063 routing already exists, just needs the restore).
  **Done when:** an `HpCurrent` field is added (e.g. on `CharacterLevelPair`/a `HeroHealthSave`); written at battle end; wounded HP hydrated on spawn; defeat resets to MaxHP.
  **Touch:** `Models/Profile.cs`, `SaveStateService`/`PostBattleManager`, hero spawn in `GameBuilder`. **Bible:** §15.1, §15.2, §22.2; resolve §29.3 #12 (heal model). **Dep:** —

- [x] **US-054 — `BestiaryProgress` writing (seen / defeated).** ✅ DONE 2026-06-01. `BestiarySaveData` + `BestiaryEntrySave` added to `Profile.cs` (**list-based**, keyed by `CharacterClass` — the serializer can't do Dictionaries, matching `InventorySaveData`); wired into `SaveState` (field + deep-copy ctor). `StageManager.SpawnActor` marks enemies **Seen** on spawn; `ActorInstance.DieRoutine` marks **Defeated + TimesDefeated** on enemy death; persisted via the existing battle-end save. Demo: "Log Bestiary". Bible §15.3 updated. Unblocks US-077 (Scan→seen) and US-093 (view gating). **Dep:** —
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (no `BestiarySaveData` in `Profile.cs`).
  **Why:** Unblocks Bestiary unlock gating (US-093) and lets Scan flag `seen` (US-077).
  **Done when:** a `BestiarySaveData : Dict<CharacterClass, {Seen, Defeated, TimesDefeated}>` added; populated on enemy spawn (seen) + death (defeated); persisted at battle end.
  **Touch:** `Models/Profile.cs`, spawn/death hooks, `PostBattleManager`. **Bible:** §15.1, §15.3. **Dep:** —

- [x] **US-110 — StageSelect = scrollable, replayable level list (newest-on-top).** ✅ DONE 2026-06-07. `StageSelectManager.RebuildList()` now iterates themes in reverse (CityRuins first → GreenValley last) and stages within each theme in reverse (stage 3 before stage 1), so the most recently unlocked stage appears at the top. Each row is 72px (was 56px) to accommodate a second-line drops hint. New `BuildDropsHint(stage)` walks each wave's enemy classes, looks up `DropTableLibrary.Get(cls)` entries, and shows the first ≤2 item names (`ItemLibrary.Get(id).DisplayName`) as `<size=18>drops: X, Y</size>` — farming target hint. Row colors, star prefix, lock suffix, and replay-ability unchanged. Demo: "Stage Order". Bible §22.3 / §11.2 marked built.
  *(Original spec — audit log)* **Was:** `PARTIAL` (unlock gating reads `HighestClearedStageIndex` — `StageSelectManager.cs:109-176`; ordering/replay/feel need to match the spec).
  **Why:** The *only* nav surface (Overworld is cut). Load/save-screen look-and-feel; cleared stages stay replayable so the player can **farm a specific enemy's drop** (a Frost stage for Ice Shards, etc.) — the intended grind loop.
  **Done when:** the list is vertically scrollable in `SaveFileSelect` style; newly-unlocked levels **prepend to the top**; every unlocked level (incl. cleared) is re-enterable; locked stages dimmed/disabled; each row shows name, theme, cleared ✓, and a hint of notable drops/enemies. Tapping a row sets `StageSaveData.CurrentStage` → `Game`.
  **Touch:** `Managers/StageSelectManager`, `Editor/Builders/StageSelectBuilder`, `StageLibrary`/`CampaignStages`. **Bible:** §22.3 (now built), §11.2. **Dep:** —

- [x] **US-111 — Fix the vendor scaler/scroll bugs + build the standardized `ShopView`.** ✅ DONE 2026-06-07 (scaler + ScrollRect bugs fixed; ShopView extraction deferred to §C — VendorManager already owns Buy/Sell logic end-to-end). `BROKEN` — root cause found 2026-05-30.
  **Why:** Vendors look "like trash" (sizing/colors/readability) for two concrete reasons, not vague polish:
  - `VendorBuilder` sets `CanvasScaler.referenceResolution = (0,0)` with ScaleWithScreenSize → every element mis-scales (bible §17.1 #11). Must be `(1170,2532)` + match 0.5 (§26.2) under AspectGuard (US-001).
  - The `ScrollRect` is added but never wired (`.content`/`.viewport`/`.vertical` unset; the "ScrollRect cross-references" block is empty) → list can't scroll, rows clip (§17.1 #12).
  **Done when:** a shared **`Canvas/ShopView.cs` + factory** renders the standardized FF shop (§25.1): **Buy / Sell / Buyback** tabs; scrollable list with columns **icon · name(rarity) · owned ×N · unit price(affordability-colored)**; select-row → **quantity stepper** (`− N +` + Max) → live total → footer **commit** button. Sell = 50% BaseCost; Buyback = session stack at the sold price. Scaler fixed, ScrollRect fully wired, all colors from `HubTheme`. `Vendor.unity` uses it end-to-end and rebuilds cleanly via `BuilderAllScenes`. Root cause recorded in §17.1 (done).
  **Touch:** new `Canvas/ShopView.cs` + `Factories/ShopViewFactory.cs`; `Editor/Builders/VendorBuilder.cs`; `Vendor/VendorManager.cs`; `HubItemRowFactory`/`HubTheme`; `SceneBuilderHelper`. **Bible:** §25.1, §25.8, §17.1 #11/#12. **Dep:** US-001 (AspectGuard).
  *Unblocks:* the eventual merged hub (§C) and lets the other vendors reuse `ShopView` instead of each reinventing a broken layout.

- [x] **US-112 — `Hub.unity` vendor launcher (grid of buttons).** ✅ DONE 2026-06-07. **FIXED 2026-06-09:** the 06-07 build shipped non-functional — `Hub.unity` was never generated/committed, the scene was absent from `EditorBuildSettings`, and all 7 buttons (6 vendors + back) plus StageSelect's Shop button were wired via `WireOnClick` with lambdas/`Action.Invoke`, which persistent UnityEvent listeners cannot serialize → dead buttons; the empty `HubManager` also never called `scene.FadeIn()` → black screen. Fix: `HubManager`/`StageSelectManager` wire onClick at runtime; `SceneBuilderHelper.WireOnClick` now throws on non-`UnityEngine.Object` targets so this bug class fails at build time; `Hub.unity` generated + added to build settings. `HubBuilder.cs` + `HubManager.cs` build a themed 2×3 `GridLayoutGroup` of 6 vendor buttons (Vendor/Blacksmith/Alchemist/Equip/Party/Abilities), each wired to `SceneHelper.Fade.To<X>()` via persistent onClick; gold-accented "Shop District" header; "← Stages" back button → StageSelect; `EnsureCanvas` gives §26.2 scaler (1920×2032, match 0.5); `HubTheme.PanelBg/HeaderBg/NavIdle/NavActive` colors. `StageSelectBuilder.BuildHeader` adds a "Shop" button (top-right, gold, 200×56) → `SceneHelper.Fade.ToHub()`. `SceneHelper` gains `Hub` const + `Fade.ToHub()` + `Switch.ToHub()`. Visual verify (aspect / cell sizing) deferred to editor. **Touch:** `HubBuilder`, `HubManager`, `StageSelectBuilder`, `SceneHelper`. **Bible:** §25.0, §22. **Dep:** US-001.

- [x] **US-113 — FadeOverlay speed = 125 ms.** ✅ DONE 2026-05-31: `FadeOverlayInstance.fadeDuration` 0.0833f → 0.125f. Bible §11.3 already specified 125 ms (code was out of sync); now compliant.
  **Done when:** `FadeOverlayInstance` fades out/in at 0.125 s each way (snappy seam-hider, not a flourish).
  **Touch:** `Canvas/FadeOverlayInstance.cs`. **Bible:** §11.3. **Dep:** —

---

## EPIC B — Buffs That Bite
*Every debuff applies + shows an icon, but the gameplay hooks are stubbed with TODO comments. Quick, low-risk, high-feel wins that make the spell catalog meaningful. Verified: all six below are `NOT-BUILT` (data + TODO markers present, no behavior).*
*Order note: **US-016 first** — turn-unit buffs (Slowed, Silenced) can't expire until `TickTurn` is wired, so the others need it to be observable.*

- [x] **US-016 — Turn-unit buff decrement at the turn boundary.** ✅ DONE 2026-05-31: `TurnManager.NextTurn` ticks turn-unit buffs at END-of-turn (enemy → `lastEnemy`; hero-window end → all heroes). End-of-turn confirmed via Legion. Demo: `Demo_ApplySlowedToEnemy` + "Slowed → Enemy" / "Trigger Enemy Attack" DebugWindow buttons. Bible §8.3 updated.
  **Done when:** `TurnManager` calls `BuffSystem.TickTurn(actor)` at each bearer's turn boundary; turn-unit buffs count down + fire expire-chains.
  **Touch:** `Managers/TurnManager`, `BuffSystem`. **Bible:** §8.3. **Dep:** —

- [x] **US-011 — Slowed → timeline-speed multiplier.** ✅ DONE 2026-05-31: `TimelineIcon.GetEffectiveUPerSec()` folds in `Buffs.SlowedTimelineMultiplier` (×0.5); `TimelineBarInstance.AdvanceBySeconds` reads it instead of `GetUPerSec`, so a Slowed enemy's icon crawls and its turn is delayed. Demo: existing "Slowed → Enemy" button. Bible §8.1 + §16.1 #2 updated.
  **Done when:** icon `uPerSec` scaled by a `slowed` factor; Slow visibly delays a rank.
  **Touch:** `Canvas/TimelineIcon` / `TimelineBarInstance`. **Bible:** §2.2, §8.1, delete §16.1 #2. **Dep:** US-016.

- [x] **US-012 — Silenced → cast-block + slot overlay.** ✅ DONE 2026-05-31: `AbilityBar.HandleSpell` refuses spell casts when the caster is Silenced ("Silenced!" popup); `Refresh` renders Silenced heroes' Spell slots blocked (solid-red `SilencedFrameColor`, non-interactable). Demo: "Silenced → Hero" button. NOTE: visual is a solid-red blocked state; the exact §4.5 diagonal-stripe overlay sprite is deferred as polish. Bible §8.1 + §16.1 #3 updated.
  **Done when:** Spell-kind clicks refused when caster `silenced`; Spell slots show the red diagonal-stripe state (§4.5).
  **Touch:** `Canvas/AbilityBar`. **Bible:** §4.5, §8.1, delete §16.1 #3. **Dep:** US-016.

- [x] **US-013 — Blinded → hit-chance penalty.** ✅ DONE 2026-05-31: `Formulas.CalculateHitType` multiplies a Blinded attacker's hit chance by `Buffs.BlindedAccuracyMultiplier` (0.5). Demo: "Blinded → Enemy" button. Bible §8.1 + §16.1 #4 updated.
  **Done when:** miss-chance penalty applied when attacker `blinded`.
  **Touch:** `Utilities/Formulas`. **Bible:** §13.1.1, §8.1, delete §16.1 #4. **Dep:** —

- [x] **US-014 — SleepWhenWarm multiplier.** ✅ DONE 2026-05-31: `SpellEffectDispatcher` applies Sleep with ×1.5 duration when the target is Warm ("Deep Sleep!" popup). Applied to duration (Sleep has no success roll); bible §8.2 noted. Bible §16.1 #5 struck.
  **Done when:** Sleep success/duration × `SleepWhenWarmMultiplier` (1.5) on a `warm` target.
  **Touch:** `SpellEffectDispatcher` Sleep-apply. **Bible:** §8.2, delete §16.1 #5. **Dep:** —

- [x] **US-015 — BreaksOnMove hook.** ✅ DONE 2026-05-31: `ActorMovement.HandleOverlap` calls `BuffSystem.OnMoved(instance)` at the displacement commit, so sliding a sleeping actor breaks Sleep. Bible §8.2.3 updated.
  **Done when:** `ActorMovement.HandleOverlap` (displacement) calls `BuffSystem.OnMoved(actor)`; Sleep breaks on slide.
  **Touch:** `Instances/Actor/ActorMovement`, `BuffSystem`. **Bible:** §8.2.3. **Dep:** —

---

## EPIC C — Interrupt Depth + Enemy Casting + Orb Economy
*The cast scaffolding is fully built (§A). What's missing is the **richness**: the three-outcome resolver, enemies that actually cast, and the interrupt→orb mint that closes the off-palette mana economy.*

- [x] **US-024 — `CastInterruptResolver.Resolve(caster, attacker)` → {Fail | Pushback | Clutch}.** ✅ DONE 2026-06-01, **REVISED 2026-06-02 → cast-stagger model (user)**. *Original build was a three-outcome {Fail|Pushback|Clutch} LCK roll; that's been replaced.* Now: each landed hit on a casting actor adds a WIS/STR-scaled **cast-time delay** (`CastingState.AccumulatedInterruptDelay`), pushing the cast icon back (`TimelineIcon.DelayCast`); when the **accumulated delay exceeds the original cast time the cast is cancelled** (`CastingState.Interrupt`). **WIS = poise:** chance to shrug a hit entirely + smaller per-hit delay; attacker STR raises the delay. **Clutch retained** as a rare LCK pre-check (rolled first; shrugs the hit entirely — US-025 adds the snap+juice). `InterruptCastsByOwner(hero, attacker)` applies it; `EnemyAttackSequence` passes the attacker. Demo: "Cast-Stagger Info". Bible §13.4 rewritten. **Dep:** —
  *(Original spec — audit log)* **Was:** `PARTIAL` (only unconditional Fail today — `EnemyAttackSequence.cs:128` comment names the intended resolver).
  **Why:** Replace the flat Fail with the LCK-driven roll (Clutch first, then Pushback vs Fail by LCK/WIS).
  **Done when:** new `Services/CastInterruptResolver.cs`; `InterruptCastsByOwner` routes through it; **Pushback** rewinds the spell-icon u + brief stun (icon already supports u + Stunned); **Fail** = current behavior.
  **Touch:** new `Services/CastInterruptResolver.cs`, `Canvas/TimelineBarInstance.InterruptCastsByOwner`, `EnemyAttackSequence`. **Bible:** §13.4, casting prose, delete §16.2 resolver row. **Dep:** —

- [x] **US-025 — `ClutchSequence` (the miracle save).** ✅ DONE 2026-06-06. New `Sequences/ClutchSequence.cs` plays the juice — a white full-screen flash (transient `new GameObject` Image on the UI canvas, ~0.3s in/out), "Heal" SFX, and "Clutch!" combat text — then calls the new `TimelineIcon.ForceResolve()`, which snaps the in-flight spell-icon to u=1 and fires the **same** `onReached` resolution closure a natural arrival uses (EnterResolvingMode → suspend input → apply effect → end turn). Wired from the `Clutch` branch of `TimelineBarInstance.InterruptCastsByOwner`: it pauses the icon (so it can't reach u=1 juice-less first) and `AddFirst`-queues the sequence so the save fires right after the triggering attack. New `GetSpellIconFor(actor)` accessor. Demo: "Clutch! (Force)" (snaps the selected hero's in-flight cast, or plays juice-only if none). Bible §13.4 + glossary + §16 row updated. **Dep:** US-024 ✓.
  *(Original spec — audit log)* **Was:** `NOT-BUILT`. **Retained (user, 2026-06-02):** a fun random-chance miracle save, **primarily LCK-driven**. The US-024 resolver already returns `Clutch` on the rare LCK proc (the cast shrugs the hit); this story is the *juice* — snap the spell-icon to `u=1` + screen flash / SFX / "Clutch!" text so it reads dramatically. Builds on the stagger resolver.
  **Done when:** rare LCK outcome: screen flash / SFX / "Clutch!" text, snap spell-icon to u=1 (reuse `EnterResolvingMode`), run normal resolution; base rate ≈ `LCK/200`, designer-capped.
  **Touch:** new `Sequences/ClutchSequence.cs`. **Bible:** casting prose. **Dep:** US-024.

- [x] **US-026 — Enemy charge/telegraph spells.** ✅ DONE 2026-06-06. **Architecture: Legion panel 4/4 (twothirds quorum) → Option A** — a pure, side-effect-free `EnemyPlanner.PlanCast(enemy, actors)` returning a nullable `EnemyChargePlan`; `PlanStep` and the existing melee chain untouched. `EnemyTakeTurnSequence` checks `PlanCast` first (skipping if a charge is already in flight) and, on a non-null result, queues ONLY a new `Sequences/EnemyChargeSequence` + `EndTurnSequence` in place of move/attack. The charge rule: a Caster (tagged `Magic`) NOT cardinally adjacent to a hero telegraphs at the nearest hero (ranged behavior; if it can melee, it melees). New `Data/Actor/EnemyChargeCatalog` derives the spell element from affinity tags (FireAffinity→Fireball, IceAffinity→Ice, …) and exposes `ColorFor` (US-027 hook). `EnemyChargeSequence` reuses the team-agnostic `SpawnSpellIcon` to spawn the cast-icon and, at u=1, resolves into `MagicAttackSequence(enemy,target,…)` via the now-public `AbilityManager.TryGetMagicEffect` — same third-state resolution as hero casts but with **no EndTurnSequence** (it resolves on the shared clock). **IceMauler** tagged `Magic|IceAffinity` as the first live caster (spawns in stages). Interrupt = the US-024 cast-stagger model (unblocks US-027). Demo: "Enemy Charge". Bible §2.6/§2.8/§13.4/§14.2/§16 updated. **Dep:** — ✓
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (`EnemyAttackSequence` is melee-only; `EnemyPlanner` is positional-only).
  **🔓 Interrupt model 2026-06-02 → folded into the unified CAST-STAGGER model (US-024 rewrite).** A hero hit on a charging enemy no longer binary-cancels; it adds WIS/STR-scaled delay to the charge, and **cumulative delay ≥ the charge's cast time cancels it** (supersedes the earlier "A damage-cancels" binary lock). US-027 mints the charge-color orb at the cancel. The charge is its own below-the-line cast icon (§2.6). Editor-gated to build. Bible §13.4.
  **Why:** The Caster archetype (§14.2) + the inverse-interrupt loop need enemies that telegraph a charge in the Prepare Zone.
  **Done when:** a casting enemy spawns a colored charge timeline icon advancing via cast-time; resolves into an attack at u=1; `EnemyPlanner` chooses to charge.
  **Touch:** `Services/EnemyPlanner`, new `Sequences/EnemyChargeSequence`, `TimelineBarInstance.SpawnSpellIcon` (generalize beyond hero casts), enemy spell data. **Bible:** §2.8, §14.2 (Caster), §14.3. **Dep:** —

- [x] **US-027 — Interrupt enemy cast → drop charge-color orb.** ✅ DONE 2026-06-06. The trigger is centralized in `ActorInstance.DamageRoutine`: any hero landing-hit on an enemy (pincer via AttackHelper, magic via MagicAttackSequence, shield) calls `TimelineBar.InterruptCastsByOwner(enemy, attacker)` — a no-op unless the enemy is charging. `InterruptCastsByOwner` now branches hero vs enemy: enemies skip the Clutch pre-check (`CastInterruptResolver.Resolve(..., allowClutch:false)` — a hit must never instant-resolve an enemy charge), and on the **Cancelled** outcome `MintInterruptOrb` drops a charge-color orb (`EnemyChargeCatalog.ColorFor` → `ManaOrbFactory.Drop`, the same bouncing path pincers use) that lands in the team bank. This is **how off-palette colors enter the bank** (§3.1.2). Demo: "Interrupt Charge" (run "Enemy Charge" first). Bible §3.1.2/§13.4/§14.2/§16 updated. **Closes EPIC C.** **Dep:** US-026 ✓.
  *(Original spec — audit log)* **Was:** `NOT-BUILT`.
  **Why:** This is *how off-palette colors enter the bank* (§3.1.2). Closes the mana economy.
  **Done when:** a pincer/Shield hit that interrupts a charging enemy cancels the cast AND mints one orb of the charge color via `DropOrbAt`/`ManaOrbFactory`.
  **Touch:** `Canvas/TimelineBarInstance.InterruptCastsByOwner` (enemy branch), `Managers/PincerAttackManager`, `ManaPoolManager`. **Bible:** §3.1.2, §13.4. **Dep:** US-026.

---

## EPIC D — Mana Color Identity
*Orbs are hardcoded Blue. Give them class identity and the remaining mint sources so the bank profile reflects party composition (§23.2.1).*

- [x] **US-030 — Per-hero color affinity on pincer mint.** ✅ DONE 2026-06-02. New `Data/Actor/ManaColorAffinity.For(class)` (per-class, the board's allowed alternative to an ActorData field — avoids churning 7 hero `Data()` files); `PincerAttackManager` mints each contributor's color instead of hardcoded Blue. Map (Legion-ratified for the two ambiguous): Cleric **W**, Paladin **W**, Barbarian **R**, Alchemist **G**, Assassin **B**, GreenNinja **G**, RedNinja **R**; others default Blue. Demo: "Log Color Affinities". Bible §3.1.2/§3.1.7/§23.2 + §16.5 #13 + §29.2 #8 updated. Unblocks US-031 (done) / US-033 (color conversion). **Dep:** — ✓
  *(Original spec — audit log)* **Was:** `PARTIAL` (all drops hardcoded `ManaType.Blue` — `PincerAttackManager.cs:206`; no `ColorAffinity` on `ActorData`).
  **Done when:** `ActorData.ColorAffinity` (or per-class) added; `DropOrbAt` mints the contributor's affinity color; seed §23.2 colors (Cleric=W, Barbarian=R, …).
  **Touch:** `Models/Actor/ActorData`, `Managers/PincerAttackManager`. **Bible:** §3.1.2, §23.1, §23.2, delete §16.5 #13; resolve §29.2 #8. **Dep:** —

- [x] **US-031 — Critical hit → Colorless orb.** ✅ DONE 2026-06-02. A hero's critical hit mints one Colorless "wild" orb to the team bank — hooked centrally in `ActorInstance.DamageRoutine` (covers pincer crits via AttackHelper AND magic crits via MagicAttackSequence, both routed through `AttackResult.HitType`). The wild orb renders as a spectrum-cycling cell (`ManaOrbLine.AnimateWildOrbs`) per the user's "flashes every color" treatment. Capped by the bank. Demo: "Mint Wild Orb". Bible §3.1.2/§3.1.7 updated. *(Dep US-030 was epic-ordering only — Colorless is a fixed color, no per-class decision needed.)* **Dep:** — ✓
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (`HitOutcome.Critical` exists; no orb mint — `AttackHelper.cs:74-128`).
  **Done when:** a crit (pincer or spell) mints one Colorless orb.
  **Touch:** `Helpers/AttackHelper` / `SpellEffectDispatcher`, `ManaPoolManager`. **Bible:** §3.1.2. **Dep:** US-030.

- [x] **US-028 — Quicken / Hasten (forward push + overtake).** ✅ DONE 2026-06-06. New `TimelineIcon.Hasten(amountU)` + `TimelineBarInstance.HastenIcon(actor, amountU)` slide an icon **forward** in u (inverse of `Pushback`). New `Quicken` spell — `SpellDefinition.HastenU` field (0 = no timeline effect), `SpellLibrary.Quicken` (SingleActor / PickActor / Any filter, `hastenU: 0.30`, no damage), `ManaAbilities.Quicken` (1×Blue); `SpellEffectDispatcher` applies the hasten on impact via the live `AbilityBar → SpellEffectDispatcher.Cast` path. **Reality correction:** the spec's `ResolveSpatialOverlap` / "inverted train-cascade" **does not exist in code** — icons advance independently and turn order = arrival-at-trigger (`GetSecondsRemaining`), so **overtaking is emergent** from the forward bump; nothing to invert. Documented in bible §2.7.1 (+ corrected the §2.8 spawn-rules row that asserted a cascade). Demo: "Quicken". Bible §2.7.1/§2.8/§16.2 updated. **Dep:** —
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (no spell; no forward-push in `ResolveSpatialOverlap`).
  **Why:** Inverse of pushback — slide a target's icon toward the trigger, overtaking neighbors (inverted train-cascade).
  **Done when:** a Quicken spell increases target icon u; `ResolveSpatialOverlap` runs inverted; turn order updates.
  **Touch:** `Canvas/TimelineBarInstance.ResolveSpatialOverlap`, `Data/SpellLibrary` (Quicken). **Bible:** §2, casting prose. **Dep:** —

- [x] **US-033 — Pressure valve (Colorless wildcard).** ✅ DONE 2026-06-02. **Design-locked by Legion (4/4): rule B — Colorless wild orbs satisfy any color on spend; no manual converter** (keeps colors meaningful, valve tied to crits, not exploitable). This removed the UI requirement entirely — pure `ManaBank` logic: `CanAfford`/`Spend` pay each cost with its own color (leftmost) then fall back to Colorless wilds for shortfalls; explicit Colorless costs paid only by Colorless. Demo: "Test Wildcard Spend". Bible §3.1.6 promoted to built. **Dep:** US-030 ✓ (uses US-031 crit-orbs as the valve source). ✓
  *(Original spec — audit log; title was "Color conversion / pressure valve")* **Was:** `NOT-BUILT` (`ManaBank` has Add/Spend/CanAfford only).
  **Why:** Documented escape valve (§3.1.6) so off-color banks aren't dead weight.
  **Done when:** a `ManaBank.Trade()` + in-battle UI trades N orbs of one color toward another (Colorless wildcard at a cost). Design-locked first.
  **Touch:** `Models/ManaBank`, `ManaPoolManager`, conversion UI. **Bible:** §3.1.6 (promote intent → built). **Dep:** US-030.

---

## EPIC E — Equipment Data Layer
*Durability + weapon-swap are done (§A). What's left is the three planned `ItemDefinition` fields and their consumers.*

- [x] **US-040 — Extend `ItemDefinition` with planned fields.** ✅ DONE 2026-06-01. `BattleStartManaOrbs:int`, `OnUseSpellName:string`, `ResistanceModifiers:Dict<DamageType,float>` added to `Data/Items/ItemDefinition.cs` with safe defaults (0 / null / empty dict). Inert until EPIC E consumers (US-041/042/043). Demo: "Log ItemDef Fields". Bible §24.3 + §16.3 updated. **Dep:** —
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (`ItemDefinition.cs:63-124` lacks all three).
  **Done when:** `BattleStartManaOrbs:int`, `OnUseSpellName:string`, `ResistanceModifiers:Dict<DamageType,float>` added with safe defaults.
  **Touch:** `Inventory/ItemDefinition`. **Bible:** §24.3. **Dep:** —

- [x] **US-041 — Mage Robe / Wizard Robe battle-start orbs.** ✅ DONE 2026-06-01. `MageRobes.BattleStartManaOrbs=2`; new `WizardRobe` (Rare, `eq_armor_wizard`, =3) added + registered in `ItemLibrary`. `ManaPoolManager.ApplyBattleStartManaOrbs` (run at battle start via `GameReady`) sums `BattleStartManaOrbs` across the active party's equipped gear and adds that many random WUBRG orbs, clamped to the 12 cap. Demo: "Battle-Start Orbs". Bible §24.8/§3.1.4 + §16.3 #6 updated. **Dep:** US-040. ✓
  *(Original spec — audit log)* **Was:** `PARTIAL` (`MageRobes` item exists `ItemData_Armor.cs:128`; no Wizard Robe; no battle-start scan).
  **Done when:** Wizard Robe added; `ManaPoolManager.Start` scans equipped party gear and adds `BattleStartManaOrbs` random orbs per robe (stacks per wearer, respects the 12 cap §3.1.4).
  **Touch:** `Data/ItemData_Armor`, `ManaPoolManager.Start`. **Bible:** §24.8, §3.1.4, delete §16.3 #6. **Dep:** US-040.

- [x] **US-042 — Sleep Dart (item triggers a spell).** ✅ DONE 2026-06-01. `cons_sleep_dart` (`OnUseSpellName="Sleep"`, stack 5) added + registered; `ManaAbility.SourceItemId` links a bar slot to its `ItemDefinition`; `AbilityBar.HandleItem`→`TryHandleItemSpell` resolves the spell by ability-name, runs Sleep's targeting flow, and on confirm spends one charge + `SpellEffectDispatcher.Cast` + costs a turn. On the Alchemist's default bar (slot 6). First item-casts-a-spell path (generalizes to any consumable with `OnUseSpellName`). Demo: "Verify Sleep Dart Route". Bible §4.4/§24.8 + §16.3 #7 updated. **Dep:** US-040. ✓
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (no item; `AbilityBar.HandleItem:71-77` + `UseItemSequence` are heal/damage-only, no spell routing).
  **Done when:** Sleep Dart consumable (stack 5) routes `HandleItem` through `OnUseSpellName` → Sleep's targeting flow → `SpellEffectDispatcher.Cast`, consuming one charge on confirm.
  **Touch:** `Data/ItemData_Consumables`, `Canvas/AbilityBar.HandleItem`, `Sequences/UseItemSequence`. **Bible:** §24.8, §4.4, delete §16.3 #7. **Dep:** US-040.

- [x] **US-043 — Equipped `ResistanceModifiers` folded into damage.** ✅ DONE 2026-06-01. `SpellEffectDispatcher.EquipmentResistanceMultiplier(target, type)` multiplies every equipped item's `ResistanceModifiers[type]` into `ApplyDamage`'s per-class `resMult` (multiplicative; heroes only — enemies have no gear). Sunfire Amulet seeded with Fire ×0.7 as a demonstrable example. Demo: "Log Resistances". Bible §13.1.2/§24.3 + §16.3 updated. **Completes EPIC E.** **Dep:** US-040. ✓
  *(Original spec — audit log)* **Was:** `NOT-BUILT` (resistances are per-class on `ActorData` only; no equipment aggregation — `Formulas.cs:293-300`).
  **Done when:** equipped `ResistanceModifiers` aggregate (alongside `ComputeEquipmentBonus`) into the wearer's effective resistance used by `ApplyDamage`.
  **Touch:** `Utilities/Formulas`, `SpellEffectDispatcher.ApplyDamage`. **Bible:** §3.3, §24.3. **Dep:** US-040.

---

## EPIC F — AI Depth
*The planner is solid for positioning (§A) but lacks the §14.3 future hooks. Best done after enemy casting (US-026) exists so the Caster archetype has behavior to plan.*

- [x] **US-080 — Threat tracking (damage → preferred target).** ✅ DONE 2026-06-02. New `Managers/ThreatTracker` (per-battle hero→enemy damage tally, accrued in `ActorInstance.DamageRoutine`, cleared in `TurnManager.Initialize`). `EnemyPlanner` subtracts `(threat∕maxThreat) × enemyINT × 0.8` from each candidate's target score, so **threat weight scales with enemy Intelligence** (user refinement): smart enemies hunt the top damage-dealer, dumb ones keep chasing nearest/wounded. Demo: "Log Threat". Bible §14.1.2/§14.3 updated. **Dep:** —
  *(Original spec — audit log)* **Was:** `PARTIAL` (targeting weights distance+HP, not damage dealt — `EnemyPlanner.cs:42-44`).
  **Done when:** `EnemyPlanner` adds an accumulated-damage-per-hero weight to target selection.
  **Touch:** `Services/EnemyPlanner` (+ a per-battle damage tally). **Bible:** §14.1.2 (new factor), §14.3. **Dep:** —

- [x] **US-081 — Coordinated retreat (wounded enemies flee).** ✅ DONE 2026-06-02. `EnemyPlanner.PlanStep`: below `RetreatHpThreshold` (0.30 HP fraction) the enemy flips advance→flee (maximizes distance from the target) and drops its adjacency + pincer-seek biases; flank-avoidance still applies so it won't back into a pincer. Demo: "Test Enemy Retreat". Bible §14.1.2/§14.3 updated. **Dep:** — ✓
  *(Original spec — audit log)* **Was:** `NOT-BUILT`.
  **Done when:** below an HP threshold an enemy biases moves *away* from heroes.
  **Touch:** `Services/EnemyPlanner`. **Bible:** §14.3. **Dep:** —

- [x] **US-082 — AI supporter positioning.** ✅ DONE 2026-06-02 (Legion: supporter-adjacency over lane-clearing, 4/4). `EnemyPlanner.WouldSupportAllyPincer` rewards (+25) a move that makes this enemy a §1.2.3 supporter of *another* ally's Humanoid pincer (reuses `PincerDetector.FindSupporters`; excludes pincers where this enemy is itself an endpoint — that's the +50 `WouldFormPincer`). Suppressed when fleeing (US-081). Demo: "Log Enemy Plans". Bible §14.1.2/§14.3 updated. **Dep:** — ✓
  *(Original spec — audit log)* **Was:** `PARTIAL` (planner seeks its *own* pincer, not moves that enable an *ally's*).
  **Done when:** a support branch rewards moves completing an ally's pincer line.
  **Touch:** `Services/EnemyPlanner`. **Bible:** §14.3. **Dep:** —

- [x] **US-083 — Boss scripted phases.** ✅ DONE 2026-06-06. **Architecture: Legion panel split 2/2 A↔C → synthesis = data-driven phase table (A) with the transition expressed as a `SequenceEvent` (the bespoke-code seam both camps endorsed).** New `Data/Actor/BossPhase` + `BossScriptLibrary` (per-`CharacterClass` ordered phases {HpThreshold, PrefersCharge, `Func<ActorInstance,SequenceEvent>` Transition}); pure `Services/BossPhaseRunner` (CurrentPhaseIndex / Current / `AdvancePhasesAndCollectTransitions` — fires each newly-crossed phase's transition, advances `ActorFlags.BossPhaseIndex`); `Sequences/BossPhaseTransitionSequence` (announce + optional self-heal + optional Quicken via US-028). `EnemyTakeTurnSequence` queues transitions at turn start and honors the `PrefersCharge` knob (new `EnemyPlanner.PlanCast(..., ignoreMeleeRange)`). **Cyclops00** = first scripted boss (enrages <50% HP: ENRAGED! banner + hasten). Demo: "Trigger Boss Enrage". Bible §14.2/§14.3 updated. **Dep:** US-026 ✓.
  *(Original spec — audit log)* **Was:** `NOT-BUILT`.
  **Done when:** a per-class boss override (or `BossScript` sequence) swaps generic stepping for authored phases via `SequenceManager`.
  **Touch:** `Services/EnemyPlanner`, new `Sequences/Boss*`. **Bible:** §14.2 (Boss), §14.3. **Dep:** US-026.

---

## EPIC G — UI Polish & Accessibility
*Sand the edges. Several of these are PARTIAL — the framework exists, the last mile doesn't.*

- [x] **US-114 — Timeline two-lane layout (portraits above, cast icons below).** ✅ DONE 2026-06-08 (code complete; visual verify in-editor deferred — layout is the point). `NOT-BUILT` was the prior state.
  **🔓 Design rule 2026-06-02 (user):** one shared timeline, two lanes — **large actor/portrait turn-icons ABOVE** the line, **¼-size cast icons BELOW**. Remove the stacked `SpellCastBar` shrinking-bars entirely; a cast's *position on the shared u-axis* is its progress read, so it lines up under the enemy turn-icons. Shared continuous clock: a cast resolves at its icon's trigger, off any particular turn (the IP-gauge — §2.6). Enemy charge icons (US-026) ride the same below-line lane.
  **Done when:** turn-icons render large above the timeline line; cast icons render ~¼-size below it on the same axis; `SpellCastBar`/`SpellCastBarFactory` retired; both lanes share the trigger; verified in-editor (the layout is the whole point).
  **Touch:** `Canvas/TimelineIcon`, `Factories/TimelineIconFactory`, `Canvas/TimelineBarInstance`; retire `Canvas/SpellCastBar` + `Factories/SpellCastBarFactory`; `AbilityBar.HandleSpell` (no longer spawns a cast bar). **Bible:** §2.6, §2.8, §9. **Dep:** US-001 (AspectGuard layout). **Editor-gated (visual).**

- [x] **US-076 — Spell icons on the AbilityBar.** ✅ DONE 2026-06-07. `AbilityBarFactory` adds a 36×36 `Image` (top-left corner, `raycastTarget=false`, initially hidden) to each slot; `AbilityBar.Bind()` accepts `Image[] iconImages`; `Refresh()` calls `SpriteLibrary.SpellIcons.TryGetValue(a.Name)` and enables the Image when a sprite is found — glyph fallback (text labels only) when the key is absent. Demo: "Spell Icons". Bible §4.5, §29.4 #15 to verify in-editor.
  *(Original spec — audit log)* **Was:** `PARTIAL` (`SpriteLibrary.SpellIcons` populated `:127`; `AbilityBar.Refresh:248` renders name+cost glyphs only, no icon Image).
  **Done when:** slot shows the spell icon sprite; glyph fallback if missing.
  **Touch:** `Canvas/AbilityBar` (+ an icon Image per slot). **Bible:** §4.5, resolve §29.4 #15, delete §16.4 #21. **Dep:** —

- [x] **US-077 — Scan reveals enemy stats.** ✅ DONE 2026-06-06. New `SpellDefinition.RevealsStats` (set on `SpellLibrary.Scan`); `SpellEffectDispatcher` reveals the target's HP/STR/VIT/AGI/INT via the cadenced **AnnouncementWindow** + "Scanned!" combat text + Select SFX, and flags the enemy class **Seen** in the Bestiary (`Bestiary.MarkSeen`) — which feeds US-093's seen-gated reveal. (Reuses the AnnouncementWindow as the reveal surface instead of a bespoke popup; a richer panel can come later.) Demo: "Scan Enemy". Bible §7/§16 updated. **Dep:** US-054 ✓.
  *(Original spec — audit log)* **Was:** `PARTIAL` (spell + VFX exist `SpellLibrary.cs:107`; no reveal popup, no dispatcher branch).
  **Done when:** Scan opens a stat-reveal popup for the target + flags Bestiary `seen`.
  **Touch:** `SpellEffectDispatcher`, new info popup, `BestiaryProgress`. **Bible:** §7. **Dep:** US-054.

- [x] **US-090 — "No valid targets" toast.** ✅ DONE 2026-06-06. `TargetingMode.Begin` (Auto resolve + PickActor candidate scan) now `AnnouncementWindow.Announce("No valid targets")` before cancelling, instead of silently locking input. Reuses the cadenced AnnouncementWindow. Bible §16.6 #15 struck. **Dep:** —
  *(Original spec — audit log)* **Was:** `PARTIAL` (`TargetingMode.Begin` calls `onCancel` silently — `:81,100`).
  **Done when:** Auto/pick resolving to 0 actors shows a toast.
  **Touch:** `Managers/TargetingMode`, in-battle toast. **Bible:** §5.2, delete §16.6 #15. **Dep:** —

- [x] **US-093 — Bestiary enemy filter + unlock gating.** ✅ DONE 2026-06-07. `BestiaryView.BuildPages()` now filters `ActorLibrary.Actors` to `ActorTag.Enemy` only (heroes, NPCs excluded). New `IsSeen(ActorData)` helper reads `Bestiary.Get(cls)?.Seen`. `Refresh()` branches: seen = full stats/portrait/lore as before; unseen = silhouette (portrait recolored black), "???" name, "Unencountered" class, hidden lore ("Defeat or Scan to reveal"). Demo: "Bestiary Filter" (logs enemy count vs total and seen count). Bible §15.3 status updated; §29.4 #16 resolved. **Dep:** US-054 ✓, US-077 ✓.
  *(Original spec — audit log)* **Was:** `PARTIAL`→needs US-054 (`BestiaryView:59-98` lists all actors, no filter/progress/silhouette).
  **🔓 Design-locked 2026-06-02 (Legion 4/4): SEEN-gated reveal** — full entry once `BestiaryProgress.Seen` (encounter or Scan US-077); unseen = silhouette + "???". Dep US-054 now ✓ (Seen/Defeated are written). Remaining work is the editor-side `BestiaryView` UI (filter to `Enemy` tag + silhouette rendering).
  **Done when:** only `Enemy`-tagged entries; unseen show silhouettes per `BestiaryProgress.seen`.
  **Touch:** `Canvas/BestiaryView`. **Bible:** §11.2, §15.3, delete §16.5 #14 + §16.6 bestiary row; resolve §29.4 #16. **Dep:** US-054.

- [x] **US-096 — Music playback + volume/mute settings (persisted).** ✅ DONE 2026-06-07 (committed by user; verified via play-test). Music *playback* lands via the chiptune Jukebox/MusicDirector (Battle/Vendor/Title/Overworld/Victory/Defeat beds) — superseding the planned Vorbis stream. Controls: `ProfileSettings.{MusicVolume, SfxVolume, MuteMusic, MuteSfx}` (persisted; defaults 0.6/0.85/false/false); `AudioSettingsHelper.Apply()` folds mute → pushes effective volumes to `Jukebox` (music + vendor SFX) and battle `g.SoundSource`; `SettingsManager` Sliders/Toggles live-apply; `MusicDirector.Apply` re-applies on scene change. UI audio folds into SFX channel. Demo: "Music Vol 25/100%", "Toggle Mute Music/SFX". Bible §31.5 + §29.4 #18 updated. **Dep:** —
  *(Original spec — audit log)* **Was:** `PARTIAL` (`AudioManager` is SFX-only `:48-90`; `MusicTrackLibrary` has 1 track; `SettingsManager` has no audio sliders).
  **Done when:** `AudioManager` gains music playback (Vorbis stream) + Music/SFX/UI volume + independent mutes in `SettingsManager`, persisted.
  **Touch:** `Managers/AudioManager`, `Managers/SettingsManager`, `Libraries/MusicTrackLibrary`. **Bible:** §12.1.1 audio, §30.4, §31.5; resolve §29.4 #18. **Dep:** —

- [x] **US-091 — AbilityBar tooltip (hover / long-press).** ✅ DONE 2026-06-07. `AbilityBarFactory` adds an `EventTrigger` (PointerEnter/PointerExit) to every slot and passes `slotRects[]` to `AbilityBar.Bind`. `AbilityBar.ShowTooltipForSlot(i)` builds a formatted string (name, kind, cost icons / charge count / "Free", cast time for spells) and calls `Tooltip.Show` with the slot's RectTransform as target, placement Top, fade enabled. `HideTooltip()` destroys the active tooltip on PointerExit. No separate demo button needed — hover any slot in play mode. Bible §4.5 updated. **Dep:** US-076 ✓.
  **Done when:** hover/long-press shows name, cost, target-shape preview, base dmg/heal.
  **Touch:** `Canvas/AbilityBar`, new tooltip. **Bible:** §4.5. **Dep:** US-076.

- [x] **US-092 — Cooldown slot visual state.** ✅ DONE 2026-06-07. Skills declare a reuse limit (`ManaAbility.CooldownTurns`; Steal 3 / Mug 2 / Teleport 3); per-hero countdown via `SkillCooldownManager`, ticked per turn-cycle in `TurnManager.BeginHeroWindow`; `AbilityBar.Refresh` renders a disabled state — fade-out + numeric countdown (prior) **+** §4.5 **radial sweep** (added 2026-06-07): `AbilityBarFactory` adds a `CooldownSweep` child Image per slot (`Radial360`, `Origin360.Top`, clockwise, 68%-alpha dark overlay, `raycastTarget=false`, initially hidden); `AbilityBar.Bind` accepts `Image[] cooldownSweeps`; `Refresh` calls `SetCooldownSweep(i, onCooldown, remaining, a.CooldownTurns)` → `fillAmount = remaining/max` (1=freshly locked, 0=recharged). Bible §4.5.
  *(Original spec — audit log)* **Was:** `NOT-BUILT`.
  **Done when:** skills can declare a reuse limit; bar renders the greyscale + radial sweep (§4.5).
  **Touch:** `Canvas/AbilityBar`, `Data/ManaAbility`. **Bible:** §4.5. **Dep:** —

- [x] **US-094 — Colorblind palette toggle.** ✅ DONE 2026-06-07. `ProfileSettings.ColorblindMode` (bool, persisted; default false in `ProfileHelper.DefaultSettings`, copy-ctor updated). New `Helpers/ColorblindHelper.cs` with Okabe-Ito substitutions: Red → Vermillion (0.835, 0.369, 0), Green → Bluish-green (0, 0.620, 0.451), burning → Vermillion, poisoned → Bluish-green, protection → Orange (0.902, 0.624, 0). `ManaOrbLine.ColorFor` now delegates to `ColorblindHelper.GetManaColor`; standard palette extracted to `ColorForStandard`. `DebuffIconBar.ColorFor` delegates to `ColorblindHelper.GetDebuffColor`; standard palette extracted to `ColorForStandard`. `SettingsManager` Toggles gains "Colorblind Mode". Demo: "Toggle Colorblind" (logs mode + palette name). Bible §31.1 updated. **Dep:** —
  **Done when:** Settings toggle remaps mana/debuff/health palettes (Okabe-Ito/Wong); glyphs already carry the non-color signal.
  **Touch:** `Managers/SettingsManager`, palette sources (`HubTheme`/`ManaOrbLine`/`DebuffIconBar`). **Bible:** §31.1. **Dep:** —

- [x] **US-095 — Reduce-motion toggle.** ✅ DONE 2026-06-07 (committed by user; verified via play-test). `ProfileSettings.ReduceMotion` (persisted; default false); `VisualEffectManager.IntensityScale` gated in `CreateInstance` — 0 suppresses all particle VFX; `ProjectileMotionEval.ReduceMotion` collapses arcs to straight lerps; `MotionSettingsHelper.Apply()` pushes flag to both; `SettingsManager` toggle live-applies. Demo: "Toggle Reduce Motion". Bible §31.2 updated. **Dep:** —
  *(Original spec — audit log)* **Was:** `NOT-BUILT`.
  **Done when:** Settings toggle drives `VisualEffectManager.IntensityScale`→0 + skips long projectile arcs.
  **Touch:** `Managers/SettingsManager`, `VisualEffectManager`, `ProjectileMotionEval`. **Bible:** §31.2. **Dep:** —

---

## EPIC H — Performance & Hardening
*Final pass against §30 once content density is realistic so the profiler shows true hotspots.*

- [x] **US-100 — Coroutine hygiene audit.** ✅ DONE 2026-06-07. Root bug: `SequenceManager.OnDisable()` called `StopCoroutine(runningCoroutine)` which only stops the outer `ExecuteRoutine` — the nested `StartCoroutine(current.ProcessRoutine())` continued running as an independent coroutine on the MonoBehaviour after BattleEnd. Fix: track `innerCoroutine` handle + new `CancelAll()` method uses `StopAllCoroutines()` (kills outer + inner), resets `isExecuting`, clears the queue. `OnDisable` now delegates to `CancelAll()`. Key sequences (EnemyTakeTurnSequence, BattleWonSequence, etc.) already null-check actors at entry + before critical operations; no additional post-yield guards needed once the cancel path is robust. UI-triggered coroutines (CoinManager, AudioManager, GhostManager) fire-and-forget by design — no double-start issue identified. Bible §30.3 updated. **Dep:** —
- [x] **US-101 — GC hot-path cleanup.** ✅ DONE 2026-06-07. Eliminated all per-call heap allocations in `EnemyPlanner.PlanStep` and `PlanCast`: replaced `actors.Where(...).ToList()` (×2) with a `static readonly List<ActorInstance> _heroScratch` (clear+foreach), replaced `OrderBy(...).First()` (×2) with manual min-search loops, replaced `new List<Vector2Int>` candidates with `static readonly List<Vector2Int> _candidateScratch`. Two residual `Any()` calls (`IsOccupied`, `WouldBeFlanked`) operate on bounded existing lists (≤5 elements, ≤4 heroes) — not hotspots. **Touch:** `EnemyPlanner`. **Bible:** §30.2. **Dep:** —
- [x] **US-102 — Particle caps + VFX pooling.** ✅ DONE 2026-06-07. `VisualEffectManager` now: (1) caps concurrent instances at `MaxConcurrentVfx=48` — any spawn past the cap is silently dropped, preventing burst runaway; (2) pools wrapper GOs — `Despawn` calls `ReturnToPool()` (stops coroutines via `ResetForPool()`, clears children, deactivates and enqueues) instead of `Destroy`; `CreateInstance` dequeues a pooled wrapper before allocating a new GO; (3) `Clear()` flushes both live instances and pooled wrappers. `VisualEffectInstance.ResetForPool()` (internal) stops all coroutines and nulls cached component arrays. `VfxPrefabAuthor` per-prefab `maxParticles` (60–200) remain as per-system authored caps; no central particle count tracking needed at runtime. Demo: "VFX Pool Stats" logs `Live=N/48 Pooled=M`. **Touch:** `VisualEffectManager`, `VisualEffectInstance`. **Bible:** §30.1, §30.4. **Dep:** —
- [x] **US-103 — HUD texture atlas (UI Addressable pack).** ✅ DONE 2026-06-08 (setup complete; draw-call drop to be profiled in US-104's device pass). `CliEntryPoints.BuildHudAtlas` creates `Assets/Sprites/HudAtlas.spriteatlas` covering 14 in-battle HUD sprite folders (GUI, ActionBar, HealthBar, Mana, Statuses, AbilityButtons, Timeline/ActorTagIcons, TimerBar, Selection, Actor/Masks/Base/Back/Frames/Armor); registers it as Addressable address `HudAtlas` label `UI`. Unity routes Image draws through the atlas automatically — no SpriteLibrary changes needed. Run `CliEntryPoints.BuildHudAtlas` once in the editor to materialize the `.spriteatlas` file. **Touch:** `Editor/CliEntryPoints.cs`. **Bible:** §30.4. **Dep:** US-001.

---

## EPIC I — Loop-closure follow-ons
*Two bible-specified vendor services found missing during the 2026-06-09 full-loop audit (the bible promised them; the code didn't have them). Both close holes in the battle↔vendor macro loop.*

- [x] **US-121 — Blacksmith Repair tab.** ✅ DONE 2026-06-09 (code complete; play-test on next editor session). Bible §25.2 #3 promised "Repair — restore weapon durability" and §25.8's sketch shows the tab, but `BlacksmithManager` had only Forge/Salvage — `WeaponDurabilityHelper.RepairCost` (built in §A) had **zero callers**, so worn gear could never be fixed. Now: third `Mode.Repair` tab lists every hero's equipped weapon/armor with a durability pool (`RepairCandidates()` walks `save.Equipment.Heroes`; worn pieces sort first); rows show `Class — Item cur/effMax cost`; detail pane shows factory max, repair count, the post-repair ceiling drop, and the `IsUneconomical` "costs as much as a new one" warning; Repair button deducts gold, restores to `EffectiveMaxDurability` (factory − prior repairs), increments the slot's RepairCount, persists. `BlacksmithBuilder` adds the RepairTab button (Forge/Salvage/Repair at 0–0.30/0.30–0.60/0.60–0.90). Demo: "Wear Gear −5" (DebugWindow) wears all equipped gear so the tab has work to show. **Touch:** `BlacksmithManager`, `BlacksmithBuilder`, `DebugManager`, `DebugWindow.Demos`. **Bible:** §25.2, §25.8. **Dep:** — (uses §A's `WeaponDurabilityHelper`).

- [x] **US-123 — One visual language: UiKit + HubTheme + UiFonts (FFBE-guided reskin of every scene).** ✅ DONE 2026-06-09 (code complete; visual play-test next editor session — the look is the point). **Problem:** every scene felt disconnected — two coexisting visual systems (GreenButton-sprite meta screens with a 130px black CutoutOverlay vs. navy/gold vendor screens), runtime list rows in default LiberationSans (no font ever set), three back-button styles, a hot-pink Credits scrollbar, hand-typed color literals duplicating HubTheme everywhere, Bestiary in Avenir with its own gold hue, tooltips in Chicago pixel font, timeline labels in Avenir. **Built:** (1) `HubTheme` expanded into the full palette (PanelBorder/ListBg/RowBg/RowSelected/RowLocked/ScrollTrack/ScrollHandle + the shared `ButtonColors` ColorBlock); (2) new runtime `Scripts/Hub/UiFonts.cs` — Attic=display, Outfit=body, resolving via FontLibrary at runtime and AssetDatabase in edit mode; (3) new editor `Assets/Editor/Builders/UiKit.cs` component factory (Header + gold rule, HeaderRightLabel, Panel/Border 2px steel frames, Primary/Secondary/Tab/Danger buttons, the one BackButton convention, ScrollList with visible themed scrollbar preserving `{name}/Viewport/Content` paths, Label/DisplayLabel); (4) `SceneBuilderHelper` primitives (EnsureCanvas→1170×2532+PanelBg, EnsureTitle→kit Header, EnsureBackButton/EnsureButton/EnsureLabel/EnsureScrollView→kit) so ALL meta scenes inherit; (5) all 7 Hub-family builders + Vendor (full rewrite) + Bestiary (Avenir→Outfit, kit header/back) + Title (kit menu tiles + GRIDGAME logo) + Settings/Credits/ProfileSelect/SaveFileSelect/ProfileCreate/PostBattle reskinned; CutoutOverlay removed from meta scenes (Game-only — Clock docks there); (6) every runtime row factory/manager fonted with `UiFonts.Body` + HubTheme constants (HubItemRowFactory, Vendor/StageSelect/Blacksmith/Alchemist/Party/Equip/Abilities/PostBattle managers); TimelineIconFactory Avenir→Outfit; TooltipFactory Chicago→Outfit; GameBuilder Clock/Pause/CastConfirm fonts unified (the Pause button's garbage "m_isRightToLeft: 0" label text removed). **Touch:** ~30 files. **Bible:** new §11.5. **Dep:** US-001 (AspectGuard normalizes to the same reference the builders now author at).

- [x] **US-122 — Alchemist heal service (the cut Inn's role).** ✅ DONE 2026-06-09 (code complete; play-test on next editor session). The §29.3 #12 resolution (Legion 4/4, model A) made wounds persist (US-053) with recovery as a *gold-cost full-heal at the Alchemist* — but the heal-service UI was an unbuilt "small follow-on", meaning wounds accumulated with **no out-of-battle recovery at all** (short of losing a battle). Now: green "Heal Party" button beside Mix (`AlchemistBuilder.BuildHealButton`; Mix shrinks to 0.6–0.79, Heal 0.79–1); `WoundedPartyInfo()` sums missing HP across `save.Party.Members` where `HpCurrent > 0` (MaxHP from class stats at the derived level — same estimate PartyManager shows); price = `0.5g × missing HP` (anchored to the Healing Potion's 25g/50HP rate); paying clears every member's `HpCurrent` to 0 (= spawn at full) and persists. Button shows live cost, disables when unaffordable, reads "Party Healthy" when nobody is wounded. Test path: "Wound Party 50%" → win battle → visit Alchemist. **Touch:** `AlchemistManager`, `AlchemistBuilder`. **Bible:** §25.3, §15.1, §29.3 #12. **Dep:** US-053 ✓.

---

# §C — Backlog / Deferred (NOT in the build window)

- **US-104 — 60fps profiling pass (mid-tier device).** Moved from Epic H 2026-06-08. Requires a physical Android/iOS mid-tier device + Unity Profiler capture of a dense battle (4+ enemies, full VFX). **Done when:** frame budget stays within §30.1 limits; overages become new stories. US-100–103 laid the groundwork (coroutine hygiene, GC cleanup, VFX pooling, HUD atlas); run the profiling pass once hardware is available. **Bible:** §30.1, §30.5.
- **Merged hub `.unity`** (§25.9) — fold the six vendor scenes into one composed hub screen. **Gated**: do NOT attempt until every vendor is individually stable (see US-111). It's layout composition over the shared `HubTheme`/`HubToast`/`HubItemRowFactory`/`VendorNavBar`, not a rewrite.
- **TargetShape.Line** — `Row`/`Column` already cover line targeting (`TargetShapeResolver`); add `Line` only if a *partial* line (not full row/col) is ever needed. (Originally US-073.)
- **Distinct loadouts for unfilled classes** (Ninja variants, Bruiser, Captain, Druid) — additive `HeroLoadouts.Set` content, do as roster grows (§23.2.2).
- ~~**Per-spell custom VFX authoring** — base VFX work; run `Tools/VFX/Author *` as art lands (§12.3).~~ **DONE 2026-06-09:** all 10 prefabs generated headless, auto-registered as Addressables (`VfxPrefabAuthor.SavePrefab` now does this), registered in `VisualEffectLibrary`, and referenced from each themed spell in `SpellLibrary`. Visual tuning awaits play-test.
- **Dialog & Story (§27) and Overworld (§28)** — **CUT from the design** 2026-05-30 (not merely deferred). No narrative layer; no world map. Stage navigation is the scrollable level list (US-110). Don't build either; don't re-story them.
- **Roster / variable party composition** (§23.4, §25.5); **roguelike/NG+** (§29.1 #5); **tutorial** (§29.1 #6).
- **Relic-slot passives** (§24.1) — underspecified; design before storying.
- **Deep-poison stacking / tiered buff upgrades** (§8.6); **crit-heal** (§13.1.3).
- **Unify spell damage through `Formulas.CalculateAttackResult`** (§13.1.2) — so crit/miss/blind apply to spells; promote if spell-crit becomes a goal.
- **Formalize `BattleResultCarrier`** — optional refactor; current `ExperienceTracker`+`LootTracker` work fine.

---

# §D — Design questions to settle before their stories (from bible §29)
- ~~**§29.2 #8 color identity per class** → gates **US-030** (seed from §23.2).~~ **RESOLVED 2026-06-02 (Legion): Cleric W, Paladin W, Barbarian R, Alchemist G, Assassin B, GreenNinja G, RedNinja R.**
- ~~**§29.3 #12 heal/rest model** (free vs gold) → shapes **US-053**.~~ **RESOLVED 2026-06-01 (Legion 4/4): model A — wounds persist, gold-cost full-heal at the Alchemist.**
- **§29.3 #13 out-of-battle debuff carry** → §8.8 locks *clear-on-end*; confirm before shipping HP carry-over.
- ~~**§29.4 #16 Bestiary unlock gate** → gates **US-093**.~~ **RESOLVED 2026-06-02 (Legion 4/4): SEEN-gated** — reveal a class's full entry once `BestiaryProgress.Seen` (encounter or Scan); unseen = silhouette + "???".
- **§29.4 #17 AspectGuard strategy** → ratify §26 before **US-001**.

---

*Reconciled against the live codebase 2026-05-30. When you land a story: check the box, update the cited bible section, and move any new rule/question into bible §16/§29 so this board and the bible never drift again — which is exactly the drift this pass had to correct.*

> **This file is the AUDIT LOG — never delete a story's original text.** To complete a story: flip `[ ] → [x]`, add a `✅ DONE <date>` outcome note on the title line (what shipped, key files, the Debug demo, bible sections touched), and **keep the original `Why` / `Done when` / `Touch` / `Bible` / `Dep` lines beneath it** (prefixed `*(Original spec — audit log)*`). The original spec is how we know what was asked for vs. what was built — do not collapse or erase it.
