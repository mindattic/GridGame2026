using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;
using Scripts.Models;
using Scripts.Libraries;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Factories
{
    /// <summary>
    /// SPELLCASTBARFACTORY - Builds a <see cref="SpellCastBar"/> as a spell-sprite ICON that loads
    /// left→right on a lane just BELOW the timeline, in parallel with the enemy icons (US-#3).
    /// Each call spawns its own icon; concurrent casts <b>stack</b> one lane lower (handled by
    /// SpellCastBar's slot manager). The icon is the spell's placeholder sprite (already color-coded
    /// by its mana cost), with the spell name as a small label above it.
    /// </summary>
    public static class SpellCastBarFactory
    {
        public const float IconSize = 28f;
        // Fallback geometry when no timeline exists in the scene (casting normally only happens in
        // the Game scene, which has a timeline; this keeps the call safe elsewhere).
        public const float FallbackHalfWidth = 450f;
        public const float FallbackTopY = -300f;
        public const float FallbackLaneStride = 32f;

        public static SpellCastBar Create(Transform canvas, ManaAbility spell, Action onResolved,
            Scripts.Instances.Actor.ActorInstance caster = null)
        {
            if (spell == null) return null;

            var sprite = ResolveSpellSprite(spell.Name);
            int lane = SpellCastBar.NextFreeSlot();

            RectTransform iconRT;
            float leftX, rightX;

            var tb = g.TimelineBar;
            if (tb != null)
            {
                // Preferred path: a real lane below the enemy rows, sharing the bar's u→x space so
                // the cast visibly loads in parallel with the enemy icons.
                iconRT = tb.CreateCastLaneIcon(sprite, Color.white, lane, out leftX, out rightX);
            }
            else
            {
                // Fallback: a canvas-anchored traveling icon (no timeline to ride).
                if (canvas == null) return null;
                var go = new GameObject($"SpellCastIcon_{spell.Name}", typeof(RectTransform), typeof(Image));
                go.layer = LayerMask.NameToLayer("UI");
                iconRT = (RectTransform)go.transform;
                iconRT.SetParent(canvas, false);
                iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 1f);
                iconRT.pivot = new Vector2(0.5f, 0.5f);
                iconRT.sizeDelta = new Vector2(IconSize, IconSize);
                leftX = -FallbackHalfWidth;
                rightX = FallbackHalfWidth;
                iconRT.anchoredPosition = new Vector2(leftX, FallbackTopY - lane * FallbackLaneStride);
                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.color = sprite != null ? Color.white : ColorForSpell(spell);
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            // Small spell-name label above the icon so the player can read what's casting.
            AttachLabel(iconRT, spell.Name);

            var bar = iconRT.gameObject.AddComponent<SpellCastBar>();
            bar.Begin(spell.Name, spell.CastTimeSeconds, iconRT, leftX, rightX, lane, onResolved, caster);
            return bar;
        }

        /// <summary>The authored placeholder sprite for the spell (Sprites/Spells/{name}), or null.</summary>
        private static Sprite ResolveSpellSprite(string spellName)
        {
            var icons = SpriteLibrary.SpellIcons;
            if (icons != null && icons.TryGetValue(spellName, out var s)) return s;
            return null;
        }

        private static void AttachLabel(RectTransform iconRT, string text)
        {
            var font = BorrowSceneFont();
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.layer = iconRT.gameObject.layer;
            var lrt = (RectTransform)labelGO.transform;
            lrt.SetParent(iconRT, false);
            lrt.anchorMin = new Vector2(0.5f, 1f);
            lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 2f);
            lrt.sizeDelta = new Vector2(120f, 16f);
            var lbl = labelGO.GetComponent<TextMeshProUGUI>();
            if (font != null) lbl.font = font;
            lbl.fontSize = 13;
            lbl.alignment = TextAlignmentOptions.Bottom;
            lbl.color = new Color(0.95f, 0.92f, 0.7f);
            lbl.text = text;
            lbl.raycastTarget = false;
        }

        /// <summary>Pick a representative color for the spell — the color of its <b>dominant</b>
        /// mana cost. Used only for the no-sprite fallback tint (the authored sprite already encodes
        /// the spell's color). Falls back to a neutral cast-blue if the recipe is null/empty.</summary>
        private static Color ColorForSpell(ManaAbility spell)
        {
            if (spell.Cost != null && spell.Cost.Costs.Count > 0)
            {
                var dominant = spell.Cost.Costs[0];
                for (int i = 1; i < spell.Cost.Costs.Count; i++)
                    if (spell.Cost.Costs[i].Amount > dominant.Amount) dominant = spell.Cost.Costs[i];
                var c = ManaOrbLine.ColorFor(dominant.Type);
                c.a = 0.92f;
                return c;
            }
            return new Color(0.35f, 0.65f, 1f, 0.85f);
        }

        private static TMP_FontAsset BorrowSceneFont()
        {
            var any = UnityEngine.Object.FindFirstObjectByType<TextMeshProUGUI>();
            return any != null ? any.font : null;
        }
    }
}
