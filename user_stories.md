# user_stories.md — The Build Board

**Purpose.** A single, **dependency-ordered** backlog of *genuinely remaining* work, distilled from `game_bible.md` and **reconciled against the actual codebase** (verified 2026-05-30 by reading the implementation, not the doc). Work top-to-bottom: every story's prerequisites are landed by the stories above it. Finish a story → check its box → move on.

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

- [ ] **US-001 — AspectGuard + CameraViewportSync (portrait lock).** `NOT-BUILT` (`CameraManager.cs` has no aspect logic; no AspectGuard file).
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

- [ ] **US-110 — StageSelect = scrollable, replayable level list (newest-on-top).** `PARTIAL` (unlock gating reads `HighestClearedStageIndex` — `StageSelectManager.cs:109-176`; ordering/replay/feel need to match the spec).
  **Why:** The *only* nav surface (Overworld is cut). Load/save-screen look-and-feel; cleared stages stay replayable so the player can **farm a specific enemy's drop** (a Frost stage for Ice Shards, etc.) — the intended grind loop.
  **Done when:** the list is vertically scrollable in `SaveFileSelect` style; newly-unlocked levels **prepend to the top**; every unlocked level (incl. cleared) is re-enterable; locked stages dimmed/disabled; each row shows name, theme, cleared ✓, and a hint of notable drops/enemies. Tapping a row sets `StageSaveData.CurrentStage` → `Game`.
  **Touch:** `Managers/StageSelectManager`, `Editor/Builders/StageSelectBuilder`, `StageLibrary`/`CampaignStages`. **Bible:** §22.3 (now built), §11.2. **Dep:** —

- [ ] **US-111 — Fix the vendor scaler/scroll bugs + build the standardized `ShopView`.** `BROKEN` — root cause found 2026-05-30.
  **Why:** Vendors look "like trash" (sizing/colors/readability) for two concrete reasons, not vague polish:
  - `VendorBuilder` sets `CanvasScaler.referenceResolution = (0,0)` with ScaleWithScreenSize → every element mis-scales (bible §17.1 #11). Must be `(1170,2532)` + match 0.5 (§26.2) under AspectGuard (US-001).
  - The `ScrollRect` is added but never wired (`.content`/`.viewport`/`.vertical` unset; the "ScrollRect cross-references" block is empty) → list can't scroll, rows clip (§17.1 #12).
  **Done when:** a shared **`Canvas/ShopView.cs` + factory** renders the standardized FF shop (§25.1): **Buy / Sell / Buyback** tabs; scrollable list with columns **icon · name(rarity) · owned ×N · unit price(affordability-colored)**; select-row → **quantity stepper** (`− N +` + Max) → live total → footer **commit** button. Sell = 50% BaseCost; Buyback = session stack at the sold price. Scaler fixed, ScrollRect fully wired, all colors from `HubTheme`. `Vendor.unity` uses it end-to-end and rebuilds cleanly via `BuilderAllScenes`. Root cause recorded in §17.1 (done).
  **Touch:** new `Canvas/ShopView.cs` + `Factories/ShopViewFactory.cs`; `Editor/Builders/VendorBuilder.cs`; `Vendor/VendorManager.cs`; `HubItemRowFactory`/`HubTheme`; `SceneBuilderHelper`. **Bible:** §25.1, §25.8, §17.1 #11/#12. **Dep:** US-001 (AspectGuard).
  *Unblocks:* the eventual merged hub (§C) and lets the other vendors reuse `ShopView` instead of each reinventing a broken layout.

- [ ] **US-112 — `Hub.unity` vendor launcher (grid of buttons).** `NOT-BUILT` (the old monolithic Hub was deleted; this is a new lightweight launcher).
  **Why:** Central navigation from StageSelect to the six vendors; replaces the floating `VendorNavBar` as the primary path.
  **Done when:** `HubBuilder.cs` + `HubManager.cs` build a themed `GridLayoutGroup` of 6 equal buttons (Vendor/Blacksmith/Alchemist/Equip/Party/Abilities), each → `SceneHelper.Fade.To<X>()`, plus a Back → StageSelect; reached via a "Hub" button on StageSelect; uses §26.2 scaler + AspectGuard + `HubTheme`. No shop logic in the hub — it only routes.
  **Touch:** new `Editor/Builders/HubBuilder.cs`, `Scripts/Hub/HubManager.cs`; `StageSelectBuilder` (Hub button); `SceneHelper` (`ToHub`). **Bible:** §25.0, §22. **Dep:** US-001. **Note:** add `Hub.unity` to Build Settings (§11.4).

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

- [ ] **US-083 — Boss scripted phases.** `NOT-BUILT`.
  **Done when:** a per-class boss override (or `BossScript` sequence) swaps generic stepping for authored phases via `SequenceManager`.
  **Touch:** `Services/EnemyPlanner`, new `Sequences/Boss*`. **Bible:** §14.2 (Boss), §14.3. **Dep:** US-026.

---

## EPIC G — UI Polish & Accessibility
*Sand the edges. Several of these are PARTIAL — the framework exists, the last mile doesn't.*

- [ ] **US-114 — Timeline two-lane layout (portraits above, cast icons below).** `NOT-BUILT` (today actor + cast icons share one row; casts render as stacked shrinking `SpellCastBar` bars).
  **🔓 Design rule 2026-06-02 (user):** one shared timeline, two lanes — **large actor/portrait turn-icons ABOVE** the line, **¼-size cast icons BELOW**. Remove the stacked `SpellCastBar` shrinking-bars entirely; a cast's *position on the shared u-axis* is its progress read, so it lines up under the enemy turn-icons. Shared continuous clock: a cast resolves at its icon's trigger, off any particular turn (the IP-gauge — §2.6). Enemy charge icons (US-026) ride the same below-line lane.
  **Done when:** turn-icons render large above the timeline line; cast icons render ~¼-size below it on the same axis; `SpellCastBar`/`SpellCastBarFactory` retired; both lanes share the trigger; verified in-editor (the layout is the whole point).
  **Touch:** `Canvas/TimelineIcon`, `Factories/TimelineIconFactory`, `Canvas/TimelineBarInstance`; retire `Canvas/SpellCastBar` + `Factories/SpellCastBarFactory`; `AbilityBar.HandleSpell` (no longer spawns a cast bar). **Bible:** §2.6, §2.8, §9. **Dep:** US-001 (AspectGuard layout). **Editor-gated (visual).**

- [ ] **US-076 — Spell icons on the AbilityBar.** `PARTIAL` (`SpriteLibrary.SpellIcons` populated `:127`; `AbilityBar.Refresh:248` renders name+cost glyphs only, no icon Image).
  **Done when:** slot shows the spell icon sprite; glyph fallback if missing.
  **Touch:** `Canvas/AbilityBar` (+ an icon Image per slot). **Bible:** §4.5, resolve §29.4 #15, delete §16.4 #21. **Dep:** —

- [ ] **US-077 — Scan reveals enemy stats.** `PARTIAL` (spell + VFX exist `SpellLibrary.cs:107`; no reveal popup, no dispatcher branch).
  **Done when:** Scan opens a stat-reveal popup for the target + flags Bestiary `seen`.
  **Touch:** `SpellEffectDispatcher`, new info popup, `BestiaryProgress`. **Bible:** §7. **Dep:** US-054.

- [ ] **US-090 — "No valid targets" toast.** `PARTIAL` (`TargetingMode.Begin` calls `onCancel` silently — `:81,100`).
  **Done when:** Auto/pick resolving to 0 actors shows a toast.
  **Touch:** `Managers/TargetingMode`, in-battle toast. **Bible:** §5.2, delete §16.6 #15. **Dep:** —

- [ ] **US-093 — Bestiary enemy filter + unlock gating.** `PARTIAL`→needs US-054 (`BestiaryView:59-98` lists all actors, no filter/progress/silhouette).
  **🔓 Design-locked 2026-06-02 (Legion 4/4): SEEN-gated reveal** — full entry once `BestiaryProgress.Seen` (encounter or Scan US-077); unseen = silhouette + "???". Dep US-054 now ✓ (Seen/Defeated are written). Remaining work is the editor-side `BestiaryView` UI (filter to `Enemy` tag + silhouette rendering).
  **Done when:** only `Enemy`-tagged entries; unseen show silhouettes per `BestiaryProgress.seen`.
  **Touch:** `Canvas/BestiaryView`. **Bible:** §11.2, §15.3, delete §16.5 #14 + §16.6 bestiary row; resolve §29.4 #16. **Dep:** US-054.

- [ ] **US-096 — Music playback + Settings volume sliders.** `PARTIAL` (`AudioManager` is SFX-only `:48-90`; `MusicTrackLibrary` has 1 track; `SettingsManager` has no audio sliders).
  **Done when:** `AudioManager` gains music playback (Vorbis stream) + Music/SFX/UI volume + independent mutes in `SettingsManager`, persisted.
  **Touch:** `Managers/AudioManager`, `Managers/SettingsManager`, `Libraries/MusicTrackLibrary`. **Bible:** §12.1.1 audio, §30.4, §31.5; resolve §29.4 #18. **Dep:** —

- [ ] **US-091 — AbilityBar tooltip (hover / long-press).** `NOT-BUILT`.
  **Done when:** hover/long-press shows name, cost, target-shape preview, base dmg/heal.
  **Touch:** `Canvas/AbilityBar`, new tooltip. **Bible:** §4.5. **Dep:** US-076.

- [~] **US-092 — Cooldown slot visual state.** ✅ MOSTLY DONE 2026-06-01 (commit `1bdb322b`). Skills declare a reuse limit (`ManaAbility.CooldownTurns`; Steal 3 / Mug 2 / Teleport 3); per-hero countdown via `SkillCooldownManager`, ticked per turn-cycle in `TurnManager.BeginHeroWindow`; `AbilityBar.Refresh` renders a disabled state — **slot fades out + shows the turns-remaining number**, button non-interactable. Bible §4.1.1. *Remaining polish only:* the exact §4.5 **greyscale + radial-sweep** visual (current state is fade + numeric countdown).
  *(Original spec — audit log)* **Was:** `NOT-BUILT`.
  **Done when:** skills can declare a reuse limit; bar renders the greyscale + radial sweep (§4.5).
  **Touch:** `Canvas/AbilityBar`, `Data/ManaAbility`. **Bible:** §4.5. **Dep:** —

- [ ] **US-094 — Colorblind palette toggle.** `NOT-BUILT` (`SettingsManager` has toggles, not this).
  **Done when:** Settings toggle remaps mana/debuff/health palettes (Okabe-Ito/Wong); glyphs already carry the non-color signal.
  **Touch:** `Managers/SettingsManager`, palette sources (`HubTheme`/`ManaOrbLine`/`DebuffIconBar`). **Bible:** §31.1. **Dep:** —

- [ ] **US-095 — Reduce-motion toggle.** `NOT-BUILT`.
  **Done when:** Settings toggle drives `VisualEffectManager.IntensityScale`→0 + skips long projectile arcs.
  **Touch:** `Managers/SettingsManager`, `VisualEffectManager`, `ProjectileMotionEval`. **Bible:** §31.2. **Dep:** —

---

## EPIC H — Performance & Hardening
*Final pass against §30 once content density is realistic so the profiler shows true hotspots.*

- [ ] **US-100 — Coroutine hygiene audit.** **Done when:** every `SequenceManager` coroutine has a BattleEnd cancel + post-yield null-guards; UI-triggered coroutines guard double-start. **Touch:** `Managers/SequenceManager`, sequences. **Bible:** §30.3. **Dep:** —
- [ ] **US-101 — GC hot-path cleanup.** **Done when:** profiler shows no per-frame allocs in planner/dispatcher/manager `Update()`s; scratch lists reused. **Touch:** `EnemyPlanner`, `SpellEffectDispatcher`, manager `Update()`s. **Bible:** §30.2. **Dep:** —
- [ ] **US-102 — Particle caps + VFX pooling.** **Done when:** per-spell VFX ≤32/s sustained, bursts ≤100; `VisualEffectManager` pools. **Touch:** `VisualEffectManager`, `VfxPrefabAuthor`. **Bible:** §30.1, §30.4. **Dep:** —
- [ ] **US-103 — HUD texture atlas (UI Addressable pack).** **Done when:** UI sprites share one atlas/label; draw calls drop. **Touch:** Addressables config, `SpriteLibrary`. **Bible:** §30.4. **Dep:** US-001.
- [ ] **US-104 — 60fps profiling pass (mid-tier device).** **Done when:** a dense-battle capture stays within §30.1 budgets; overages filed as new stories. **Touch:** profiling. **Bible:** §30.1, §30.5. **Dep:** US-100, US-101, US-102.

---

# §C — Backlog / Deferred (NOT in the build window)

- **Merged hub `.unity`** (§25.9) — fold the six vendor scenes into one composed hub screen. **Gated**: do NOT attempt until every vendor is individually stable (see US-111). It's layout composition over the shared `HubTheme`/`HubToast`/`HubItemRowFactory`/`VendorNavBar`, not a rewrite.
- **TargetShape.Line** — `Row`/`Column` already cover line targeting (`TargetShapeResolver`); add `Line` only if a *partial* line (not full row/col) is ever needed. (Originally US-073.)
- **Distinct loadouts for unfilled classes** (Ninja variants, Bruiser, Captain, Druid) — additive `HeroLoadouts.Set` content, do as roster grows (§23.2.2).
- **Per-spell custom VFX authoring** — base VFX work; run `Tools/VFX/Author *` as art lands (§12.3).
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
