using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;

namespace Scripts.Canvas
{
    /// <summary>
    /// BESTIARYVIEW - Runtime controller for the Bestiary scene.
    ///
    /// <para>Walks every entry registered in <see cref="ActorLibrary"/> as one page each, rendering
    /// name, portrait, stats, abilities, and lore (description + expectations + trivia). Prev/Next
    /// buttons cycle pages; swipe gestures (horizontal drag) also navigate, mobile-first.</para>
    ///
    /// <para>References are wired by the BestiaryBuilder at scene build; the view stays passive
    /// until <see cref="Refresh"/> is called from <see cref="Start"/>.</para>
    /// </summary>
    public sealed class BestiaryView : MonoBehaviour
    {
        // Wired by BestiaryBuilder via name-lookup (cheap, code-only — no SerializeField).
        /// <summary>Method-on-MonoBehaviour for the Back button. The BestiaryBuilder wires this via
        /// SceneBuilderHelper.WireOnClick (persistent listener), which requires a target deriving
        /// from UnityEngine.Object — a lambda's compiler-generated closure doesn't qualify.</summary>
        public void OnBackButtonClicked()
        {
            Scripts.Helpers.SceneHelper.Fade.ToTitleScreen();
        }

        public TMP_Text TitleLabel;
        public TMP_Text PageLabel;
        public TMP_Text NameLabel;
        public TMP_Text ClassLabel;
        public TMP_Text StatsBlock;
        public TMP_Text AbilitiesBlock;
        public TMP_Text LoreBlock;
        public Image   PortraitImage;
        public Button  PrevButton;
        public Button  NextButton;

        public const float SwipeThresholdPx = 80f;

        private List<ActorData> pages;
        private int index;
        private Vector2 swipeStart;
        private bool swiping;

        private void Start()
        {
            BuildPages();
            if (PrevButton != null) PrevButton.onClick.AddListener(() => Step(-1));
            if (NextButton != null) NextButton.onClick.AddListener(() => Step(+1));
            Refresh();
        }

        private void BuildPages()
        {
            pages = new List<ActorData>();
            var actors = ActorLibrary.Actors;
            if (actors == null) return;
            // US-093: only Enemy-tagged entries (filters out heroes, NPCs, etc.)
            foreach (var kv in actors)
                if (kv.Value != null && kv.Value.InGroups(ActorTag.Enemy))
                    pages.Add(kv.Value);
            pages = pages.OrderBy(d => d.CharacterClass.ToString()).ToList();
        }

        private static bool IsSeen(ActorData d)
        {
            return Scripts.Helpers.ProfileHelper.CurrentProfile?.CurrentSave?.Bestiary?.Get(d.CharacterClass)?.Seen ?? false;
        }

        public void Step(int delta)
        {
            if (pages == null || pages.Count == 0) return;
            index = (index + delta + pages.Count) % pages.Count;
            Refresh();
        }

        private void Refresh()
        {
            if (pages == null || pages.Count == 0)
            {
                if (NameLabel != null) NameLabel.text = "(no entries)";
                return;
            }

            var d = pages[index];
            bool seen = IsSeen(d);
            if (TitleLabel != null) TitleLabel.text = "BESTIARY";
            if (PageLabel  != null) PageLabel.text  = $"{index + 1} / {pages.Count}";

            if (seen)
            {
                if (NameLabel  != null) NameLabel.text  = string.IsNullOrEmpty(d.CharacterName) ? d.CharacterClass.ToString() : d.CharacterName;
                if (ClassLabel != null) ClassLabel.text = $"{d.CharacterClass} · Lv. {d.Level}";
                if (PortraitImage != null)
                {
                    PortraitImage.sprite = d.Portrait;
                    PortraitImage.enabled = d.Portrait != null;
                    PortraitImage.color = Color.white;
                }
                if (StatsBlock     != null) StatsBlock.text     = FormatStats(d);
                if (AbilitiesBlock != null) AbilitiesBlock.text = FormatAbilities(d);
                if (LoreBlock      != null) LoreBlock.text      = FormatLore(d);
            }
            else
            {
                // US-093: unseen entry — silhouette portrait + "???" text
                if (NameLabel  != null) NameLabel.text  = "???";
                if (ClassLabel != null) ClassLabel.text = "<i>Unencountered</i>";
                if (PortraitImage != null)
                {
                    PortraitImage.sprite  = d.Portrait;
                    PortraitImage.enabled = d.Portrait != null;
                    PortraitImage.color   = Color.black; // silhouette
                }
                if (StatsBlock     != null) StatsBlock.text     = "<i>(not yet encountered)</i>";
                if (AbilitiesBlock != null) AbilitiesBlock.text = "<i>???</i>";
                if (LoreBlock      != null) LoreBlock.text      = "<i>Defeat or Scan this enemy to reveal its entry.</i>";
            }
        }

        private static string FormatStats(ActorData d)
        {
            var s = d.BaseStats;
            if (s == null) return "<i>(no stats)</i>";
            // Fix #5: stats are floats; format as integers so the panel reads "100" not "100.0".
            return
                $"<b>HP</b>  {s.MaxHP:0}\n" +
                $"<b>STR</b> {s.Strength:0}\n" +
                $"<b>VIT</b> {s.Vitality:0}\n" +
                $"<b>AGI</b> {s.Agility:0}\n" +
                $"<b>SPD</b> {s.Speed:0}\n" +
                $"<b>INT</b> {s.Intelligence:0}\n" +
                $"<b>WIS</b> {s.Wisdom:0}\n" +
                $"<b>LCK</b> {s.Luck:0}";
        }

        private static string FormatAbilities(ActorData d)
        {
            if (d.Abilities == null || d.Abilities.Count == 0) return "<i>(none)</i>";
            var lines = new List<string>();
            foreach (var a in d.Abilities) if (a != null) lines.Add($"• {a.name}");
            return string.Join("\n", lines);
        }

        private static string FormatLore(ActorData d)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(d.Description))  parts.Add(d.Description);
            if (!string.IsNullOrEmpty(d.Expectations)) parts.Add(d.Expectations);
            if (!string.IsNullOrEmpty(d.Lore))         parts.Add(d.Lore);
            if (d.Trivia != null)
                foreach (var t in d.Trivia)
                    if (!string.IsNullOrEmpty(t)) parts.Add($"• {t}");
            return parts.Count == 0 ? "<i>(no lore yet)</i>" : string.Join("\n\n", parts);
        }

        // ── Swipe gesture (horizontal drag) ──
        private void Update()
        {
            // Mouse / single-finger horizontal swipe to flip pages.
            if (Input.GetMouseButtonDown(0)) { swipeStart = Input.mousePosition; swiping = true; }
            else if (Input.GetMouseButtonUp(0) && swiping)
            {
                swiping = false;
                Vector2 d = (Vector2)Input.mousePosition - swipeStart;
                if (Mathf.Abs(d.x) > SwipeThresholdPx && Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.5f)
                    Step(d.x < 0 ? +1 : -1); // swipe-left = next page (forward)
            }
        }
    }
}
