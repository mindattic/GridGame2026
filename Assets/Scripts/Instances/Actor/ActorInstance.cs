using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Instances.Actor;
using Scripts.Instances.Actor;
using Scripts.Libraries;
using Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static Scripts.Helpers.GameObjectHelper;
using g = Scripts.Helpers.GameHelper;
using s = Scripts.Helpers.SettingsHelper;
using Scripts.Managers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances.Actor
{
/// <summary>
/// ACTORINSTANCE - Runtime character on the battlefield (hero or enemy).
/// 
/// PURPOSE:
/// Represents a single character during combat. Both player-controlled heroes
/// and AI-controlled enemies use this same class, differentiated by the team field.
/// 
/// TEAM IDENTIFICATION:
/// - team == Team.Hero: Player-controlled character
/// - team == Team.Enemy: AI-controlled opponent
/// - Use IsHero/IsEnemy properties for checks
/// 
/// GRID POSITION:
/// - location: Vector2Int(column, row) on the board
/// - previousLocation: Location before last move (for undo/animation)
/// - currentTile: TileInstance at current location
/// 
/// STATE PROPERTIES:
/// - IsAlive: HP > 0
/// - IsPlaying: Active and alive (can participate in combat)
/// - IsDying: Active but HP <= 0 (death animation pending)
/// - IsDead: Inactive and dead
/// - IsSpawnable: Ready to spawn (turn reached, not yet spawned)
/// 
/// COMPONENT MODULES (composition pattern):
/// - Stats: HP, AP, Strength, Vitality, etc. (ActorStats)
/// - Flags: HasSpawned, HasActed, etc. (ActorFlags)
/// - Render: SpriteRenderer references (ActorRenderers)
/// - Animation: Animation state machine (ActorAnimation)
/// - Move: Position lerping (ActorMovement)
/// - HealthText: HP readout text (ActorHealthText)
/// - Weapon: Weapon sprite/type (ActorWeapon)
/// - Vfx: Particle effects (ActorVFX)
/// - Thumbnail: Portrait display (ActorThumbnail)
/// - Abilities: List of special moves
/// 
/// SORTING/LAYERING:
/// Uses SortingGroup for z-ordering during combat (pincer attacks, focus, etc.).
/// Subscribes to SortingManager.OnSortRequested for global sort events.
/// 
/// LLM CONTEXT:
/// This is the core character class. Access actors via g.Actors.All,
/// g.Actors.Heroes, or g.Actors.Enemies. Filter with .IsPlaying for
/// combat-active characters. Location is always a Vector2Int on the grid.
/// </summary>
public partial class ActorInstance : MonoBehaviour
{
    /// <summary>Tracks the last attacker that reduced this actor to 0 HP (for kill credit).</summary>
    private ActorInstance _lastAttacker;

    #region Instance Properties

    public TileInstance currentTile => g.TileMap.GetTile(location);
    public bool IsHero => team.Equals(Team.Hero);
    public bool IsEnemy => team.Equals(Team.Enemy);
    public bool IsActive => isActiveAndEnabled;
    public bool IsAlive => Stats.HP > 0;
    public bool IsPlaying => IsActive && IsAlive;
    public bool IsDying => IsActive && Stats.HP < 1;
    public bool IsDead => !IsActive && !IsAlive;
    public bool IsSpawnable => !Flags.HasSpawned && spawnTurn <= g.TurnManager.CurrentTurn;
    public bool HasMaxAP => Stats.AP == Stats.MaxAP;
    public bool IsInvincible => (IsEnemy && g.DebugManager.isEnemyInvincible) || (IsHero && g.DebugManager.isHeroInvincible);

    public Transform Parent
    {
        get => transform.parent;
        set => transform.SetParent(value, true);
    }

    public Vector3 Position
    {
        get => transform.position;
        set => transform.position = value;
    }

    public Vector3 ThumbnailPosition
    {
        get => transform.GetChild("Thumbnail").position;
        set => transform.GetChild("Thumbnail").position = value;
    }

    public Quaternion Rotation
    {
        get => transform.rotation;
        set => transform.rotation = value;
    }

    public Vector3 Scale
    {
        get => transform.localScale;
        set => transform.localScale = value;
    }

    public SortingGroup SortingGroup => GetComponent<SortingGroup>();

    #endregion


    #region Sorting

    /// <summary>
    /// Apply sorting layer and explicit order.
    /// </summary>
    public void SetSorting(string sortingLayer, int sortingOrder = 0)
    {
        SortingGroup.sortingLayerID = SortingLayer.NameToID(sortingLayer);
        SortingGroup.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// Subscribe to global sort events.
    /// </summary>
    private void OnEnable()
    {
        SortingManager.OnSortRequested += HandleSortEvent;
    }

    /// <summary>
    /// Unsubscribe to avoid leaks.
    /// </summary>
    private void OnDisable()
    {
        SortingManager.OnSortRequested -= HandleSortEvent;
    }

    /// <summary>
    /// Handle sort intents for focus, drag, overlap, etc.
    /// </summary>
    private void HandleSortEvent(SortEvent e)
    {
        switch (e.Type)
        {
            case SortEventType.Focus:
                if (this == e.Initiator) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Max);
                else SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                break;

            case SortEventType.Drag:
                if (this == e.Initiator) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Max);
                else SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                break;

            case SortEventType.LocationChanged:
                if (this == e.Initiator) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Max);
                else SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                break;

            case SortEventType.Drop:
                SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                break;

            case SortEventType.ActorMoving:
                if (this == e.Initiator) SetSorting(SortingHelper.Layer.ActorAbove, 0);
                else SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                break;

            case SortEventType.Overlap:
                if (this == e.Initiator) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Max);
                else if (this == e.Target) SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                break;

            case SortEventType.PincerAttack:
                {
                    bool isAttacker = e.Participants.pair.Any(p => p.attacker1 == this || p.attacker2 == this);
                    bool isOpponent = e.Participants.pair.SelectMany(p => p.opponents).Contains(this);
                    bool isSupporter = e.Participants.pair.SelectMany(p => p.supporters1.Concat(p.supporters2)).Contains(this);

                    if (isAttacker) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Attacker);
                    else if (isOpponent) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Opponent);
                    else if (isSupporter) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Supporter);
                    else SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                    break;
                }

            case SortEventType.Bump:
                if (this == e.Initiator) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Max);
                else if (this == e.Target) SetSorting(SortingHelper.Layer.ActorAbove, SortingHelper.Order.Min);
                break;

            default:
                SetSorting(SortingHelper.Layer.ActorBelow, SortingHelper.Order.Min);
                break;
        }
    }

    #endregion

    #region Core fields and modules

    // Keep your existing module objects and data.

    public Vector2Int previousLocation;
    public Vector3 previousPosition;
    public Vector2Int location;
    public Team team = Team.Neutral;
    public int spawnTurn = 0;
    public CharacterClass characterClass;

    public ActorRenderers Render = new ActorRenderers();
    public ActorStats Stats = new ActorStats();
    public ActorFlags Flags = new ActorFlags();
    public Scripts.Models.StatusList Statuses = new Scripts.Models.StatusList();
    public ActorVFX Vfx = new ActorVFX();
    public ActorWeapon Weapon = new ActorWeapon();
    public ActorAnimation Animation = new ActorAnimation();
    public ActorMovement Move = new ActorMovement();
    public ActorHealthText HealthText = new ActorHealthText();
    public ActorThumbnail Thumbnail;
    public List<Ability> Abilities = new List<Ability>();

    #endregion

    #region Spatial helpers

    /// <summary>
    /// Get direction from this actor to another. Optionally enforce adjacency.
    /// </summary>
    public Direction GetDirectionTo(ActorInstance other, bool mustBeAdjacent = true)
    {
        if (other == null)
        {
            Debug.LogError($"GetDirectionTo called with null 'other' by {name}");
            return Direction.None;
        }

        if (mustBeAdjacent && !Geometry.IsAdjacentTo(this, other))
            return Direction.None;

        var dx = location.x - other.location.x;
        var dy = location.y - other.location.y;

        if (dx == 0 && dy > 0) return Direction.North;
        if (dx == 0 && dy < 0) return Direction.South;
        if (dx > 0 && dy == 0) return Direction.West;
        if (dx < 0 && dy == 0) return Direction.East;

        if (dx > 0 && dy > 0) return Direction.NorthWest;
        if (dx < 0 && dy > 0) return Direction.NorthEast;
        if (dx > 0 && dy < 0) return Direction.SouthWest;
        if (dx < 0 && dy < 0) return Direction.SouthEast;

        return Direction.None;
    }

    /// <summary>
    /// True if any active actor is within the given range in a cardinal direction.
    /// </summary>
    public bool HasAdjacent(Direction direction, int range)
    {
        for (int i = 1; i <= range; i++)
        {
            Vector2Int check = location;
            switch (direction)
            {
                case Direction.North: check += new Vector2Int(0, -i); break;
                case Direction.South: check += new Vector2Int(0, i); break;
                case Direction.East: check += new Vector2Int(i, 0); break;
                case Direction.West: check += new Vector2Int(-i, 0); break;
            }

            if (g.Actors.All.Any(a => a.IsPlaying && a.location == check))
                return true;
        }
        return false;
    }

    /// <summary>
    /// True if any active actor is within the given range in a diagonal direction.
    /// </summary>
    public bool HasDiagonal(Direction direction, int range)
    {
        for (int i = 1; i <= range; i++)
        {
            Vector2Int check = location;
            switch (direction)
            {
                case Direction.NorthEast: check += new Vector2Int(i, -i); break;
                case Direction.NorthWest: check += new Vector2Int(-i, -i); break;
                case Direction.SouthEast: check += new Vector2Int(i, i); break;
                case Direction.SouthWest: check += new Vector2Int(-i, i); break;
            }

            if (g.Actors.All.Any(a => a.IsPlaying && a.location == check))
                return true;
        }
        return false;
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initialize render and behavior modules.
    /// </summary>
    private void Awake()
    {
        Render.Initialize(this);
        Animation.Initialize(this);
        Move.Initialize(this);
        HealthText.Initialize(this);

        Thumbnail = transform.Find(GameObjectHelper.Actor.Front.Thumbnail)
                             .GetComponent<ActorThumbnail>();
    }

    /// <summary>Cleans up resources when the object is destroyed.</summary>
    private void OnDestroy()
    {
    }

    /// <summary>
    /// Spawn this actor at a grid location with visuals, stats, and FX seeded.
    /// No TurnDelay is assigned here. Timeline will manage and display countdown numbers.
    /// </summary>
    public void Spawn(Vector2Int startLocation)
    {
        location = startLocation;
        previousLocation = location;

        Position = Geometry.GetPositionByLocation(location);
        previousPosition = Position;

        Thumbnail.Initialize(this);

        Weapon.Type = RNG.WeaponType();
        Weapon.Attack = RNG.Float(10, 15);
        Weapon.Defense = RNG.Float(0, 5);
        Weapon.Name = $"{Weapon.Type}";

        if (IsHero)
        {
            Render.SetBackdropColor(ColorHelper.Solid.White);
            Render.SetFrameColor(ColorHelper.Solid.White);
            Vfx.Attack = VisualEffectLibrary.VisualEffects["BlueSlash1"];
        }
        else if (IsEnemy)
        {
            Render.SetBackdropColor(ColorHelper.Solid.GunMetal);
            Render.SetFrameColor(ColorHelper.Solid.White);
            Vfx.Attack = VisualEffectLibrary.VisualEffects["DoubleClaw"];

            // Enemy frame flickers between black and dark red using Quake's broken-fluorescent
            // pattern — same effect ActorPanel uses for the enemy backdrop, applied here to the
            // per-actor frame overlay so enemies read as menacing at a glance.
            StartCoroutine(EnemyBackdropFlickerRoutine());
        }

        Render.SetNameTagText(characterClass.ToString());
        Render.SetNameTagEnabled(isEnabled: g.DebugManager.showActorNameTag);

        HealthText.Refresh();

        if (IsSpawnable)
        {
            gameObject.SetActive(true);
            Flags.HasSpawned = true;
            Animation.FadeIn();
            Animation.Spin360();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Activate this actor if its spawn turn has arrived.
    /// </summary>
    public void ActivateIfSpawnable()
    {
        if (!Flags.HasSpawned && spawnTurn <= g.TurnManager.CurrentTurn)
        {
            gameObject.SetActive(true);
            Flags.HasSpawned = true;
            Animation.FadeIn();
            Animation.Spin360();
        }
    }

    /// <summary>
    /// Lerps the enemy's Backdrop overlay between black and dark red using Quake's
    /// FluorescentFlicker pattern (id Software, 1996). Stops automatically when
    /// the actor leaves play (IsPlaying false) — caller does not need to cancel.
    /// </summary>
    private IEnumerator EnemyBackdropFlickerRoutine()
    {
        // Flicker disabled per design — enemy backdrop is a solid red marker for now.
        // (Placeholder treatment; revisit once a final enemy-highlight look is decided.)
        var solidRed = new Color(0.8f, 0f, 0f, 0.85f);
        while (IsPlaying)
        {
            Render.SetBackdropColor(solidRed);
            yield return null;
        }
    }

    #endregion

    #region Combat API

    /// <summary>
    /// Placeholder damage at the end of ShieldRush bump.
    /// </summary>
    public IEnumerator ShieldRushDamageRoutine(ActorInstance target)
    {
        if (target != null && target.IsPlaying)
        {
            g.CombatTextManager.Spawn("ShieldRush", target.Position, "Damage");
        }
        yield return null;
    }

    /// <summary>
    /// Decides this enemy's next move tile and records it in <see cref="location"/> (the body then
    /// animates there). The tactical decision — which hero to pressure, and stepping toward it
    /// without walking into a pincer — lives in the pure <see cref="Scripts.Services.EnemyPlanner"/>.
    /// </summary>
    public void CalculateAttackStrategy()
    {
        location = Scripts.Services.EnemyPlanner.PlanStep(this, g.Actors.All, g.TileMap);
    }

    /// <summary>Fire damage.</summary>
    public void FireDamage(float amount)
    {
        if (!IsPlaying) return;              // avoid starting coroutine on inactive/dead objects
        StartCoroutine(FireDamageRoutine(amount));
    }

    /// <summary>
    /// Apply fire damage feedback and text.
    /// </summary>
    public IEnumerator FireDamageRoutine(float amount)
    {
        g.CombatTextManager.Spawn($"Fireball: - {amount} HP", Position);
        yield return Wait.None();
    }

    /// <summary>Heal.</summary>
    public void Heal(int amount)
    {
        if (!IsPlaying) return;              // avoid starting coroutine on inactive/dead objects
        StartCoroutine(HealRoutine(amount));
    }

    /// <summary>
    /// Apply healing and show feedback.
    /// </summary>
    public IEnumerator HealRoutine(int amount)
    {
        if (!IsInvincible)
        {
            Stats.PreviousHP = Stats.HP;
            Stats.HP = Mathf.Clamp(Stats.HP + amount, 0, Stats.MaxHP);
            HealthText.Refresh();
        }

        g.CombatTextManager.Spawn(amount.ToString(), Position, "Heal");
        g.AudioManager.Play("Heal");
        yield break;
    }

    /// <summary>Damage.</summary>
    public void Damage(AttackResult attackResult)
    {
        if (!IsPlaying) return;              // avoid starting coroutine on inactive/dead objects
        StartCoroutine(DamageRoutine(attackResult));
    }

    /// <summary>
    /// Advances this actor's status effects by one turn (DoT damage, Regen healing) and shows
    /// feedback. Pure rules live in StatusList; this just applies the net HP delta through the
    /// same HP/health-bar/combat-text path as normal damage and healing. Death from a DoT is
    /// picked up by the DeathSequence the turn flow already runs.
    /// </summary>
    public IEnumerator TickStatusesRoutine()
    {
        if (Statuses == null || !IsPlaying)
            yield break;

        var result = Statuses.AdvanceOneTurn();
        if (result.HpDelta != 0 && !IsInvincible)
        {
            Stats.PreviousHP = Stats.HP;
            Stats.HP = Mathf.Clamp(Stats.HP + result.HpDelta, 0, Stats.MaxHP);
            HealthText.Refresh();

            string style = result.HpDelta < 0 ? "Damage" : "Heal";
            g.CombatTextManager.Spawn(Mathf.Abs(result.HpDelta).ToString(), Position, style);
            if (result.HpDelta < 0 && Stats.HP <= 0)
                _lastAttacker = null; // DoT kill — no single attacker to credit
        }

        yield return Wait.None();
    }

    /// <summary>
    /// Apply damage and show feedback.
    /// </summary>
    public IEnumerator DamageRoutine(AttackResult attackResult)
    {
        if (!IsInvincible)
        {
            Stats.PreviousHP = Stats.HP;
            Stats.HP = Mathf.Clamp(Stats.HP - attackResult.Damage, 0, Stats.MaxHP);
            HealthText.Refresh();

            if (Stats.HP <= 0 && attackResult != null && attackResult.Attacker != null)
            {
                _lastAttacker = attackResult.Attacker;
            }

            // Rewards for a hero damaging an enemy (not enemy→hero, not ally hits).
            if (this.IsEnemy && attackResult?.Attacker != null && attackResult.Attacker.IsHero)
            {
                // US-080: accrue threat so smart (high-INT) enemies prefer whoever's hurt them.
                if (attackResult.Damage > 0)
                    Scripts.Managers.ThreatTracker.AddThreat(attackResult.Attacker, attackResult.Damage);

                // US-031: a critical hit mints one "wild" (Colorless) orb to the team bank — the
                // off-palette source. Covers pincers (AttackHelper) and magic (MagicAttackSequence),
                // both of which apply damage via this routine. Capped by the bank (Add no-ops if full).
                if (attackResult.HitType == HitOutcome.Critical)
                    g.ManaPoolManager?.Bank?.Add(ManaType.Colorless, 1);

                // US-027: a landing hit on a charging enemy interrupts its cast via the US-024
                // stagger model (cancel mints a charge-color orb). Symmetric to the hero-cast
                // interrupt in EnemyAttackSequence; a no-op if this enemy isn't casting.
                if (attackResult.HitType != HitOutcome.Miss && attackResult.Damage > 0)
                    g.TimelineBar?.InterruptCastsByOwner(this, attackResult.Attacker);
            }
        }

        var style = CombatTextHelper.GetStyle(attackResult);
        g.CombatTextManager.Spawn(attackResult.Damage.ToString(), Position, style);
        g.AudioManager.Play("Click");
        yield break;
    }

    /// <summary>
    /// Show a miss and play a dodge animation.
    /// </summary>
    public IEnumerator AttackMissRoutine()
    {
        g.CombatTextManager.Spawn("Miss!", Position, "Miss");
        yield return Animation.DodgeRoutine();
    }

    /// <summary>
    /// Begin death sequence and notify stage manager when finished.
    /// </summary>
    public void Die()
    {
        StartCoroutine(DieRoutine());
    }

    /// <summary>
    /// Fade out, spawn coins, disable, and notify.
    /// </summary>
    public IEnumerator DieRoutine()
    {
        var alpha = 1f;
        Render.SetAlpha(alpha);

        if (HealthText.IsAnimating)
            yield return new WaitUntil(() => HealthText.IsEmpty);

        // Award XP only when an enemy dies
        if (this.IsEnemy)
        {
            // US-054: record the defeat in the Bestiary (seen + defeated + times-defeated).
            ProfileHelper.CurrentProfile?.CurrentSave?.Bestiary?.MarkDefeated(this.characterClass);

            int baseXp = ExperienceHelper.Calculate(this);
            if (baseXp > 0)
            {
                // Accumulate to ExperienceTracker for PostBattle screen (both modes)
                // `save` is null-conditional above, so the whole chain must be guarded — the old
                // `save.Party...` dereferenced a possibly-null save (no profile loaded), and the
                // trailing `?? new ...` was dead (ToHashSet/ToList never return null).
                var save = ProfileHelper.CurrentProfile?.CurrentSave;
                var party = save?.Party?.Members?.Select(m => m.CharacterClass).ToHashSet() ?? new HashSet<CharacterClass>();
                var roster = save?.Roster?.Members?.Select(m => m.CharacterClass).ToList() ?? new System.Collections.Generic.List<CharacterClass>();

                foreach (var character in roster)
                {
                    if (character == CharacterClass.None) continue;
                    int amount = baseXp;
                    bool inParty = party.Contains(character);

                    if (!inParty)
                    {
                        amount = Mathf.FloorToInt(baseXp * 0.5f); // half for non-party roster
                    }
                    else if (_lastAttacker != null && _lastAttacker.characterClass == character)
                    {
                        amount = Mathf.RoundToInt(baseXp * 1.1f); // +10% killer bonus
                    }

                    Scripts.Managers.ExperienceTracker.AddParticipant(character);
                    Scripts.Managers.ExperienceTracker.AddXP(character, amount);
                }
            }

            // Roll drop table and track loot
            var drops = Scripts.Data.Items.DropTableLibrary.RollDrops(this.characterClass);
            Scripts.Managers.LootTracker.AddDropResults(drops);

            // Humanoid enemies have a chance to drop equipment they were "wearing".
            Scripts.Helpers.EnemyGearDropHelper.RollFor(this);

            // Celebratory on-map pickup burst (rarity-tinted, purely visual — loot already booked above)
            if (drops != null && drops.Count > 0)
                g.ItemPickupManager?.SpawnBurst(Position, drops);

            // Record toward the player's active bounty contract, if any
            Scripts.Helpers.BountyHelper.RecordKill(this.characterClass);
        }

        g.PortraitManager.Dissolve(this);
        g.AudioManager.Play("Death");
        g.CoinManager.SpawnBurst(Position, Mathf.RoundToInt((ExperienceHelper.Calculate(this)) * s.CoinCountMulitiplier));

        location = LocationHelper.Nowhere;
        Position = PositionHelper.Nowhere;
        gameObject.SetActive(false);
        g.StageManager.OnActorDeath();
    }

    /// <summary>
    /// Increase this hero's level by granting enough XP to reach the target number of levels.
    /// Persists by updating TotalXP only. Level is always derived at runtime.
    /// </summary>
    public void LevelUp(int levels = 1)
    {
        if (!IsHero || characterClass == CharacterClass.None || levels == 0)
            return;

        // Compute current derived state from TotalXP
        int totalXP = Mathf.Max(0, Stats.TotalXP);
        var (curLevel, curRemainder) = ExperienceHelper.DeriveFromTotalXP(totalXP);

        int targetLevels = Mathf.Abs(levels);
        int levelsToGain = targetLevels;
        int xpToAdd = 0;

        while (levelsToGain > 0)
        {
            int neededForNext = ExperienceHelper.NextLevel(curLevel) - curRemainder;
            xpToAdd += Mathf.Max(0, neededForNext);
            curLevel += 1;
            curRemainder = 0;
            levelsToGain--;
        }

        if (xpToAdd > 0)
        {
            ExperienceHelper.Gain(this, xpToAdd);
        }
    }

    #endregion

    #region Teleport

    /// <summary>
    /// Teleport to a board location if in bounds. Kicks existing occupant elsewhere.
    /// </summary>
    public void Teleport(Vector2Int newLocation)
    {
        if (!g.Board.InBounds(newLocation))
            return;

        var occupant = g.Actors.All.FirstOrDefault(x => x.IsPlaying && x.location == newLocation);
        if (occupant.Exists())
            occupant.Teleport(RNG.Location);

        location = newLocation;
        transform.position = Geometry.GetPositionByLocation(location);
    }

    /// <summary>
    /// Teleport to the first unoccupied tile that comes after a reference tile in board order.
    /// </summary>
    public void TeleportAfter(Vector2Int after)
    {
        var tiles = g.Tiles.ToList();
        if (tiles.Count == 0)
        {
            Debug.LogWarning("TeleportAfter: no tiles available.");
            return;
        }

        int startIndex = tiles.FindIndex(t => t != null && t.location == after);
        if (startIndex < 0)
        {
            Debug.LogWarning($"TeleportAfter: starting tile {after} not found.");
            return;
        }

        for (int step = 1; step <= tiles.Count; step++)
        {
            int idx = (startIndex + step) % tiles.Count;
            var tile = tiles[idx];

            if (tile != null && !tile.IsOccupied)
            {
                Teleport(tile.location);
                return;
            }
        }

        Debug.LogWarning($"TeleportAfter: no unoccupied tile found after {after}.");
    }

    /// <summary>
    /// Teleport one step toward a direction if within bounds.
    /// </summary>
    public void TeleportToward(Vector2Int direction)
    {
        if (!g.Board.InBounds(location + direction))
            return;

        var newLocation = location + direction;
        var tile = g.TileMap.GetTile(newLocation);
        if (tile == null) return;

        Teleport(tile.location);
    }

    #endregion

    #region AP gating

    /// <summary>
    /// Refresh AP for a new enemy turn.
    /// </summary>
    public void SetReady()
    {
        if (!IsActive || !IsAlive || !IsEnemy)
            return;

        Stats.AP = Stats.MaxAP;
        Stats.PreviousAP = Stats.MaxAP;
    }

    #endregion
}

}
