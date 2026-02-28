using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// TUTORIAL - Multi-page tutorial data.
    /// 
    /// PURPOSE:
    /// Contains a collection of tutorial pages that teach
    /// players game mechanics through text and images.
    /// 
    /// PROPERTIES:
    /// - Key: Unique identifier
    /// - Pages: List of TutorialPage content
    /// 
    /// RELATED FILES:
    /// - TutorialLibrary.cs: Tutorial registry
    /// - TutorialPopup.cs: Tutorial display UI
    /// </summary>
    [Serializable]
    public class Tutorial
    {
        public string Key;
        public List<TutorialPage> Pages = new List<TutorialPage>();

        public Tutorial() { }

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

    /// <summary>
    /// TUTORIALPAGE - Single page of tutorial content.
    /// 
    /// PROPERTIES:
    /// - TextureKey: Image resource key
    /// - Title: Page title text
    /// - Content: Page body text
    /// </summary>
    [Serializable]
    public class TutorialPage
    {
        public string TextureKey;
        public string Title;
        public string Content;

        public TutorialPage() { }

        public TutorialPage(TutorialPage other)
        {
            if (other == null) return;

            TextureKey = other.TextureKey;
            Title = other.Title;
            Content = other.Content;
        }
    }
}
