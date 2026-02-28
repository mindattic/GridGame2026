using Assets.Helpers;
using Game.Models.Profile;

namespace Assets.Helpers
{
    /// <summary>
    /// SETTINGSHELPER - Typed access to player settings.
    /// 
    /// PURPOSE:
    /// Provides strongly-typed property accessors for player settings
    /// stored in ProfileHelper.CurrentProfile.Settings with null-safe
    /// fallbacks to default values.
    /// 
    /// SETTINGS AVAILABLE:
    /// - DragSensitivity: Touch/mouse drag responsiveness
    /// - GameSpeed: Global game speed multiplier
    /// - CoinCountMultiplier: Coin reward scaling
    /// - ApplyMovementTilt: Enable actor tilt during movement
    /// - ReloadThumbnailSettings: Debug setting for sprite reload
    /// 
    /// NULL SAFETY:
    /// All getters return default values if profile/settings is null.
    /// All setters are no-ops if settings is null.
    /// 
    /// USAGE:
    /// ```csharp
    /// using s = Assets.Helpers.SettingsHelper;
    /// 
    /// float speed = s.GameSpeed;
    /// s.DragSensitivity = 0.1f;
    /// ```
    /// 
    /// RELATED FILES:
    /// - ProfileHelper.cs: Owns the settings data
    /// - ProfileSettings.cs: Settings data model
    /// - SettingsManager.cs: Settings UI screen
    /// </summary>
    public static class SettingsHelper
    {
        #region Profile Access

        private static Profile CurrentProfile => ProfileHelper.CurrentProfile;
        private static ProfileSettings Settings => CurrentProfile?.Settings;

        #endregion

        #region Settings Properties

        /// <summary>Touch/mouse drag responsiveness.</summary>
        public static float DragSensitivity
        {
            get => Settings?.DragSensitivity ?? ProfileHelper.DefaultSettings.DragSensitivity;
            set
            {
                if (Settings != null)
                {
                    Settings.DragSensitivity = value;
                }
            }
        }

        /// <summary>Coin reward scaling multiplier.</summary>
        public static float CoinCountMulitiplier
        {
            get => Settings?.CoinCountMultiplier ?? ProfileHelper.DefaultSettings.CoinCountMultiplier;
            set
            {
                if (Settings != null)
                {
                    Settings.CoinCountMultiplier = value;
                }
            }
        }

        /// <summary>Debug: Force thumbnail sprite reload.</summary>
        public static bool ReloadThumbnailSettings
        {
            get => Settings?.ReloadThumbnailSettings ?? ProfileHelper.DefaultSettings.ReloadThumbnailSettings;
            set
            {
                if (Settings != null)
                {
                    Settings.ReloadThumbnailSettings = value;
                }
            }
        }

        /// <summary>Global game speed multiplier (1.0 = normal).</summary>
        public static float GameSpeed
        {
            get => Settings?.GameSpeed ?? ProfileHelper.DefaultSettings.GameSpeed;
            set
            {
                if (Settings != null)
                {
                    Settings.GameSpeed = value;
                }
            }
        }

        /// <summary>Enable actor tilt during drag movement.</summary>
        public static bool ApplyMovementTilt
        {
            get => Settings?.ApplyMovementTilt ?? ProfileHelper.DefaultSettings.ApplyMovementTilt;
            set
            {
                if (Settings != null)
                {
                    Settings.ApplyMovementTilt = value;
                }
            }
        }

        #endregion
    }
}
