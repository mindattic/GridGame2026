using Assets.Helpers;
using Assets.Scripts.Canvas;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Libraries
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



            isLoaded = true;
        }

        // Returns the best-matching tag icon for a given ActorTag mask based on a priority list.
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
