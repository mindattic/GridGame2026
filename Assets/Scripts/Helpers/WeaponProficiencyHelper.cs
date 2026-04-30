using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
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

namespace Scripts.Helpers
{
    /// <summary>How well a hero class can wield a particular weapon type.</summary>
    public enum WeaponProficiency
    {
        /// <summary>Class trained on this weapon — equip allowed, no penalty.</summary>
        Proficient = 0,
        /// <summary>Class can equip but is poorly suited (warning shown, fight penalty intended).</summary>
        Poor = 1,
        /// <summary>Class refuses to equip — visible but disabled in the picker.</summary>
        Forbidden = 2,
    }

    /// <summary>
    /// WEAPONPROFICIENCYHELPER - Resolves whether a (class, weapon) pair is a good fit.
    /// <para>PURPOSE: A Paladin should not wield a Magic Wand; a Cleric should not lug a Greatsword.
    /// The Hub equipment UI uses this helper to show ✓ / ⚠ / ✕ markers and to refuse forbidden equips.
    /// Combat damage formulas can also consult <see cref="GetProficiency"/> to apply a penalty when
    /// <see cref="WeaponProficiency.Poor"/> weapons are wielded.</para>
    /// <para>RULES SOURCE: Encoded in code (not data) so adding a new hero class only requires editing
    /// the <see cref="GetProficiency"/> switch. Unmapped classes default to <see cref="WeaponProficiency.Proficient"/>
    /// for every weapon type, so adding a new hero never accidentally locks them out of gear.</para>
    /// <para>RELATED FILES: WeaponType.cs, ItemDefinition.cs, ItemData_Weapons.cs, EquipSection.cs,
    /// PartySection.cs.</para>
    /// </summary>
    public static class WeaponProficiencyHelper
    {
        /// <summary>Resolves the proficiency of <paramref name="cls"/> with weapons of <paramref name="weapon"/>.
        /// Returns <see cref="WeaponProficiency.Proficient"/> by default (open by default, restrict explicitly).</summary>
        public static WeaponProficiency GetProficiency(CharacterClass cls, WeaponType weapon)
        {
            if (weapon == WeaponType.None) return WeaponProficiency.Proficient;

            switch (cls)
            {
                // Heavy front-line fighters — physical melee, no magic.
                case CharacterClass.Paladin:
                case CharacterClass.Knight:
                case CharacterClass.Defender:
                case CharacterClass.ShieldMaiden:
                case CharacterClass.JadeKnight:
                case CharacterClass.DarkTemplar:
                    return weapon switch
                    {
                        WeaponType.Wand   => WeaponProficiency.Forbidden,
                        WeaponType.Staff  => WeaponProficiency.Forbidden,
                        WeaponType.Bow    => WeaponProficiency.Poor,
                        WeaponType.Dagger => WeaponProficiency.Poor,
                        _                 => WeaponProficiency.Proficient,
                    };

                // Holy / hybrid casters — blunt weapons + magic; refuse heavy edged weapons.
                case CharacterClass.Cleric:
                case CharacterClass.Monk:
                case CharacterClass.Ritualist:
                case CharacterClass.Sage:
                    return weapon switch
                    {
                        WeaponType.Greatsword => WeaponProficiency.Forbidden,
                        WeaponType.Axe        => WeaponProficiency.Forbidden,
                        WeaponType.Sword      => WeaponProficiency.Poor,
                        WeaponType.Dagger     => WeaponProficiency.Poor,
                        WeaponType.Bow        => WeaponProficiency.Poor,
                        WeaponType.Spear      => WeaponProficiency.Poor,
                        _                     => WeaponProficiency.Proficient,
                    };

                // Pure casters — magic-focused; refuse heavy melee.
                case CharacterClass.RedMage:
                case CharacterClass.BlackWitch:
                    return weapon switch
                    {
                        WeaponType.Greatsword => WeaponProficiency.Forbidden,
                        WeaponType.Hammer     => WeaponProficiency.Forbidden,
                        WeaponType.Axe        => WeaponProficiency.Forbidden,
                        WeaponType.Sword      => WeaponProficiency.Poor,
                        WeaponType.Spear      => WeaponProficiency.Poor,
                        WeaponType.Bow        => WeaponProficiency.Poor,
                        WeaponType.Mace       => WeaponProficiency.Poor,
                        _                     => WeaponProficiency.Proficient,
                    };

                // Ranged / scouts.
                case CharacterClass.NightHunter:
                case CharacterClass.Phantom:
                case CharacterClass.Operative:
                case CharacterClass.Drifter:
                    return weapon switch
                    {
                        WeaponType.Greatsword => WeaponProficiency.Poor,
                        WeaponType.Hammer     => WeaponProficiency.Poor,
                        WeaponType.Axe        => WeaponProficiency.Poor,
                        WeaponType.Wand       => WeaponProficiency.Poor,
                        WeaponType.Staff      => WeaponProficiency.Poor,
                        _                     => WeaponProficiency.Proficient,
                    };

                // Rogue / assassin / ninja — daggers + light blades.
                case CharacterClass.Assassain:
                case CharacterClass.BlueNinja:
                case CharacterClass.RedNinja:
                case CharacterClass.GreenNinja:
                case CharacterClass.BlackNinja:
                case CharacterClass.ChromaNinja:
                case CharacterClass.Reaper:
                case CharacterClass.Ripper:
                case CharacterClass.Harbinger:
                    return weapon switch
                    {
                        WeaponType.Greatsword => WeaponProficiency.Poor,
                        WeaponType.Hammer     => WeaponProficiency.Poor,
                        WeaponType.Wand       => WeaponProficiency.Poor,
                        WeaponType.Staff      => WeaponProficiency.Poor,
                        _                     => WeaponProficiency.Proficient,
                    };

                // Heavy berserkers / brutes.
                case CharacterClass.Barbarian:
                case CharacterClass.Bruiser:
                case CharacterClass.IceMauler:
                case CharacterClass.MountainTroll:
                    return weapon switch
                    {
                        WeaponType.Wand   => WeaponProficiency.Forbidden,
                        WeaponType.Staff  => WeaponProficiency.Forbidden,
                        WeaponType.Bow    => WeaponProficiency.Poor,
                        WeaponType.Dagger => WeaponProficiency.Poor,
                        _                 => WeaponProficiency.Proficient,
                    };

                // Spear specialists.
                case CharacterClass.Lancer:
                case CharacterClass.Myrmidon:
                    return weapon switch
                    {
                        WeaponType.Wand  => WeaponProficiency.Poor,
                        WeaponType.Staff => WeaponProficiency.Poor,
                        WeaponType.Bow   => WeaponProficiency.Poor,
                        _                => WeaponProficiency.Proficient,
                    };

                // Default: any class not enumerated above can wield anything (open by default).
                default:
                    return WeaponProficiency.Proficient;
            }
        }

        /// <summary>Convenience: resolve an item's proficiency for a class. Non-weapons return Proficient.</summary>
        public static WeaponProficiency GetProficiency(CharacterClass cls, ItemDefinition item)
        {
            if (item == null || item.Slot != EquipmentSlot.Weapon) return WeaponProficiency.Proficient;
            return GetProficiency(cls, item.WeaponType);
        }

        /// <summary>True if the class can equip the weapon (Proficient or Poor — Forbidden is the only no).</summary>
        public static bool CanEquip(CharacterClass cls, ItemDefinition item)
            => GetProficiency(cls, item) != WeaponProficiency.Forbidden;

        /// <summary>Short marker shown next to weapon rows in the equipment picker. Rich-text coloured.</summary>
        public static string Marker(WeaponProficiency p)
        {
            switch (p)
            {
                case WeaponProficiency.Proficient: return "<color=#55DD55>✓</color>";
                case WeaponProficiency.Poor:       return "<color=#DDBB22>⚠</color>";
                case WeaponProficiency.Forbidden:  return "<color=#DD5555>✕</color>";
                default:                           return "";
            }
        }

        /// <summary>One-line explanation for the detail / tooltip panel.</summary>
        public static string Reason(WeaponProficiency p, CharacterClass cls, WeaponType w)
        {
            switch (p)
            {
                case WeaponProficiency.Proficient: return $"<color=#55DD55>{cls} can wield {WeaponTypeHelper.DisplayName(w)} effectively.</color>";
                case WeaponProficiency.Poor:       return $"<color=#DDBB22>⚠ Poor match — {cls} fights at reduced effectiveness with a {WeaponTypeHelper.DisplayName(w)}.</color>";
                case WeaponProficiency.Forbidden:  return $"<color=#DD5555>✕ {cls} cannot wield a {WeaponTypeHelper.DisplayName(w)}.</color>";
                default:                           return "";
            }
        }
    }
}
