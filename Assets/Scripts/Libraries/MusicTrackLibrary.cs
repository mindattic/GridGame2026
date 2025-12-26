using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class MusicTrackLibrary
    {
        private static Dictionary<string, AudioClip> musicTracks;
        private static bool isLoaded = false;

        public static Dictionary<string, AudioClip> MusicTracks
        {
            get
            {
                if (!isLoaded)
                    Load();
                return musicTracks;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;
            musicTracks = new Dictionary<string, AudioClip>
            {
                { "MelancholyLull", AssetHelper.LoadAsset<AudioClip>("MusicTracks/MelancholyLull") }
            };
            isLoaded = true;
        }

        /// <summary>
        /// Retrieves a single music track asynchronously by key.
        /// </summary>
        public static AudioClip Get(string key)
        {
            if (!isLoaded) Load();
            if (musicTracks.TryGetValue(key, out var clip))
                return clip;

            Debug.LogError($"Music track '{key}' not found in MusicTrackRepo.");
            return null;
        }
    }
}
