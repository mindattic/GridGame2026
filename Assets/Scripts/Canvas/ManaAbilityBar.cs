using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Data;
using Scripts.Helpers;
using Scripts.Models;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Canvas
{
    /// <summary>
    /// MANAABILITYBAR - The Row-13 6-slot ability bar.
    ///
    /// <para>Each hero has their own 6-slot loadout (resolved via
    /// <see cref="HeroLoadouts.For"/>); the bar follows whichever hero is currently selected and
    /// re-binds when selection changes. The whole party <b>shares one</b> <see cref="ManaBank"/> —
    /// every selected hero spends from the same line of orbs.</para>
    ///
    /// <para>Per slot:</para>
    /// <list type="bullet">
    ///   <item><b>Spell</b> → costs colored mana orbs (e.g. Fireball = (R)(R)); pays via
    ///   <see cref="ManaBank.Spend"/>. Interactable iff <see cref="ManaBank.CanAfford"/>.
    ///   (Future: starts a casting-time countdown on the timeline; current V1 resolves instantly.)
    ///   </item>
    ///   <item><b>Item</b> → instant; <see cref="ManaAbility.TryConsumeCharge"/> on click. Stack
    ///   count gates re-use; interactable iff Charges > 0.</item>
    ///   <item><b>Reserved (null)</b> → disabled placeholder.</item>
    /// </list>
    ///
    /// <para>When no hero is selected (or selection isn't a playing hero) all 6 slots hide —
    /// mirrors the legacy AbilityButtonManager Show/Hide pattern.</para>
    /// </summary>
    public sealed class ManaAbilityBar : MonoBehaviour
    {
        private ManaBank bank;
        private Button[] buttons;
        private TMP_Text[] nameLabels;
        private TMP_Text[] costLabels;
        private Image[] frames;

        private IReadOnlyList<ManaAbility> currentLoadout;
        private CharacterClass currentClass = CharacterClass.None;

        public void Bind(ManaBank bank, Button[] buttons, TMP_Text[] nameLabels, TMP_Text[] costLabels, Image[] frames)
        {
            this.bank = bank;
            this.buttons = buttons;
            this.nameLabels = nameLabels;
            this.costLabels = costLabels;
            this.frames = frames;
        }

        /// <summary>Click handler — uses the CURRENT selected-hero loadout, not a global slot list.</summary>
        public void OnSlotClicked(int slot)
        {
            if (currentLoadout == null || slot < 0 || slot >= currentLoadout.Count) return;
            var a = currentLoadout[slot];
            if (a == null) { Debug.Log("[ManaAbilityBar] Reserved slot."); return; }

            if (a.Kind == AbilityKind.Item)
            {
                // Items are INSTANT and consume one charge from their stack.
                if (a.TryConsumeCharge())
                    Debug.Log($"[ManaAbilityBar] Item '{a.Name}' used ({a.Charges}/{a.MaxCharges} left).");
                else
                    Debug.LogWarning($"[ManaAbilityBar] Item '{a.Name}' is empty (0/{a.MaxCharges}).");
                return;
            }

            // Spell flow: pre-check affordability (don't deduct yet), enter targeting, then on
            // CONFIRMED pick deduct orbs + start the cast bar + on resolve dispatch the VFX chain
            // against every picked target. Cancel during targeting = free (no orbs spent).
            if (bank == null || !bank.CanAfford(a.Cost))
            {
                Debug.LogWarning($"[ManaAbilityBar] Can't afford '{a.Name}' ({ManaAbilities.CostIcons(a)}).");
                return;
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var spell = ResolveSpell(a);
            var caster = g.Actors.SelectedActor;
            if (spell == null)
            {
                Debug.LogWarning($"[ManaAbilityBar] '{a.Name}' has no SpellDefinition — skipping cast.");
                return;
            }

            Scripts.Managers.TargetingMode.Begin(spell, caster,
                onConfirm: targets =>
                {
                    // Fix #7: don't start a 5th simultaneous cast — refuse without spending orbs.
                    if (SpellCastBar.IsAtCapacity)
                    {
                        Debug.LogWarning($"[ManaAbilityBar] Too many casts in flight (max {SpellCastBar.MaxConcurrent}) — wait for one to resolve.");
                        return;
                    }
                    if (!bank.Spend(a.Cost))
                    {
                        Debug.LogWarning($"[ManaAbilityBar] Orbs changed mid-pick — couldn't afford '{a.Name}'.");
                        return;
                    }
                    Debug.Log($"[ManaAbilityBar] Casting '{a.Name}' ({a.CastTimeSeconds:0.0}s) on {targets.Count} target(s)…");

                    Scripts.Factories.SpellCastBarFactory.Create(canvas.transform, a, onResolved: () =>
                    {
                        foreach (var t in targets)
                            Scripts.Managers.SpellEffectDispatcher.Cast(spell, caster, t);
                    }, caster: caster);
                },
                onCancel: () => Debug.Log($"[ManaAbilityBar] Cast of '{a.Name}' cancelled — no orbs spent."));
        }

        private static Scripts.Models.SpellDefinition ResolveSpell(Scripts.Models.ManaAbility a)
        {
            // Map ManaAbility → SpellDefinition by name. Easy to swap to a dictionary later.
            foreach (var s in Scripts.Data.SpellLibrary.All)
                if (s.Ability == a) return s;
            return null;
        }

        private void Update()
        {
            // Track selection — when the active hero changes, re-bind to that hero's 6-slot loadout.
            var sel = g.Actors.SelectedActor;
            var cls = (sel != null && sel.IsHero && sel.IsPlaying) ? sel.characterClass : CharacterClass.None;
            if (cls != currentClass)
            {
                currentClass = cls;
                currentLoadout = (cls == CharacterClass.None) ? null : HeroLoadouts.For(cls);
            }
            Refresh();
        }

        private void Refresh()
        {
            if (buttons == null) return;
            bool active = currentLoadout != null;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (!active)
                {
                    // No hero selected — hide all slots.
                    if (buttons[i] != null && buttons[i].gameObject.activeSelf)
                        buttons[i].gameObject.SetActive(false);
                    continue;
                }

                if (buttons[i] != null && !buttons[i].gameObject.activeSelf)
                    buttons[i].gameObject.SetActive(true);

                var a = i < currentLoadout.Count ? currentLoadout[i] : null;
                if (a == null)
                {
                    if (buttons[i] != null) buttons[i].interactable = false;
                    if (nameLabels[i] != null) nameLabels[i].text = "—";
                    if (costLabels[i] != null) costLabels[i].text = "Reserved";
                    if (frames[i] != null) frames[i].color = new Color(0.15f, 0.15f, 0.18f, 0.6f);
                    continue;
                }

                bool affordable = a.Kind == AbilityKind.Item ? a.Charges > 0 : (bank != null && bank.CanAfford(a.Cost));
                if (buttons[i] != null) buttons[i].interactable = affordable;
                if (nameLabels[i] != null) nameLabels[i].text = a.Name;
                if (costLabels[i] != null) costLabels[i].text = ManaAbilities.CostIcons(a);
                if (frames[i] != null) frames[i].color = FrameColorFor(a, affordable);
            }
        }

        private static Color FrameColorFor(ManaAbility a, bool affordable)
        {
            // Spells tint cool; items tint warm; dim when unaffordable.
            Color baseColor = a.Kind == AbilityKind.Item
                ? new Color(0.45f, 0.30f, 0.18f, 0.92f)   // warm — items
                : new Color(0.20f, 0.25f, 0.40f, 0.92f);  // cool — spells
            if (!affordable) baseColor *= new Color(0.55f, 0.55f, 0.55f, 1f);
            return baseColor;
        }
    }
}
