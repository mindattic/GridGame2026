using Scripts.Helpers;
using Scripts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Libraries
{
    /// <summary>
    /// MUSICTRACKLIBRARY - Registry of background music tracks.
    /// 
    /// PURPOSE:
    /// Lazy-loads and caches AudioClip references for
    /// background music used in scenes.
    /// 
    /// USAGE:
    /// ```csharp
    /// var track = MusicTrackLibrary.Get("MelancholyLull");
    /// AudioManager.PlayMusic(track);
    /// ```
    /// 
    /// RELATED FILES:
    /// - AudioManager.cs: Music playback
    /// - Resources/MusicTracks/: Audio files
    /// </summary>
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
