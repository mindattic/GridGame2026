using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Canvas;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Utilities;
using Scripts.Managers;
using Scripts.Instances;
using Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas; // added for TimelineBarInstance
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Managers
{
public class GameManager : Singleton<GameManager>
{
    // Device
    [HideInInspector] public string deviceType;

    // Settings
    [HideInInspector] public TargetFrameRate targetFramerate = TargetFrameRate.Fps60;
    [HideInInspector] public VSyncCount vSyncCount = VSyncCount.VSync1;
    [HideInInspector] public float dragSensitivity = 0.05f;
    [HideInInspector] public float coinCountMultiplier = 0.05f;


    public float gameSpeed = 1.0f;
    public bool applyMovementTilt = false;

    // Selection behavior toggle for hero control during hero turns
    [SerializeField] public TurnSelectionMode turnSelectionMode = TurnSelectionMode.FreeSelect;

    //Debug
    public bool reloadThumbnailSettings = false;

    // Audio
    [HideInInspector] public AudioSource soundSource;
    [HideInInspector] public AudioSource musicSource;

    // Canvas
    [HideInInspector] public ActorCard card;
    [HideInInspector] public TutorialPopup tutorialPopup;
    [HideInInspector] public Vector2 viewport;
    [HideInInspector] public float tileSize;
    [HideInInspector] public Vector3 tileScale;
    [HideInInspector] public UnityEngine.Canvas canvas3D;
    [HideInInspector] public WaveAnnouncement waveAnnouncement;
    [HideInInspector] public TargetModeOverlay targetModeOverlay;
        [HideInInspector] public AbilityCastConfirm abilityCastConfirm;

    // NEW: Victory/Defeat Announcement references
    [HideInInspector] public VictoryAnnouncement victoryAnnouncement;
    [HideInInspector] public DefeatAnnouncement defeatAnnouncement;

    // Board layout tuning (Inspector adjustable)
    [Header("Board Layout")]
    [Tooltip("Horizontal fraction of visible world width the board may occupy (for width limit).")]
    public float boardHorizontalPercent = 0.96f;
    [Tooltip("Top reserved fraction of visible world height for UI (TimerBar/CoinBar/etc).")]
    public float topReservePercent = 0.12f;
    [Tooltip("Bottom reserved fraction of visible world height for UI (Card, etc).")]
    public float bottomReservePercent = 0.15f;

    // Managers
    [HideInInspector] public InputManager inputManager;
    [HideInInspector] public CameraManager cameraManager;
    [HideInInspector] public StageManager stageManager;
    [HideInInspector] public BoardManager boardManager;
    [HideInInspector] public TurnManager turnManager;
    [HideInInspector] public SupportLineManager supportLineManager;
    [HideInInspector] public AttackLineManager attackLineManager;
    [HideInInspector] public CombatTextManager combatTextManager;
    [HideInInspector] public GhostManager ghostManager;
    [HideInInspector] public PortraitManager portraitManager;
    [HideInInspector] public ActorManager actorManager;
    [HideInInspector] public SelectionManager selectedHeroManager;
    [HideInInspector] public HeroManager heroManager;
    [HideInInspector] public EnemyManager enemyManager;
    [HideInInspector] public TileManager tileManager;
    [HideInInspector] public FootstepManager footstepManager;
    [HideInInspector] public AudioManager audioManager;
    [HideInInspector] public VisualEffectManager visualEffectManager;
    [HideInInspector] public CoinManager coinManager;
    [HideInInspector] public PauseMenu pauseMenu;
    [HideInInspector] public DebugManager debugManager;
    [HideInInspector] public ConsoleManager consoleManager;
    [HideInInspector] public LogManager logManager;
    [HideInInspector] public DottedLineManager dottedLineManager;
    [HideInInspector] public ProjectileManager projectileManager;
    [HideInInspector] public SequenceManager sequenceManager;
    [HideInInspector] public PincerAttackManager pincerAttackManager;
    [HideInInspector] public SortingManager sortingManager;
    [HideInInspector] public TargetLineManager targetLineManager;
    [HideInInspector] public AbilityButtonManager abilityButtonManager;
    [HideInInspector] public AbilityManager abilityManager;
    [HideInInspector] public SynergyLineManager synergyLineManager;
    [HideInInspector] public ManaPoolManager manaPoolManager;

    // New timeline bar (replaces old block timeline)
    [HideInInspector] public TimelineBarInstance timelineBar;

    // Ability bar for displaying ability names when executed
    [HideInInspector] public AbilityBar abilityBar;

    [HideInInspector] public BackgroundInstance background;

    // Board
    [HideInInspector] public BoardOverlay boardOverlay;


    // Input
    [HideInInspector] public Vector3 touchPosition2D;
    [HideInInspector] public Vector3 touchPosition3D;
    [HideInInspector] public Vector3 touchOffset;
    [HideInInspector] public float cursorFocus;
    [HideInInspector] public float swapFocus;
    [HideInInspector] public float moveFocus;
    [HideInInspector] public float dragThreshold;
    [HideInInspector] public float bumpFocus;

    // Actors
    [HideInInspector] public List<ActorInstance> actors;
    [HideInInspector] public IEnumerable<ActorInstance> heroes => actors.Where(x => x.team == Team.Hero);
    [HideInInspector] public IEnumerable<ActorInstance> enemies => actors.Where(x => x.team == Team.Enemy);


    [HideInInspector] public ActorInstance preselectHero;
    [HideInInspector] public bool hasPreselectHero => preselectHero != null;


    [HideInInspector] public ActorInstance selectedActor;
    [HideInInspector] public bool hasSelectedActor => selectedActor != null;

    [HideInInspector] public ActorInstance movingHero;
    [HideInInspector] public bool hasMovingHeroHero => movingHero != null;

    [HideInInspector] public ActorInstance targetActor;
    [HideInInspector] public bool hasTargetActor => targetActor != null;

    // Instances
    [HideInInspector] public TileMap tileMap;
    [HideInInspector] public RectTransform portraitsRect;
    [HideInInspector] public BoardInstance board;
    [HideInInspector] public List<TileInstance> tiles;
    [HideInInspector] public List<SupportLineInstance> supportLines;
    [HideInInspector] public List<AttackLineInstance> attackLines;

    // CoinManager
    [HideInInspector] public CoinCounter coinCounter;

    // Audio indices
    [HideInInspector] public const int SoundSourceIndex = 0;
    [HideInInspector] public const int MusicSourceIndex = 1;


    // Debug


    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        if (!ProfileHelper.HasProfiles())
            return;

        var canvasRoot = GameObject.Find("Canvas");

                // PauseMenu is now a scene object - find it (must be active initially, then we deactivate it)
                // transform.Find works on inactive children, but we need the Canvas first
                if (canvasRoot != null)
                {
                    // transform.Find searches children including inactive ones
                    var pauseMenuTransform = canvasRoot.transform.Find("PauseMenu");
                    if (pauseMenuTransform != null)
                    {
                        pauseMenu = pauseMenuTransform.GetComponent<PauseMenu>();
                        // Don't deactivate here - let Initialize() handle it after wiring buttons
                    }
                    else
                    {
                        Debug.LogWarning("PauseMenu not found under UnityEngine.Canvas. Make sure it exists in the scene.");
                    }
                }

        // Use factory for TutorialPopup
        var go = TutorialPopupFactory.Create(canvasRoot?.transform);
        go.name = "TutorialPopup";
        tutorialPopup = go.GetComponent<TutorialPopup>();

        // Apply settings
        Application.targetFrameRate = targetFramerate.ToInt();
        QualitySettings.vSyncCount = VSyncCount.VSync1.ToInt();

        // Compute a robust tileSize using both width and height constraints,
        // reserving space for top/bottom UI and clamping across aspect ratios.
        Rect visible = UnitConversionHelper.World.VisibleRect();
        float worldWidth = visible.width;
        float worldHeight = visible.height;

        // Board logical dimensions (must match BoardInstance defaults)
        const int columns = 6;
        const int rows = 8;

        // Horizontal available width and vertical available height after UI reserves
        float availableW = worldWidth * Mathf.Clamp01(boardHorizontalPercent);
        float clampedTop = Mathf.Clamp01(topReservePercent);
        float clampedBottom = Mathf.Clamp01(bottomReservePercent);
        float availableH = worldHeight * Mathf.Max(0f, 1f - clampedTop - clampedBottom);

        float tileSizeFromWidth = availableW / columns;
        float tileSizeFromHeight = availableH / rows;
        tileSize = Mathf.Min(tileSizeFromWidth, tileSizeFromHeight);

        tileScale = new Vector3(tileSize, tileSize, 1f);
        tileMap = new TileMap();

        cursorFocus = tileSize * 0.5f;
        swapFocus = tileSize * 0.1666f;
        moveFocus = tileSize * 0.125f;
        bumpFocus = tileSize * 0.08f;
        dragThreshold = tileSize * 0.125f;

        ShakeIntensity.Initialize(tileSize);

        // UnityEngine.Canvas
        card = GameObjectHelper.Game.Card.Instance;
        canvas3D = GameObjectHelper.Game.Canvas3D;
        portraitsRect = GameObjectHelper.Game.Portraits;

        var coinCounterGO = GameObject.Find(GameObjectHelper.Game.CoinCounter);
        if (coinCounterGO != null)
            coinCounter = coinCounterGO.GetComponent<CoinCounter>();

        var waveRoot = GameObjectHelper.Game.WaveAnnouncement.Root;
        if (waveRoot != null)
            waveAnnouncement = waveRoot.GetComponent<WaveAnnouncement>();

        // NEW: wire Victory/Defeat Announcement
        var victoryRoot = GameObjectHelper.Game.VictoryAnnouncement.Root;
        if (victoryRoot != null)
            victoryAnnouncement = victoryRoot.GetComponent<VictoryAnnouncement>();
        var defeatRoot = GameObjectHelper.Game.DefeatAnnouncement.Root;
        if (defeatRoot != null)
            defeatAnnouncement = defeatRoot.GetComponent<DefeatAnnouncement>();

        var bgRoot = GameObjectHelper.Game.Background.Root;
        if (!string.IsNullOrEmpty(bgRoot))
        {
            var bgGo = GameObject.Find(bgRoot);
            if (bgGo != null) background = bgGo.GetComponent<BackgroundInstance>();
        }

        // Board
        board = GameObjectHelper.Game.Board.Instance;
        boardOverlay = GameObjectHelper.Game.Board.BoardOverlay;
        targetModeOverlay = GameObjectHelper.Game.Board.TargetModeOverlay;

        var gameRoot = GameObject.Find("Game");

        // Audio
        if (gameRoot != null)
        {
            var sources = gameRoot.GetComponents<AudioSource>();
            if (sources != null && sources.Length > MusicSourceIndex)
            {
                soundSource = sources[SoundSourceIndex];
                musicSource = sources[MusicSourceIndex];
            }
        }

        // Managers
        if (gameRoot != null)
        {
            cameraManager = gameRoot.GetComponent<CameraManager>();
            stageManager = gameRoot.GetComponent<StageManager>();
            boardManager = gameRoot.GetComponent<BoardManager>();
            turnManager = gameRoot.GetComponent<TurnManager>();
            inputManager = gameRoot.GetComponent<InputManager>();
            actorManager = gameRoot.GetComponent<ActorManager>();
            supportLineManager = gameRoot.GetComponent<SupportLineManager>();
            attackLineManager = gameRoot.GetComponent<AttackLineManager>();
            combatTextManager = gameRoot.GetComponent<CombatTextManager>();
            ghostManager = gameRoot.GetComponent<GhostManager>();
            portraitManager = gameRoot.GetComponent<PortraitManager>();
            selectedHeroManager = gameRoot.GetComponent<SelectionManager>();
            heroManager = gameRoot.GetComponent<HeroManager>();
            enemyManager = gameRoot.GetComponent<EnemyManager>();
            tileManager = gameRoot.GetComponent<TileManager>();
            footstepManager = gameRoot.GetComponent<FootstepManager>();
            audioManager = gameRoot.GetComponent<AudioManager>();
            debugManager = gameRoot.GetComponent<DebugManager>();
            consoleManager = gameRoot.GetComponent<ConsoleManager>();
            logManager = gameRoot.GetComponent<LogManager>();
            visualEffectManager = gameRoot.GetComponent<VisualEffectManager>();
            coinManager = gameRoot.GetComponent<CoinManager>();
            dottedLineManager = gameRoot.GetComponent<DottedLineManager>();
            projectileManager = gameRoot.GetComponent<ProjectileManager>();
            sequenceManager = gameRoot.GetComponent<SequenceManager>();
            pincerAttackManager = gameRoot.GetComponent<PincerAttackManager>();
            sortingManager = gameRoot.GetComponent<SortingManager>();
            targetLineManager = gameRoot.GetComponent<TargetLineManager>();
            abilityButtonManager = gameRoot.GetComponent<AbilityButtonManager>();
            abilityManager = gameRoot.GetComponent<AbilityManager>();
            synergyLineManager = gameRoot.GetComponent<SynergyLineManager>();
            manaPoolManager = gameRoot.GetComponent<ManaPoolManager>();
        }

        // Find TimelineBar in Canvas (expects object named "TimelineBar" under Canvas)
        var timelineBarGO = GameObject.Find("Canvas/TimelineBar");
        if (timelineBarGO != null)
            timelineBar = timelineBarGO.GetComponent<TimelineBarInstance>();
        
        // Find AbilityBar in Canvas
        var abilityBarGO = GameObject.Find("Canvas/AbilityBar");
        if (abilityBarGO != null)
            abilityBar = abilityBarGO.GetComponent<AbilityBar>();
        
        // Find AbilityCastConfirm UI
        var abilityCastConfirmGO = GameObject.Find("Canvas/AbilityCastConfirm");
        if (abilityCastConfirmGO != null)
            abilityCastConfirm = abilityCastConfirmGO.GetComponent<AbilityCastConfirm>();

        // Platform-dependent compilation
#if UNITY_STANDALONE_WIN
        deviceType = "UNITY_STANDALONE_WIN";
#elif UNITY_STANDALONE_LINUX
        deviceType = "UNITY_STANDALONE_LINUX";
#elif UNITY_IPHONE
        deviceType = "UNITY_IPHONE";
#elif UNITY_STANDALONE_OSX
        deviceType = "UNITY_STANDALONE_OSX";
#elif UNITY_WEBPLAYER
        deviceType = "UNITY_WEBPLAYER";
#elif UNITY_WEBGL
        deviceType = "UNITY_WEBGL";
#else
        deviceType = "Unknown";
#endif
    }

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start()
    {
        if (!ProfileHelper.HasProfiles())
            return;

        // Initialize UI/Managers that require instantiated prefabs
        if (pauseMenu != null) pauseMenu.Initialize();
        if (tutorialPopup != null) tutorialPopup.Initialize();

        // Ensure board reference is valid after scene loads/reloads
        if (board == null)
            board = GameObjectHelper.Game.Board.Instance;

        // Show in specific order
        if (board != null) board.Initialize();
        if (stageManager != null) stageManager.Initialize();
        if (targetModeOverlay != null) targetModeOverlay.Initialize();
        if (turnManager != null) turnManager.Initialize();

        // Spawn initial tags for existing enemies
        timelineBar?.SpawnInitialForAllEnemies();

        GameReady.Confirm();
    }
}

}
