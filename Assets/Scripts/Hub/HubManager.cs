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
    /// HUBMANAGER - Central vendor launcher.
    ///
    /// <para>PURPOSE: Backs the Hub.unity scene (US-112) — a simple 6-button grid that routes the
    /// player to each vendor scene (Vendor, Blacksmith, Alchemist, Equip, Party, Abilities).
    /// No shop logic lives here; each vendor scene owns its own inventory flow.</para>
    ///
    /// <para>LIFECYCLE: Stateless. Navigation buttons are wired in <see cref="HubBuilder"/> via
    /// persistent onClick listeners — this MonoBehaviour exists only for scene identity.</para>
    /// </summary>
    public class HubManager : MonoBehaviour
    {
        private void Awake()
        {
            // Hydrate profile so vendors have save data to read.
            ProfileHelper.EnsureProfileLoaded();
        }
    }
}
