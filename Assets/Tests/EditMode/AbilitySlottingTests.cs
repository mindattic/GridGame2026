// ABILITYSLOTTINGTESTS — EditMode tests for the skill/spell → ability-bar path the
// Abilities scene writes (AbilityBarSlotSave.AbilityName) and combat reads
// (HeroLoadout.LoadFromSave → AbilityLibrary.Get). Proves a slotted spell survives the
// save round trip and resolves to a real Ability at loadout time.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class AbilitySlottingTests
    {
        [Test]
        public void Named_ability_slot_resolves_through_hero_loadout()
        {
            var ability = AbilityLibrary.Get("Fireball");
            Assert.IsNotNull(ability, "AbilityLibrary must know 'Fireball'.");

            var save = new HeroEquipmentSave
            {
                AbilityBarSlots = new List<AbilityBarSlotSave>
                {
                    new AbilityBarSlotSave(abilityName: "Fireball", itemId: null),
                },
            };

            var loadout = new HeroLoadout();
            loadout.LoadFromSave(save);

            Assert.IsTrue(loadout.EquippedAbilities.Any(a => a != null && a.name == "Fireball"),
                "A named-ability slot must hydrate into the combat loadout.");
        }

        [Test]
        public void Ability_slot_save_roundtrips_by_kind()
        {
            var abilitySlot = new AbilityBarSlotSave(abilityName: "Fire", itemId: null);
            var itemSlot = new AbilityBarSlotSave(abilityName: null, itemId: "healing_potion_basic");
            var empty = new AbilityBarSlotSave();

            Assert.IsTrue(abilitySlot.IsAbility);
            Assert.IsFalse(abilitySlot.IsItem);
            Assert.IsTrue(itemSlot.IsItem);
            Assert.IsTrue(empty.IsEmpty);
        }

        [Test]
        public void Every_hero_class_ability_list_resolves_in_ability_library()
        {
            // The Abilities scene lists ActorData.Abilities and assigns them by NAME; any
            // name AbilityLibrary can't resolve would silently drop from the combat bar.
            var party = new[]
            {
                Scripts.Helpers.CharacterClass.Cleric,
                Scripts.Helpers.CharacterClass.Paladin,
                Scripts.Helpers.CharacterClass.Barbarian,
            };

            foreach (var cls in party)
            {
                var data = ActorLibrary.Get(cls);
                if (data?.Abilities == null) continue;
                foreach (var ability in data.Abilities.Where(a => a != null && a.IsActive))
                {
                    Assert.IsNotNull(AbilityLibrary.Get(ability.name),
                        $"{cls}'s ability '{ability.name}' must resolve in AbilityLibrary or it can never reach the combat bar.");
                }
            }
        }
    }
}
