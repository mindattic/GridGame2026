using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Libraries
{
    /// <summary>
    /// SPRITELIBRARY - Static registry for all game sprites.
    /// 
    /// PURPOSE:
    /// Centralized sprite loading and caching. All sprites are loaded once
    /// on first access and cached for performance.
    /// 
    /// CATEGORIES:
    /// - Backgrounds: Parallax background images
    /// - GUI: UI element sprites (buttons, panels, etc.)
    /// - Icons: 32x32 UI icon sprites (246 addressable icons)
    /// - Seamless: Tiling textures
    /// - Sprites: General game sprites
    /// - WeaponTypes: Weapon icon sprites
    /// - Leaves: Decorative leaf sprites
    /// - TutorialPages: Tutorial screen images
    /// - Logos: Brand/logo images
    /// - TagIcons: Timeline tag icons
    /// - AbilityButtons: Ability button icons
    /// 
    /// USAGE:
    /// ```csharp
    /// var sprite = SpriteLibrary.Sprites["Footstep"];
    /// var bg = SpriteLibrary.Backgrounds["Moors_0"];
    /// var icon = SpriteLibrary.GUI["DestinationMarker"];
    /// ```
    /// 
    /// LOADING:
    /// Uses AssetHelper.LoadAsset<Sprite>() to load from Resources.
    /// Sprites are lazy-loaded on first access to any category.
    /// 
    /// LLM CONTEXT:
    /// Use this instead of loading sprites directly. All factories and
    /// managers should reference sprites through this library.
    /// </summary>
    public static class SpriteLibrary
    {
        #region Dictionaries

        private static Dictionary<string, Sprite> actor;
        private static Dictionary<string, Sprite> backgrounds;
        private static Dictionary<string, Sprite> gui;
        private static Dictionary<string, Sprite> seamless;
        private static Dictionary<string, Sprite> sprites;
        private static Dictionary<string, Sprite> weaponTypes;
        private static Dictionary<string, Sprite> leaves;
        private static Dictionary<string, Sprite> tutorialPages;
        private static Dictionary<string, Sprite> logos;
        private static Dictionary<string, Sprite> actorTagIcon;
        private static Dictionary<string, Sprite> abilityButtons;
        private static Dictionary<string, Sprite> icons;
        private static bool isLoaded = false;

        #endregion

        #region Public Accessors

        /// <summary>Actor-related sprites (masks, frames, health bars, etc.).</summary>
        public static Dictionary<string, Sprite> Actor { get { if (!isLoaded) Load(); return actor; } }

        /// <summary>Background sprites for parallax layers.</summary>
        public static Dictionary<string, Sprite> Backgrounds { get { if (!isLoaded) Load(); return backgrounds; } }

        /// <summary>GUI element sprites (buttons, panels, icons).</summary>
        public static Dictionary<string, Sprite> GUI { get { if (!isLoaded) Load(); return gui; } }

        /// <summary>Seamless tiling textures.</summary>
        public static Dictionary<string, Sprite> Seamless { get { if (!isLoaded) Load(); return seamless; } }

        /// <summary>General game sprites (footsteps, tiles, etc.).</summary>
        public static Dictionary<string, Sprite> Sprites { get { if (!isLoaded) Load(); return sprites; } }

        /// <summary>Weapon type icon sprites.</summary>
        public static Dictionary<string, Sprite> WeaponTypes { get { if (!isLoaded) Load(); return weaponTypes; } }

        /// <summary>Decorative leaf sprites.</summary>
        public static Dictionary<string, Sprite> Leaves { get { if (!isLoaded) Load(); return leaves; } }

        /// <summary>Tutorial page images.</summary>
        public static Dictionary<string, Sprite> TutorialPages { get { if (!isLoaded) Load(); return tutorialPages; } }


        /// <summary>Logo/brand images.</summary>
        public static Dictionary<string, Sprite> Logos { get { if (!isLoaded) Load(); return logos; } }

        /// <summary>Timeline tag actor icons.</summary>
        public static Dictionary<string, Sprite> TagIcons { get { if (!isLoaded) Load(); return actorTagIcon; } }

        /// <summary>Ability button icon sprites.</summary>
        public static Dictionary<string, Sprite> AbilityButtons { get { if (!isLoaded) Load(); return abilityButtons; } }

        /// <summary>32x32 UI icon sprites (246 addressable icons).</summary>
        public static Dictionary<string, Sprite> Icons { get { if (!isLoaded) Load(); return icons; } }

        #endregion

        #region Loading

        /// <summary>
        /// Loads all sprite dictionaries. Called lazily on first access.
        /// </summary>
        private static void Load()
        {
            if (isLoaded) return;

            // Actor sprites (used by ActorFactory)
            actor = new Dictionary<string, Sprite>
            {
                // Masks
                { "Mask4", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Masks/mask-4") },
                { "Mask7", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Masks/mask-7") },
                // Base (quality layer)
                { "Base4", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Base/base-4") },
                // Back layer
                { "Back2", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Back/back-2") },
                // Frame
                { "Frame4", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Frames/frame-4") },
                // Glow (thumbnail-fade is in Thumbnails folder)
                { "ThumbnailFade", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Thumbnails/thumbnail-fade") },
                // Gradient
                { "Gradient", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Gradient/Gradient") },
                // Status
                { "StatusNone", AssetHelper.LoadAsset<Sprite>("Sprites/Statuses/status-none") },
                // Health bars
                { "HealthBar5", AssetHelper.LoadAsset<Sprite>("Sprites/HealthBar/health-bar-5") },
                { "HealthBarBack3", AssetHelper.LoadAsset<Sprite>("Sprites/HealthBar/health-bar-back-3") },
                { "HealthBar3", AssetHelper.LoadAsset<Sprite>("Sprites/HealthBar/health-bar-3") },
                { "ActionBar2", AssetHelper.LoadAsset<Sprite>("Sprites/ActionBar/action-bar-2") },
                // Radial (in ActionBar folder)
                { "RingBack1", AssetHelper.LoadAsset<Sprite>("Sprites/ActionBar/ring-back-1") },
                { "Ring1", AssetHelper.LoadAsset<Sprite>("Sprites/ActionBar/ring-1") },
                // Armor (all four directions)
                { "ArmorNorth", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Armor/armor-north") },
                { "ArmorEast", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Armor/armor-east") },
                { "ArmorSouth", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Armor/armor-south") },
                { "ArmorWest", AssetHelper.LoadAsset<Sprite>("Sprites/Actor/Armor/armor-west") },
                // Indicators (in root Sprites folder)
                { "ActiveIndicator", AssetHelper.LoadAsset<Sprite>("Sprites/active-indicator") },
                { "FocusIndicator", AssetHelper.LoadAsset<Sprite>("Sprites/focus-indicator") },
                { "TargetIndicator", AssetHelper.LoadAsset<Sprite>("Sprites/target-indicator") },
                // NOTE: Thumbnail sprites are loaded dynamically from Portraits based on CharacterClass
            };

            abilityButtons = new Dictionary<string, Sprite>
            {
                { "Heal", AssetHelper.LoadAsset<Sprite>("Sprites/AbilityButtons/Heal") },
                { "ShieldBash", AssetHelper.LoadAsset<Sprite>("Sprites/AbilityButtons/ShieldBash") },
                { "Trap", AssetHelper.LoadAsset<Sprite>("Sprites/AbilityButtons/Trap") },
                { "Smite", AssetHelper.LoadAsset<Sprite>("Sprites/AbilityButtons/Smite") },
            };

            backgrounds = new Dictionary<string, Sprite>();
            var backgroundSets = new (BackgroundSet set, int count)[]
            {
                (BackgroundSet.Moors, 5),
                (BackgroundSet.RedThorns, 5),
                (BackgroundSet.UnderTheBridge, 4),
                (BackgroundSet.CyberNecropolis, 16),
                (BackgroundSet.ElectricWasteland, 5),
            };

            foreach (var (set, count) in backgroundSets)
            {
                string name = set.ToString();
                for (int i = 0; i < count; i++)
                {
                    var key = $"{name}.{i:D2}";
                    var path = $"Sprites/Backgrounds/{name}/{i:D2}";
                    backgrounds[key] = AssetHelper.LoadAsset<Sprite>(path);
                }
            }

            gui = new Dictionary<string, Sprite>
            {
                { "BackButton", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/BackButton") },
                { "Indicator", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/Indicator") },
                { "PortraitMask", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/PortraitMask") },
                { "TeamIcon", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/TeamIcon") },
                { "Joystick00", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/Joystick00") },
                { "Joystick01", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/Joystick01") },
                { "Joystick02", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/Joystick02") },
                { "Camera00", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/Camera00") },
                { "Camera01", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/Camera01") },
                { "DestinationMarker", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/DestinationMarker") },
                { "TimelineBlock", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/TimelineBlock") },
                { "TimelineDivider", AssetHelper.LoadAsset<Sprite>("Sprites/GUI/TimelineDivider") },
            };

            leaves = new Dictionary<string, Sprite>
            {
                { "Leaf1", AssetHelper.LoadAsset<Sprite>("Sprites/Leaves/Leaf1") },
                { "Leaf2", AssetHelper.LoadAsset<Sprite>("Sprites/Leaves/Leaf2") },
                { "MapleLeaf1", AssetHelper.LoadAsset<Sprite>("Sprites/Leaves/MapleLeaf1") },
                { "MapleLeaf2", AssetHelper.LoadAsset<Sprite>("Sprites/Leaves/MapleLeaf2") },
            };

            logos = new Dictionary<string, Sprite>
            {
                { "Mindattic.64x64", AssetHelper.LoadAsset<Sprite>("Sprites/Logos/Mindattic.64x64") },
                { "Mindattic.128x128", AssetHelper.LoadAsset<Sprite>("Sprites/Logos/Mindattic.128x128") },
                { "Mindattic.256x256", AssetHelper.LoadAsset<Sprite>("Sprites/Logos/Mindattic.256x256") },
                { "Mindattic.512x512", AssetHelper.LoadAsset<Sprite>("Sprites/Logos/Mindattic.512x512") },
                { "Mindattic.1024x1024", AssetHelper.LoadAsset<Sprite>("Sprites/Logos/Mindattic.1024x1024") },
            };

            seamless = new Dictionary<string, Sprite>
            {
                { "BlackFire1", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/BlackFire1") },
                { "BlackFire2", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/BlackFire2") },
                { "Fire1", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/Fire1") },
                { "RedFire1", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/RedFire1") },
                { "Swords1", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/Swords1") },
                { "Swords2", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/Swords2") },
                { "WhiteFire1", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/WhiteFire1") },
                { "WhiteFire2", AssetHelper.LoadAsset<Sprite>("Sprites/Seamless/WhiteFire2") },
            };

                        sprites = new Dictionary<string, Sprite>
                        {
                            { "Coin", AssetHelper.LoadAsset<Sprite>("Sprites/Coin") },
                            { "DottedLine", AssetHelper.LoadAsset<Sprite>("Sprites/DottedLine") },
                            { "DottedLineArrow", AssetHelper.LoadAsset<Sprite>("Sprites/DottedLineArrow") },
                            { "DottedLineTurn", AssetHelper.LoadAsset<Sprite>("Sprites/DottedLineTurn") },
                            { "Footstep", AssetHelper.LoadAsset<Sprite>("Sprites/Footstep") },
                            { "Pause", AssetHelper.LoadAsset<Sprite>("Sprites/Pause") },
                            { "Paused", AssetHelper.LoadAsset<Sprite>("Sprites/Paused") },
                            { "Forest", AssetHelper.LoadAsset<Sprite>("Sprites/Forest") },
                            { "Black32x32", AssetHelper.LoadAsset<Sprite>("Sprites/Black32x32") },
                { "SynergySpark", AssetHelper.LoadAsset<Sprite>("Sprites/SynergySpark") },
                { "White32x32", AssetHelper.LoadAsset<Sprite>("Sprites/White32x32") },
                { "Transparent32x32", AssetHelper.LoadAsset<Sprite>("Sprites/Transparent32x32") },
                { "Tile", AssetHelper.LoadAsset<Sprite>("Sprites/Tiles/tile3") },
            };

            weaponTypes = new Dictionary<string, Sprite>
            {
                { "Bow", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Bow") },
                { "Claw", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Claw") },
                { "Crossbow", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Crossbow") },
                { "Dagger", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Dagger") },
                { "Grenade", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Grenade") },
                { "Hammer", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Hammer") },
                { "Katana", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Katana") },
                { "Mace", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Mace") },
                { "Pistol", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Pistol") },
                { "Polearm", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Polearm") },
                { "Potion", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Potion") },
                { "Scythe", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Scythe") },
                { "Shield", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Shield") },
                { "Shuriken", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Shuriken") },
                { "Spear", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Spear") },
                { "Staff", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Staff") },
                { "Sword", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Sword") },
                { "Wand", AssetHelper.LoadAsset<Sprite>("Sprites/WeaponTypes/Wand") },
            };

            tutorialPages = new Dictionary<string, Sprite>
            {
                { "Tutorial.1-1", AssetHelper.LoadAsset<Sprite>("Sprites/TutorialPages/Tutorial.1-1") },
                { "Tutorial.1-2", AssetHelper.LoadAsset<Sprite>("Sprites/TutorialPages/Tutorial.1-2") },
                { "Tutorial.1-3", AssetHelper.LoadAsset<Sprite>("Sprites/TutorialPages/Tutorial.1-3") },
            };

            actorTagIcon = new Dictionary<string, Sprite>
            {
                { "Beast", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Beast") },
                { "Enemy", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Enemy") },
                { "Flying", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Flying") },
                { "Goblin", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Goblin") },
                { "Hero", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Hero") },
                { "Insect", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Insect") },
                { "Soldier", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Soldier") },
                { "Undead", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Undead") },
                { "Unknown", AssetHelper.LoadAsset<Sprite>("Sprites/Timeline/ActorTagIcons/Unknown") },
            };

            // 32x32 UI icons (246 addressable sprites)
            icons = new Dictionary<string, Sprite>
            {
                { "16Colors", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/16Colors") },
                { "256Colors", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/256Colors") },
                { "3DBarChart", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/3DBarChart") },
                { "3DChart", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/3DChart") },
                { "3DGraph", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/3DGraph") },
                { "About", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/About") },
                { "Add", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Add") },
                { "AddFolder", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/AddFolder") },
                { "Angle", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Angle") },
                { "Apply", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Apply") },
                { "Arc", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Arc") },
                { "Arrow", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Arrow") },
                { "Attach", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Attach") },
                { "Back", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Back") },
                { "BitmapEditor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/BitmapEditor") },
                { "Brightness", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Brightness") },
                { "Brush", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Brush") },
                { "Camera", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Camera") },
                { "Cancel", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Cancel") },
                { "ChartXy", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ChartXy") },
                { "CheckBoxes", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/CheckBoxes") },
                { "Circle", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Circle") },
                { "Clear", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Clear") },
                { "Clipboard", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Clipboard") },
                { "Close", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Close") },
                { "CloseFile", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/CloseFile") },
                { "CloseFolder", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/CloseFolder") },
                { "CMYK", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/CMYK") },
                { "Coffe", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Coffe") },
                { "Coffee", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Coffee") },
                { "Color", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Color") },
                { "ColorBalance", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ColorBalance") },
                { "ColorFilter", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ColorFilter") },
                { "ColorLayers", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ColorLayers") },
                { "ColorPalette", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ColorPalette") },
                { "ColorProfile", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ColorProfile") },
                { "ColorTest", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ColorTest") },
                { "Comment", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Comment") },
                { "Contrast", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Contrast") },
                { "Copy", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Copy") },
                { "Create", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Create") },
                { "CriticalDetails", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/CriticalDetails") },
                { "Curve", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Curve") },
                { "CurvePoints", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/CurvePoints") },
                { "Cut", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Cut") },
                { "Danger", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Danger") },
                { "DecreaseTime", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/DecreaseTime") },
                { "Delete", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Delete") },
                { "DeleteFrame", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/DeleteFrame") },
                { "DeleteFrames", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/DeleteFrames") },
                { "Designer", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Designer") },
                { "Diagram", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Diagram") },
                { "Down", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Down") },
                { "Download", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Download") },
                { "DownloadImage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/DownloadImage") },
                { "Dropper", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Dropper") },
                { "Edit", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Edit") },
                { "EditPage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/EditPage") },
                { "EditText", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/EditText") },
                { "Ellipse", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Ellipse") },
                { "EMail", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/EMail") },
                { "Equipment", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Equipment") },
                { "Erase", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Erase") },
                { "Eraser", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Eraser") },
                { "Error", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Error") },
                { "Exit", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Exit") },
                { "Export", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Export") },
                { "Favourites", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Favourites") },
                { "Feather", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Feather") },
                { "FileExetension", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/FileExetension") },
                { "Fill", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Fill") },
                { "Find", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Find") },
                { "FineBrush", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/FineBrush") },
                { "Flip", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Flip") },
                { "FlipHorizontally", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/FlipHorizontally") },
                { "FlipVertically", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/FlipVertically") },
                { "FlowBlock", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/FlowBlock") },
                { "Flower", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Flower") },
                { "Folder", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Folder") },
                { "Form", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Form") },
                { "Forward", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Forward") },
                { "Frames", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Frames") },
                { "Funnel", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Funnel") },
                { "GoDown", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/GoDown") },
                { "GoUp", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/GoUp") },
                { "Gpadient", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Gpadient") },
                { "GraphicDesigner", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/GraphicDesigner") },
                { "GraphicFile", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/GraphicFile") },
                { "GraphicTools", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/GraphicTools") },
                { "Grid", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Grid") },
                { "Help", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Help") },
                { "HelpBook", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/HelpBook") },
                { "Hexagon", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Hexagon") },
                { "Hide", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Hide") },
                { "Hint", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Hint") },
                { "Hints", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Hints") },
                { "Home", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Home") },
                { "Homepage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Homepage") },
                { "HSL", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/HSL") },
                { "HSV", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/HSV") },
                { "Ico", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Ico") },
                { "IconWizard", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/IconWizard") },
                { "Import", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Import") },
                { "IncreaseTime", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/IncreaseTime") },
                { "Index", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Index") },
                { "Info", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Info") },
                { "Key", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Key") },
                { "Knife", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Knife") },
                { "LABColorModel", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/LABColorModel") },
                { "Layers", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Layers") },
                { "Left", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Left") },
                { "LeftRight", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/LeftRight") },
                { "Line", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Line") },
                { "List", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/List") },
                { "Lock", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Lock") },
                { "LockColor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/LockColor") },
                { "LockTransparency", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/LockTransparency") },
                { "MagicHat", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/MagicHat") },
                { "Measure", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Measure") },
                { "MicrosoftFlag", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/MicrosoftFlag") },
                { "Monitor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Monitor") },
                { "Monitors", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Monitors") },
                { "Mouse", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Mouse") },
                { "MousePointer", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/MousePointer") },
                { "Move", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Move") },
                { "Movie", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Movie") },
                { "NewClipArt", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/NewClipArt") },
                { "NewFile", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/NewFile") },
                { "NewFrame", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/NewFrame") },
                { "NewFrame1", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/NewFrame1") },
                { "NewImage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/NewImage") },
                { "NewImagelist", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/NewImagelist") },
                { "NewVideo", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/NewVideo") },
                { "No", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/No") },
                { "Objects", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Objects") },
                { "Ok", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Ok") },
                { "Open", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Open") },
                { "OpenColors", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/OpenColors") },
                { "OpenFile", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/OpenFile") },
                { "OpenV2", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/OpenV2") },
                { "Painter", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Painter") },
                { "PaintOverPixels", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/PaintOverPixels") },
                { "Pantone", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Pantone") },
                { "Paste", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Paste") },
                { "Pen", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Pen") },
                { "Pencil", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Pencil") },
                { "PickColor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/PickColor") },
                { "Picture", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Picture") },
                { "PieChart", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/PieChart") },
                { "Pin", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Pin") },
                { "Pinion", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Pinion") },
                { "PixelEditor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/PixelEditor") },
                { "Pixels", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Pixels") },
                { "Play", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Play") },
                { "Preview", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Preview") },
                { "Print", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Print") },
                { "Problem", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Problem") },
                { "Properties", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Properties") },
                { "RedBook", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RedBook") },
                { "RedEyeRemoving", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RedEyeRemoving") },
                { "Redo", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Redo") },
                { "Refresh", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Refresh") },
                { "Registration", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Registration") },
                { "Registry", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Registry") },
                { "Rename", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Rename") },
                { "ReplacePixels", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ReplacePixels") },
                { "ResizeImage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ResizeImage") },
                { "Restangle", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Restangle") },
                { "Revert", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Revert") },
                { "RGB", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RGB") },
                { "Right", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Right") },
                { "RotateCCW", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RotateCCW") },
                { "RotateCW", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RotateCW") },
                { "RotateLeft", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RotateLeft") },
                { "RotateRight", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RotateRight") },
                { "Rotation", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Rotation") },
                { "RoundedRectangle", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/RoundedRectangle") },
                { "Save", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Save") },
                { "SaveAs", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SaveAs") },
                { "SaveColor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SaveColor") },
                { "SaveData", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SaveData") },
                { "SaveImage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SaveImage") },
                { "SavePicture", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SavePicture") },
                { "ScanFilm", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ScanFilm") },
                { "ScanImage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ScanImage") },
                { "Scanner", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Scanner") },
                { "Scenario", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Scenario") },
                { "Script", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Script") },
                { "Search", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Search") },
                { "SearchComputer", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SearchComputer") },
                { "SearchFolder", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SearchFolder") },
                { "SearchOnline", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SearchOnline") },
                { "SearchText", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SearchText") },
                { "SelectGpadient", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SelectGpadient") },
                { "Selection", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Selection") },
                { "Settings", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Settings") },
                { "Sharpness", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Sharpness") },
                { "Show", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Show") },
                { "Sizes", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Sizes") },
                { "Smooth", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Smooth") },
                { "SmoothLine", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SmoothLine") },
                { "SpellChecking", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/SpellChecking") },
                { "Spiral", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Spiral") },
                { "Spray", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Spray") },
                { "Square", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Square") },
                { "Stop", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Stop") },
                { "StopPlaying", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/StopPlaying") },
                { "Synchronize", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Synchronize") },
                { "Tag", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Tag") },
                { "Target", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Target") },
                { "Target1", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Target1") },
                { "TestLine", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TestLine") },
                { "TextColor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TextColor") },
                { "TextReplace", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TextReplace") },
                { "TextTool", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TextTool") },
                { "Time", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Time") },
                { "TipOfTheDay", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TipOfTheDay") },
                { "ToDoList", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ToDoList") },
                { "Tools", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Tools") },
                { "Touch", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Touch") },
                { "Transparency", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Transparency") },
                { "TransparentBackground", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TransparentBackground") },
                { "TransparentColor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TransparentColor") },
                { "Triangle", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Triangle") },
                { "TrueColor", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/TrueColor") },
                { "Undo", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Undo") },
                { "Units", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Units") },
                { "Unlock", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Unlock") },
                { "Up", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Up") },
                { "UpDown", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/UpDown") },
                { "UploadImage", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/UploadImage") },
                { "Wait", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Wait") },
                { "Warning", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Warning") },
                { "Webcam", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Webcam") },
                { "WebDesigner", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/WebDesigner") },
                { "WideBrush", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/WideBrush") },
                { "Wizard", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Wizard") },
                { "WorkArea", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/WorkArea") },
                { "WritingPencil", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/WritingPencil") },
                { "Wrong", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Wrong") },
                { "Yes", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Yes") },
                { "YUVColorSpace", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/YUVColorSpace") },
                { "Zoom", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/Zoom") },
                { "ZoomAuto", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ZoomAuto") },
                { "ZoomIn", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ZoomIn") },
                { "ZoomOut", AssetHelper.LoadAsset<Sprite>("Sprites/Icons/32x32/ZoomOut") },
            };

            isLoaded = true;
        }

        // Returns the best-matching tag icon for a given ActorTag mask based on a priority list.
        /// <summary>Gets the actor tag icon.</summary>
        public static Sprite GetActorTagIcon(ActorTag tags)
        {
            if (!isLoaded) Load();

            if (actorTagIcon == null) return null;

            // Ordered priority from most-specific / desirable to least
            var priority = new ActorTag[] {
                ActorTag.Hero,
                ActorTag.Boss,
                ActorTag.Elite,
                ActorTag.Dragonkin,
                ActorTag.Demonkin,
                ActorTag.Undead,
                ActorTag.Beast,
                ActorTag.Humanoid,
                ActorTag.Mechanical,
                ActorTag.Flying,
                ActorTag.Insect,
                ActorTag.Elemental,
                ActorTag.Magic,
                ActorTag.Construct,
                ActorTag.Aquatic,
                ActorTag.PlantBased,
                ActorTag.ShadowCreature,
                ActorTag.Soldier,
                ActorTag.Goblin,
                ActorTag.Healer,
                ActorTag.Enemy
            };

            foreach (var tag in priority)
            {
                if ((tags & tag) == tag)
                {
                    var key = tag.ToString();
                    if (actorTagIcon.TryGetValue(key, out var s) && s != null) return s;
                }
            }

            return actorTagIcon["Unknown"];
        }

        #endregion
    }
}
