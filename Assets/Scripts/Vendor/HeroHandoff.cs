using Scripts.Helpers;

namespace Scripts.Vendor
{
    /// <summary>
    /// HEROHANDOFF - Cross-scene hero selection passthrough.
    /// <para>PURPOSE: When PartyManager routes the player to Abilities or Equip, it stuffs
    /// the chosen hero here. The destination scene reads + clears on its first Refresh.
    /// Static state is acceptable for in-process scene transitions; it does NOT survive
    /// app restart (which is fine — the player is asked to pick again).</para>
    /// <para>RELATED FILES: PartyManager.cs, AbilitiesManager.cs (slice 4), EquipManager.cs (slice 5)</para>
    /// </summary>
    public static class HeroHandoff
    {
        public static CharacterClass Pending = CharacterClass.None;
    }
}
