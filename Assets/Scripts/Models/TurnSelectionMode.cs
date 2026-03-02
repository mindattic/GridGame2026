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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
    /// <summary>
    /// TURNSELECTIONMODE - Hero selection behavior during battles.
    /// 
    /// PURPOSE:
    /// Controls whether the player can freely select any hero or
    /// must wait for the active hero's turn.
    /// 
    /// MODES:
    /// - FreeSelect: Player can move any living hero at any time
    /// - ActiveOnly: Player can only move the hero whose turn it is
    /// 
    /// RELATED FILES:
    /// - SelectionRules.cs: Enforces selection mode
    /// - TurnManager.cs: Manages active actor
    /// - InputManager.cs: Checks mode for input handling
    /// </summary>
    public enum TurnSelectionMode
    {
        FreeSelect = 0,            // Player can move any hero
        ActiveOnly = 1             // Player can only move the hero on the current timeline block
    }
}
