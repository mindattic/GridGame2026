using Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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
    /// TUTORIALLIBRARY - Registry of tutorial content.
    /// 
    /// PURPOSE:
    /// Defines multi-page tutorials that teach players
    /// game mechanics through text and images.
    /// 
    /// STRUCTURE:
    /// - Key: Tutorial identifier
    /// - Pages: List of TutorialPage (image + text)
    /// 
    /// USAGE:
    /// ```csharp
    /// var tutorial = TutorialLibrary.Get("Tutorial1");
    /// TutorialPopup.Show(tutorial);
    /// ```
    /// 
    /// RELATED FILES:
    /// - Tutorial.cs: Tutorial data structure
    /// - TutorialPopup.cs: Tutorial display UI
    /// </summary>
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

        /// <summary>Load.</summary>
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

        /// <summary>Get.</summary>
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
