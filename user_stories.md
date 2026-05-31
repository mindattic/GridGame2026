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

- [ ] **US-053 — HP carry-over between battles.** `NOT-BUILT` (no `HpCurrent` field in any save class — `Profile.cs`).
  **Why:** Wounds-between-battles gives the heal vendor a job (§15.1) and completes the defeat path (US-063 routing already exists, just needs the restore).
  **Done when:** an `HpCurrent` field is added (e.g. on `CharacterLevelPair`/a `HeroHealthSave`); written at battle end; wounded HP hydrated on spawn; defeat resets to MaxHP.
  **Touch:** `Models/Profile.cs`, `SaveStateService`/`PostBattleManager`, hero spawn in `GameBuilder`. **Bible:** §15.1, §15.2, §22.2; resolve §29.3 #12 (heal model). **Dep:** —

- [ ] **US-054 — `BestiaryProgress` writing (seen / defeated).** `NOT-BUILT` (no `BestiarySaveData` in `Profile.cs`).
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

- [ ] **US-113 — FadeOverlay speed = 125 ms.** Quick tuning.
  **Done when:** `FadeOverlayInstance` fades out/in at 0.125 s each way (snappy seam-hider, not a flourish).
  **Touch:** `Canvas/FadeOverlayInstance.cs`. **Bible:** §11.3. **Dep:** —

---

## EPIC B — Buffs That Bite
*Every debuff applies + shows an icon, but the gameplay hooks are stubbed with TODO comments. Quick, low-risk, high-feel wins that make the spell catalog meaningful. Verified: all six below are `NOT-BUILT` (data + TODO markers present, no behavior).*
*Order note: **US-016 first** — turn-unit buffs (Slowed, Silenced) can't expire until `TickTurn` is wired, so the others need it to be observable.*

- [ ] **US-016 — Turn-unit buff decrement at the turn boundary.** `NOT-BUILT` (`BuffSystem.TickTurn` exists, never called — `TurnManager.cs:146-170`).
  **Done when:** `TurnManager` calls `BuffSystem.TickTurn(actor)` at each bearer's turn boundary; turn-unit buffs count down + fire expire-chains.
  **Touch:** `Managers/TurnManager`, `BuffSystem`. **Bible:** §8.3. **Dep:** —

- [ ] **US-011 — Slowed → timeline-speed multiplier.** `NOT-BUILT` (`TimelineIcon.UpdateApproaching` reads only "frozen"; `Buffs.cs:79` TODO).
  **Done when:** icon `uPerSec` scaled by a `slowed` factor; Slow visibly delays a rank.
  **Touch:** `Canvas/TimelineIcon` / `TimelineBarInstance`. **Bible:** §2.2, §8.1, delete §16.1 #2. **Dep:** US-016.

- [ ] **US-012 — Silenced → cast-block + slot overlay.** `NOT-BUILT` (`AbilityBar.HandleSpell:157-193` has no check; `Buffs.cs:86` TODO).
  **Done when:** Spell-kind clicks refused when caster `silenced`; Spell slots show the red diagonal-stripe state (§4.5).
  **Touch:** `Canvas/AbilityBar`. **Bible:** §4.5, §8.1, delete §16.1 #3. **Dep:** US-016.

- [ ] **US-013 — Blinded → hit-chance penalty.** `NOT-BUILT` (`Formulas.CalculateHitType:167-180` ignores buffs; `Buffs.cs:93` TODO).
  **Done when:** miss-chance penalty applied when attacker `blinded`.
  **Touch:** `Utilities/Formulas`. **Bible:** §13.1.1, §8.1, delete §16.1 #4. **Dep:** —

- [ ] **US-014 — SleepWhenWarm multiplier.** `NOT-BUILT` (`Buffs.cs:22` constant defined, never read).
  **Done when:** Sleep success/duration × `SleepWhenWarmMultiplier` (1.5) on a `warm` target.
  **Touch:** `SpellEffectDispatcher` Sleep-apply. **Bible:** §8.2, delete §16.1 #5. **Dep:** —

- [ ] **US-015 — BreaksOnMove hook.** `NOT-BUILT` (`BuffSystem.OnMoved:120-123` exists, never called from `ActorMovement`).
  **Done when:** `ActorMovement.HandleOverlap` (displacement) calls `BuffSystem.OnMoved(actor)`; Sleep breaks on slide.
  **Touch:** `Instances/Actor/ActorMovement`, `BuffSystem`. **Bible:** §8.2.3. **Dep:** —

---

## EPIC C — Interrupt Depth + Enemy Casting + Orb Economy
*The cast scaffolding is fully built (§A). What's missing is the **richness**: the three-outcome resolver, enemies that actually cast, and the interrupt→orb mint that closes the off-palette mana economy.*

- [ ] **US-024 — `CastInterruptResolver.Resolve(caster, attacker)` → {Fail | Pushback | Clutch}.** `PARTIAL` (only unconditional Fail today — `EnemyAttackSequence.cs:128` comment names the intended resolver).
  **Why:** Replace the flat Fail with the LCK-driven roll (Clutch first, then Pushback vs Fail by LCK/WIS).
  **Done when:** new `Services/CastInterruptResolver.cs`; `InterruptCastsByOwner` routes through it; **Pushback** rewinds the spell-icon u + brief stun (icon already supports u + Stunned); **Fail** = current behavior.
  **Touch:** new `Services/CastInterruptResolver.cs`, `Canvas/TimelineBarInstance.InterruptCastsByOwner`, `EnemyAttackSequence`. **Bible:** §13.4, casting prose, delete §16.2 resolver row. **Dep:** —

- [ ] **US-025 — `ClutchSequence` (the miracle save).** `NOT-BUILT`.
  **Done when:** rare LCK outcome: screen flash / SFX / "Clutch!" text, snap spell-icon to u=1 (reuse `EnterResolvingMode`), run normal resolution; base rate ≈ `LCK/200`, designer-capped.
  **Touch:** new `Sequences/ClutchSequence.cs`. **Bible:** casting prose. **Dep:** US-024.

- [ ] **US-026 — Enemy charge/telegraph spells.** `NOT-BUILT` (`EnemyAttackSequence` is melee-only; `EnemyPlanner` is positional-only).
  **Why:** The Caster archetype (§14.2) + the inverse-interrupt loop need enemies that telegraph a charge in the Prepare Zone.
  **Done when:** a casting enemy spawns a colored charge timeline icon advancing via cast-time; resolves into an attack at u=1; `EnemyPlanner` chooses to charge.
  **Touch:** `Services/EnemyPlanner`, new `Sequences/EnemyChargeSequence`, `TimelineBarInstance.SpawnSpellIcon` (generalize beyond hero casts), enemy spell data. **Bible:** §2.8, §14.2 (Caster), §14.3. **Dep:** —

- [ ] **US-027 — Interrupt enemy cast → drop charge-color orb.** `NOT-BUILT`.
  **Why:** This is *how off-palette colors enter the bank* (§3.1.2). Closes the mana economy.
  **Done when:** a pincer/Shield hit that interrupts a charging enemy cancels the cast AND mints one orb of the charge color via `DropOrbAt`/`ManaOrbFactory`.
  **Touch:** `Canvas/TimelineBarInstance.InterruptCastsByOwner` (enemy branch), `Managers/PincerAttackManager`, `ManaPoolManager`. **Bible:** §3.1.2, §13.4. **Dep:** US-026.

---

## EPIC D — Mana Color Identity
*Orbs are hardcoded Blue. Give them class identity and the remaining mint sources so the bank profile reflects party composition (§23.2.1).*

- [ ] **US-030 — Per-hero color affinity on pincer mint.** `PARTIAL` (all drops hardcoded `ManaType.Blue` — `PincerAttackManager.cs:206`; no `ColorAffinity` on `ActorData`).
  **Done when:** `ActorData.ColorAffinity` (or per-class) added; `DropOrbAt` mints the contributor's affinity color; seed §23.2 colors (Cleric=W, Barbarian=R, …).
  **Touch:** `Models/Actor/ActorData`, `Managers/PincerAttackManager`. **Bible:** §3.1.2, §23.1, §23.2, delete §16.5 #13; resolve §29.2 #8. **Dep:** —

- [ ] **US-031 — Critical hit → Colorless orb.** `NOT-BUILT` (`HitOutcome.Critical` exists; no orb mint — `AttackHelper.cs:74-128`).
  **Done when:** a crit (pincer or spell) mints one Colorless orb.
  **Touch:** `Helpers/AttackHelper` / `SpellEffectDispatcher`, `ManaPoolManager`. **Bible:** §3.1.2. **Dep:** US-030.

- [ ] **US-028 — Quicken / Hasten (forward push + overtake).** `NOT-BUILT` (no spell; no forward-push in `ResolveSpatialOverlap`).
  **Why:** Inverse of pushback — slide a target's icon toward the trigger, overtaking neighbors (inverted train-cascade).
  **Done when:** a Quicken spell increases target icon u; `ResolveSpatialOverlap` runs inverted; turn order updates.
  **Touch:** `Canvas/TimelineBarInstance.ResolveSpatialOverlap`, `Data/SpellLibrary` (Quicken). **Bible:** §2, casting prose. **Dep:** —

- [ ] **US-033 — Color conversion / pressure valve.** `NOT-BUILT` (`ManaBank` has Add/Spend/CanAfford only).
  **Why:** Documented escape valve (§3.1.6) so off-color banks aren't dead weight.
  **Done when:** a `ManaBank.Trade()` + in-battle UI trades N orbs of one color toward another (Colorless wildcard at a cost). Design-locked first.
  **Touch:** `Models/ManaBank`, `ManaPoolManager`, conversion UI. **Bible:** §3.1.6 (promote intent → built). **Dep:** US-030.

---

## EPIC E — Equipment Data Layer
*Durability + weapon-swap are done (§A). What's left is the three planned `ItemDefinition` fields and their consumers.*

- [ ] **US-040 — Extend `ItemDefinition` with planned fields.** `NOT-BUILT` (`ItemDefinition.cs:63-124` lacks all three).
  **Done when:** `BattleStartManaOrbs:int`, `OnUseSpellName:string`, `ResistanceModifiers:Dict<DamageType,float>` added with safe defaults.
  **Touch:** `Inventory/ItemDefinition`. **Bible:** §24.3. **Dep:** —

- [ ] **US-041 — Mage Robe / Wizard Robe battle-start orbs.** `PARTIAL` (`MageRobes` item exists `ItemData_Armor.cs:128`; no Wizard Robe; no battle-start scan).
  **Done when:** Wizard Robe added; `ManaPoolManager.Start` scans equipped party gear and adds `BattleStartManaOrbs` random orbs per robe (stacks per wearer, respects the 12 cap §3.1.4).
  **Touch:** `Data/ItemData_Armor`, `ManaPoolManager.Start`. **Bible:** §24.8, §3.1.4, delete §16.3 #6. **Dep:** US-040.

- [ ] **US-042 — Sleep Dart (item triggers a spell).** `NOT-BUILT` (no item; `AbilityBar.HandleItem:71-77` + `UseItemSequence` are heal/damage-only, no spell routing).
  **Done when:** Sleep Dart consumable (stack 5) routes `HandleItem` through `OnUseSpellName` → Sleep's targeting flow → `SpellEffectDispatcher.Cast`, consuming one charge on confirm.
  **Touch:** `Data/ItemData_Consumables`, `Canvas/AbilityBar.HandleItem`, `Sequences/UseItemSequence`. **Bible:** §24.8, §4.4, delete §16.3 #7. **Dep:** US-040.

- [ ] **US-043 — Equipped `ResistanceModifiers` folded into damage.** `NOT-BUILT` (resistances are per-class on `ActorData` only; no equipment aggregation — `Formulas.cs:293-300`).
  **Done when:** equipped `ResistanceModifiers` aggregate (alongside `ComputeEquipmentBonus`) into the wearer's effective resistance used by `ApplyDamage`.
  **Touch:** `Utilities/Formulas`, `SpellEffectDispatcher.ApplyDamage`. **Bible:** §3.3, §24.3. **Dep:** US-040.

---

## EPIC F — AI Depth
*The planner is solid for positioning (§A) but lacks the §14.3 future hooks. Best done after enemy casting (US-026) exists so the Caster archetype has behavior to plan.*

- [ ] **US-080 — Threat tracking (damage → preferred target).** `PARTIAL` (targeting weights distance+HP, not damage dealt — `EnemyPlanner.cs:42-44`).
  **Done when:** `EnemyPlanner` adds an accumulated-damage-per-hero weight to target selection.
  **Touch:** `Services/EnemyPlanner` (+ a per-battle damage tally). **Bible:** §14.1.2 (new factor), §14.3. **Dep:** —

- [ ] **US-081 — Coordinated retreat (wounded enemies flee).** `NOT-BUILT`.
  **Done when:** below an HP threshold an enemy biases moves *away* from heroes.
  **Touch:** `Services/EnemyPlanner`. **Bible:** §14.3. **Dep:** —

- [ ] **US-082 — AI supporter positioning.** `PARTIAL` (planner seeks its *own* pincer, not moves that enable an *ally's*).
  **Done when:** a support branch rewards moves completing an ally's pincer line.
  **Touch:** `Services/EnemyPlanner`. **Bible:** §14.3. **Dep:** —

- [ ] **US-083 — Boss scripted phases.** `NOT-BUILT`.
  **Done when:** a per-class boss override (or `BossScript` sequence) swaps generic stepping for authored phases via `SequenceManager`.
  **Touch:** `Services/EnemyPlanner`, new `Sequences/Boss*`. **Bible:** §14.2 (Boss), §14.3. **Dep:** US-026.

---

## EPIC G — UI Polish & Accessibility
*Sand the edges. Several of these are PARTIAL — the framework exists, the last mile doesn't.*

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
  **Done when:** only `Enemy`-tagged entries; unseen show silhouettes per `BestiaryProgress.seen`.
  **Touch:** `Canvas/BestiaryView`. **Bible:** §11.2, §15.3, delete §16.5 #14 + §16.6 bestiary row; resolve §29.4 #16. **Dep:** US-054.

- [ ] **US-096 — Music playback + Settings volume sliders.** `PARTIAL` (`AudioManager` is SFX-only `:48-90`; `MusicTrackLibrary` has 1 track; `SettingsManager` has no audio sliders).
  **Done when:** `AudioManager` gains music playback (Vorbis stream) + Music/SFX/UI volume + independent mutes in `SettingsManager`, persisted.
  **Touch:** `Managers/AudioManager`, `Managers/SettingsManager`, `Libraries/MusicTrackLibrary`. **Bible:** §12.1.1 audio, §30.4, §31.5; resolve §29.4 #18. **Dep:** —

- [ ] **US-091 — AbilityBar tooltip (hover / long-press).** `NOT-BUILT`.
  **Done when:** hover/long-press shows name, cost, target-shape preview, base dmg/heal.
  **Touch:** `Canvas/AbilityBar`, new tooltip. **Bible:** §4.5. **Dep:** US-076.

- [ ] **US-092 — Cooldown slot visual state.** `NOT-BUILT`.
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
- **§29.2 #8 color identity per class** → gates **US-030** (seed from §23.2).
- **§29.3 #12 heal/rest model** (free vs gold) → shapes **US-053**.
- **§29.3 #13 out-of-battle debuff carry** → §8.8 locks *clear-on-end*; confirm before shipping HP carry-over.
- **§29.4 #16 Bestiary unlock gate** → gates **US-093**.
- **§29.4 #17 AspectGuard strategy** → ratify §26 before **US-001**.

---

*Reconciled against the live codebase 2026-05-30. When you land a story: check the box, update the cited bible section, and move any new rule/question into bible §16/§29 so this board and the bible never drift again — which is exactly the drift this pass had to correct.*
