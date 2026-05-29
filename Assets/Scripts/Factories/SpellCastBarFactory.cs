using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;
using Scripts.Models;

namespace Scripts.Factories
{
    /// <summary>
    /// SPELLCASTBARFACTORY - Builds a <see cref="SpellCastBar"/> under the Canvas, anchored just
    /// below the timeline. Each call spawns its own bar; concurrent casts <b>stack</b> below the
    /// previous (handled by SpellCastBar's slot manager). The bar's fill color comes from the
    /// spell's dominant mana cost color so a Fireball cast reads red, Frost reads blue, etc.
    /// </summary>
    public static class SpellCastBarFactory
    {
        public const float BarWidth = 900f;
        public const float BarHeight = 18f;
        public const float YBelowTimeline = -300f; // sits ~50px below the timeline (Row 2 area)

        public static SpellCastBar Create(Transform canvas, ManaAbility spell, Action onResolved,
            Scripts.Instances.Actor.ActorInstance caster = null)
        {
            if (canvas == null || spell == null) return null;

            var rootGO = new GameObject($"SpellCastBar_{spell.Name}", typeof(RectTransform), typeof(SpellCastBar));
            rootGO.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)rootGO.transform;
            rt.SetParent(canvas, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, YBelowTimeline);
            rt.sizeDelta = new Vector2(BarWidth, BarHeight + 22f);

            // Label (spell name) — kept small; the dominant info is the colored shrinking bar.
            var font = BorrowSceneFont();
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.layer = rootGO.layer;
            var lrt = (RectTransform)labelGO.transform;
            lrt.SetParent(rootGO.transform, false);
            lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(0f, 20f);
            var lbl = labelGO.GetComponent<TextMeshProUGUI>();
            if (font != null) lbl.font = font;
            lbl.fontSize = 18;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = new Color(0.95f, 0.92f, 0.7f);
            lbl.text = spell.Name;
            lbl.raycastTarget = false;

            // Track (faint outline) — gives the shrinking fill a frame to read against.
            var bgGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
            bgGO.layer = rootGO.layer;
            var bgRT = (RectTransform)bgGO.transform;
            bgRT.SetParent(rootGO.transform, false);
            bgRT.anchorMin = new Vector2(0.5f, 0f); bgRT.anchorMax = new Vector2(0.5f, 0f);
            bgRT.pivot = new Vector2(0.5f, 0f);
            bgRT.anchoredPosition = Vector2.zero;
            bgRT.sizeDelta = new Vector2(BarWidth, BarHeight);
            bgGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            // Fill — anchored to the RIGHT edge of the track so shrinking width pulls the LEFT
            // side rightward (bar visibly vanishes toward the right).
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.layer = rootGO.layer;
            var fillRT = (RectTransform)fillGO.transform;
            fillRT.SetParent(bgRT, false);
            fillRT.anchorMin = new Vector2(1f, 0f); fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.pivot     = new Vector2(1f, 0.5f);
            fillRT.anchoredPosition = Vector2.zero;
            fillRT.sizeDelta = new Vector2(BarWidth, 0f);
            fillGO.GetComponent<Image>().color = ColorForSpell(spell);

            var bar = rootGO.GetComponent<SpellCastBar>();
            bar.Begin(spell.Name, spell.CastTimeSeconds, fillRT, BarWidth, YBelowTimeline, onResolved, caster);
            return bar;
        }

        /// <summary>Pick a representative color for the spell — the color of its <b>dominant</b>
        /// mana cost (the cost line with the highest count; ties broken by first listed). Falls
        /// back to a neutral cast-blue if the recipe is null/empty. Uses the shared orb palette
        /// (<see cref="ManaOrbLine.ColorFor"/>) so the cast bar visually echoes the orbs it cost.</summary>
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
