namespace Assets.Scripts.Models
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
