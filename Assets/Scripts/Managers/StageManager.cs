using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Models;
using Scripts.Managers;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Managers;
using Scripts.Sequences;
using Scripts.Libraries;
using Scripts.Factories;
using System.Collections.Generic;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// STAGEMANAGER - Controls stage loading, wave spawning, and victory conditions.
/// 
/// PURPOSE:
/// Manages the lifecycle of a battle stage - loading definitions, spawning
/// heroes and enemies, tracking waves, and detecting victory/defeat.
/// 
/// STAGE STRUCTURE:
/// - Stage: Contains metadata and list of Waves
/// - Wave: Contains list of enemies to spawn on specific turn
/// - Victory: All waves cleared and all enemies defeated
/// - Defeat: All heroes killed
/// 
/// INITIALIZATION FLOW:
/// 1. Initialize() - Load stage from profile, setup heroes
/// 2. RestartStage() - Reset state, spawn first wave
/// 3. OnTurnAdvanced() - Check for wave spawns each turn
/// 4. CheckVictory() - Test if stage complete
/// 
/// HERO SPAWNING:
/// Heroes are loaded from the player's saved party (ProfileHelper.CurrentProfile).
/// Positions come from party member data or default spawn locations.
/// 
/// ENEMY SPAWNING:
/// Enemies spawn according to wave definitions. Each wave has a spawnTurn
/// that triggers when TurnManager.CurrentTurn reaches that value.
/// 
/// ENDLESS MODE:
/// When GameModeHelper.IsEndless is true, waves generate procedurally
/// with increasing difficulty instead of using stage definitions.
/// 
/// LLM CONTEXT:
/// Access via g.StageManager. Called by GameManager during scene setup.
/// Works closely with TurnManager (turn advancement) and ActorManager (spawning).
/// </summary>
public class StageManager : MonoBehaviour
{
    #region Properties

    /// <summary>Count of living enemies on the board.</summary>
    public int enemyCount => g.Actors.All.FindAll(x => x.IsEnemy).Count;

    #endregion

    #region Fields

    /// <summary>Currently loaded stage definition.</summary>
    public Stage currentStage;

    /// <summary>Current wave index (0-based).</summary>
    private int currentWave = 0;

    /// <summary>True if playing endless mode (procedural waves).</summary>
    private bool IsEndless => GameModeHelper.IsEndless;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the stage from the player's profile.
    /// Loads stage definition and spawns initial actors.
    /// </summary>
    public void Initialize()
    {
        // CurrentSave is the live save the rest of this class reads (RestartStage included);
        // reading LatestSave here diverged when the player loaded an older save file.
        var currentSave = ProfileHelper.CurrentProfile.CurrentSave;
        if (currentSave == null)
        {
            Debug.LogError("No saved game state found.");
            return;
        }

        // Begin a new XP session with current party participants
        var participants = currentSave.Party.Members.Select(m => m.CharacterClass);
        ExperienceTracker.StartSession(participants);
        LootTracker.StartSession();
        GoldTracker.StartSession();

        if (IsEndless)
        {
            InitializeEndless();
            return;
        }

        Debug.Log($"[StageManager] Initialize: stage='{currentSave.Stage.CurrentStage}' wave={currentSave.Stage.CurrentWave}");
        currentStage = StageLibrary.Get(currentSave.Stage.CurrentStage);
        RestartStage();
        Debug.Log("[StageManager] Initialize complete");
    }

    #endregion

    #region Endless Mode

    /// <summary>
    /// Initializes endless mode with procedural wave generation.
    /// </summary>
    private void InitializeEndless()
    {
        // Reset
        currentWave = 0;
        g.ActorManager.Clear();
        g.DottedLineManager.Clear();
        g.SynergyLineManager.Clear();
        g.CoinCounter.Refresh();
        g.TileManager.Reset();

        // Build a nominal stage placeholder
        currentStage = new Stage
        {
            Name = "Endless",
            Description = "Endless",
            CompletionCondition = "Endless",
            CompletionValue = 0,
            Waves = new System.Collections.Generic.List<StageWave>()
        };

        // Spawn party heroes from the save directly (no PartyManager / no level overrides)
        foreach (var partyMember in ProfileHelper.CurrentProfile.CurrentSave.Party.Members)
        {
            var hero = ActorLibrary.Get(partyMember.CharacterClass);
            if (hero == null) { Debug.LogWarning($"Skipping party member with invalid class: {partyMember.CharacterClass}"); continue; }
            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, partyMember.TotalXP));
            int level = Mathf.Max(1, derived.level);
            var stageActor = new StageActor(partyMember.CharacterClass, Team.Hero, level, location: RNG.UnoccupiedLocation);
            SpawnActor(stageActor, rebuildTimeline: false);
        }

        // Generate and load wave 1 (this handles timeline rebuild)
        LoadEndlessWave(0);

        scene.FadeIn();
    }

    /// <summary>Load endless wave.</summary>
    private void LoadEndlessWave(int waveIndex)
    {
        int nextWaveNumber = waveIndex + 1;
        var wave = Scripts.Managers.EndlessWaveGenerator.Generate(nextWaveNumber, GameModeHelper.Tags);

        // Track current index
        currentWave = waveIndex;

        // Pre-spawn actors; those with SpawnTurn > current will stay inactive until turn threshold
        foreach (var stageActor in wave.Actors)
        {
            SpawnActor(stageActor, rebuildTimeline: false);
        }

        // Announcement (total unknown/infinite)
        g.WaveAnnouncement?.ShowEndless(nextWaveNumber);

        // Clear old tags and rebuild timeline for new wave enemies
        g.TimelineBar?.RebuildForNewWave();
    }

    /// <summary>
    /// Loads the selected stage and initializes the first wave.
    /// </summary>
    public void RestartStage()
    {
        Debug.Log("[StageManager] RestartStage: resetting managers");
        // Reset everything for a new stage.
        currentWave = ProfileHelper.CurrentProfile.CurrentSave.Stage.CurrentWave;
        g.ActorManager.Clear();
        g.DottedLineManager.Clear();
        g.SynergyLineManager.Clear();
        g.CoinCounter.Refresh();
        g.TileManager.Reset();
        // Per-battle static state must reset with the actors — ghost buffs, stale skill
        // cooldowns, and threat scores otherwise survive into the restarted run.
        // TurnManager.Initialize() itself is intentionally NOT called here: on first load
        // GameManager.Start() runs it right after this, and calling BeginHeroWindow twice
        // double-ticks cooldowns and queues two HeroStartSequences.
        BuffSystem.Clear();
        SkillCooldownManager.Clear();
        ThreatTracker.Clear();
        TrapManager.Clear();
        SnakeBossManager.Clear();

        // Show persistent hero actors from ProfileHelper
        Debug.Log("[StageManager] RestartStage: spawning heroes");
        foreach (var partyMember in ProfileHelper.CurrentProfile.CurrentSave.Party.Members)
        {
            // Derive level from TotalXP in save
            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, partyMember.TotalXP));
            int level = Mathf.Max(1, derived.level);
            var stageActor = new StageActor(partyMember.CharacterClass, Team.Hero, level, location: RNG.UnoccupiedLocation);
            // Defer timeline rebuild during bulk spawns
            SpawnActor(stageActor, rebuildTimeline: false);
        }

        Debug.Log("[StageManager] RestartStage: loading wave");
        // Load the wave based on currentWave.
        if (currentStage.Waves != null && currentStage.Waves.Count > 0)
        {
            LoadWave(currentWave);
        }
        else
        {
            Debug.LogError($"Stage {currentStage.Name} has no waves defined.");
        }

        scene.FadeIn();
    }

    /// <summary>
    /// Loads the given wave index.
    /// </summary>
    private void LoadWave(int waveIndex)
    {
        if (currentStage == null || currentStage.Waves == null)
        {
            Debug.LogError("LoadWave: currentStage or Waves is null.");
            return;
        }

        if (waveIndex >= currentStage.Waves.Count)
        {
            Debug.LogError($"Wave index {waveIndex} is out of bounds for stage {currentStage.Name}.");
            return;
        }

        StageWave wave = currentStage.Waves[waveIndex];

        // Show actors for this wave
        var actors = wave?.Actors;
        if (actors != null)
        {
            foreach (var stageActor in actors)
            {
                // Defer timeline rebuild until all spawns are finished
                var spawned = SpawnActor(stageActor, rebuildTimeline: false);

                // US-140 canon rule: every spawned Naga00 is a snake-boss chain head — grow its
                // body segments behind it (segments are real enemies but never act on their own).
                if (spawned != null && spawned.characterClass == CharacterClass.Naga00
                    && !SnakeBossManager.IsChainMember(spawned))
                {
                    SnakeBossManager.CreateChain(spawned, SnakeBossManager.DefaultSegmentCount,
                        loc => SpawnActor(new StageActor(CharacterClass.Naga00, Team.Enemy,
                            Mathf.Max(1, spawned.Stats.Level), location: loc), rebuildTimeline: false));
                }
            }
        }

        // Show dotted supportLines' for this wave
        var dottedLines = wave?.DottedLines;
        if (dottedLines != null)
        {
            foreach (var stageDottedLine in dottedLines)
            {
                var segment = stageDottedLine.Segment;
                var location = stageDottedLine.Location;
                g.DottedLineManager.Spawn(segment, location);
            }
        }

        // Clear old tags and rebuild timeline for new wave enemies
        g.TimelineBar?.RebuildForNewWave();

        g.WaveAnnouncement?.Show(waveIndex + 1, currentStage.Waves.Count);
    }


    /// <summary>
    /// Spawns a new actor on a guaranteed free tile.
    /// Always assigns a fresh unoccupied location to the StageActor.
    /// </summary>
    public ActorInstance SpawnActor(StageActor stageActor, bool rebuildTimeline = true)
    {
        if (stageActor == null || stageActor.CharacterClass == CharacterClass.None)
        {
            Debug.LogWarning("SpawnActor called with null or None CharacterClass. Skipping spawn.");
            return null;
        }

        var data = ActorLibrary.Get(stageActor.CharacterClass);
        if (data == null)
        {
            Debug.LogWarning($"Actor data not found for CharacterClass {stageActor.CharacterClass}. Skipping spawn.");
            return null;
        }

        // Build actor via factory (replaces ActorPrefab.prefab)
        var go = ActorFactory.Create(g.Board.transform);
        go.transform.localPosition = Vector3.zero;
        var instance = go.GetComponent<ActorInstance>();
        instance.name = $"{stageActor.CharacterClass}_{Guid.NewGuid():N}";
        instance.characterClass = stageActor.CharacterClass;
        instance.team = stageActor.Team;
        // Multi-tile footprint (2×2 bosses). Heroes/normal enemies stay 1×1. Must be set BEFORE
        // placement so the spawn rectangle + occupancy are footprint-aware.
        instance.Footprint = data.Footprint == Vector2Int.zero ? Vector2Int.one : data.Footprint;

        // Stats and metadata.
        // US-135: campaign difficulty curve — enemies floor to the stage's recommended level
        // (stage 1 → lvl 1 … stage 15 → lvl 15). Authored per-actor levels still win when
        // higher; Test-*/Endless stages return 1 from RecommendedLevel and are unaffected.
        int spawnLevel = stageActor.Level;
        if (stageActor.Team == Team.Enemy && currentStage != null)
            spawnLevel = Mathf.Max(spawnLevel, CampaignStages.RecommendedLevel(currentStage.Name));
        instance.Stats = data.GetStats(spawnLevel);

        // Seed hero progress from save: derive Level/CurrentXP from TotalXP
        if (stageActor.Team == Team.Hero)
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party != null)
            {
                var entry = party.FirstOrDefault(m => m != null && m.CharacterClass == stageActor.CharacterClass);
                if (entry != null)
                {
                    var (lvl, cur) = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, entry.TotalXP));
                    // Ensure consistency
                    instance.Stats.Level = Mathf.Max(1, lvl);
                    instance.Stats.CurrentXP = Mathf.Max(0, cur);
                    instance.Stats.TotalXP = Mathf.Max(0, entry.TotalXP);
                }
            }

            // Apply equipped-gear stat bonuses so upgraded weapons & armor actually matter in battle.
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Equipment?.Heroes != null)
            {
                var heroSave = save.Equipment.Heroes.FirstOrDefault(h => h != null && h.CharacterClass == stageActor.CharacterClass);
                if (heroSave != null)
                {
                    var loadout = new HeroLoadout { CharacterClass = stageActor.CharacterClass };
                    loadout.LoadFromSave(heroSave);
                    Formulas.ApplyEquipmentBonus(instance.Stats, loadout);
                }
            }

            // US-053: hydrate carried-over wounds AFTER equipment finalizes MaxHP. HpCurrent > 0
            // means the hero ended a prior battle wounded; 0 = spawn at full (fresh / healed / post-defeat).
            if (party != null)
            {
                var hpEntry = party.FirstOrDefault(m => m != null && m.CharacterClass == stageActor.CharacterClass);
                if (hpEntry != null && hpEntry.HpCurrent > 0f)
                {
                    instance.Stats.HP = Mathf.Clamp(hpEntry.HpCurrent, 1f, instance.Stats.MaxHP);
                    instance.Stats.PreviousHP = instance.Stats.HP;
                }
            }
        }

        // US-054: record an enemy class as "seen" the moment it appears on the board.
        if (stageActor.Team == Team.Enemy)
            ProfileHelper.CurrentProfile?.CurrentSave?.Bestiary?.MarkSeen(stageActor.CharacterClass);

        // Base body scale = one tile; a multi-tile actor grows to span its footprint (the centered
        // body + collider follow in ActorInstance.Spawn via CenterPosition).
        instance.transform.localScale = instance.IsMultiTile
            ? Vector3.Scale(GameManager.instance.tileScale, new Vector3(instance.Footprint.x, instance.Footprint.y, 1f))
            : GameManager.instance.tileScale;
        instance.spawnTurn = stageActor.SpawnTurn;

        // Pick and assign location, then spawn.
        // 1×1 path is UNCHANGED (always re-roll a free tile at spawn — avoids the construct-time
        // collisions the original guarded against). Multi-tile actors honor an explicit, fitting
        // StageActor.Location (designer-placed boss), else find a whole free footprint rectangle.
        Vector2Int location;
        if (instance.IsMultiTile)
        {
            if (stageActor.Location.HasValue
                && stageActor.Location.Value != LocationHelper.Nowhere
                && RNG.FootprintFitsFree(stageActor.Location.Value, instance.Footprint))
                location = stageActor.Location.Value;
            else
                location = RNG.UnoccupiedFootprintAnchor(instance.Footprint);
        }
        else
        {
            // US-140: honor an explicit, FREE 1×1 location (snake-boss segments chain behind
            // their head); everything else keeps the safe re-roll.
            if (stageActor.Location.HasValue
                && stageActor.Location.Value != LocationHelper.Nowhere
                && g.TileMap != null
                && g.TileMap.ContainsLocation(stageActor.Location.Value)
                && !g.Actors.IsTileOccupied(stageActor.Location.Value))
                location = stageActor.Location.Value;
            else
                location = RNG.UnoccupiedLocation;
        }

        // This ensures that the game's stage data "knows" where this actor is starting.
        // Used for saving, AI planning, and any systems that read StageActor info.
        stageActor.Location = location;

        // This physically places the GameObject in the scene and updates the tile's occupancy.
        instance.Spawn(location);

        // Register the new actor
        g.Actors.All.Add(instance);

        // If requested, seed timeline tags immediately (useful for single off-cycle spawns)
        if (rebuildTimeline)
        {
            g.TimelineBar?.EnsureTagsForAllEnemies(false);
        }

        return instance;
    }

    /// <summary>
    /// Called once per turn advance (from TurnManager) to activate any actors whose spawnTurn has arrived.
    /// </summary>
    public void OnTurnAdvanced()
    {
        foreach (var a in g.Actors.All)
        {
            if (a == null) continue;
            a.ActivateIfSpawnable();
        }
    }

    /// <summary>
    /// When an actor dies we may need to: pull-forward pending spawns to avoid empty enemy turns,
    /// advance waves, or trigger win/lose states.
    /// </summary>
    public void OnActorDeath()
    {
        // Immediately refresh timeline so dead enemies' tags are removed
        g.TimelineBar?.EnsureTagsForAllEnemies(false);

        // 1) If there are no enemies currently playing but there are pending (not yet spawned)
        //    enemies scheduled for future turns, pull the next batch forward to the current turn
        //    so the board is never empty of enemies.
        var enemiesPlaying = g.Actors.Enemies.Any(e => e != null && e.IsPlaying);
        var pending = g.Actors.Enemies.Where(e => e != null && !e.Flags.HasSpawned).ToList();
        if (!enemiesPlaying && pending.Count > 0)
        {
            int currentTurn = g.TurnManager.CurrentTurn;
            int nextSpawnTurn = pending.Min(e => e.spawnTurn);
            var nextBatch = pending.Where(e => e.spawnTurn == nextSpawnTurn).ToList();
            foreach (var e in nextBatch)
                e.spawnTurn = currentTurn;

            // Activate immediately and refresh timeline
            OnTurnAdvanced();
            g.TimelineBar?.EnsureTagsForAllEnemies(false);
            return; // do not advance wave/stage; we just filled the gap
        }

        // 2) Otherwise continue with standard flow
        CheckBattleLost();
        if (IsEndless) CheckEndlessWaveComplete(); else CheckWaveComplete();
    }

    /// <summary>
    /// Endless flow: when all enemies are dead, generate and load the next wave.
    /// </summary>
    private void CheckEndlessWaveComplete()
    {
        bool allEnemiesDead = g.Actors.Enemies.All(x => x.Flags.HasSpawned && x.IsDead);
        if (!allEnemiesDead)
            return;

        currentWave++;
        LoadEndlessWave(currentWave);
    }

    /// <summary>
    /// Checks if the current wave is complete and moves to the next wave or completes the stage.
    /// </summary>
    private void CheckWaveComplete()
    {
        bool allEnemiesDead = g.Actors.Enemies.All(x => x.Flags.HasSpawned && x.IsDead);
        if (!allEnemiesDead)
            return;

        currentWave++;

        if (currentWave < currentStage.Waves.Count)
        {
            Debug.Log($"All enemies defeated. Loading next wave: {currentWave + 1}");
            LoadWave(currentWave);
        }
        else
        {
            CheckBattleWon();
        }
    }

    /// <summary>
    /// Handles what happens when all waves of a stage are completed.
    /// Updates campaign progression so the next stage unlocks in StageSelect.
    /// </summary>
    private void CheckBattleWon()
    {
        if (currentWave < currentStage.Waves.Count)
            return;

        bool allEnemiesDead = g.Actors.Enemies.All(x => x.Flags.HasSpawned && x.IsDead);
        if (!allEnemiesDead)
            return;

        // Mark this stage as cleared in the save so StageSelect unlocks the next one.
        // No-op when the stage isn't in the campaign (Test stages, etc.).
        Scripts.Libraries.CampaignStages.MarkCleared(currentStage.Name);

        g.SequenceManager.Add(new BattleWonSequence());
        g.SequenceManager.Execute();
    }

    /// <summary>
    /// Checks whether the game is over.
    /// </summary>
    private void CheckBattleLost()
    {
        bool allHeroesDead = g.Actors.Heroes.All(x => x.Flags.HasSpawned && x.IsDead);
        if (!allHeroesDead)
            return;

        g.SequenceManager.Add(new BattleLostSequence());
        g.SequenceManager.Execute();
    }

    /// <summary>
    /// Convenience method for adding a new attacker actor.
    /// </summary>
    /// <param name="characterClass">characterName type for the attacker.</param>
    public ActorInstance AddEnemy(CharacterClass characterClass)
    {
        var stageActor = new StageActor(characterClass, Team.Enemy, level: 1, location: RNG.UnoccupiedLocation);
        return SpawnActor(stageActor);
    }

    #endregion

}

}
