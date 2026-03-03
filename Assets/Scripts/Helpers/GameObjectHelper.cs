using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// GAMEOBJECTHELPER - GameObject path constants for scene lookups.
    /// 
    /// PURPOSE:
    /// Centralized string constants for GameObject.Find() paths,
    /// avoiding hardcoded strings scattered throughout the codebase.
    /// 
    /// ORGANIZATION:
    /// Nested classes mirror scene/prefab hierarchy:
    /// - Actor.*: Actor prefab child paths
    /// - Game.*: Game scene UI paths
    /// - StageSelect.*: Stage selection scene paths
    /// - etc.
    /// 
    /// USAGE:
    /// ```csharp
    /// // Instead of: GameObject.Find("Canvas/Title")
    /// var title = GameObject.Find(GameObjectHelper.StageSelect.Title);
    /// 
    /// // Actor child lookup
    /// var healthBar = actor.transform.Find(GameObjectHelper.Actor.Front.HealthBar.Fill);
    /// ```
    /// 
    /// PATH FORMAT:
    /// - Absolute paths for scene objects: "Canvas/Title"
    /// - Relative paths for prefab children: "Front/Thumbnail"
    /// 
    /// RELATED FILES:
    /// - All scene managers use these paths
    /// - ActorInstance.cs: Uses Actor.* paths
    /// - Factory classes: Reference child paths
    /// </summary>
    public static class GameObjectHelper
    {
        #region Actor Paths

        /// <summary>Actor prefab child object paths.</summary>
        public static class Actor
        {
            public static class Front
            {
                public const string Root = "Front";
                public const string Opaque = Root + "/Opaque";
                public const string Quality = Root + "/Quality";
                public const string Glow = Root + "/Glow";
                public const string Parallax = Root + "/Parallax";
                public const string Thumbnail = Root + "/Thumbnail";
                public const string Frame = Root + "/Frame";
                public const string StatusIcon = Root + "/StatusIcon";

                public static class HealthBar
                {
                    public const string Root = Front.Root + "/HealthBar";
                    public const string Back = Root + "/HealthBarBack";
                    public const string Drain = Root + "/HealthBarDrain";
                    public const string Fill = Root + "/HealthBarFill";
                    public const string Text = Root + "/HealthBarText";
                }

                public static class ActionBar
                {
                    public const string Root = Front.Root + "/ActionBar";
                    public const string Mask = Root + "/Mask";
                    public const string RadialBack = Root + "/RadialBack";
                    public const string RadialFill = Root + "/RadialFill";
                    public const string RadialText = Root + "/RadialText";
                    public const string TurnDelayText = Root + "/TurnDelayText";
                }
            }

            public const string Armor = "Armor";
            public const string Back = "Back";
        }

        #endregion

        #region Scene Paths

        /// <summary>Credits scene paths.</summary>
        public static class Credits
        {
            public const string Title = "Canvas/Title";
            public const string ScrollView = "Canvas/ScrollView";
            public const string Viewport = "Canvas/ScrollView/Viewport";
            public const string Content = "Canvas/ScrollView/Viewport/Content";
            public const string Textarea = "Canvas/ScrollView/Viewport/Content/Textarea";
        }

        /// <summary>Game scene paths.</summary>
        public static class Game
        {
            public static UnityEngine.Canvas Canvas3D => GameObject.Find("Canvas3D").GetComponent<UnityEngine.Canvas>();



            public static class WaveAnnouncement
            {
                public static GameObject Root => GameObject.Find("Canvas/Announcements/WaveAnnouncement");
                public static Image Image => GameObject.Find("Canvas/Announcements/WaveAnnouncement/Image").GetComponent<Image>();
                public static TextMeshProUGUI Back => GameObject.Find("Canvas/Announcements/WaveAnnouncement/Back").GetComponent<TextMeshProUGUI>();
                public static TextMeshProUGUI Front => GameObject.Find("Canvas/Announcements/WaveAnnouncement/Front").GetComponent<TextMeshProUGUI>();
            }

            // NEW: VictoryAnnouncement
            public static class VictoryAnnouncement
            {
                public static GameObject Root => GameObject.Find("Canvas/Announcements/VictoryAnnouncement");
                public static Image Image => GameObject.Find("Canvas/Announcements/VictoryAnnouncement/Image").GetComponent<Image>();
                public static TextMeshProUGUI Back => GameObject.Find("Canvas/Announcements/VictoryAnnouncement/Back").GetComponent<TextMeshProUGUI>();
                public static TextMeshProUGUI Front => GameObject.Find("Canvas/Announcements/VictoryAnnouncement/Front").GetComponent<TextMeshProUGUI>();
            }

            // NEW: DefeatAnnouncement
            public static class DefeatAnnouncement
            {
                public static GameObject Root => GameObject.Find("Canvas/Announcements/DefeatAnnouncement");
                public static Image Image => GameObject.Find("Canvas/Announcements/DefeatAnnouncement/Image").GetComponent<Image>();
                public static TextMeshProUGUI Back => GameObject.Find("Canvas/Announcements/DefeatAnnouncement/Back").GetComponent<TextMeshProUGUI>();
                public static TextMeshProUGUI Front => GameObject.Find("Canvas/Announcements/DefeatAnnouncement/Front").GetComponent<TextMeshProUGUI>();
            }


            public const string TimelineContainer = "Canvas/Timeline";


            public static RectTransform Portraits => GameObject.Find("Canvas/Portraits").GetComponent<RectTransform>();

            public const string CoinCounter = "Canvas/CoinCounter";

            public static class TutorialPopup
            {
                public const string Root = "Canvas/TutorialPopup";
                public static GameObject Panel => GameObject.Find("Canvas/TutorialPopup/Panel");
                public static Image Image => GameObject.Find("Canvas/TutorialPopup/Panel/Image").GetComponent<Image>();
                public static TextMeshProUGUI TitleTextX => GameObject.Find("Canvas/TutorialPopup/Panel/Title").GetComponent<TextMeshProUGUI>();
                public static TextMeshProUGUI ContentTextX => GameObject.Find("Canvas/TutorialPopup/Panel/Content").GetComponent<TextMeshProUGUI>();
                public static Button PreviousButton => GameObject.Find("Canvas/TutorialPopup/Panel/PreviousButton").GetComponent<Button>();
                public static Button NextButton => GameObject.Find("Canvas/TutorialPopup/Panel/NextButton").GetComponent<Button>();
                public static Button CloseButton => GameObject.Find("Canvas/TutorialPopup/Panel/CloseButton").GetComponent<Button>();
            }

            public static class PauseButton
            {
                public static Button Root => GameObject.Find("Canvas/PauseButton").GetComponent<Button>();
                public static Image Image => GameObject.Find("Canvas/PauseButton").GetComponent<Image>();

            }

            public static class PauseMenu
            {
                public static GameObject Root => GameObject.Find("Canvas/PauseMenu");
                public static Button ResumeButton => GameObject.Find("Canvas/PauseMenu/Inner/ResumeButton").GetComponent<Button>();
                public static TextMeshProUGUI ResumeButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/ResumeButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button RunAwayButton => GameObject.Find("Canvas/PauseMenu/Inner/RunAwayButton").GetComponent<Button>();
                public static TextMeshProUGUI RunAwayButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/RunAwayButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button QuickSaveGameButton => GameObject.Find("Canvas/PauseMenu/Inner/QuickSaveGameButton").GetComponent<Button>();
                public static TextMeshProUGUI QuickSaveGameButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/QuickSaveGameButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button CreateSaveGameButton => GameObject.Find("Canvas/PauseMenu/Inner/CreateSaveGameButton").GetComponent<Button>();
                public static TextMeshProUGUI CreateSaveGameButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/CreateSaveGameButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button RestartStageButton => GameObject.Find("Canvas/PauseMenu/Inner/RestartStageButton").GetComponent<Button>();
                public static TextMeshProUGUI RestartStageButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/RestartStageButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button PartyManagerButton => GameObject.Find("Canvas/PauseMenu/Inner/PartyManagerButton").GetComponent<Button>();
                public static TextMeshProUGUI PartyManagerButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/PartyManagerButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button StageSelectButton => GameObject.Find("Canvas/PauseMenu/Inner/StageSelectButton").GetComponent<Button>();
                public static TextMeshProUGUI StageSelectButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/StageSelectButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button SettingsButton => GameObject.Find("Canvas/PauseMenu/Inner/SettingsButton").GetComponent<Button>();
                public static TextMeshProUGUI SettingsButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/SettingsButton/Label").GetComponent<TextMeshProUGUI>();

                public static Button QuitButton => GameObject.Find("Canvas/PauseMenu/Inner/QuitButton").GetComponent<Button>();
                public static TextMeshProUGUI QuitButtonLabel => GameObject.Find("Canvas/PauseMenu/Inner/QuitButton/Label").GetComponent<TextMeshProUGUI>();
            }


            public static class Background
            {
                public const string Root = "Background";
            }

            public static class Board
            {
                public static BoardInstance Instance => GameObject.Find("Board").GetComponent<BoardInstance>();
                public static BoardOverlay BoardOverlay => GameObject.Find("Board/BoardOverlay").GetComponent<BoardOverlay>();
                public static TargetModeOverlay TargetModeOverlay => GameObject.Find("Board/TargetModeOverlay").GetComponent<TargetModeOverlay>();
            }

            public static class ManaPool
            {
                public static ManaPoolManager Instance => GameObject.Find("ManaPoolManager").GetComponent<ManaPoolManager>();

                public static RectTransform Root => GameObject.Find("Canvas/ManaPool").GetComponent<RectTransform>();
                public static Button BankButton => GameObject.Find("Canvas/ManaPool/BankButton").GetComponent<Button>();

                // Hero bar uses an Image set to Filled (Horizontal, Left).
                // Back is a static background image that never changes.
                public static Image HeroBack => GameObject.Find("Canvas/ManaPool/HeroBar/Back").GetComponent<Image>();
                public static Image HeroFill => GameObject.Find("Canvas/ManaPool/HeroBar/Fill").GetComponent<Image>();

                // Enemy bar is optional (toggle from ManaPoolManager.showEnemyMana).
                public static Image EnemyBack => GameObject.Find("Canvas/ManaPool/EnemyBar/Back").GetComponent<Image>();
                public static Image EnemyFill => GameObject.Find("Canvas/ManaPool/EnemyBar/Fill").GetComponent<Image>();
            }

            public static class Card
            {
                public static RectTransform Rect => GameObject.Find("Canvas/Card").GetComponent<RectTransform>();
                public static ActorCard Instance => GameObject.Find("Canvas/Card").GetComponent<ActorCard>();
                public static RectTransform Backdrop => GameObject.Find("Canvas/Card/Backdrop").GetComponent<RectTransform>();
                public static RectTransform Portrait => GameObject.Find("Canvas/Card/Portrait").GetComponent<RectTransform>();
                public static RectTransform Title => GameObject.Find("Canvas/Card/Title").GetComponent<RectTransform>();
                public static RectTransform Details => GameObject.Find("Canvas/Card/Details").GetComponent<RectTransform>();
                public static RectTransform AbilityButtonContainer => GameObject.Find("Canvas/Card/AbilityButtonContainer").GetComponent<RectTransform>();



            }

           

            // New: AbilityCastConfirm helpers (mirror for renamed UI object)
            public static class AbilityCastConfirm
            {
                /// <summary>Finds the under canvas.</summary>
                private static Transform FindUnderCanvas(string path)
                {
                    var canvas = GameObject.Find("Canvas");
                    if (canvas == null) return null;
                    return canvas.transform.Find(path);
                }

                public static RectTransform Root
                {
                    get { var t = FindUnderCanvas("AbilityCastConfirm"); return t != null ? t.GetComponent<RectTransform>() : null; }
                }

                public static CanvasGroup CanvasGroup
                {
                    get { var t = FindUnderCanvas("AbilityCastConfirm"); return t != null ? t.GetComponent<CanvasGroup>() : null; }
                }

                public static TextMeshProUGUI Label
                {
                    get { var t = FindUnderCanvas("AbilityCastConfirm/Label"); return t != null ? t.GetComponent<TextMeshProUGUI>() : null; }
                }

                public static Button CancelButton
                {
                    get { var t = FindUnderCanvas("AbilityCastConfirm/CancelButton"); return t != null ? t.GetComponent<Button>() : null; }
                }

                public static Button CastButton
                {
                    get { var t = FindUnderCanvas("AbilityCastConfirm/CastButton"); return t != null ? t.GetComponent<Button>() : null; }
                }
            }

            public static class Timeline
            {
                public static RectTransform Root => GameObject.Find("Canvas/Timeline").GetComponent<RectTransform>();
                public static RectTransform Viewport => GameObject.Find("Canvas/Timeline/Viewport").GetComponent<RectTransform>();
                public static RectTransform Content => GameObject.Find("Canvas/Timeline/Viewport/Content").GetComponent<RectTransform>();
            }

            public static class AbilityBar
            {
                public static RectTransform Root => GameObject.Find("Canvas/AbilityBar").GetComponent<RectTransform>();
                public static TextMeshProUGUI Label => GameObject.Find("Canvas/AbilityBar/Label").GetComponent<TextMeshProUGUI>();
            }

            // NEW: CutoutOverlay helpers
            public static class CutoutOverlay
            {
                public const string Root = "Canvas/CutoutOverlay";
                public const string Top = Root + "/Top";
                public const string LeftPane = Top + "/LeftPane";
                public const string CenterPane = Top + "/CenterPane";
                public const string RightPane = Top + "/RightPane";
                public const string Bottom = Root + "/Bottom";

                public static RectTransform RootRect => GameObject.Find(Root).GetComponent<RectTransform>();
                public static Scripts.Canvas.CutoutOverlay Instance => GameObject.Find(Root).GetComponent<Scripts.Canvas.CutoutOverlay>();
                public static RectTransform TopRoot => GameObject.Find(Top).GetComponent<RectTransform>();
                public static RectTransform LeftPaneRect => GameObject.Find(LeftPane).GetComponent<RectTransform>();
                public static RectTransform CenterPaneRect => GameObject.Find(CenterPane).GetComponent<RectTransform>();
                public static RectTransform RightPaneRect => GameObject.Find(RightPane).GetComponent<RectTransform>();
                public static RectTransform BottomRoot => GameObject.Find(Bottom).GetComponent<RectTransform>();
                public static Image TopImage => GameObject.Find(Top).GetComponent<Image>();
                public static Image BottomImage => GameObject.Find(Bottom).GetComponent<Image>();
            }

        }

        // Add TimelineBlock prefab internal paths
        public static class TimelineBlock
        {
            public const string Back = "Back";
            public const string Mask = "Mask";
            public const string Portrait = "Mask/Portrait";
            public const string Label = "Label";
            public const string ActiveIndicator = "ActiveIndicator"; // renamed from Indicator
            public const string FocusIndicator = "FocusIndicator";   // renamed from Selection
        }

        public static class LoadingScreen
        {
            public static TextMeshProUGUI LoreText => GameObject.Find("Canvas/LoreText").GetComponent<TextMeshProUGUI>();

        }

        public static class PartyManager
        {
            public const string Title = "Canvas/Title";
            public const string AddRemovePartyMemberButton = "Canvas/AddRemovePartyMemberButton";
            public const string AddRemovePartyMemberButtonLabel = "Canvas/AddRemovePartyMemberButton/Label";
            public const string PartyMemberCountLabel = "Canvas/PartyMemberCountLabel";
            public const string StatsDisplay = "Canvas/StatsDisplay";
            public const string RosterPanel = "Canvas/RosterCarousel/Panel";
        }

        public static class Overworld
        {
            public static class Canvas
            {
                public const string Root = "Canvas";
                public const string Title = "Canvas/Title";
                public const string OffscreenArrow = "Canvas/OffscreenArrow";
                public const string VirtualJoystick = "Canvas/VirtualJoystick";
                public const string InputModeButton = "Canvas/InputModeButton";
                public const string InputModeImage = "Canvas/InputModeButton/Image";
                public const string InputModeLabel = "Canvas/InputModeButton/Label";

                public const string CameraModeButton = "Canvas/CameraModeButton";
                public const string CameraModeImage = "Canvas/CameraModeButton/Image";
                public const string CameraModeLabel = "Canvas/CameraModeButton/Label";

            }
            public static class Map
            {
                public const string Root = "Map";
                public const string Terrain = "Map/Terrain";
                public const string Surface = "Map/Surface";
                public const string Canopy = "Map/Canopy";
                public const string Hero = "Map/Heroes/Hero_00";
            }

            public const string BattleTransition = "BattleTransition";

        }

        public static class ProfileCreate
        {

        }


        public static class ProfileSelect
        {
            public const string Title = "Canvas/Title";
            public const string ScrollView = "Canvas/ScrollView";
            public const string Content = "Canvas/ScrollView/Viewport/Content";

        }

        public static class SplashScreen
        {
        }

        public static class Settings
        {
            public const string Title = "Canvas/Title";
            public const string ScrollView = "Canvas/ScrollView";
            public const string Content = "Canvas/ScrollView/Viewport/Content";
            public const string ActorPanMultiplier = "Canvas/ScrollView/Viewport/Content/ActorPanMultiplier";

            public static RectTransform ContentRect => GameObject.Find("Canvas/ScrollView/Viewport/Content").GetComponent<RectTransform>();


        }

        public static class StageSelect
        {
            public const string Title = "Canvas/Title";
            public const string ScrollView = "Canvas/ScrollView";
            public const string Content = "Canvas/ScrollView/Viewport/Content";
        }

        public static class TitleScreen
        {
            public const string Panel = "Canvas/Panel";
            public const string ContinueButton = "Canvas/Panel/ContinueButton";
            public const string LoadGameButton = "Canvas/Panel/LoadGameButton";
            public const string SettingsButton = "Canvas/Panel/SettingsButton";
            public const string CreditsButton = "Canvas/Panel/CreditsButton";
            public const string ProfileButton = "Canvas/ProfileButton";
            public const string ProfileButtonLabel = "Canvas/ProfileButton/Label";
        }

        public static class ConfirmationDialog
        {
            public const string ConfirmDialog = "Canvas/ConfirmationDialog";
            public const string Panel = "Canvas/ConfirmationDialog/Panel";
            public const string Prompt = "Canvas/ConfirmationDialog/Panel/Prompt";
            public const string ButtonYes = "Canvas/ConfirmationDialog/Panel/ButtonYes";
            public const string ButtonNo = "Canvas/ConfirmationDialog/Panel/ButtonNo";
        }

        public static class MessageBox
        {
            public const string ConfirmDialog = "Canvas/MessageBox";
            public const string Panel = "Canvas/MessageBox/Panel";
            public const string Prompt = "Canvas/MessageBox/Panel/Prompt";
            public const string ButtonOk = "Canvas/MessageBox/Panel/ButtonOk";
        }

        public static class KeyboardDialog
        {

            public const string Keyboard = "Canvas/Keyboard";
            public const string Panel = Keyboard + "/Panel";
            public const string Prompt = Panel + "/Prompt";
            public const string InputBackdrop = Panel + "/InputBackdrop";
            public const string InputLabel = Panel + "/InputLabel";
            public const string KeysContainer = Panel + "/KeysContainer";

            // Row 1: digits
            public const string Row1 = KeysContainer + "/Row1";
            public const string Key1 = Row1 + "/Key1";
            public const string Key2 = Row1 + "/Key2";
            public const string Key3 = Row1 + "/Key3";
            public const string Key4 = Row1 + "/Key4";
            public const string Key5 = Row1 + "/Key5";
            public const string Key6 = Row1 + "/Key6";
            public const string Key7 = Row1 + "/Key7";
            public const string Key8 = Row1 + "/Key8";
            public const string Key9 = Row1 + "/Key9";
            public const string Key0 = Row1 + "/Key0";

            // Row 2: QP
            public const string Row2 = KeysContainer + "/Row2";
            public const string KeyQ = Row2 + "/KeyQ";
            public const string KeyW = Row2 + "/KeyW";
            public const string KeyE = Row2 + "/KeyE";
            public const string KeyR = Row2 + "/KeyR";
            public const string KeyT = Row2 + "/KeyT";
            public const string KeyY = Row2 + "/KeyY";
            public const string KeyU = Row2 + "/KeyU";
            public const string KeyI = Row2 + "/KeyI";
            public const string KeyO = Row2 + "/KeyO";
            public const string KeyP = Row2 + "/KeyP";

            // Row 3: AL
            public const string Row3 = KeysContainer + "/Row3";
            public const string KeyA = Row3 + "/KeyA";
            public const string KeyS = Row3 + "/KeyS";
            public const string KeyD = Row3 + "/KeyD";
            public const string KeyF = Row3 + "/KeyF";
            public const string KeyG = Row3 + "/KeyG";
            public const string KeyH = Row3 + "/KeyH";
            public const string KeyJ = Row3 + "/KeyJ";
            public const string KeyK = Row3 + "/KeyK";
            public const string KeyL = Row3 + "/KeyL";

            // Row 4: ZM
            public const string Row4 = KeysContainer + "/Row4";
            public const string KeyZ = Row4 + "/KeyZ";
            public const string KeyX = Row4 + "/KeyX";
            public const string KeyC = Row4 + "/KeyC";
            public const string KeyV = Row4 + "/KeyV";
            public const string KeyB = Row4 + "/KeyB";
            public const string KeyN = Row4 + "/KeyN";
            public const string KeyM = Row4 + "/KeyM";

            // Row 5: CapsLock, Spacebar, Backspace, Enter
            public const string Row5 = KeysContainer + "/Row5";
            public const string KeyCapsLock = Row5 + "/KeyCapsLock";
            public const string KeySpace = Row5 + "/KeySpace";
            public const string KeyBackspace = Row5 + "/KeyBackspace";
            public const string KeyEnter = Row5 + "/KeyEnter";

            public const string ConfirmationContainer = Panel + "/ConfirmationContainer";
            public const string Confirmation = ConfirmationContainer + "/Confirmation";
            public const string ButtonYes = ConfirmationContainer + "/ButtonYes";
            public const string ButtonNo = ConfirmationContainer + "/ButtonNo";
        }

        public static class PostBattleScreen
        {
            public const string ScrollView = "Canvas/ScrollView";
            public const string Content = "Canvas/ScrollView/Viewport/Content";
            public const string NextButton = "Canvas/BottomBar/NextButton";
        }

        // Add Hub navigation + panel paths (use simple names; adjust if scene hierarchy differs).
        public static class Hub
        {
            // Buttons
            public const string PartyButton = "PartyButton";
            public const string ShopButton = "ShopButton";
            public const string MedicalButton = "MedicalButton";
            public const string ResidenceButton = "ResidenceButton";
            public const string BlacksmithButton = "BlacksmithButton";
            public const string OverworldButton = "OverworldButton";
            public const string BattleButton = "BattleButton";

            // Panels
            public const string PartyPanel = "PartyPanel";
            public const string ShopPanel = "ShopPanel";
            public const string MedicalPanel = "MedicalPanel";
            public const string ResidencePanel = "ResidencePanel";
            public const string BlacksmithPanel = "BlacksmithPanel";
        }

        #endregion
    }
}
