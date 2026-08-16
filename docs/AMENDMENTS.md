---
codex: 1
project: GridGame2026
code: GG
layer: amendments
status: living
updated: 2026-06-07
---

# GridGame2026 — Amendments (append-only; amendment wins over the bible)

> Never rewrite an amendment — supersede it with a new one. Beyond ~25, fold into the bible (L0)
> and start a new epoch, noting the git tag; history stays in git.

## GG-A1 — Adopt the MindAttic Codex documentation standard (supersedes the root `game_bible.md` + `user_stories.md`)

**What changed.** The repo's bible system was migrated to the Codex layout:

- `game_bible.md` (root) → [`docs/BIBLE.md`](BIBLE.md), reformatted to the L0 nine-section template
  with stable IDs (`{#GG-§N}`, `{#GG-LAW-n}`). The full original canon is preserved verbatim in
  BIBLE Appendix A so all `§N` cross-references keep resolving. A 1-line pointer remains at the old
  `game_bible.md` path.
- `user_stories.md` (root) → [`docs/USER_STORIES.md`](USER_STORIES.md), with Codex front-matter and
  the legend/audit-log conventions. **Story IDs were kept as `US-NNN`** (not renamed to
  `GG-US-<Epic><n>`) because the bible cross-references hundreds of them by number; renaming would
  break every link. A pointer remains at the old path.
- Duplicated structured canon was extracted to **L5 data** under [`docs/data/`](data/) with JSON
  Schemas in [`docs/data/_schema/`](data/_schema/): `spells.json` (GG-§7), `buffs.json` (GG-§8),
  `classes.json` (GG-§23), `enemy_archetypes.json` (GG-§14), `item_rarities.json` (GG-§24). Prose now
  cites entities by `id`.
- The SessionStart hook `.claude/hooks/inject-bible.ps1` (read `game_bible.md`) was **replaced** by
  `.claude/hooks/inject-digest.ps1`, which injects the generated `docs/BIBLE.digest.md`.
  `.claude/settings.json` was updated to point at the new hook.
- Added `tools/codex.ps1` (`doctor` + `digest`) and a Codex section to `CLAUDE.md`.

**Why.** Single home per fact, machine-checkable cross-references and data, and a smaller
authoritative digest injected at session start instead of the full ~180 KB bible.

**Migration notes / defaults chosen.**
- The org-wide house rules at `../MindAttic.HouseRules.md` already existed and is **inherited by
  reference** from BIBLE §5 — not copied or modified.
- The L0 template's nine sections are the new outline; the original 30+ sections live in Appendix A
  rather than being lost or truncated ("preserve all content").
- `doctor`'s "✅ names a test" check is **best-effort**: this project verifies via in-editor
  play-test + code-reading (headless Unity is unlicensed here, GG-§6), so doctor warns rather than
  hard-fails when a ✅ story's evidence token is a source file/demo rather than a test method.

## GG-A2 — Player spell cast renders as a parallel cast-lane icon BELOW the timeline (not riding on it)

**What changed.** The player-initiated spell cast (AbilityBar → `SpellCastBar`) now renders as the
spell's ICON loading left→right on a dedicated lane just BELOW the enemy-icon rows, advancing in
**parallel** with the enemy icons (real time — the player keeps dragging heroes while it loads). It
was previously a shrinking colored line below the timeline. This supersedes the bible's description
(GG-§2) of a cast as an icon that rides ON the main timeline rows and, on reaching the trigger,
suspends all input as a "third turn state."

**Why.** Design direction from the user: the cast countdown should read as a separate track that
loads alongside the enemies, and casting should not seize the turn — leaving the player free to
reposition while a spell loads preserves the real-time tension of the timeline.

**Scope / what did NOT change.**
- The on-timeline, turn-suspending path (`TimelineBarInstance.SpawnSpellIcon` →
  `TimelineIconMode.Resolving` + `TurnManager.BeginCastResolution`) still EXISTS and is unchanged. It
  remains available for flows that *want* a turn-suspending cast (e.g. enemy charge/telegraph casts,
  Epic C). Only the player AbilityBar cast was routed to the new parallel lane.
- Cast timing, MP-spent-at-cast-start, the caster-died interrupt, and the brief post-resolve input
  lock are unchanged (still owned by `SpellCastBar`).

**Code.** `Canvas/SpellCastBar.cs` (travels a sprite icon instead of shrinking a fill),
`Factories/SpellCastBarFactory.cs` (builds the icon from `SpriteLibrary.SpellIcons`),
`Canvas/TimelineBarInstance.cs` (`CreateCastLaneIcon` — geometry for the below-rows lane).

## GG-A3 — The game loop is StageSelect-centric: Hub retired, boot = SplashScreen, coins bridge into gold

**What changed.** Four connected corrections that make the macro loop (§22) actually playable
end-to-end, superseding the bible's Hub-centric prose:

1. **Boot scene is `SplashScreen`** (`StartSceneConfig.StartScene`). It had drifted to `"Hub"`,
   which skipped the entire front door (Splash → Title → Profile → StageSelect) and silently
   auto-created a "Dev" profile. TitleScreen **Continue now routes to StageSelect** (was: straight
   to Game, which dropped fresh profiles into a random `Test-*` stage) —
   `StageSelectManager.ConfirmLaunch` is the only surface that sets `Stage.CurrentStage/CurrentWave`
   and the post-battle return scene.
2. **Hub.unity is retired from the flow** (soft-disabled per HOUSE-LAW-2: removed from
   `EditorBuildSettings.scenes` and the StageSelect "Shop" button deleted; files remain). The
   **`VendorNavBar` hamburger is the primary — and only — vendor navigation**, on StageSelect and
   every vendor scene. This supersedes §25.0's claim that the Hub "replaces the floating
   VendorNavBar as the primary way to reach vendors" (it was the other way around in practice:
   nothing routed back to the Hub). `Overworld.unity` (already cut per §3) also left the build list.
3. **PostBattle → StageSelect is canonical** (was already true in code; comments/§22 diagram said
   "Hub"). The reward pipeline is the static session-tracker trio
   **`ExperienceTracker` / `LootTracker` / `GoldTracker`** consumed by `PostBattleManager` — the
   bible's `BattleResultCarrier` / `SaveStateService.ApplyBattleResult` classes were never built and
   are hereby struck.
4. **Battle coins now become spendable gold.** Coin pickups increment the lifetime ticker
   (`save.Global.TotalCoins`) which vendors never read; vendors spend `save.Inventory.Gold`.
   Nothing bridged them — the CoinCounter racked up coins the shop never saw. New
   **`GoldTracker`** (mirror of `LootTracker`) snapshots the ticker at battle start and commits the
   per-battle delta into `Inventory.Gold` at the PostBattle loot phase, which now leads with a
   "Gold +N" row. Gold-per-stage = coins actually collected fighting it (no invented flat bonus);
   the lifetime ticker itself is untouched (it is a stat, not a wallet — supersedes §24.9's implied
   direct pickup→gold flow).

**Why.** Play-testing found "a bunch of half-baked parts": the shipped boot scene bypassed the whole
front door, StageSelect was unreachable before a first battle, and the economy loop was severed. The
automated test harness (see GG-A4) reproduced all of it headless.

**Code.** `StartSceneConfig.cs`, `TitleScreenManager.cs`, `StageSelectBuilder.cs` /
`StageSelectManager.cs`, `ProjectSettings/EditorBuildSettings.asset`, `Managers/GoldTracker.cs`
(new), `StageManager.cs` (also fixed: `Initialize()` read `LatestSave` while `RestartStage()` read
`CurrentSave` — divergence when loading older saves), `PostBattleManager.cs`.

## GG-A4 — Automated verification harness; bounty board on StageSelect; skills/spells slottable in Abilities

**What changed.**

1. **The project now verifies headless.** Game code moved into an assembly definition
   (`Assets/Scripts/Scripts.asmdef`, name "Scripts") so test assemblies can reference it —
   supersedes GG-A1's note that verification is play-test-only and §6's "headless Unity is
   unlicensed" (licensing works; verified). Suites: `Assets/Tests/EditMode` (pure logic: formulas,
   pincer rules, save round-trip, campaign unlocks, gold bridge, bounty flow, ability slotting) and
   `Assets/Tests/PlayMode` (scene-boot smokes for every live scene + battle-loop scenarios on the
   new deterministic `Test-Harness` stage). Runner: `tools/run-tests.ps1` (3-signal gating: results
   XML + zero `error CS` + exit code). Production hooks: `Scripts/Helpers/TestHooks.cs`,
   `RNG.Seed/Unseed`, `FolderHelper.Folder.TestProfileRootOverride` (tests never touch real saves).
   Editor hooks that hijacked test runs now stand down during batch/`-runTests` sessions:
   `StartSceneAuthority` (playModeStartScene), `DebugWindowBootstrapper` (auto-opened an
   EditorWindow that wedged the main loop), `CustomPlayBehaviour` (cancelled/re-entered play mode).
2. **Bounty board (US backlog → built).** The fully-coded but unreachable bounty system
   (`BountyHelper`/`BountyLibrary`/`BountyData_Hunts`) got its UI: a **BountyBar strip on
   StageSelect** — browse the posted contracts, Accept (single active slot), watch kill progress
   (`RecordKill` was already wired at enemy death), Claim gold + reward item.
3. **Abilities scene slots skills/spells, not just consumables.** The assignables list now leads
   with the hero's own active abilities (`ActorData.Abilities`), assigned by name via the existing
   `AbilityBarSlotSave.AbilityName` path that combat already resolves
   (`HeroLoadout.LoadFromSave` → `AbilityLibrary.Get`).

**Why.** Owner directive 2026-08-15: complete the game to a working, showable proof of concept with
automated verification as far as possible; wire the bounty system rather than delete it; make the
RPG loadout loop real.

**Code.** `Scripts.asmdef`, `Assets/Tests/**`, `tools/run-tests.ps1`, `TestHooks.cs`,
`StageLibrary.cs` (`Test-Harness` fixture stage), `StageSelectBuilder.cs`/`StageSelectManager.cs`
(BountyBar), `AbilitiesManager.cs` (Skills & Spells section), `Singleton.cs`
(`HasLiveInstance` teardown-safe probe — fixes GameManager resurrection during scene unload).

## GG-A5 — Minimal story crawl (partially reverses the §27 Dialog & Story cut); summon vendor; combat announcement feed

**What changed.** Three player-facing additions for the proof-of-concept build (owner directive
2026-08-15 evening):

1. **Story crawl — the §27 cut is PARTIALLY reversed.** §3 / §27 cut Dialog & Story entirely
   ("do not re-story"). The owner now wants a barebones plot: a **skippable text-crawl screen**
   shown before selected stages — per-theme intro paragraphs, data-driven, nothing more. What
   stays cut: the dialog system, character dialogue, branching, cutscenes, and the Overworld.
   (US-131.)
2. **Summon vendor.** Roster growth leaves the backlog: a NavBar-reachable vendor scene where the
   player spends **gold** to recruit one of the built hero classes into the roster, with a rising
   cost per hero owned. Deliberate purchase, not a pull — GG's "not a gacha" pillar stands.
   (US-132.)
3. **Combat announcement feed.** Every combat event (pincers, spells, supporter assists,
   buff/status ticks, deaths, loot) narrates through the AnnouncementWindow as a scrolling feed —
   "Paladin casts Heal", "Enemy bites Rogue; Rogue is poisoned" — with **inline icons via TMP
   `<sprite>` tags** (class/spell/status sprite asset). Reinforces the effect-cadence rule: every
   event announces. (US-133.)

**Why.** The PoC bar is "a full loop, polished enough to show"; the owner enumerated the genre
staples the demo must visibly have. Supporters remain passive-bonus-only for the PoC (active
support abilities are a future story).

**Code (planned).** New `StoryCrawl` scene + builder + data (US-131); new `Summon` scene +
builder/manager + `VendorNavBar` entry (US-132); `AnnouncementWindow` feed mode + TMP sprite
asset + combat-event call sites (US-133).

## GG-A6 — Ability gating split (cooldowns / MP / stock) + time-banked orbs + progressive bar slots

**What changed** (owner direction 2026-08-15, late session):

1. **Three-way ability gating.** Skills are **innate and cooldown-gated** (new
   `Ability.CooldownSeconds`, free to use); **Spells cost MP** from the shared orb bank
   (unchanged, GG-LAW-6); **Items consume stock** from the shared inventory stack
   (unchanged). Previously skills and spells both gated on mana. (US-141.)
2. **Time-banking returns — as an orb mint (hybrid ruling).** The Phase-B orb-harvest economy
   STAYS (pincer mints, interrupt mints, colors, wilds, 12-cap). ADDED: when the hero window
   ends (an enemy icon reaches the trigger), the **unspent remainder of the window converts to
   orbs** (rate designer-tuned, e.g. 1 orb per N seconds remaining) — deliberately ending your
   window early is now a resource play. This revives the *fantasy* of the retired Bank button
   without unwinding the orb economy. (US-142.)
3. **Progressive ability-bar slots.** The 5-slot bar starts partially locked and unlocks with
   campaign progress (max 5 — one clear button each). (US-143.)
4. **Touch tooltips everywhere.** Dynamically generated press-hold tooltips for ability buttons
   (name, description, cost/cooldown) and tap-info for heroes/enemies (stats/statuses), built on
   `TooltipFactory` + `ActorPanel`. (US-144.)

**Why.** Owner's combat-UX spec: each of the ≤5 buttons must be legible at a glance, resources
must be readable (cooldown wheel vs orb cost vs stack count), and the timeline's tempo decisions
should extend into the resource game.

**Code (planned).** `Ability` (+CooldownSeconds, +UsesThisBattle stays), `AbilityButtonManager`
(cooldown wheel/dim states), `TurnManager`/`TimelineBarInstance` (window-remainder measure at
trigger), `ManaPoolManager` (mint path), `HeroEquipmentSave`/progression flag (slot unlocks),
`TooltipFactory` call sites.
