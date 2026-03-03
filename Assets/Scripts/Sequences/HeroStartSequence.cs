// --- File: Assets/Scripts/Events/Sequences/HeroStartSequence.cs ---
using System.Collections;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// Performs start-of-turn logic for the hero team.
    /// Refills the hero Animation timer and makes sure UI is in the right mode.
    /// </summary>
    public class HeroStartSequence : SequenceEvent
    {
        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            // Only run during hero turns
            if (!g.TurnManager.IsHeroTurn)
                yield break;

            // Put input back into hero mode and refill the turn timer UI
            g.InputManager.InputMode = InputMode.PlayerTurn;

            yield break;
        }
    }
}
