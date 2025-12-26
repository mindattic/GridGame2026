using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class Tutorial
    {
        public string Key;
        public List<TutorialPage> Pages = new List<TutorialPage>();

        // Default constructor
        public Tutorial() { }

        // Copy constructor
        public Tutorial(Tutorial other)
        {
            if (other == null) return;

            Key = other.Key;
            Pages = new List<TutorialPage>();
            foreach (var page in other.Pages)
            {
                Pages.Add(new TutorialPage(page));
            }
        }
    }

    [Serializable]
    public class TutorialPage
    {
        public string TextureKey;
        public string Title;
        public string Content;

        // Default constructor
        public TutorialPage() { }

        // Copy constructor
        public TutorialPage(TutorialPage other)
        {
            if (other == null) return;

            TextureKey = other.TextureKey;
            Title = other.Title;
            Content = other.Content;
        }
    }
}
