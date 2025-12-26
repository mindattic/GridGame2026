using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class SoundEffectLibrary
    {
        private static Dictionary<string, AudioClip> soundEffects;
        private static bool isLoaded = false;

        public static Dictionary<string, AudioClip> SoundEffects
        {
            get
            {
                if (!isLoaded)
                    Load();
                return soundEffects;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;
            soundEffects = new Dictionary<string, AudioClip>
            {
                { "Click",      AssetHelper.LoadAsset<AudioClip>("SoundEffects/Click") },
                { "Death",      AssetHelper.LoadAsset<AudioClip>("SoundEffects/Death") },
                { "Defeat",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Defeat") },
                { "Heal",       AssetHelper.LoadAsset<AudioClip>("SoundEffects/Heal") },
                { "Move00",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Move00") },
                { "Move01",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Move01") },
                { "Move02",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Move02") },
                { "Move03",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Move03") },
                { "Move04",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Move04") },
                { "Move05",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Move05") },
                { "NextTurn",   AssetHelper.LoadAsset<AudioClip>("SoundEffects/NextTurn") },
                { "Portrait",   AssetHelper.LoadAsset<AudioClip>("SoundEffects/Portrait") },
                { "Rumble",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Rumble") },
                { "Select",     AssetHelper.LoadAsset<AudioClip>("SoundEffects/Select") },
                { "Slash00",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slash00") },
                { "Slash01",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slash01") },
                { "Slash02",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slash02") },
                { "Slash03",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slash03") },
                { "Slash04",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slash04") },
                { "Slash05",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slash05") },
                { "Slash06",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slash06") },
                { "Slide",      AssetHelper.LoadAsset<AudioClip>("SoundEffects/Slide") },
                { "Victory",    AssetHelper.LoadAsset<AudioClip>("SoundEffects/Victory") }
            };
            isLoaded = true;
        }

        /// <summary>
        /// Retrieves a sound effect by key.
        /// </summary>
        public static AudioClip Get(string key)
        {
            if (!isLoaded) Load();
            if (soundEffects.TryGetValue(key, out var clip))
                return clip;

            Debug.LogError($"SoundEffect '{key}' not found in SoundEffectRepo.");
            return null;
        }
    }
}
