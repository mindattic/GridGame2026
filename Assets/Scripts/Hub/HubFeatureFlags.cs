using UnityEngine;
using UnityEngine.UI;
using TMPro;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub.Sections;
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

namespace Scripts.Hub
{
    /// <summary>
    /// HUBFEATUREFLAGS - Central enable/disable table for Hub sections.
    /// <para>PURPOSE: While the game is being built section-by-section we want only the sections
    /// that are actually wired-up and tested to appear in the Hub. Both the scaffold (which builds
    /// the menu buttons + section panels) and HubManager (which routes nav clicks) consult this
    /// table so flipping a single bool here lights up that section everywhere.</para>
    /// <para>USAGE: Flip a bool to true → re-run <c>Tools/Scenes/Hub/Load</c> to rebuild the scene.
    /// HubManager picks up the change on next scene entry.</para>
    /// <para>RELATED FILES: HubScaffold.cs, HubManager.cs</para>
    /// </summary>
    public static class HubFeatureFlags
    {
        // Only Party is on while we focus on Hub → Battle → Hub round-trip with XP.
        // Add the rest back as each section's behaviour is finished + tested.
        public const bool Party      = true;
        public const bool Shop       = false;
        public const bool Alchemist  = false;
        public const bool Inn        = false;
        public const bool Blacksmith = true;
        public const bool Training   = false;
        public const bool Equip      = true;
        public const bool Inventory  = false;
        public const bool Enchanter  = false;
        public const bool Salvage    = false;
        public const bool Places     = false;
        public const bool Bounty     = false;

        /// <summary>True when the named section type is enabled. Used by HubManager to route Show&lt;T&gt; calls.</summary>
        public static bool IsEnabled<TSection>() where TSection : HubSection
        {
            var t = typeof(TSection);
            if (t == typeof(PartySection))         return Party;
            if (t == typeof(GeneralStoreSection))  return Shop;
            if (t == typeof(AlchemistSection))     return Alchemist;
            if (t == typeof(InnSection))           return Inn;
            if (t == typeof(BlacksmithSection))    return Blacksmith;
            if (t == typeof(TrainingSection))      return Training;
            if (t == typeof(EquipSection))         return Equip;
            if (t == typeof(InventorySection))     return Inventory;
            if (t == typeof(EnchantSection))       return Enchanter;
            if (t == typeof(SalvageSection))       return Salvage;
            if (t == typeof(PlacesSection))        return Places;
            if (t == typeof(BountySection))        return Bounty;
            return false;
        }

        /// <summary>True when the named section type (by reflection) is enabled. Used by HubScaffold.</summary>
        public static bool IsEnabled(System.Type sectionType)
        {
            if (sectionType == typeof(PartySection))         return Party;
            if (sectionType == typeof(GeneralStoreSection))  return Shop;
            if (sectionType == typeof(AlchemistSection))     return Alchemist;
            if (sectionType == typeof(InnSection))           return Inn;
            if (sectionType == typeof(BlacksmithSection))    return Blacksmith;
            if (sectionType == typeof(TrainingSection))      return Training;
            if (sectionType == typeof(EquipSection))         return Equip;
            if (sectionType == typeof(InventorySection))     return Inventory;
            if (sectionType == typeof(EnchantSection))       return Enchanter;
            if (sectionType == typeof(SalvageSection))       return Salvage;
            if (sectionType == typeof(PlacesSection))        return Places;
            if (sectionType == typeof(BountySection))        return Bounty;
            return false;
        }
    }
}
