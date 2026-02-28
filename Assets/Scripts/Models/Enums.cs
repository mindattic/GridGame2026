using System;

/// <summary>
/// ENUMS - Game-wide enumeration definitions.
/// 
/// PURPOSE:
/// Centralized location for all enum types used throughout the game.
/// Provides type-safe constants for game states, actions, and configurations.
/// </summary>

#region Ability Enums

/// <summary>
/// Category of ability: determines when/how the ability triggers.
/// - Active: Player manually activates (Heal, Smite)
/// - Passive: Always active, modifies behavior (DoubleAttack)
/// - Reactive: Triggers automatically in response to events (CounterAttack)
/// </summary>
public enum AbilityCategory
{
    Active,
    Passive,
    Reactive
}

/// <summary>
/// Targeting type for abilities.
/// - Passive: No targeting required
/// - Self: Targets the caster
/// - TargetAlly: Must select an allied actor
/// - TargetAny: Can select any actor
/// - TargetOpponent: Must select an enemy actor
/// </summary>
public enum AbilityType
{
    Passive,
    Self,
    TargetAlly,
    TargetAny,
    TargetOpponent
}

#endregion

#region Combat Enums

/// <summary>
/// Result of an attack roll.
/// </summary>
public enum AttackOutcome
{
    None = 0,
    Miss = 1,
    Hit = 2,
    CriticalHit = 3
}

/// <summary>
/// AI targeting strategy for enemy actors.
/// </summary>
public enum AttackStrategy
{
    AttackClosest,
    AttackRandom,
    AttackStrongest,
    AttackWeakest,
    MoveAnywhere
}

#endregion

#region Direction/Position Enums

/// <summary>
/// Axis orientation (horizontal or vertical).
/// </summary>
public enum Axis
{
    Horizontal,
    Vertical
}

/// <summary>
/// Available background art sets for stages.
/// </summary>
public enum BackgroundSet
{
    CyberNecropolis,
    Moors,
    RedThorns,
    UnderTheBridge,
    ElectricWasteland
}

/// <summary>
/// Named positions on the game board (3x3 grid reference points).
/// </summary>
public enum BoardPoint
{
    BottomCenter,
    BottomLeft,
    BottomRight,
    MiddleCenter,
    MiddleLeft,
    MiddleRight,
    TopCenter,
    TopLeft,
    TopRight
}

#endregion

#region Character Enums

/// <summary>
/// All available character types in the game.
/// Used for sprite loading, stats lookup, etc.
/// </summary>
public enum Characters
{
    Barbarian,
    Bat,
    Cleric,
    GreenNinja,
    Paladin,
    PandaGirl,
    RedNinja,
    Scorpion,
    Slime,
    Thief,
    Vampire,
    Yeti,

    // Additional characters
    Alchemist,
    Assassain,
    BadGirl,
    Basher,
    Bat00,
    Bat01,
    BlackNinja,
    BlackWitch,
    BlueLion,
    BlueNinja,
    Bruiser,
    Captain00,
    ChromaNinja,
    Courier,
    CyberZombie00,
    CyberZombie01,
    CyberZombie02,
    CyberZombie03,
    CyberZombie04,
    Cyclops00,
    Cyclops01,
    Cyclops02,
    Cyclops03,
    Cyclops04,
    Cyclops06,
    DarkTemplar,
    Defender,
    DemonLord,
    Dervish,
    Doctor,
    Drifter,
    Duelist,
    Engineer,
    Fencer,
    Fighter,
    FlyingMonkey,
    frog00,
    Frog01,
    Frog02,
    Frog_03,
    Ganger00,
    Ganger01,
    Ganger02,
    Ganger03,
    Ganger04,
    Ganger05,
    Ganger06,
    Ghost,
    GoblinThug00,
    Hag00,
    Hag01,
    Hag02,
    Hag03,
    Harbinger,
    Hoplite,
    IceMauler,
    JadeKnight,
    Knight,
    Lancer,
    Lurker00,
    Lurker01,
    Lurker02,
    Machinist,
    Mannequin,
    MarshShambler00,
    MarshShambler01,
    MarshShambler03,
    MartialArtist,
    MechaArmor00,
    MechaArmor01,
    MechaArmor02,
    Monk,
    MountainTroll,
    Myrmidon,
    Naga00,
    NightHunter,
    Odachi,
    Oni00,
    Oni01,
    Oni02,
    Operator,
    Paladin_v1,
    PalladiumKnight00,
    PalladiumKnight01,
    PalladiumKnight02,
    PalladiumKnight03,
    PalladiumKnight04,
    PalladiumKnight05,
    PalladiumKnight06,
    Phantom,
    PrizeFighter,
    Pugilist,
    PurplePrototype00,
    PurplePrototype01,
    PurplePrototype02,
    PurplePrototype03,
    PurplePrototype04,
    Raider,
    Reaper,
    RedMage,
    Ripper,
    Ritualist,
    Ronin,
    Sage,
    SandMaw,
    Sellsword,
    ShieldMaiden,
    Sister,
    Skelepede,
    Skelepede01,
    Skelepede02,
    Slasher,
    Slime00,
    Slime01,
    Slime02,
    Slime03,
    Soldier00,
    Soldier01,
    Soldier02,
    Soldier03,
    Speedster,
    SteppinRazor00,
    SteppinRazor01,
    SteppinRazor02,
    SteppinRazor04,
    SteppinRazor05,
    StreetFighter,
    Striker,
    SwampMistress00,
    SwordMaster,
    Tank,
    TechGremlin00,
    TechGremlin01,
    TechGremlin02,
    Technician,
    Templar00,
    Templar01,
    Templar02,
    Templar03,
    Templar05,
    Tinkerer,
    Toad00,
    TreeGolem00,
    TreeGolem01,
    TreeGolem02,
    TreeGolem03,
    TreeGolem04,
    TreeGolem06,
    Undead00,
    Undead01,
    Undead02,
    Undead04,
    Vulture,
    WarChief,
    Werewolf00,
    WhiteNinja,
    WhiteWitch,
    WildChild,
    Wolf00,
    Wolf01,
    Wolf02,
    Wolf03,
    YellowNinja,
    Yeti00,
}

public enum CoinState
{
    Bounce,
    Seek,
    Despawn
}

public enum DebugOptions
{
    None,
    AddExperience,
    ArrangeSingleCombo,
    ArrangeDoubleCombo,
    ArrangeTripleCombo,
    ArrangeSurroundCombo,
    Bump,
    Dodge,
    Fireball,
    Heal,
    KillEnemies,
    KillHeroes,
    GotoPostBattleScreen,
    PortraitPopIn,
    Portrait2DSlideIn,
    Portrait3DSlideIn,
    RandomizeBackground,
    Shake,
    Spin,
    SpawnCoins,
    SpawnDamageText,
    SpawnHealText,
    SpawnSupportLines,
    SpawnSynergyLines,
    SpawnTitle,
    SpawnTooltip1,
    SpawnTooltip2,
    TriggerEnemyMoveAttack,
    TriggerEnemyAttack,
}


public enum Direction
{
    None,
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
}

public enum DodgeStage
{
    End,
    Start,
    TwistBackward,
    TwistForward
}

public enum DottedLineSegment
{
    ArrowDown,
    ArrowLeft,
    ArrowRight,
    ArrowUp,
    Horizontal,
    None,
    TurnBottomLeft,
    TurnBottomRight,
    TurnTopLeft,
    TurnTopRight,
    Vertical
}

public enum GameSpeedOption
{
    Paused = 0,
    Percent25 = 1,
    Percent50 = 2,
    Normal = 3,
    Percent125 = 4,
    Percent150 = 5
}

public enum Glow
{
    Blue,
    Green,
    None,
    Red,
    White
}

//public enum GlowState
//{
//    Off,
//    On
//}

public enum InputMode
{
    None,
    PlayerTurn,
    EnemyTurn,
    AnyTarget,
    LinearTarget,
}

public enum LogLevel
{
    None = 0,
    Info = 1,
    Success = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}

public enum MoveDirection
{
    Idle = 0,
    Up = 1,
    Right = 2,
    Down = 3,
    Left = 4
}

public enum PlayStateProcess
{
    Editing,
    PreStarting,
    Ready,
    Starting
}

public enum ProjectilePath
{
    AnimationCurve,
    BezierCurve
}

public enum Shadow
{
    Blue,
    Green,
    None,
    Red,
    White
}

public enum StageCompletionCondition
{
    CollectCoins,
    DefeatAllEnemies,
    SurviveTurns
}

public enum Status
{
    None,
    Poisoned,
    Cursed,
    Sleeping,
    Doom
}

public enum Team
{
    Enemy,
    Hero,
    Neutral
}

public enum TextMotion
{
    Bounce,
    Float,
    None,
    Oscillate
}

public enum TooltipPlacement
{
    Bottom,
    Left,
    Right,
    Top
}

public enum TooltipTextAlignment
{
    Center,
    TopLeft
}

public enum TurnPhase
{
    Attack,
    End,
    Move,
    PostAttack,
    PreAttack,
    Start
}

public enum TypewriterMode
{
    CharacterByCharacter,
    LineByLine
}

public enum VFX
{
    AcidSplash,
    AirSlash,
    BloodClaw,
    BlueSlash1,
    BlueSlash2,
    BlueSlash3,
    BlueSlash4,
    BlueSword,
    BlueSword4X,
    BlueYellowSword,
    BlueYellowSword3X,
    BuffLife,
    DoubleClaw,
    FireRain,
    GodRays,
    GoldBuff,
    GreenBuff,
    HexShield,
    LevelUp,
    LightningExplosion,
    LightningStrike,
    MoonFeather,
    None,
    OrangeSlash,
    PinkSpark,
    PuffyExplosion,
    RedSlash2X,
    RedSword,
    TechSword,
    RotaryKnife,
    ToxicCloud,
    VFXTest_Ray_Blast,
    YellowHit
}

public enum WeaponType
{
    Dagger,
    Hammer,
    Katana,
    Mace,
    Spear,
    Sword,
    Wand
}

public enum TargetFrameRate
{
    Fps60 = 60,
    Fps45 = 45,
    Fps30
}

public enum VSyncCount
{
    VSync2 = 2,
    VSync1 = 1,
    VSync0 = 0
}

[Flags]
public enum ActorTag : uint
{
    None = 0,
    Hero = 1u << 0,
    Enemy = 1u << 1,
    Soldier = 1u << 2,
    Goblin = 1u << 3,
    Undead = 1u << 4,
    Beast = 1u << 5,
    Boss = 1u << 6,
    Humanoid = 1u << 7,
    Mechanical = 1u << 8,
    Elite = 1u << 9,
    Flying = 1u << 10,
    Insect = 1u << 11,
    Elemental = 1u << 12,
    Magic = 1u << 13,
    SwampCreature = 1u << 14,
    FireAffinity = 1u << 15,
    IceAffinity = 1u << 16,
    ElectricAffinity = 1u << 17,
    PoisonAffinity = 1u << 18,
    DarkAffinity = 1u << 19,
    LightAffinity = 1u << 20,
    UndeadAffinity = 1u << 21,
    Dragonkin = 1u << 22,
    Demonkin = 1u << 23,
    Construct = 1u << 24,
    Aquatic = 1u << 25,
    PlantBased = 1u << 26,
    ShadowCreature = 1u << 27,
    Healer = 1u << 28,

}


enum DefenseTiming { None, Dodge, Parry }


/// <summary>
/// Motion styles supported by the projectile system.
/// </summary>
public enum MotionStyle
{
    Straight,
    Wiggle,
    LobbedArc,
    HomingSpiral
}


/// <summary>Camera behavior mode for overworld exploration.</summary>
public enum OverworldCameraMode
{
    FollowHero,
    FreeCamera,
}

/// <summary>
/// Game progression mode.
/// - Campaign: Story-based progression through stages
/// - Endless: Infinite wave survival mode
/// </summary>
public enum GameMode
{
    Campaign,
    Endless
}

#endregion