using Assets.Helpers;
using Game.Models.Profile;

namespace Assets.Helpers
{
    public static class SettingsHelper
    {

        private static Profile CurrentProfile => ProfileHelper.CurrentProfile;
        private static ProfileSettings Settings => CurrentProfile?.Settings;

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
    }
}
