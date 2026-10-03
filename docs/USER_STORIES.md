---
codex: 1
project: GridGame2026
code: GG
layer: stories
status: living
updated: 2026-10-03
---

# GridGame2026 — User Stories

> ✅ done (shipped & verified) · 🟡 partial · ⬜ planned. Every ✅ cites its evidence: the verifying
> test where one exists (suites in [BIBLE §6](BIBLE.md#gg-§6)), otherwise the implementing file and
> its `DebugManager` demo. Story IDs are `US-NNN`; the bible cites them by number. Epics group them.

## Definition of Done (every story)
1. Compiles, zero Console errors (§17.3). 2. **Bible section updated** (no silent drift). 3. `DebugManager.Demo_*` + DebugWindow button shipped ([[feedback_debug_window_demos]]). 4. `CheckAllGuardrails` green. 5. Automated suites green (`tools/run-tests.ps1`) where testable, plus an in-editor play-test for layout/feel. 6. One commit at the end ([[feedback_commit_granularity]]).

---

## Core systems

**Casting**
- ✅ `Formulas.CastTime(baseSeconds, wis, int)` WIS/INT cast-time scaling (`Formulas.cs`; `CastingState.cs`).
- ✅ Cast icons on the timeline: `TimelineIconMode.Resolving` + `EnterResolvingMode()` (`TimelineIcon.cs`), spawned by `TimelineBarInstance.SpawnSpellIcon`.
- ✅ TurnManager third state: `IsResolvingCast` + input suspension (`TurnManager.cs`).
- ✅ Hits on a casting hero route to `TimelineBarInstance.InterruptCastsByOwner` (`EnemyAttackSequence.cs`, `ActorInstance.DamageRoutine`).

**Buffs**
- ✅ Burning/Poisoned per-tick damage on the timeline-advancing gate (`BuffTickManager.cs`).

**Mana**
- ✅ Orb line responsive/equidistant layout (`ManaOrbLineFactory.cs`; `ManaPoolManager.cs`).

**Equipment & durability**
- ✅ Weapon shatter dual-damage — target bonus + wielder self-damage (`WeaponDurabilityHelper.cs`).
- ✅ Repair max-durability cap + escalating cost (`WeaponDurabilityHelper.cs`).
- ✅ Ability-bar weapon swap end-to-end (`ChangeEquippedWeaponSequence.cs`; `AbilityLibrary.FromWeapon`).

**Save & macro loop**
- ✅ `HeroEquipmentSave.AbilityBarSlots` round-trip (`Profile.cs`; `HeroLoadout.cs`; `SaveRoundTripTests`).
- ✅ Stage carrier via `StageSaveData.CurrentStage` (`StageManager.cs`; `Profile.cs`).
- ✅ `StageLibrary` with 15+ stages, waves, per-wave actors (`StageLibrary.cs`).
- ✅ Game→PostBattle on victory + defeat (`BattleWonSequence.cs`, `BattleLostSequence.cs`).
- ✅ PostBattle XP/loot/gold reveal + save commit via `ExperienceTracker` / `LootTracker` / `GoldTracker` (`PostBattleManager.cs`).
- ✅ All vendors hydrate-on-Awake + commit-before-navigate (`VendorManager`, `BlacksmithManager`, `AlchemistManager`, `EquipManager`, `PartyManager`, `AbilitiesManager`, `SummonManager`).
- ✅ StageSelect unlock gating reads `HighestClearedStageIndex` (`StageSelectManager.cs`; `CampaignStagesTests`).

**Content & data**
- ✅ `DropTableLibrary` — 16 populated per-enemy tables (`DropTableLibrary.cs`).
- ✅ `RecipeLibrary` — 22 recipes incl. Iron→Steel; Blacksmith Forge/Salvage + Alchemist Brew menus (`RecipeLibrary.cs`, `BlacksmithManager.cs`, `AlchemistManager.cs`).
- ✅ `EnchantLibrary` — 4-element weapon affinity recipes (`EnchantLibrary.cs`).
- ✅ 172 concrete enemy classes with archetype stat leans + tags (`ActorLibrary.cs`).
- ✅ EnemyPlanner core: HP-weighted targeting, pincer-seek (+50), flank-avoid (−100), immobilize (`EnemyPlanner.cs`; `PincerDetectorTests`).
- ✅ Per-spell VFX: all 10 prefabs generated, auto-registered as Addressables (`VfxPrefabAuthor.SavePrefab`), registered in `VisualEffectLibrary`, referenced from `SpellLibrary`.

---

## EPIC A — Foundations

- [x] **US-120 ✅ Multi-tile enemies (2×2 bosses).** As a player I fight bosses that occupy a 2×2 footprint. `location` is the anchor + `ActorData.Footprint`; occupancy is centralized in `ActorInstance.Occupies(tile)` + `GameHelper.Actors.ActorAt/IsTileOccupied`, so movement / pincer / targeting / spawn / support lines are footprint-aware. Four rules: the boss is an immovable wall to hero slides; it is pincered by flanking its width (counts as one opponent, `PincerDetector.Detect`); it shoves heroes on its own turn (`ActorInstance.StepFootprint` + `ResolveShoveChain`, `EnemyPlanner.FootprintStepLegal`); one timeline icon. `Cyclops00` is the first 2×2 boss. Demo: "Spawn 2×2 Boss". Bible §1.5.
- [x] **US-001 ✅ AspectGuard + portrait lock.** `Utilities/AspectGuard.cs` self-installs on every scene: snaps to the closest valid portrait aspect and letterboxes `Camera.main.rect` with a black background camera; normalizes every `CanvasScaler` to 1170×2532 / match 0.5; insets `SafeArea` children. Bible §26. *(See US-136 for the verification.)*
- [x] **US-002 ✅ Builders clear roots before rebuild.** `GameBuilder.Build()` calls `SceneBuilderHelper.ClearAllRootObjectsSilent()` first, so rebuilds are warning-free. Bible §17.1 #8.
- [x] **US-003 ✅ `LayerMask.NameToLayer("UI")` guard.** Every camera `cullingMask` site uses `uiLayer >= 0 ? (1<<uiLayer) : ~0` (`BestiaryBuilder.cs` is the only one). Bible §17.1 #10.
- [x] **US-004 ✅ 60 fps pinned at launch.** `Scripts.Helpers.Bootstrap` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`) sets `Application.targetFrameRate = 60`; `GameManager` later applies the user's setting. Bible §30.5.
- [x] **US-053 ✅ HP carry-over between battles.** `CharacterLevelPair.HpCurrent` persists wounds on victory (a hero who fell revives at 1), is hydrated on spawn (`StageManager.SpawnActor`), and defeat resets the party to full (`BattleLostSequence`). Demos: "Wound Party 50%" / "Heal Party Full". Bible §15.1.
- [x] **US-054 ✅ Bestiary progress (seen / defeated).** `BestiarySaveData` (list-based) marks enemies Seen on spawn (`StageManager.SpawnActor`) and Defeated/TimesDefeated on death (`ActorInstance.DieRoutine`), persisted at battle end. Demo: "Log Bestiary". Bible §15.3.
- [x] **US-110 ✅ StageSelect = scrollable, replayable level list (newest-on-top).** `StageSelectManager.RebuildList()` lists themes and stages in reverse so the newest unlocked stage is on top; every unlocked stage is replayable; each row carries a `drops: X, Y` farming hint (`BuildDropsHint`). Demo: "Stage Order". Bible §22.3.
- [x] **US-111 ✅ Vendor scenes scale and scroll.** `VendorBuilder` uses the §26.2 scaler (1170×2532, match 0.5) and a fully wired `ScrollRect` (`viewport`, `content`, vertical only). Bible §17.1 #11, §25.1.
- [x] **US-113 ✅ FadeOverlay speed = 125 ms.** `FadeOverlayInstance.fadeDuration` = 0.125 s each way. Bible §11.3.

---

## EPIC B — Buffs That Bite

- [x] **US-016 ✅ Turn-unit buff decrement at the turn boundary.** `TurnManager.NextTurn` ticks turn-unit buffs at END of turn (enemy turn → that enemy; hero-window end → every hero). Demo: "Slowed → Enemy" / "Trigger Enemy Attack". Bible §8.3.
- [x] **US-011 ✅ Slowed slows the timeline.** `TimelineIcon.GetEffectiveUPerSec()` folds in `Buffs.SlowedTimelineMultiplier` (×0.5), read by `TimelineBarInstance.AdvanceBySeconds`. Demo: "Slowed → Enemy". Bible §8.1.
- [x] **US-012 ✅ Silenced blocks casting.** `AbilityBar.HandleSpell` refuses spell casts from a Silenced caster ("Silenced!"); `Refresh` renders their Spell slots as a solid-red blocked state. Demo: "Silenced → Hero". Bible §4.5, §8.1.
- [x] **US-013 ✅ Blinded lowers hit chance.** `Formulas.CalculateHitType` multiplies a Blinded attacker's hit chance by `Buffs.BlindedAccuracyMultiplier` (0.5). Demo: "Blinded → Enemy". Bible §13.1.1.
- [x] **US-014 ✅ Sleep lasts longer on Warm targets.** `SpellEffectDispatcher` applies Sleep with ×1.5 duration on a Warm target ("Deep Sleep!"). Bible §8.2.
- [x] **US-015 ✅ Displacement breaks Sleep.** `ActorMovement.HandleOverlap` calls `BuffSystem.OnMoved(instance)`. Bible §8.2.3.

---

## EPIC C — Interrupt Depth + Enemy Casting + Orb Economy

- [x] **US-024 ✅ Cast-stagger interrupts.** Each landing hit on a casting actor adds a WIS/STR-scaled delay (`CastingState.AccumulatedInterruptDelay`, `TimelineIcon.DelayCast`); when the accumulated delay exceeds the original cast time the cast is cancelled (`CastingState.Interrupt`). WIS can shrug a hit; a rare LCK Clutch is rolled first (`CastInterruptResolver`). Demo: "Cast-Stagger Info". Bible §13.4.
- [x] **US-025 ✅ Clutch — the miracle save.** `Sequences/ClutchSequence.cs` plays a white flash + "Heal" SFX + "Clutch!" text, then `TimelineIcon.ForceResolve()` snaps the cast to u=1 and resolves it through the normal closure. Demo: "Clutch! (Force)". Bible §13.4.
- [x] **US-026 ✅ Enemy charge/telegraph spells.** The pure `EnemyPlanner.PlanCast` lets a `Magic`-tagged caster that is not adjacent to a hero telegraph a charge; `EnemyTakeTurnSequence` queues `EnemyChargeSequence`, which spawns a cast icon via `SpawnSpellIcon` and resolves into `MagicAttackSequence` at u=1 on the shared clock. `EnemyChargeCatalog` derives the element from affinity tags; IceMauler is a live caster. Demo: "Enemy Charge". Bible §13.4, §14.2.
- [x] **US-027 ✅ Cancelling an enemy charge mints its color.** `ActorInstance.DamageRoutine` routes any hero hit on a charging enemy to `InterruptCastsByOwner` (no Clutch for enemies); on cancel `MintInterruptOrb` drops an orb of the charge color (`ManaOrbFactory.Drop`). Demo: "Interrupt Charge". Bible §3.1.2.

---

## EPIC D — Mana Color Identity

- [x] **US-030 ✅ Per-hero color affinity on pincer mint.** `Data/Actor/ManaColorAffinity.For(class)` (Cleric W, Paladin W, Barbarian R, Alchemist G, Assassin B, GreenNinja G, RedNinja R; others Blue); `PincerAttackManager` mints each contributor's color. Demo: "Log Color Affinities". Bible §3.1.2, §23.2.
- [x] **US-031 ✅ Critical hit → Colorless wild orb.** Hooked in `ActorInstance.DamageRoutine`; the wild orb cycles the spectrum in the line (`ManaOrbLine.AnimateWildOrbs`). Demo: "Mint Wild Orb". Bible §3.1.2.
- [x] **US-028 ✅ Quicken / Hasten.** `TimelineIcon.Hasten` + `TimelineBarInstance.HastenIcon` slide an icon forward in u; `SpellLibrary.Quicken` (`HastenU` 0.30, 1×Blue) applies it via `SpellEffectDispatcher`; overtaking is emergent. Demo: "Quicken". Bible §2.7.1.
- [x] **US-033 ✅ Pressure valve (Colorless wildcard).** `ManaBank.CanAfford`/`Spend` pay each cost with its own color first, then Colorless wilds; explicit Colorless costs only take Colorless. Demo: "Test Wildcard Spend". Bible §3.1.6.

---

## EPIC E — Equipment Data Layer

- [x] **US-040 ✅ `ItemDefinition` battle fields.** `BattleStartManaOrbs`, `OnUseSpellName`, `ResistanceModifiers` with safe defaults (`ItemDefinition.cs`). Demo: "Log ItemDef Fields". Bible §24.3.
- [x] **US-041 ✅ Mage / Wizard Robe battle-start orbs.** `MageRobes` = 2, `WizardRobe` = 3; `ManaPoolManager.ApplyBattleStartManaOrbs` adds that many random WUBRG orbs at battle start, capped at 12. Demo: "Battle-Start Orbs". Bible §24.8.
- [x] **US-042 ✅ Sleep Dart (item casts a spell).** `cons_sleep_dart` (`OnUseSpellName="Sleep"`, stack 5); `ManaAbility.SourceItemId` links the slot to its item; `AbilityBar.HandleItem`→`TryHandleItemSpell` runs Sleep's targeting and spends a charge on confirm. Demo: "Verify Sleep Dart Route". Bible §4.4, §24.8.
- [x] **US-043 ✅ Equipped resistances fold into damage.** `SpellEffectDispatcher.EquipmentResistanceMultiplier` multiplies every equipped item's `ResistanceModifiers[type]` into `ApplyDamage` (Sunfire Amulet: Fire ×0.7). Demo: "Log Resistances". Bible §13.1.2.

---

## EPIC F — AI Depth

- [x] **US-080 ✅ Threat tracking.** `Managers/ThreatTracker` tallies hero→enemy damage; `EnemyPlanner` subtracts `(threat/maxThreat) × INT × 0.8` from target scores. Demo: "Log Threat". Bible §14.1.2.
- [x] **US-081 ✅ Wounded enemies retreat.** Below `RetreatHpThreshold` (0.30) `EnemyPlanner.PlanStep` flees and drops adjacency/pincer-seek bonuses. Demo: "Test Enemy Retreat". Bible §14.1.2.
- [x] **US-082 ✅ AI supporter positioning.** `EnemyPlanner.WouldSupportAllyPincer` rewards (+25) becoming a supporter of another ally's pincer. Demo: "Log Enemy Plans". Bible §14.3.
- [x] **US-083 ✅ Boss scripted phases.** `BossScriptLibrary` phase table + pure `BossPhaseRunner` + `BossPhaseTransitionSequence`; per-phase `PrefersCharge`. Cyclops00 enrages below 50% HP. Demo: "Trigger Boss Enrage". Bible §14.2, §14.3.

---

## EPIC G — UI Polish & Accessibility

- [x] **US-114 ✅ Timeline two-lane layout.** Actor turn icons ride above the timeline line; spell cast icons (¼ size) ride below on the same u-axis (`TimelineBarInstance.SpawnSpellIcon`, `TimelineIconFactory.CreateForCast`); `AbilityBar.HandleSpell` spawns cast icons; enemy charge icons share the lane. Bible §2.6.
- [x] **US-076 ✅ Spell icons on the AbilityBar.** `AbilityBarFactory` adds a 36×36 icon `Image` per slot; `AbilityBar.Refresh` shows `SpriteLibrary.SpellIcons[name]`, glyph fallback when absent. Demo: "Spell Icons". Bible §4.5.
- [x] **US-077 ✅ Scan reveals enemy stats.** `SpellDefinition.RevealsStats`; `SpellEffectDispatcher` announces HP/STR/VIT/AGI/INT via the AnnouncementWindow and marks the class Seen (`Bestiary.MarkSeen`). Demo: "Scan Enemy". Bible §7.
- [x] **US-090 ✅ "No valid targets" toast.** `TargetingMode.Begin` announces "No valid targets" before cancelling when Auto / PickActor resolves nothing. Bible §5.2.
- [x] **US-093 ✅ Bestiary enemy filter + seen gate.** `BestiaryView.BuildPages()` lists `ActorTag.Enemy` only; unseen classes show a silhouette, "???" and hidden lore. Demo: "Bestiary Filter". Bible §15.3.
- [x] **US-096 ✅ Music + volume/mute settings.** `ProfileSettings.{MusicVolume, SfxVolume, MuteMusic, MuteSfx}` (persisted); `AudioSettingsHelper.Apply()` pushes effective volumes to `Jukebox` and the battle `SoundSource`; `SettingsManager` sliders/toggles live-apply. Demos: "Music Vol 25/100%", "Toggle Mute Music/SFX". Bible §31.5.
- [x] **US-091 ✅ AbilityBar tooltip.** `AbilityBar.ShowTooltipForSlot(i)` → `Tooltip.Show` (name, kind, cost / charges / "Free", cooldown, cast time) on hover / long-press. Bible §4.5.
- [x] **US-092 ✅ Cooldown slot visual state.** `ManaAbility.CooldownTurns` (Steal 3 / Mug 2 / Teleport 3), per-hero `SkillCooldownManager`; `AbilityBar` fades the slot, shows the turns left and a `CooldownSweep` radial overlay. Demos: "Lock Skill CDs" / "Tick CD". Bible §4.1.1, §4.5.
- [x] **US-094 ✅ Colorblind palette toggle.** `ProfileSettings.ColorblindMode`; `Helpers/ColorblindHelper.cs` (Okabe-Ito) feeds `ManaOrbLine.ColorFor` and `DebuffIconBar.ColorFor`. Demo: "Toggle Colorblind". Bible §31.1.
- [x] **US-095 ✅ Reduce-motion toggle.** `ProfileSettings.ReduceMotion`; `VisualEffectManager.IntensityScale` = 0 suppresses particle VFX; `ProjectileMotionEval.ReduceMotion` flattens arcs. Demo: "Toggle Reduce Motion". Bible §31.2.

---

## EPIC H — Performance & Hardening

- [x] **US-100 ✅ Coroutine hygiene.** `SequenceManager.CancelAll()` stops outer + inner coroutines, resets state and clears the queue; `OnDisable` delegates to it. Bible §30.3.
- [x] **US-101 ✅ GC hot-path cleanup.** `EnemyPlanner.PlanStep` / `PlanCast` use static scratch lists and manual min-search instead of per-call LINQ. Bible §30.2.
- [x] **US-102 ✅ Particle caps + VFX pooling.** `VisualEffectManager` caps live instances at `MaxConcurrentVfx` = 48 and pools wrapper GOs (`VisualEffectInstance.ResetForPool`). Demo: "VFX Pool Stats". Bible §30.1.
- [x] **US-103 ✅ HUD texture atlas.** `CliEntryPoints.BuildHudAtlas` creates `Assets/Sprites/HudAtlas.spriteatlas` over 14 HUD sprite folders, Addressable `HudAtlas` / label `UI`. Bible §30.4.

---

## EPIC I — Vendor services & visual language

- [x] **US-121 ✅ Blacksmith Repair tab.** Third `Mode.Repair` tab lists every hero's equipped weapon/armor with a durability pool (worn first); repairs cost `WeaponDurabilityHelper.RepairCost`, restore to `EffectiveMaxDurability`, and warn when uneconomical (`BlacksmithManager`, `BlacksmithBuilder`). Demo: "Wear Gear −5". Bible §25.2.
- [x] **US-123 ✅ One visual language.** `HubTheme` palette + `ButtonColors`, `UiFonts` (Attic display / Outfit body), editor `UiKit` component factory; every scene builder and runtime row factory composes from them (`UiKit.cs`, `SceneBuilderHelper.cs`). Bible §11.5.
- [x] **US-122 ✅ Alchemist heal service.** "Heal Party" button prices the party's missing HP at 0.5g/HP and clears `HpCurrent` (`AlchemistManager`, `AlchemistBuilder`). Bible §25.3.

---

## EPIC J — Working proof of concept

- [x] **US-124 ✅ Automated verification harness.** Game code in `Scripts.asmdef`; EditMode + PlayMode suites; `TestHooks.cs` (profile isolation, `RNG.Seed`, deterministic placement, pacing accel); `tools/run-tests.ps1` 3-signal gate; editor hooks stand down in `-runTests` sessions. *(Verified by `SceneBootSmokeTests`, `BattleLoopScenarioTests`.)* Bible §6.
- [x] **US-125 ✅ Boot flow.** `StartSceneConfig.StartScene` = `"SplashScreen"`; TitleScreen Continue → StageSelect; `StageManager.Initialize()` reads `CurrentSave`. *(Verified by `SceneBootSmokeTests`.)* Bible §22.
- [x] **US-126 ✅ Coins become gold.** `Managers/GoldTracker.cs` snapshots `Global.TotalCoins` at battle start and commits the per-battle delta into `Inventory.Gold` at the PostBattle loot phase ("Gold +N" row). *(Verified by `GoldTrackerTests`.)* Bible §24.9.
- [x] **US-127 ✅ VendorNavBar is the only vendor navigation.** StageSelect and every vendor scene carry the `VendorNavBar`; `Hub.unity` / `Overworld.unity` are out of `EditorBuildSettings.scenes` (`StageSelectBuilder.cs`, `VendorNavBar.cs`). Bible §25.0.
- [x] **US-128 ✅ Bounty board on StageSelect.** `BountyBar` strip (`StageSelectBuilder.BuildBountyBar`, `StageSelectManager` via `BountyHelper`): browse / Accept / progress / Abandon / Claim. *(Verified by `BountyFlowTests`.)* Bible §22.4.
- [x] **US-129 ✅ Abilities scene slots skills and spells.** The assignables list leads with the hero's `ActorData.Abilities`, assigned by name via `AbilityBarSlotSave.AbilityName` (`AbilitiesManager`). *(Verified by `AbilitySlottingTests`.)* Bible §25.6.
- [x] **US-130 ✅ Safe scene teardown.** `Singleton.HasLiveInstance` (non-creating probe) guards `TargetModeOverlay` / `AbilityButtonManager` `OnDestroy`, so Game→PostBattle never resurrects GameManager; ProfileCreate fades in from `Start`. *(Verified by `SceneBootSmokeTests`.)*
- [x] **US-131 ✅ Story crawl.** `StoryCrawl` scene: 26 s upward crawl, skip by button or tap; text in `Data/StoryCrawlData.cs` per theme; shown on first entry into a theme (`GlobalSaveData.SeenStoryCrawls`). *(Verified by `SceneBootSmokeTests`.)* Bible §27.
- [x] **US-132 ✅ Summon Circle.** `Summon` scene + NavBar entry; `Services/SummonService` (7-class pool, cost 250 + 250×recruits, refuses duplicates / short gold); recruit appends `save.Roster`; new saves start with the trio. *(Verified by `SummonServiceTests`.)* Bible §25.10.
- [x] **US-133 ✅ Combat feed with inline icons.** `Canvas/CombatFeed` (7 aging lines) mirrors every announcement plus damage / status / heal / assist lines; `CombatFeedSpriteAssetAuthor` packs the `CombatFeedIcons` TMP sprite asset (`CombatFeed.cs`, `CombatFeedFactory`). Bible §9.
- [x] **US-134 ✅ Buff effects bite in battle.** Ticks via `EndTurnSequence`→`TickStatusesRoutine` (fed to the combat feed); Slowed (`TimelineIcon`), Silenced (`AbilityBar`), Blinded (`Formulas.CalculateHitType`), Lightning×Wet (`SpellEffectDispatcher`), immobile (`EnemyPlanner`); `Buffs.cs` comments name each live hook. Bible §8.
- [x] **US-135 ✅ Campaign difficulty curve.** `CampaignStages.RecommendedLevel(stage)` (stage N → level N) floors enemy levels in `StageManager.SpawnActor`; StageSelect shows "Recommended level: N". *(Verified by `CampaignStagesTests`.)* Bible §22.3.
- [x] **US-136 ✅ Multi-aspect verification.** `AspectGuard.LetterboxRect` / `SafeAreaAnchors` / `ClosestValidAspect` are pure and checked across a 9-device matrix + landscape. *(Verified by `AspectGuardTests`.)* Bible §26.3.
- [x] **US-137 ✅ Royalty-free audio + Credits attribution.** Authored music beds via `MusicTrackLibrary` with chiptune fallback; `AudioAddressableRegistrar` registers the audio Addressables; `Data/AudioCredits` renders full attribution in Credits. *(Verified by `AudioCreditsTests`.)* Bible §12.0.
- [x] **US-138 ✅ Line-shaped elemental enemy attacks.** Fire-affinity charges lock a cardinal line (`Services/LineThreat`), glow it red (`LineTelegraph`), and hit every hero still on it at u=1. *(Verified by `TrapAndLineThreatTests`.)* Bible §14.2.
- [x] **US-139 ✅ Tile traps.** Scorpion lays Venom Snare traps (`TrapCatalog`, `PlaceTrapSequence`, `TrapManager`); slides and displacements both trigger them (`ActorMovement`). *(Verified by `TrapAndLineThreatTests`.)* Bible §14.2.
- [x] **US-140 ✅ Segmented snake boss.** Every `Naga00` grows a 3-segment chain (`SnakeBossManager.CreateChain`); segments follow the head, have no icon, and are armored until the tail-side members fall. *(Verified by `SnakeBossTests`.)* Bible §14.2.
- [x] **US-141 ✅ Ability gating split.** Skills are free with turn-cycle cooldowns (`ManaAbility.CooldownTurns`, `SkillCooldownManager`); Spells cost orbs (`ManaBank.CanAfford`); Items consume charges; `AbilityBar.Refresh` renders all three states. Bible §4.1.
- [x] **US-142 ✅ Time-banked orbs.** `ManaPoolManager.RecordHeroActionForTimeBank` (at each drop) + `MintTimeBankedOrbs` (at `TurnManager.BeginEnemyTurn`): one Blue orb per 3 s banked, max 3 per window. Bible §3.1.8.
- [x] **US-143 ✅ Progressive ability-bar slots.** `Services/AbilitySlotProgression`: 2 slots fresh, +1 at `HighestClearedStageIndex` 0 / 2 / 5, max 5; enforced in `AbilityBar` and `AbilitiesManager`. *(Verified by `AbilitySlotProgressionTests`.)* Bible §4.7.
- [x] **US-144 ✅ Touch info for abilities and actors.** Ability long-press tooltips are generated live from the ability (`AbilityBar.ShowTooltipForSlot`); tapping an actor binds the tabbed `ActorPanel` (stats / equipment / lore); Scan reveals enemy stats. Bible §4.5, §9.
- [x] **US-145 ✅ Portrait pop-in variety.** `PortraitManager.SpawnPair2DRoutine` picks a `PairEntryPattern` per pincer (CounterSweep, ReverseCounterSweep, SameSideStagger, StaggeredCounter; seed-deterministic); supporters keep their pop-in.

---

## Backlog

- [ ] **US-104 ⬜ 60fps profiling pass (mid-tier device).** Profile a dense battle (4+ enemies, full VFX) on a physical Android/iOS mid-tier device with the Unity Profiler. **Done when:** the frame budget stays within §30.1; overages become new stories. **Bible:** §30.1, §30.5.
- ⬜ **Merged hub `.unity`** (§25.9) — fold the vendor scenes into one composed screen, only once every vendor is individually stable.
- ⬜ **TargetShape.Line** — `Row`/`Column` cover line targeting today; add `Line` only if a *partial* line is needed.
- ⬜ **Distinct loadouts for unfilled classes** (Ninja variants, Bruiser, Captain, Druid) — additive `HeroLoadouts.Set` content (§23.2.2).
- ⬜ **Roguelike / NG+** and **tutorial** — pending the §29.1 design questions.
- ⬜ **Relic-slot passives** (§24.1) — underspecified; design before storying.
- ⬜ **Deep-poison stacking / tiered buff upgrades** (§8.6); **crit-heal** (§13.1.3).
- ⬜ **Unify spell damage through `Formulas.CalculateAttackResult`** (§13.1.2) — so crit/miss/blind apply to dispatcher spells.

Open design questions that gate future stories live in [BIBLE §29](BIBLE.md#29-open-design-questions).
