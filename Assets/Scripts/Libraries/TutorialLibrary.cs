using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Libraries
{
    public static class TutorialLibrary
    {
        private static Dictionary<string, Tutorial> tutorials;
        private static bool isLoaded = false;

        public static Dictionary<string, Tutorial> Tutorials
        {
            get
            {
                if (!isLoaded)
                    Load();
                return tutorials;
            }
        }

        private static void Load()
        {
            if (isLoaded) return;
            tutorials = new Dictionary<string, Tutorial>
            {
                { "Tutorial1", new Tutorial
                    {
                        Key = "Tutorial1",
                        Pages = new List<TutorialPage>
                        {
                            new TutorialPage { TextureKey = "Tutorial.1-1", Title = "Tutorial 1-1", Content = "This is the first page of the tutorial." },
                            new TutorialPage { TextureKey = "Tutorial.1-2", Title = "Tutorial 1-2", Content = "This is the second page of the tutorial." },
                            new TutorialPage { TextureKey = "Tutorial.1-3", Title = "Tutorial 1-3", Content = "This is the third page of the tutorial." }
                        }
                    }
                }
            };
            isLoaded = true;
        }

        public static Tutorial Get(string key)
        {
            if (!isLoaded) Load();
            if (!tutorials.ContainsKey(key))
            {
                Debug.LogError($"Tutorial with key `{key}` not found.");
                return null;
            }
            return new Tutorial(tutorials[key]);
        }
    }
}
