using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Models;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
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

namespace Scripts.Canvas
{
    /// <summary>
    /// TUTORIALPOPUP - Multi-page tutorial display.
    /// 
    /// PURPOSE:
    /// Displays tutorial content with images and text,
    /// supporting multi-page navigation.
    /// 
    /// FEATURES:
    /// - Show/hide tutorial panel
    /// - Navigate between pages
    /// - Display title, content, and images
    /// - Previous/Next/Close buttons
    /// 
    /// USAGE:
    /// ```csharp
    /// g.TutorialPopup.Show("Tutorial1");
    /// ```
    /// 
    /// RELATED FILES:
    /// - TutorialLibrary.cs: Tutorial content
    /// - TutorialModels.cs: Data structures
    /// - TutorialPopupFactory.cs: UI creation
    /// </summary>
    public class TutorialPopup : MonoBehaviour
    {
        #region Components

        private GameObject panel;
        private Image image;
        private TextMeshProUGUI title;
        private TextMeshProUGUI content;
        private Button previousButton;
        private Button nextButton;
        private Button closeButton;

        #endregion

        #region State

        private List<TutorialPage> pages = new List<TutorialPage>();
        private int currentPage = 0;
        private bool initialized;

        bool hasPages => pages != null && pages.Count > 0;
        int lastPage => pages != null && pages.Count > 0 ? pages.Count - 1 : 0;

        #endregion

        #region Initialization

        private void Awake() { }

        public void Initialize()
        {
            if (initialized) return;

            panel = GameObjectHelper.Game.TutorialPopup.Panel;
            image = GameObjectHelper.Game.TutorialPopup.Image;
            title = GameObjectHelper.Game.TutorialPopup.TitleTextX;
            content = GameObjectHelper.Game.TutorialPopup.ContentTextX;
            previousButton = GameObjectHelper.Game.TutorialPopup.PreviousButton;
            nextButton = GameObjectHelper.Game.TutorialPopup.NextButton;
            closeButton = GameObjectHelper.Game.TutorialPopup.CloseButton;

            initialized = true;
        }

        private void Start()
        {
            if (!initialized) Initialize();
            bool show = g.DebugManager != null && g.DebugManager.showTutorials;
            if (panel != null) panel.SetActive(show);
        }

        public void Load(Tutorial tutorial, bool show = true)
        {
            if (g.DebugManager == null || !g.DebugManager.showTutorials) return;
            if (tutorial == null || tutorial.Pages == null || tutorial.Pages.Count < 1) return;

            pages = tutorial.Pages;
            currentPage = 0;

            if (show)
                Show();
        }

        public void Show(int currentPage = 0)
        {
            if (g.DebugManager == null || !g.DebugManager.showTutorials) return;
            if (!hasPages) return;
            if (panel != null) panel.SetActive(true);
            this.currentPage = Mathf.Clamp(currentPage, 0, lastPage);
            Navigate();
        }

        private void Navigate()
        {
            if (g.DebugManager == null || !g.DebugManager.showTutorials) return;
            if (!hasPages) return;
            var page = pages[currentPage];
            if (image != null && page != null && SpriteLibrary.TutorialPages != null && SpriteLibrary.TutorialPages.ContainsKey(page.TextureKey))
                image.sprite = SpriteLibrary.TutorialPages[page.TextureKey];
            if (title != null) title.text = page.Title ?? string.Empty;
            if (content != null) content.text = page.Content ?? string.Empty;
            if (previousButton != null) previousButton.gameObject.SetActive(currentPage > 0);
            if (nextButton != null) nextButton.gameObject.SetActive(currentPage < lastPage);
            if (closeButton != null) closeButton.gameObject.SetActive(currentPage == lastPage);
        }

        public void PreviousPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                Navigate();
            }
        }

        public void NextPage()
        {
            if (currentPage < lastPage)
            {
                currentPage++;
                Navigate();
            }
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        #endregion
    }

}
