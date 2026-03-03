using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Scripts.Helpers;
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
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
/// <summary>
/// LOADINGSCREENLORE - Loading screen lore text display.
/// 
/// PURPOSE:
/// Displays random lore text snippets during loading screens
/// to provide world-building while players wait.
/// 
/// LORE CATEGORIES:
/// - Core: Central world element mythology
/// - Orthodoxy: Religious faction lore
/// - Wilds: Forest/nature region lore
/// - Mire: Swamp region lore
/// - Rustlands: Industrial wasteland lore
/// - Septach: Political/judicial faction lore
/// 
/// SELECTION:
/// Randomly picks one lore entry from the collection
/// each time the loading screen appears.
/// 
/// RELATED FILES:
/// - SceneLoader.cs: Loading screen controller
/// - GameObjectHelper.cs: UI element paths
/// </summary>
public class LoadingScreenLore : MonoBehaviour
{
    // Reference to your TMP text element
    [SerializeField] private TextMeshProUGUI loreText;

    private List<string> lore = new List<string>();


    /// <summary>Resolves the TMP text reference and populates the lore collection.</summary>
    private void Awake()
    {

        loreText = GameObjectHelper.LoadingScreen.LoreText;

        // Core
        lore.Add("The Core burns without ceasing, a light eternal yet unseen. � Pilgrim�s Verse");
        lore.Add("Ashes return to the Core, as all things must. � Ashbinder Proverb");
        lore.Add("The heat of the Core can forge or destroy. � Blacksmith�s Canticle");
        lore.Add("Some say the Core remembers every breath taken above it. � Archivist of the Septach");
        lore.Add("Children born near the Core are said to never know cold. � Hearthmother�s Tale");

        // Orthodoxy
        lore.Add("The Orthodoxy enforces silence in its halls. � Edict of Stillness");
        lore.Add("Their scriptures are etched in steel, not parchment. � Chronicler�s Margin");
        lore.Add("A man is measured by obedience, not mercy, in the Orthodoxy. � Catechism of Iron");
        lore.Add("The Core is a tool to them, not a mystery. � Dissident Whisper");
        lore.Add("The Orthodoxy burns books more often than candles. � Scribe�s Lament");

        // Wilds
        lore.Add("The Wilds never forget a trespass. � Hunter�s Warning");
        lore.Add("Roots crack stone, given time. � Druid�s Chant");
        lore.Add("Hunters whisper to the trees and sometimes the trees answer. � Forester�s Account");
        lore.Add("Moss hides bones of the careless. � Warning Stone");
        lore.Add("Those who stay too long walk on all fours. � Campfire Tale");

        // Mire
        lore.Add("The Mire swallows all sound beneath its fog. � Ferryman�s Note");
        lore.Add("Lanterns here drown faster than men. � Mirekeeper�s Journal");
        lore.Add("The air of the Mire is half-breath, half-poison. � Apothecary�s Ledger");
        lore.Add("Some say the Mire has no bottom, only descent. � Lost Traveler�s Carving");
        lore.Add("Few return from the Mire unchanged in mind or body. � Septach Report");

        // Rustlands
        lore.Add("Iron here bleeds as if alive. � Scrapper�s Saying");
        lore.Add("Machines lie like carcasses, half buried in dust. � Scavenger Hymn");
        lore.Add("Children here play with gears instead of stones. � Rustlands Nursery Rhyme");
        lore.Add("Scrappers say the machines dream when storms roll in. � Gear-Priest Fragment");
        lore.Add("Water runs red through the Rustlands. � Wanderer�s Account");

        // Septach
        lore.Add("The Septach builds spires with no end. � Observer�s Note");
        lore.Add("Truth is divided into seven parts, they say. � Septach Axiom");
        lore.Add("Every word spoken in the Septach has a double meaning. � Spy�s Report");
        lore.Add("Seven judges weigh each soul upon entry. � Tribunal Record");
        lore.Add("The Septach archives hold maps of dreams. � Librarian�s Confession");

        // Drifting Aether
        lore.Add("The Aether shifts with no anchor. � Sailor�s Guide");
        lore.Add("Travelers tie stones to their ankles to stay grounded. � Aetherfarer�s Trick");
        lore.Add("Some speak of storms that rain glass from the sky. � Glasswalker Account");
        lore.Add("Time bends in the Drifting Aether, loops upon itself. � Chronomancer�s Diary");
        lore.Add("The Aether swallows names, leaving wanderers nameless. � Nameless Song");
    }

    /// <summary>Displays a random lore entry on first frame.</summary>
    private void Start()
    {
      

        Show();
    }

    /// <summary>Picks and displays a random lore line from the collection.</summary>
    public void Show()
    {
        if (lore.Count == 0 || loreText == null)
            return;

        int index = RNG.Int(0, lore.Count - 1);
        loreText.text = lore[index];
    }
}

}
