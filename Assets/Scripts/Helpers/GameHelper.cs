using Scripts.Canvas;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Managers;
using Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Models;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// GAMEHELPER - Central access point for all game systems.
    /// 
    /// PURPOSE:
    /// Provides static shorthand access to all managers and game state.
    /// Eliminates need to write GameManager.instance.someManager everywhere.
    /// 
    /// USAGE:
    /// ```csharp
    /// using g = Scripts.Helpers.GameHelper;
    /// 
    /// g.Actors.Heroes           // All hero ActorInstances
    /// g.TurnManager.NextTurn()  // Advance turn
    /// g.TileMap.GetTile(loc)    // Get tile by location
    /// g.SequenceManager.Add()   // Queue sequence
    /// ```
    /// 
    /// CATEGORIES:
    /// 
    /// MANAGERS (access via g.ManagerName):
    /// - TurnManager: Turn order control
    /// - SequenceManager: Async event queue
    /// - PincerAttackManager: Core combat mechanic
    /// - StageManager: Stage/wave spawning
    /// - BoardManager: Board initialization
    /// - InputManager: Touch/drag handling
    /// - ActorManager: Actor tracking
    /// - SelectionManager: Selected hero tracking
    /// - VisualEffectManager: VFX spawning
    /// - AudioManager: Sound/music
    /// - (30+ more managers...)
    /// 
    /// ACTOR COLLECTIONS (access via g.Actors.*):
    /// - g.Actors.All: All ActorInstances
    /// - g.Actors.Heroes: Heroes only (team == Team.Hero)
    /// - g.Actors.Enemies: Enemies only (team == Team.Enemy)
    /// - g.Actors.Playing: All alive and active actors
    /// 
    /// TILE MAP (access via g.TileMap.*):
    /// - g.TileMap.GetTile(location): Get TileInstance at Vector2Int
    /// - g.TileMap.Tiles: All TileInstances
    /// 
    /// UI COMPONENTS:
    /// - g.Card: ActorCard UI
    /// - g.TimelineBar: Turn order UI
    /// - g.AbilityBar: Ability display UI
    /// 
    /// BOARD DATA:
    /// - g.TileSize: World-space tile size
    /// - g.TileScale: Uniform scale vector
    /// - g.Viewport: Screen viewport size
    /// 
    /// LLM CONTEXT:
    /// Always use "using g = Scripts.Helpers.GameHelper;" at top of file.
    /// Then access everything via g.Something. This is the standard pattern
    /// throughout the codebase for accessing any game system.
    /// </summary>
    public static class GameHelper
    {
        /// <summary>Cached reference to GameManager singleton.</summary>
        private static GameManager gm => GameManager.instance;

        #region Settings

        /// <summary>
        /// Turn selection mode: FreeSelect (player moves any hero) or 
        /// Forced (must move timeline-selected hero).
        /// </summary>
        public static TurnSelectionMode TurnSelectionMode
        {
            get => gm != null ? gm.turnSelectionMode : TurnSelectionMode.FreeSelect;
            set { if (gm != null) gm.turnSelectionMode = value; }
        }

        #endregion

        #region Audio

        /// <summary>AudioSource for sound effects.</summary>
        public static AudioSource SoundSource => gm != null ? gm.soundSource : null;

        /// <summary>AudioSource for background music.</summary>
        public static AudioSource MusicSource => gm != null ? gm.musicSource : null;

        #endregion

        #region Managers
        // ========================================================================
        // MANAGER ACCESS
        // All managers are accessed through these properties.
        // Example: g.TurnManager.NextTurn()
        // ========================================================================

        /// <summary>Handles touch/mouse input and drag gestures.</summary>
        public static InputManager InputManager => gm != null ? gm.inputManager : null;

        /// <summary>Controls camera position and movement.</summary>
        public static CameraManager CameraManager => gm != null ? gm.cameraManager : null;

        /// <summary>Manages stage loading, wave spawning, and victory conditions.</summary>
        public static StageManager StageManager => gm != null ? gm.stageManager : null;

        /// <summary>Initializes and manages the board grid.</summary>
        public static BoardManager BoardManager => gm != null ? gm.boardManager : null;

        /// <summary>Controls turn order, hero/enemy turn cycling.</summary>
        public static TurnManager TurnManager => gm != null ? gm.turnManager : null;

        /// <summary>Draws support lines between adjacent allies.</summary>
        public static SupportLineManager SupportLineManager => gm != null ? gm.supportLineManager : null;

        /// <summary>Manages synergy connection lines.</summary>
        public static SynergyLineManager SynergyLineManager => gm != null ? gm.synergyLineManager : null;

        /// <summary>Draws attack indicator lines.</summary>
        public static AttackLineManager AttackLineManager => gm != null ? gm.attackLineManager : null;

        /// <summary>Spawns floating combat damage/heal numbers.</summary>
        public static CombatTextManager CombatTextManager => gm != null ? gm.combatTextManager : null;

        /// <summary>Manages ghost preview when dragging heroes.</summary>
        public static GhostManager GhostManager => gm != null ? gm.ghostManager : null;

        /// <summary>Manages actor portrait displays.</summary>
        public static PortraitManager PortraitManager => gm != null ? gm.portraitManager : null;

        /// <summary>Manages tile instances on the board.</summary>
        public static TileManager TileManager => gm != null ? gm.tileManager : null;

        /// <summary>Spawns footstep effects during movement.</summary>
        public static FootstepManager FootstepManager => gm != null ? gm.footstepManager : null;

        /// <summary>Plays sound effects and music.</summary>
        public static AudioManager AudioManager => gm != null ? gm.audioManager : null;

        /// <summary>Spawns and manages visual effects (VFX).</summary>
        public static VisualEffectManager VisualEffectManager => gm != null ? gm.visualEffectManager : null;

        /// <summary>Spawns and manages coin pickup effects.</summary>
        public static CoinManager CoinManager => gm != null ? gm.coinManager : null;

        /// <summary>Debug tools and cheat flags.</summary>
        public static DebugManager DebugManager => gm != null ? gm.debugManager : null;

        /// <summary>In-game console for debug commands.</summary>
        public static ConsoleManager ConsoleManager => gm != null ? gm.consoleManager : null;

        /// <summary>Logging and diagnostics.</summary>
        public static LogManager LogManager => gm != null ? gm.logManager : null;

        /// <summary>Tracks all actor instances (heroes and enemies).</summary>
        public static ActorManager ActorManager => gm != null ? gm.actorManager : null;

        /// <summary>Tracks currently selected hero for input.</summary>
        public static SelectionManager SelectionManager => gm != null ? gm.selectedHeroManager : null;

        /// <summary>Manages dotted movement path lines.</summary>
        public static DottedLineManager DottedLineManager => gm != null ? gm.dottedLineManager : null;

        /// <summary>Manages projectile spawning and flight.</summary>
        public static ProjectileManager ProjectileManager => gm != null ? gm.projectileManager : null;

        /// <summary>Async gameplay event queue - execute sequences in order.</summary>
        public static SequenceManager SequenceManager => gm != null ? gm.sequenceManager : null;

        /// <summary>Core combat: detects and resolves pincer attacks.</summary>
        public static PincerAttackManager PincerAttackManager => gm != null ? gm.pincerAttackManager : null;

        /// <summary>Z-order sorting for actors during combat.</summary>
        public static SortingManager SortingManager => gm != null ? gm.sortingManager : null;

        /// <summary>Draws targeting lines for abilities.</summary>
        public static TargetLineManager TargetLineManager => gm != null ? gm.targetLineManager : null;

        /// <summary>Manages ability button UI elements.</summary>
        public static AbilityButtonManager AbilityButtonManager => gm != null ? gm.abilityButtonManager : null;

        /// <summary>Handles ability execution and targeting.</summary>
        public static AbilityManager AbilityManager => gm != null ? gm.abilityManager : null;

        /// <summary>Manages mana pool resource system.</summary>
        public static ManaPoolManager ManaPoolManager => gm != null ? gm.manaPoolManager : null;

        #endregion

        #region UI Components

        /// <summary>Pause menu overlay.</summary>
        public static PauseMenu PauseMenu => gm != null ? gm.pauseMenu : null;

        /// <summary>Parallax background instance.</summary>
        public static BackgroundInstance Background => gm != null ? gm.background : null;

        /// <summary>Board overlay for highlighting.</summary>
        public static BoardOverlay BoardOverlay => gm != null ? gm.boardOverlay : null;

        /// <summary>Viewport size in world units.</summary>
        public static Vector2 Viewport => gm != null ? gm.viewport : Vector2.zero;

        /// <summary>Computed tile size in world units.</summary>
        public static float TileSize => gm != null ? gm.tileSize : 1f;

        /// <summary>Uniform scale vector for tiles.</summary>
        public static Vector3 TileScale => gm != null ? gm.tileScale : Vector3.one;

        /// <summary>World-space canvas for 3D UI.</summary>
        public static UnityEngine.Canvas Canvas3D => gm != null ? gm.canvas3D : null;

        /// <summary>Wave announcement overlay ("Wave 1", "Wave 2", etc.).</summary>
        public static WaveAnnouncement WaveAnnouncement => gm != null ? gm.waveAnnouncement : null;

        /// <summary>Victory announcement overlay.</summary>
        public static VictoryAnnouncement VictoryAnnouncement => gm != null ? gm.victoryAnnouncement : null;

        /// <summary>Defeat announcement overlay.</summary>
        public static DefeatAnnouncement DefeatAnnouncement => gm != null ? gm.defeatAnnouncement : null;

        /// <summary>Target mode overlay for ability targeting.</summary>
        public static TargetModeOverlay TargetModeOverlay => gm != null ? gm.targetModeOverlay : null;

        /// <summary>Actor info card UI.</summary>
        public static ActorCard Card => gm != null ? gm.card : null;

        /// <summary>Tutorial popup overlay.</summary>
        public static TutorialPopup TutorialPopup => gm != null ? gm.tutorialPopup : null;

        /// <summary>Turn order timeline UI.</summary>
        public static TimelineBarInstance TimelineBar => gm != null ? gm.timelineBar : null;

        /// <summary>Ability display bar UI.</summary>
        public static AbilityBar AbilityBar => gm != null ? gm.abilityBar : null;
        public static Scripts.Canvas.AbilityCastConfirm AbilityCastConfirm => gm != null ? gm.abilityCastConfirm : null;

        #endregion

        #region Input/Touch


        //Touch
        public static Vector3 TouchPosition2D => gm != null ? gm.touchPosition2D : Vector3.zero;
        public static Vector3 TouchPosition3D => gm != null ? gm.touchPosition3D : Vector3.zero;
        public static Vector3 TouchOffset
        {
            get => gm != null ? gm.touchOffset : Vector3.zero;
            set { if (gm != null) gm.touchOffset = value; }
        }
        public static Vector3 TouchPosition => TouchPosition3D + TouchOffset;

        public static float CursorFocus => gm != null ? gm.cursorFocus : 0f;
        public static float SwapFocus => gm != null ? gm.swapFocus : 0f;
        public static float MoveFocus => gm != null ? gm.moveFocus : 0f;
        public static float SnapThreshold => gm != null ? gm.actorManager.snapTheshold : 0f;
        public static float DragThreshold => gm != null ? gm.dragThreshold : 0f;
        public static float BumpFocus => gm != null ? gm.bumpFocus : 0f;

        public static class Actors
        {
            public static List<ActorInstance> All
            {
                get => gm != null ? gm.actors : null;
                set { if (gm != null) gm.actors = value; }
            }
            public static IEnumerable<ActorInstance> Heroes => gm != null ? gm.heroes : Enumerable.Empty<ActorInstance>();
            public static IEnumerable<ActorInstance> Enemies => gm != null ? gm.enemies : Enumerable.Empty<ActorInstance>();
            public static ActorInstance SelectedActor
            {
                get => gm != null ? gm.selectedActor : null;
                set { if (gm != null) gm.selectedActor = value; }
            }
            public static bool HasSelectedActor => gm != null && gm.hasSelectedActor;
            public static ActorInstance MovingHero
            {
                get => gm != null ? gm.movingHero : null;
                set { if (gm != null) gm.movingHero = value; }
            }
            public static bool HasMovingHero => gm != null && gm.hasMovingHeroHero;
            public static ActorInstance TargetActor
            {
                get => gm != null ? gm.targetActor : null;
                set { if (gm != null) gm.targetActor = value; }
            }
            public static bool HasTargetActor => gm != null && gm.hasTargetActor;
        }

        public static TileMap TileMap => gm != null ? gm.tileMap : null;
        public static BoardInstance Board => gm != null ? gm.board : null;
        public static List<TileInstance> Tiles => gm != null ? gm.tiles : null;
        public static RectTransform PortraitsContainer => gm != null ? gm.portraitsRect : null;
        public static CoinCounter CoinCounter => gm != null ? gm.coinCounter : null;

        public static int TotalCoins
        {
            get
            {
                var save = ProfileHelper.CurrentProfile?.CurrentSave;
                return save?.Global?.TotalCoins ?? 0;
            }
            set
            {
                var save = ProfileHelper.CurrentProfile?.CurrentSave;
                if (save == null || save.Global == null) return;
                save.Global.TotalCoins = Mathf.Max(0, value);
            }
        }

        public static HeroManager HeroManager => gm != null ? gm.heroManager : null;

        #endregion
    }
}
