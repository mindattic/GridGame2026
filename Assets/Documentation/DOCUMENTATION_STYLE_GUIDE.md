# GridGame2026 Documentation Style Guide

This guide defines the documentation patterns used throughout the codebase. Follow these conventions when documenting new files or updating existing documentation.

---

## Table of Contents

1. [Overview](#overview)
2. [Class-Level Documentation Structure](#class-level-documentation-structure)
3. [Standard Sections](#standard-sections)
4. [ASCII Diagrams](#ascii-diagrams)
5. [Code Examples](#code-examples)
6. [Region Organization](#region-organization)
7. [Method Documentation](#method-documentation)
8. [Complete Examples](#complete-examples)

---

## Overview

### Documentation Philosophy

- **Self-documenting**: Each file should explain its purpose without needing to read other files
- **Cross-referenced**: Link to related files so developers can navigate the codebase
- **Visual**: Use ASCII diagrams for visual concepts
- **Practical**: Include usage examples showing real code patterns
- **Consistent**: Follow the same structure across all files

### Naming Convention for Headers

Use **ALLCAPS** for the class name in the summary opening:

```csharp
/// <summary>
/// CLASSNAME - Brief one-line description.
/// </summary>
```

---

## Class-Level Documentation Structure

### Template

```csharp
/// <summary>
/// CLASSNAME - Brief one-line description.
/// 
/// PURPOSE:
/// 2-4 sentences explaining what this class does and why it exists.
/// 
/// [OPTIONAL SECTIONS - see Standard Sections below]
/// 
/// RELATED FILES:
/// - RelatedFile.cs: Brief description
/// - AnotherFile.cs: Brief description
/// 
/// ACCESS: g.ClassName (if applicable)
/// </summary>
```

### Required Elements

| Element | Description |
|---------|-------------|
| `CLASSNAME` | All-caps class name at start of summary |
| `Brief description` | One line after the dash |
| `PURPOSE:` | 2-4 sentences explaining the class |
| `RELATED FILES:` | Cross-references to connected classes |

### Optional Elements

| Element | When to Use |
|---------|-------------|
| `VISUAL APPEARANCE:` | UI components, visual effects |
| `USAGE:` | Public APIs, managers, helpers |
| `SEQUENCE FLOW:` | Sequences, state machines |
| `LIFECYCLE:` | Components with spawn/update/destroy patterns |
| `FEATURES:` | Classes with multiple capabilities |
| `PROPERTIES:` | Data classes, configurations |
| `ACCESS:` | Singletons accessible via GameHelper |

---

## Standard Sections

### PURPOSE

Always include. Explain what the class does and its role in the system.

```csharp
/// PURPOSE:
/// Manages the queue of async gameplay sequences, processing them
/// one at a time via coroutines. Ensures attacks, movements, deaths,
/// and other game events happen in the correct order.
```

### VISUAL APPEARANCE (for UI/visual components)

Use ASCII art to show what the component looks like.

```csharp
/// VISUAL APPEARANCE:
/// ```
/// ┌─────────────────────────────┐
/// │  "Paladin uses Heal"        │
/// └─────────────────────────────┘
/// ```
```

### SEQUENCE FLOW (for sequences/state machines)

Show the step-by-step flow.

```csharp
/// SEQUENCE FLOW:
/// 1. Lock input (InputMode.None)
/// 2. Play attacker animation
/// 3. Calculate damage
/// 4. Spawn projectile/VFX
/// 5. Apply damage to target
/// 6. Update health bars
/// 7. Check for death → queue DeathSequence
```

### USAGE (for public APIs)

Show real code examples.

```csharp
/// USAGE:
/// ```csharp
/// g.SequenceManager.Add(new DeathSequence(enemy));
/// g.SequenceManager.Execute();
/// ```
```

### LIFECYCLE (for spawnable objects)

Document the creation → update → destruction flow.

```csharp
/// LIFECYCLE:
/// 1. Spawned by ProjectileManager
/// 2. Travels toward target position
/// 3. On arrival, plays impact VFX
/// 4. Executes post-impact routine
/// 5. Self-destructs
```

### FEATURES (for multi-purpose classes)

List key capabilities.

```csharp
/// FEATURES:
/// - Add/Remove items with stack tracking
/// - Equipment durability tracking
/// - Currency (Gold) management
/// - Lookup by item ID
```

### PROPERTIES (for data classes)

Document key fields.

```csharp
/// PROPERTIES:
/// - Id: Unique identifier
/// - DisplayName: UI display text
/// - Description: Tooltip text
/// - Type: Active or Passive
/// - ManaCost: Mana required to use
```

### RELATED FILES

Always include. List 3-5 most relevant files.

```csharp
/// RELATED FILES:
/// - SequenceManager.cs: Queues and executes sequences
/// - DeathSequence.cs: Death processing
/// - AttackHelper.cs: Damage calculation
```

### ACCESS

For singletons/managers accessible via GameHelper.

```csharp
/// ACCESS: g.SequenceManager
```

---

## ASCII Diagrams

### Box Characters

```
┌ ─ ┐    Top corners and horizontal line
│   │    Vertical lines
└ ─ ┘    Bottom corners
═ ║      Double lines for emphasis
```

### Flow Arrows

```
→ ← ↑ ↓    Directional arrows
↗ ↘ ↙ ↖    Diagonal arrows
⟶ ⟵        Long arrows
```

### Common Patterns

#### UI Layout
```csharp
/// ```
/// ┌────────────────────────────┐
/// │  [Portrait]  Title         │
/// │              ────────      │
/// │              Details       │
/// └────────────────────────────┘
/// ```
```

#### Timeline/Sequence
```csharp
/// ```
/// [Start] → [Step 1] → [Step 2] → [End]
///              ↓
///         (optional branch)
/// ```
```

#### Grid/Board
```csharp
/// ```
/// ┌───┬───┬───┐
/// │ A │ B │ C │
/// ├───┼───┼───┤
/// │ D │ E │ F │
/// └───┴───┴───┘
/// ```
```

#### Actor Relationships
```csharp
/// ```
/// [Hero A] ═══════ [Hero B]
///     ↑              ↑
///   pincer attack targets
/// ```
```

#### Fade/Animation
```csharp
/// ```
/// [Visible] → [Fading...] → [Hidden]
///    α=1        α=0.5         α=0
/// ```
```

#### Movement Trail
```csharp
/// ```
/// [Ghost1] → [Ghost2] → [Ghost3] → [Actor]
///  fading     fading     fading    solid
/// ```
```

---

## Code Examples

### Format

Always use triple backticks with `csharp` language identifier:

```csharp
/// ```csharp
/// var enemy = RNG.Enemy;
/// g.SequenceManager.Add(new DeathSequence(enemy));
/// ```
```

### Common Patterns

#### Manager Access
```csharp
/// ```csharp
/// g.SomeManager.DoSomething();
/// ```
```

#### Sequence Creation
```csharp
/// ```csharp
/// g.SequenceManager.Add(new MySequence(params));
/// g.SequenceManager.Execute();
/// ```
```

#### Factory Usage
```csharp
/// ```csharp
/// var obj = SomeFactory.Create(parent, position);
/// ```
```

#### Coroutine Yield
```csharp
/// ```csharp
/// yield return Wait.Seconds(0.5f);
/// yield return someCoroutine;
/// ```
```

---

## Region Organization

Use `#region` to organize larger classes:

```csharp
public class MyClass : MonoBehaviour
{
    #region Fields
    
    private SomeType field;
    
    #endregion

    #region Properties
    
    public SomeType Property => field;
    
    #endregion

    #region Initialization
    
    private void Awake() { }
    public void Initialize() { }
    
    #endregion

    #region Public Methods
    
    public void DoSomething() { }
    
    #endregion

    #region Private Methods
    
    private void Helper() { }
    
    #endregion

    #region Coroutines
    
    private IEnumerator SomeRoutine() { }
    
    #endregion
}
```

### Standard Region Names

| Region | Contents |
|--------|----------|
| `Fields` | Private fields, serialized fields |
| `Properties` | Public properties |
| `Components` | Cached component references |
| `State` | Runtime state variables |
| `Configuration` | Inspector-exposed settings |
| `Initialization` | Awake, Start, Initialize |
| `Unity Lifecycle` | Update, FixedUpdate, OnDestroy |
| `Public Methods` | External API |
| `Private Methods` | Internal helpers |
| `Coroutines` | IEnumerator methods |
| `Event Handlers` | OnSomethingHappened methods |
| `Helpers` | Static utility methods |

---

## Method Documentation

### Rule: Every Method Must Have a Summary

**Every method** in the project — public, private, internal, or protected — must have a `/// <summary>` XML doc comment. No method should be left undocumented.

- Unity lifecycle methods (`Awake`, `Start`, `Update`, `OnEnable`, etc.) included.
- Coroutines included.
- Event handlers included.
- One-line summaries are acceptable for simple methods.

### Simple Methods

One-line summary is sufficient:

```csharp
/// <summary>Immediately hides the ability bar.</summary>
public void Hide()
```

### Complex Methods

Include parameters and behavior:

```csharp
/// <summary>
/// Spawns a projectile from source to target with specified settings.
/// </summary>
/// <param name="settings">Configuration for projectile appearance and motion.</param>
/// <returns>Coroutine that completes when projectile reaches target.</returns>
public IEnumerator SpawnRoutine(ProjectileSettings settings)
```

### Coroutines

Document yield behavior:

```csharp
/// <summary>
/// Main execution coroutine for this sequence.
/// Yields until all sequence steps complete.
/// </summary>
public override IEnumerator ProcessRoutine()
```

---

## Complete Examples

### Manager Class

```csharp
/// <summary>
/// SEQUENCEMANAGER - Orchestrates async gameplay event sequences.
/// 
/// PURPOSE:
/// Manages a queue of SequenceEvent objects, executing them one at a time
/// via coroutines. Ensures game events (attacks, deaths, turns) happen
/// in the correct order without overlap.
/// 
/// QUEUE OPERATIONS:
/// - Add(): Enqueue at end
/// - AddFirst(): Enqueue at front (priority)
/// - Insert(): Insert relative to existing item
/// 
/// EXECUTION:
/// ```csharp
/// g.SequenceManager.Add(new DeathSequence(enemy));
/// g.SequenceManager.Add(new EndTurnSequence());
/// g.SequenceManager.Execute();
/// ```
/// 
/// SEQUENCE TYPES:
/// - PincerAttackSequence: Combat resolution
/// - DeathSequence: Actor death processing
/// - EnemyTakeTurnSequence: Enemy AI turn
/// - BattleWonSequence: Victory handling
/// 
/// RELATED FILES:
/// - SequenceEvent.cs: Base class for all sequences
/// - QueueCollection.cs: Queue implementation
/// - TurnManager.cs: Creates turn sequences
/// 
/// ACCESS: g.SequenceManager
/// </summary>
public class SequenceManager : MonoBehaviour
```

### Instance/Component Class

```csharp
/// <summary>
/// PROJECTILEINSTANCE - Single projectile traveling to target.
/// 
/// PURPOSE:
/// Represents a projectile in flight, handling motion, VFX trails,
/// impact effects, and post-impact routines.
/// 
/// VISUAL APPEARANCE:
/// ```
/// [Source] ~~~~✦~~~~> [Target]
///          trail    projectile
/// ```
/// 
/// MOTION STYLES:
/// - Linear: Straight line to target
/// - Arc: Parabolic arc trajectory
/// - Wiggle: Sinusoidal wave motion
/// - Homing: Curves toward moving target
/// 
/// LIFECYCLE:
/// 1. Spawned by ProjectileManager
/// 2. Trail VFX attached
/// 3. Travels via selected motion style
/// 4. On arrival: impact VFX spawned
/// 5. Post-impact routine executed
/// 6. Self-destructs
/// 
/// RELATED FILES:
/// - ProjectileManager.cs: Spawns projectiles
/// - ProjectileFactory.cs: Creates GameObjects
/// - ProjectileSettings.cs: Configuration data
/// 
/// ACCESS: Created via ProjectileManager.SpawnRoutine()
/// </summary>
public class ProjectileInstance : MonoBehaviour
```

### Sequence Class

```csharp
/// <summary>
/// DEATHSEQUENCE - Handles actor death processing.
/// 
/// PURPOSE:
/// Executes the death sequence for an actor including animations,
/// VFX, cleanup, and victory/defeat checks.
/// 
/// SEQUENCE FLOW:
/// 1. Play death animation
/// 2. Spawn death VFX
/// 3. Remove from ActorManager
/// 4. Remove timeline tag (if enemy)
/// 5. Spawn coin drops (if enemy)
/// 6. Check victory/defeat conditions
/// 7. Destroy GameObject
/// 
/// RELATED FILES:
/// - SequenceEvent.cs: Base class
/// - ActorAnimation.cs: Death animation
/// - CoinManager.cs: Coin spawning
/// - StageManager.cs: Victory/defeat checks
/// </summary>
public class DeathSequence : SequenceEvent
```

### Data Class

```csharp
/// <summary>
/// PROJECTILESETTINGS - Configuration for projectile spawning.
/// 
/// PURPOSE:
/// Contains all parameters needed to spawn and configure a projectile,
/// including visuals, motion, and post-impact behavior.
/// 
/// PROPERTIES:
/// - friendlyName: Debug identifier
/// - startPosition: Spawn world position
/// - target: Target ActorInstance
/// - projectileVfxKey: VFX key for trail
/// - impactVfxKey: VFX key for impact
/// - motionStyle: Linear/Arc/Wiggle/Homing
/// - travelSeconds: Flight duration
/// - routine: Post-impact coroutine
/// 
/// USAGE:
/// ```csharp
/// var settings = new ProjectileSettings
/// {
///     friendlyName = "Fireball",
///     target = enemy,
///     motionStyle = MotionStyle.Arc,
///     travelSeconds = 0.5f
/// };
/// yield return g.ProjectileManager.SpawnRoutine(settings);
/// ```
/// 
/// RELATED FILES:
/// - ProjectileManager.cs: Uses settings
/// - ProjectileInstance.cs: Applies settings
/// </summary>
public class ProjectileSettings
```

### Helper/Utility Class

```csharp
/// <summary>
/// GEOMETRY - Grid and world position utilities.
/// 
/// PURPOSE:
/// Provides conversion functions between grid coordinates (Vector2Int)
/// and world positions (Vector3), plus distance and direction calculations.
/// 
/// COORDINATE SYSTEMS:
/// - Grid: Vector2Int (column, row), 0-indexed
/// - World: Vector3, tile-size scaled
/// 
/// KEY METHODS:
/// - ToWorldPosition(location): Grid → World
/// - ToLocation(worldPos): World → Grid
/// - Distance(a, b): Grid distance
/// - Direction(from, to): Cardinal direction
/// 
/// USAGE:
/// ```csharp
/// Vector3 worldPos = Geometry.ToWorldPosition(new Vector2Int(3, 4));
/// Vector2Int gridPos = Geometry.ToLocation(transform.position);
/// float dist = Geometry.Distance(heroLoc, enemyLoc);
/// ```
/// 
/// RELATED FILES:
/// - BoardInstance.cs: Board dimensions
/// - TileInstance.cs: Tile positions
/// - GameHelper.cs: TileSize constant
/// </summary>
public static class Geometry
```

### Actor Data Class

```csharp
/// <summary>
/// ALCHEMIST - Hero character data definition.
/// 
/// ROLE: Backline Support
/// ARCHETYPE: Healer / Buffer
/// 
/// STAT FOCUS:
/// - Primary: Intelligence (16), Wisdom (15)
/// - Secondary: Speed (12), Stamina (12)
/// - Weakness: Strength (10)
/// 
/// PLAYSTYLE:
/// Sustains allies through healing and cleansing while
/// manipulating battle tempo with buffs.
/// 
/// RELATED FILES:
/// - ActorLibrary.cs: Registers this data
/// - ActorData.cs: Data structure
/// - CharacterClass.cs: Enum definition
/// </summary>
public static class Alchemist
```

---

## Quick Reference Checklist

When documenting a new file:

- [ ] Add `CLASSNAME - brief description` header
- [ ] Write PURPOSE section (2-4 sentences)
- [ ] Add VISUAL APPEARANCE if it's a UI/visual component
- [ ] Add SEQUENCE FLOW if it's a sequence
- [ ] Add USAGE with code example if it's a public API
- [ ] Add LIFECYCLE if it's a spawnable component
- [ ] List 3-5 RELATED FILES
- [ ] Add ACCESS if singleton via GameHelper
- [ ] Use `#region` for classes > 100 lines
- [ ] Add `/// <summary>` to **every** method (public, private, coroutine, lifecycle)

### Actor Data Files (Data/Actor/*.cs)

- [ ] Add ROLE (Frontline/Backline, Tank/Support/DPS)
- [ ] Add ARCHETYPE (class fantasy)
- [ ] Add STAT FOCUS (Primary, Secondary, Weakness)
- [ ] Add PLAYSTYLE (1-2 sentence description)

---

## Version History

| Date | Author | Changes |
|------|--------|---------|
| 2025-01 | AI Assistant | Initial documentation pass (~320 files) |

