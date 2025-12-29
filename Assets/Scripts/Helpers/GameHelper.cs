using Assets.Scripts.GUI;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Game.Behaviors;
using Game.Manager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Models.Profile; // Added for Profile access
using Assets.Scripts.Canvas; // for TimelineBarInstance

namespace Assets.Helpers
{
    public static class GameHelper
    {
        private static GameManager gm => GameManager.instance;

        // New: selection mode toggle (still lives on GameManager while in-game)
        public static TurnSelectionMode TurnSelectionMode
        {
            get => gm != null ? gm.turnSelectionMode : TurnSelectionMode.FreeSelect;
            set { if (gm != null) gm.turnSelectionMode = value; }
        }

        public static AudioSource SoundSource => gm != null ? gm.soundSource : null;
        public static AudioSource MusicSource => gm != null ? gm.musicSource : null;

        // Component properties (still scene-bound)
        public static InputManager InputManager => gm != null ? gm.inputManager : null;
        public static CameraManager CameraManager => gm != null ? gm.cameraManager : null;
        public static StageManager StageManager => gm != null ? gm.stageManager : null;
        public static BoardManager BoardManager => gm != null ? gm.boardManager : null;
        public static TurnManager TurnManager => gm != null ? gm.turnManager : null;
        public static SupportLineManager SupportLineManager => gm != null ? gm.supportLineManager : null;
        public static SynergyLineManager SynergyLineManager => gm != null ? gm.synergyLineManager : null;
        public static AttackLineManager AttackLineManager => gm != null ? gm.attackLineManager : null;
        public static CombatTextManager CombatTextManager => gm != null ? gm.combatTextManager : null;
        public static GhostManager GhostManager => gm != null ? gm.ghostManager : null;
        public static PortraitManager PortraitManager => gm != null ? gm.portraitManager : null;
        public static TileManager TileManager => gm != null ? gm.tileManager : null;
        public static FootstepManager FootstepManager => gm != null ? gm.footstepManager : null;
        public static AudioManager AudioManager => gm != null ? gm.audioManager : null;
        public static VisualEffectManager VisualEffectManager => gm != null ? gm.visualEffectManager : null;
        public static CoinManager CoinManager => gm != null ? gm.coinManager : null;
        public static DebugManager DebugManager => gm != null ? gm.debugManager : null;
        public static ConsoleManager ConsoleManager => gm != null ? gm.consoleManager : null;
        public static LogManager LogManager => gm != null ? gm.logManager : null;
        public static ActorManager ActorManager => gm != null ? gm.actorManager : null;
        public static SelectionManager SelectionManager => gm != null ? gm.selectedHeroManager : null;
        public static DottedLineManager DottedLineManager => gm != null ? gm.dottedLineManager : null;
        public static ProjectileManager ProjectileManager => gm != null ? gm.projectileManager : null;
        public static SequenceManager SequenceManager => gm != null ? gm.sequenceManager : null;
        public static PincerAttackManager PincerAttackManager => gm != null ? gm.pincerAttackManager : null;
        public static SortingManager SortingManager => gm != null ? gm.sortingManager : null;
        public static TargetLineManager TargetLineManager => gm != null ? gm.targetLineManager : null;
        public static AbilityButtonManager AbilityButtonManager => gm != null ? gm.abilityButtonManager : null;
        public static AbilityManager AbilityManager => gm != null ? gm.abilityManager : null;
        public static ManaPoolManager ManaPoolManager => gm != null ? gm.manaPoolManager : null;


        public static PauseMenu PauseMenu => gm != null ? gm.pauseMenu : null;

        public static BackgroundInstance Background => gm != null ? gm.background : null;
        public static BoardOverlay BoardOverlay => gm != null ? gm.boardOverlay : null;
        public static Vector2 Viewport => gm != null ? gm.viewport : Vector2.zero;
        public static float TileSize => gm != null ? gm.tileSize : 1f;
        public static Vector3 TileScale => gm != null ? gm.tileScale : Vector3.one;
        public static Canvas Canvas3D => gm != null ? gm.canvas3D : null;


        //Cancas
        public static WaveAnnouncement WaveAnnouncement => gm != null ? gm.waveAnnouncement : null;
        public static VictoryAnnouncement VictoryAnnouncement => gm != null ? gm.victoryAnnouncement : null;
        public static DefeatAnnouncement DefeatAnnouncement => gm != null ? gm.defeatAnnouncement : null;

        public static TargetModeOverlay TargetModeOverlay => gm != null ? gm.targetModeOverlay : null;
        public static ActorCard Card => gm != null ? gm.card : null;
        public static TutorialPopup TutorialPopup => gm != null ? gm.tutorialPopup : null;
        public static TimelineBarInstance TimelineBar => gm != null ? gm.timelineBar : null; // new accessor
        public static Assets.Scripts.Canvas.AbilityCastConfirm AbilityCastConfirm => gm != null ? gm.abilityCastConfirm : null;
      


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
    }
}
