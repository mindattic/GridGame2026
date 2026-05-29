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
    /// ABILITYBAR - The Row-13 6-slot ability bar.
    ///
    /// <para>Holds <b>up to 6 abilities</b> for the currently selected hero. Each slot can be one
    /// of three kinds (<see cref="AbilityKind"/>):</para>
    ///
    /// <list type="bullet">
    ///   <item><b>Skill</b> (usually class-based) — free, reusable, costs the player's turn.
    ///   Examples: Steal, Mug.</item>
    ///   <item><b>Spell</b> — costs colored mana orbs (e.g. Fireball = (R)(R)); the dispatcher
    ///   runs a <see cref="SpellCastBar"/> countdown before resolving.</item>
    ///   <item><b>Item</b> — instant consumable with a per-slot stack (drained one charge per
    ///   use). Two stacks of 5 = two slots, each holding 5 charges that drain independently.</item>
    /// </list>
    ///
    /// <para>The bar <b>follows the selected hero</b> — when selection changes, slots re-bind to
    /// that hero's loadout via <see cref="HeroLoadouts.For"/>. All heroes share one
    /// <see cref="ManaBank"/> (the party-wide orb line), but each hero has their own Skills and
    /// Items.</para>
    ///
    /// <para>Built by <see cref="Scripts.Factories.AbilityBarFactory"/>; populated each frame from
    /// <see cref="GameHelper.Actors.SelectedActor"/> and re-painted by <see cref="Refresh"/>.</para>
    /// </summary>
    public sealed class AbilityBar : MonoBehaviour
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

        /// <summary>Click handler — dispatches by kind: Skill / Spell / Item.</summary>
        public void OnSlotClicked(int slot)
        {
            if (currentLoadout == null || slot < 0 || slot >= currentLoadout.Count) return;
            var a = currentLoadout[slot];
            if (a == null) { Debug.Log("[AbilityBar] Reserved slot."); return; }

            switch (a.Kind)
            {
                case AbilityKind.Item:  HandleItem(a);  return;
                case AbilityKind.Skill: HandleSkill(a); return;
                case AbilityKind.Spell: HandleSpell(a); return;
            }
        }

        // ── Item: instant; consumes one charge from THIS slot's stack ──
        private void HandleItem(ManaAbility a)
        {
            if (a.TryConsumeCharge())
                Debug.Log($"[AbilityBar] Item '{a.Name}' used ({a.Charges}/{a.MaxStackSize} left).");
            else
                Debug.LogWarning($"[AbilityBar] Item '{a.Name}' empty (0/{a.MaxStackSize}). Buy/craft to restock.");
        }

        // ── Skill: free; no cast bar; resolves on target confirm; then advances the turn ──
        private void HandleSkill(ManaAbility a)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var spell = ResolveSpell(a);
            if (spell == null) { Debug.LogWarning($"[AbilityBar] Skill '{a.Name}' has no SpellDefinition wired."); return; }
            var caster = g.Actors.SelectedActor;

            Scripts.Managers.TargetingMode.Begin(spell, caster,
                onConfirm: targets =>
                {
                    Debug.Log($"[AbilityBar] Skill '{a.Name}' used on {targets.Count} target(s).");
                    foreach (var t in targets)
                        Scripts.Managers.SpellEffectDispatcher.Cast(spell, caster, t);
                    // "Costs a turn" — advance timeline to next enemy.
                    g.ManaPoolManager?.OnBankButtonClicked();
                },
                onCancel: () => Debug.Log($"[AbilityBar] Skill '{a.Name}' cancelled — turn not spent."));
        }

        // ── Spell: pays orbs upfront after target pick; cast bar shrinks; dispatcher resolves ──
        private void HandleSpell(ManaAbility a)
        {
            if (bank == null || !bank.CanAfford(a.Cost))
            {
                Debug.LogWarning($"[AbilityBar] Can't afford '{a.Name}' ({ManaAbilities.CostIcons(a)}).");
                return;
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var spell = ResolveSpell(a);
            if (spell == null) { Debug.LogWarning($"[AbilityBar] Spell '{a.Name}' has no SpellDefinition wired."); return; }
            var caster = g.Actors.SelectedActor;

            Scripts.Managers.TargetingMode.Begin(spell, caster,
                onConfirm: targets =>
                {
                    if (SpellCastBar.IsAtCapacity)
                    {
                        Debug.LogWarning($"[AbilityBar] Too many casts in flight (max {SpellCastBar.MaxConcurrent}) — wait for one to resolve.");
                        return;
                    }
                    if (!bank.Spend(a.Cost))
                    {
                        Debug.LogWarning($"[AbilityBar] Orbs changed mid-pick — couldn't afford '{a.Name}'.");
                        return;
                    }
                    Debug.Log($"[AbilityBar] Casting '{a.Name}' ({a.CastTimeSeconds:0.0}s) on {targets.Count} target(s)…");

                    Scripts.Factories.SpellCastBarFactory.Create(canvas.transform, a, onResolved: () =>
                    {
                        foreach (var t in targets)
                            Scripts.Managers.SpellEffectDispatcher.Cast(spell, caster, t);
                    }, caster: caster);
                },
                onCancel: () => Debug.Log($"[AbilityBar] Cast of '{a.Name}' cancelled — no orbs spent."));
        }

        private static SpellDefinition ResolveSpell(ManaAbility a)
        {
            foreach (var s in SpellLibrary.All)
                if (s.Ability == a) return s;
            return null;
        }

        private void Update()
        {
            // Re-bind on selection change.
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

                bool affordable;
                switch (a.Kind)
                {
                    case AbilityKind.Item:  affordable = a.Charges > 0; break;
                    case AbilityKind.Skill: affordable = true; break; // free, always usable
                    default:                affordable = bank != null && bank.CanAfford(a.Cost); break;
                }
                if (buttons[i] != null) buttons[i].interactable = affordable;
                if (nameLabels[i] != null) nameLabels[i].text = a.Name;
                if (costLabels[i] != null) costLabels[i].text = ManaAbilities.CostIcons(a);
                if (frames[i] != null) frames[i].color = FrameColorFor(a, affordable);
            }
        }

        /// <summary>Color the slot's frame by kind. Skill = green, Spell = blue, Item = warm,
        /// Reserved = dark. Dimmed when unaffordable / empty.</summary>
        private static Color FrameColorFor(ManaAbility a, bool affordable)
        {
            Color baseColor;
            switch (a.Kind)
            {
                case AbilityKind.Skill: baseColor = new Color(0.25f, 0.38f, 0.22f, 0.92f); break;
                case AbilityKind.Item:  baseColor = new Color(0.45f, 0.30f, 0.18f, 0.92f); break;
                default:                baseColor = new Color(0.20f, 0.25f, 0.40f, 0.92f); break;
            }
            if (!affordable) baseColor *= new Color(0.55f, 0.55f, 0.55f, 1f);
            return baseColor;
        }
    }
}
