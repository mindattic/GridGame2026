using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Data;
using Scripts.Helpers;
using Scripts.Instances;
using Scripts.Libraries;
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
        private Image[] iconImages;
        private RectTransform[] slotRects;
        private TooltipInstance activeTooltip;

        private IReadOnlyList<ManaAbility> currentLoadout;
        private CharacterClass currentClass = CharacterClass.None;

        public void Bind(ManaBank bank, Button[] buttons, TMP_Text[] nameLabels, TMP_Text[] costLabels, Image[] frames, Image[] iconImages = null, RectTransform[] slotRects = null)
        {
            this.bank = bank;
            this.buttons = buttons;
            this.nameLabels = nameLabels;
            this.costLabels = costLabels;
            this.frames = frames;
            this.iconImages = iconImages;
            this.slotRects = slotRects;
        }

        // US-091: hover/long-press tooltip showing ability name, kind, cost, and cast time.
        public void ShowTooltipForSlot(int i)
        {
            HideTooltip();
            if (currentLoadout == null || i < 0 || i >= currentLoadout.Count) return;
            var a = currentLoadout[i];
            if (a == null) return;
            var target = slotRects != null && i < slotRects.Length ? slotRects[i] : null;
            activeTooltip = Tooltip.Show(new TooltipSettings
            {
                message   = BuildTooltipText(a),
                target    = target,
                placement = TooltipPlacement.Top,
                useFade   = true,
            });
        }

        public void HideTooltip()
        {
            if (activeTooltip != null)
            {
                UnityEngine.Object.Destroy(activeTooltip.gameObject);
                activeTooltip = null;
            }
        }

        private static string BuildTooltipText(ManaAbility a)
        {
            var parts = new List<string> { $"<b>{a.Name}</b>" };
            switch (a.Kind)
            {
                case AbilityKind.Skill:
                    parts.Add("Skill  ·  Free");
                    if (a.CooldownTurns > 0) parts.Add($"Cooldown: {a.CooldownTurns} turn(s)");
                    break;
                case AbilityKind.Spell:
                    parts.Add($"Spell  ·  {Scripts.Data.ManaAbilities.CostIcons(a)}");
                    if (a.CastTimeSeconds > 0f) parts.Add($"Cast time: {a.CastTimeSeconds:0.0}s");
                    break;
                case AbilityKind.Item:
                    parts.Add($"Item  ·  {a.Charges}/{a.MaxStackSize} charges");
                    break;
            }
            return string.Join("\n", parts);
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
            // US-042: an item that casts a spell on use (ItemDefinition.OnUseSpellName, e.g. Sleep
            // Dart → Sleep) routes through the spell's targeting flow; the charge is only spent on
            // confirm. Falls through to plain-consume for ordinary items.
            if (TryHandleItemSpell(a)) return;

            if (a.TryConsumeCharge())
                Debug.Log($"[AbilityBar] Item '{a.Name}' used ({a.Charges}/{a.MaxStackSize} left).");
            else
                Debug.LogWarning($"[AbilityBar] Item '{a.Name}' empty (0/{a.MaxStackSize}). Buy/craft to restock.");
        }

        /// <summary>US-042: if the item is backed by an ItemDefinition with an OnUseSpellName, begin
        /// that spell's targeting flow and, on confirm, spend one charge + dispatch the spell + cost
        /// a turn (like a Skill — items are instant, no cast bar). Returns true if it handled the item
        /// (spell-backed), false to let the caller plain-consume.</summary>
        private bool TryHandleItemSpell(ManaAbility a)
        {
            if (a == null || string.IsNullOrEmpty(a.SourceItemId)) return false;
            var def = Scripts.Data.Items.ItemLibrary.Get(a.SourceItemId);
            if (def == null || string.IsNullOrEmpty(def.OnUseSpellName)) return false;

            var spell = ResolveSpellByName(def.OnUseSpellName);
            if (spell == null)
            {
                Debug.LogWarning($"[AbilityBar] Item '{a.Name}' OnUseSpellName '{def.OnUseSpellName}' has no SpellDefinition — falling back to plain use.");
                return false;
            }
            if (a.Charges <= 0)
            {
                Debug.LogWarning($"[AbilityBar] Item '{a.Name}' empty (0/{a.MaxStackSize}).");
                return true; // handled (refused) — don't double-message via plain-consume
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return true;
            var caster = g.Actors.SelectedActor;

            Scripts.Managers.TargetingMode.Begin(spell, caster,
                onConfirm: targets =>
                {
                    if (!a.TryConsumeCharge())
                    {
                        Debug.LogWarning($"[AbilityBar] '{a.Name}' emptied mid-pick — nothing cast.");
                        return;
                    }
                    Debug.Log($"[AbilityBar] Item '{a.Name}' cast {def.OnUseSpellName} on {targets.Count} target(s) ({a.Charges}/{a.MaxStackSize} left).");
                    foreach (var t in targets)
                        Scripts.Managers.SpellEffectDispatcher.Cast(spell, caster, t);
                    // Costs a turn — advance timeline to next enemy (like a Skill).
                    g.ManaPoolManager?.OnBankButtonClicked();
                },
                onCancel: () => Debug.Log($"[AbilityBar] Item '{a.Name}' cancelled — no charge spent."));
            return true;
        }

        /// <summary>Resolve a SpellDefinition by its ManaAbility name (item OnUseSpellName routing).
        /// Relies on the 1:1 ability↔spell invariant (§4.1) so names are unique.</summary>
        private static SpellDefinition ResolveSpellByName(string spellAbilityName)
        {
            foreach (var s in SpellLibrary.All)
                if (s.Ability != null && s.Ability.Name == spellAbilityName) return s;
            return null;
        }

        // ── Skill: free; no cast bar; resolves on target confirm; then advances the turn ──
        private void HandleSkill(ManaAbility a)
        {
            var caster = g.Actors.SelectedActor;

            // Cooldown gate — a Skill is locked for ManaAbility.CooldownTurns turn-cycles after use.
            if (Scripts.Managers.SkillCooldownManager.IsOnCooldown(caster, a))
            {
                int turns = Scripts.Managers.SkillCooldownManager.GetRemaining(caster, a);
                Debug.LogWarning($"[AbilityBar] '{a.Name}' on cooldown ({turns} turn(s) left).");
                if (caster != null) g.CombatTextManager?.Spawn($"{turns}", caster.transform.position, "Miss");
                return;
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var spell = ResolveSpell(a);
            if (spell == null) { Debug.LogWarning($"[AbilityBar] Skill '{a.Name}' has no SpellDefinition wired."); return; }

            // Teleport is its own flow — picks an empty tile and instantly relocates the caster,
            // then checks for a pincer the new position completes.
            if (spell.IsTeleport) { HandleTeleport(spell, a, caster, canvas); return; }

            Scripts.Managers.TargetingMode.Begin(spell, caster,
                onConfirm: targets =>
                {
                    Debug.Log($"[AbilityBar] Skill '{a.Name}' used on {targets.Count} target(s).");
                    foreach (var t in targets)
                        Scripts.Managers.SpellEffectDispatcher.Cast(spell, caster, t);
                    // Start the cooldown, then "cost a turn" — advance timeline to next enemy.
                    Scripts.Managers.SkillCooldownManager.Begin(caster, a);
                    g.ManaPoolManager?.OnBankButtonClicked();
                },
                onCancel: () => Debug.Log($"[AbilityBar] Skill '{a.Name}' cancelled — turn not spent."));
        }

        /// <summary>Tile-pick → relocate caster → fire any pincer the new position completes.
        /// Direct tile-picker (bypasses SpellEffectDispatcher because there's no "target actor").</summary>
        private void HandleTeleport(SpellDefinition spell, ManaAbility ability, Scripts.Instances.Actor.ActorInstance caster, GameObject canvas)
        {
            if (caster == null) { Debug.LogWarning("[AbilityBar] Teleport requires a selected hero."); return; }
            var board = g.Board;
            int w = board != null ? board.columnCount : 6;
            int h = board != null ? board.rowCount    : 8;

            Scripts.Managers.TargetingMode.DismissAnyActive();

            Scripts.Factories.TargetPickerOverlayFactory.CreateTilePicker(
                canvas.transform, spell, caster, w, h,
                onPickedTile: anchor =>
                {
                    // Refuse if the destination tile is already occupied.
                    var occupant = Scripts.Services.TargetShapeResolver.FindActorAt(anchor);
                    if (occupant != null)
                    {
                        Debug.LogWarning($"[AbilityBar] Teleport refused — {anchor} is occupied by {occupant.name}.");
                        return;
                    }

                    // Play a quick "vanish" at the origin, then move + "appear" at the destination.
                    var vfx = g.VisualEffectManager;
                    if (vfx != null)
                    {
                        var castAsset = Scripts.Libraries.VisualEffectLibrary.Get(spell.CastVfxName);
                        if (castAsset != null) vfx.Spawn(castAsset, caster.transform.position);
                    }

                    var dest = Scripts.Utilities.Geometry.CalculatePositionByLocation(anchor);
                    caster.location = anchor;
                    caster.transform.position = dest;

                    AnnouncementWindow.Announce($"{caster.characterClass} casts Teleport");
                    g.AudioManager?.Play("Quicken"); // zippy blink cue

                    if (vfx != null)
                    {
                        var impactAsset = Scripts.Libraries.VisualEffectLibrary.Get(spell.ImpactVfxName);
                        if (impactAsset != null) vfx.Spawn(impactAsset, dest);
                    }
                    Debug.Log($"[AbilityBar] {caster.name} teleported to {anchor}.");

                    // Did the new position complete a pincer? PincerAttackManager queues it.
                    bool pincer = g.PincerAttackManager != null && g.PincerAttackManager.Check(Scripts.Models.Team.Hero, caster);
                    if (pincer) Debug.Log("[AbilityBar] Teleport landed a pincer!");

                    // Start the cooldown, then cost a turn — advance the timeline to next enemy.
                    Scripts.Managers.SkillCooldownManager.Begin(caster, ability);
                    g.ManaPoolManager?.OnBankButtonClicked();
                },
                onCancelled: () => Debug.Log("[AbilityBar] Teleport cancelled — turn not spent."));
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

            // US-012: a Silenced caster cannot cast Spells. Refuse the click (Skills/Items unaffected).
            if (caster != null && Scripts.Managers.BuffSystem.Has(caster, Scripts.Data.Buffs.Silenced.Id))
            {
                Debug.LogWarning($"[AbilityBar] '{a.Name}' refused — {caster.name} is Silenced.");
                g.CombatTextManager?.Spawn("Silenced!", caster.transform.position, "Miss");
                return;
            }

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
            // US-012: if the bound hero is Silenced, their Spell slots render blocked + non-interactable.
            var owner = g.Actors.SelectedActor;
            bool silenced = active && owner != null
                && Scripts.Managers.BuffSystem.Has(owner, Scripts.Data.Buffs.Silenced.Id);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (!active)
                {
                    if (buttons[i] != null && buttons[i].gameObject.activeSelf)
                        buttons[i].gameObject.SetActive(false);
                    SetIcon(i, null);
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
                    SetIcon(i, null);
                    continue;
                }

                // Skill cooldown for the bound hero (0 for Spells/Items, which don't use it).
                int cooldown = a.Kind == AbilityKind.Skill
                    ? Scripts.Managers.SkillCooldownManager.GetRemaining(owner, a)
                    : 0;
                bool onCooldown = cooldown > 0;

                bool affordable;
                switch (a.Kind)
                {
                    case AbilityKind.Item:  affordable = a.Charges > 0; break;
                    case AbilityKind.Skill: affordable = !onCooldown; break; // free, but locked while recharging
                    default:                affordable = bank != null && bank.CanAfford(a.Cost); break;
                }
                // US-012: Silenced blocks Spell-kind slots (Skills/Items still usable).
                bool blockedBySilence = a.Kind == AbilityKind.Spell && silenced;
                if (blockedBySilence) affordable = false;
                if (buttons[i] != null) buttons[i].interactable = affordable;
                if (nameLabels[i] != null)
                {
                    nameLabels[i].text = a.Name;
                    // Fade the skill name while it recharges (slot reads as "disabled").
                    var nc = nameLabels[i].color; nc.a = onCooldown ? 0.35f : 1f; nameLabels[i].color = nc;
                }
                // On cooldown: show the turns-remaining count where the cost normally goes.
                if (costLabels[i] != null) costLabels[i].text = onCooldown ? $"{cooldown}" : ManaAbilities.CostIcons(a);
                if (frames[i] != null)
                {
                    var fc = blockedBySilence ? SilencedFrameColor : FrameColorFor(a, affordable);
                    if (onCooldown) fc.a *= 0.4f; // fade the whole slot out while recharging
                    frames[i].color = fc;
                }
                // US-076: spell icon sprite (glyph fallback = no icon = just text labels).
                Sprite iconSprite = null;
                if (a.Kind == AbilityKind.Spell && !string.IsNullOrEmpty(a.Name))
                    SpriteLibrary.SpellIcons.TryGetValue(a.Name, out iconSprite);
                SetIcon(i, iconSprite);
            }
        }

        /// <summary>Blocked-by-Silence slot tint (US-012). A solid red "blocked" state; the exact
        /// §4.5 diagonal-stripe overlay sprite is a future visual refinement.</summary>
        private static readonly Color SilencedFrameColor = new Color(0.50f, 0.12f, 0.12f, 0.95f);

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

        private void SetIcon(int i, Sprite sprite)
        {
            if (iconImages == null || i >= iconImages.Length || iconImages[i] == null) return;
            iconImages[i].sprite  = sprite;
            iconImages[i].enabled = sprite != null;
        }
    }
}
