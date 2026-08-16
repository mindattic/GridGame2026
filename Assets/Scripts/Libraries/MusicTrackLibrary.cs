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

        /// <summary>Load. Keys are the MusicDirector track keys ("Title"/"Vendor"/"Battle"/
        /// "Victory"/"Defeat") — the Jukebox tries an authored track first and falls back to
        /// ChiptuneBank when a key has none (US-137 / GG-A5). Every authored track's license
        /// + attribution lives in <see cref="Scripts.Data.AudioCredits"/>, rendered in the
        /// Credits scene — add a row there whenever a track is added here.</summary>
        private static void Load()
        {
            if (isLoaded) return;
            musicTracks = new Dictionary<string, AudioClip>
            {
                { "Title",   AssetHelper.LoadAsset<AudioClip>("MusicTracks/TellerOfTheTales") },
                { "Vendor",  AssetHelper.LoadAsset<AudioClip>("MusicTracks/MinstrelGuild") },
                { "Battle",  AssetHelper.LoadAsset<AudioClip>("MusicTracks/Crusade") },
                { "Victory", AssetHelper.LoadAsset<AudioClip>("MusicTracks/Triumph") },
                { "Defeat",  AssetHelper.LoadAsset<AudioClip>("MusicTracks/MelancholyLull") },
            };
            isLoaded = true;
        }

        /// <summary>The authored track for a MusicDirector key, or null (quietly) when the key
        /// has no authored bed — the Jukebox then falls back to generated chiptune.</summary>
        public static AudioClip Get(string key)
        {
            if (!isLoaded) Load();
            return !string.IsNullOrEmpty(key) && musicTracks.TryGetValue(key, out var clip) ? clip : null;
        }
    }
}
