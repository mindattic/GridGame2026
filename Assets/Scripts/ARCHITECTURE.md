# GridGame2026 Architecture Documentation

> **For LLM-Assisted Development** - This document provides comprehensive context for AI coding assistants.

---

## Quick Reference

```csharp
// Standard import pattern for all game code
using g = Assets.Helpers.GameHelper;

// Access any system via g.*
g.Actors.Heroes           // All hero ActorInstances
g.TurnManager.NextTurn()  // Advance turn
g.SequenceManager.Add()   // Queue sequence
g.TileMap.GetTile(loc)    // Get tile at location
g.PincerAttackManager.Check() // Check for pincers
```

---

## Core Concepts Dictionary

### Actor
**Class**: `ActorInstance` (MonoBehaviour)  
**What**: Any character on the battlefield  
**Teams**: `Team.Hero` (player) or `Team.Enemy` (AI)  
**Location**: `Vector2Int(column, row)` on grid  

```csharp
actor.IsHero      // True if player-controlled
actor.IsAlive     // True if HP > 0
actor.IsPlaying   // True if active and alive
actor.location    // Grid position
actor.Stats.HP    // Current health
```

### Tile
**Class**: `TileInstance` (MonoBehaviour)  
**What**: Single cell on the game board  
**Coordinates**: `Vector2Int(col, row)`, 0-indexed from top-left  

```csharp
tile.IsOccupied   // True if actor here
tile.Occupier     // ActorInstance on tile
tile.location     // Grid position
```

### Board
**Class**: `BoardInstance` (MonoBehaviour)  
**What**: The tactical grid (default 6x8)  
**Origin**: Top-left, Y increases downward  

```csharp
g.Board.columnCount  // 6
g.Board.rowCount     // 8
g.Board.bounds       // World-space rectangle
```

### Pincer Attack
**Manager**: `PincerAttackManager`  
**What**: Core combat mechanic - trap enemies between allies  

```
VALID PINCER:
[Hero] ─ [Enemy] ─ [Enemy] ─ [Hero]
   ↑         trapped          ↑
 attacker1               attacker2
```

**Rules**:
1. Two heroes same row OR column
2. Enemies between (no gaps)
3. Triggers on hero drop

### Sequence
**Base Class**: `SequenceEvent`  
**What**: Async gameplay routine (IEnumerator coroutine)  
**Queue**: `SequenceManager` executes FIFO  

```csharp
g.SequenceManager.Add(new PincerAttackSequence(pair));
g.SequenceManager.Add(new DeathSequence());
g.SequenceManager.Execute();
```

### Timeline
**Class**: `TimelineBarInstance`  
**What**: Visual turn order UI with moving tags  
**Flow**: Tags move left → reach trigger → actor's turn  

---

## Project Structure

```
Assets/Scripts/
├── Managers/           # Singletons (30+)
│   ├── GameManager.cs      # Central hub
│   ├── TurnManager.cs      # Turn flow
│   ├── SequenceManager.cs  # Async queue
│   ├── PincerAttackManager.cs  # Core combat
│   ├── StageManager.cs     # Waves/spawning
│   ├── InputManager.cs     # Touch/drag
│   └── ...
│
├── Instances/          # Runtime GameObjects
│   ├── Actor/
│   │   ├── ActorInstance.cs  # Character
│   │   ├── ActorStats.cs     # HP/AP/stats
│   │   └── ...modules
│   ├── TileInstance.cs       # Grid cell
│   └── Board/
│       └── BoardInstance.cs  # Grid container
│
├── Sequences/          # Async routines
│   ├── SequenceEvent.cs      # Base class
│   ├── PincerAttackSequence.cs
│   ├── EnemyTakeTurnSequence.cs
│   ├── DeathSequence.cs
│   └── EndTurnSequence.cs
│
├── Factories/          # GameObject builders (no prefabs)
│   ├── TileFactory.cs
│   ├── CombatTextFactory.cs
│   └── ...
│
├── Libraries/          # Static asset registries
│   ├── SpriteLibrary.cs
│   ├── FontLibrary.cs
│   └── ...
│
├── Helpers/            # Static utilities
│   ├── GameHelper.cs   # Central access (g.*)
│   ├── Geometry.cs     # Coordinate math
│   └── AttackHelper.cs # Combat utilities
│
└── Canvas/             # UI components
    ├── TimelineBarInstance.cs
    ├── TimelineTag.cs
    └── ...
```

---

## Sequence Flow Examples

### Hero Turn (Pincer Attack)
```
1. Player drags hero
2. Player drops hero on tile
3. InputManager calls PincerAttackManager.Check()
4. If pincer found:
   └→ Queue: PincerAttackSequence
   └→ Queue: DeathSequence
   └→ Queue: WaveCheckSequence
   └→ Queue: EndTurnSequence
5. SequenceManager.Execute()
6. Sequences run in order
7. EndTurnSequence calls TurnManager.NextTurn()
```

### Enemy Turn
```
1. Timeline tag reaches trigger
2. TurnManager.BeginEnemyTurn(enemy)
3. Queue: EnemyTakeTurnSequence
4. EnemyTakeTurnSequence queues:
   └→ EnemyMoveSequence
   └→ EnemyPreAttackSequence
   └→ EnemyAttackSequence
   └→ EnemyPostAttackSequence
   └→ DeathSequence
   └→ EndTurnSequence
5. All execute in order
6. Returns to hero window
```

---

## Manager Quick Reference

| Manager | Purpose | Access |
|---------|---------|--------|
| GameManager | Central singleton | `GameManager.instance` |
| TurnManager | Turn flow | `g.TurnManager` |
| SequenceManager | Async queue | `g.SequenceManager` |
| PincerAttackManager | Combat | `g.PincerAttackManager` |
| StageManager | Waves | `g.StageManager` |
| InputManager | Touch | `g.InputManager` |
| SelectionManager | Selection | `g.SelectionManager` |
| ActorManager | Actors | `g.ActorManager` |
| TimelineBar | UI | `g.TimelineBar` |

---

## Common Patterns

### Factory Pattern
```csharp
// All UI/objects created via factories (no prefabs)
var tile = TileFactory.Create(parent);
var text = CombatTextFactory.Create(parent);
```

### Sequence Pattern
```csharp
// Queue sequences, execute in order
g.SequenceManager.Add(new MySequence());
g.SequenceManager.Execute();
```

### Library Pattern
```csharp
// Assets loaded from static libraries
var sprite = SpriteLibrary.Sprites["Tile"];
var font = FontLibrary.Fonts["Attic"];
```

---

## Coordinate Systems

| System | Type | Origin | Notes |
|--------|------|--------|-------|
| Grid | `Vector2Int` | Top-left (0,0) | col=X, row=Y |
| World | `Vector3` | Unity origin | Y increases up |
| Screen | `Vector2` | Bottom-left | Pixels |

**Conversions** (via `Geometry` class):
```csharp
Geometry.CalculatePositionByLocation(gridLoc)  // Grid → World
Geometry.GetLocationByPosition(worldPos)       // World → Grid
```

---

## Combat Formula Reference

```
Damage = (Attacker.Strength × WeaponMultiplier) 
       - (Target.Vitality × DefenseMultiplier)
       + SupporterBonus × SupporterCount
       × CriticalMultiplier (if crit)
```

**Stats**:
- Strength → Damage dealt
- Vitality → HP, damage reduction
- Speed → Timeline movement rate
- Luck → Crit/dodge chance

---

## LLM Instructions

When working with this codebase:

1. **Always use `g.` shorthand** for manager access
2. **Sequences are async** - use `yield return` patterns
3. **Factories replace prefabs** - no Unity prefab files
4. **Libraries are lazy-loaded** - access triggers loading
5. **Actors have team** - check `IsHero`/`IsEnemy`
6. **Grid is 0-indexed** - col 0 is left, row 0 is top
7. **Pincer is core mechanic** - two allies trap enemies

---

*Last updated: February 2026*
