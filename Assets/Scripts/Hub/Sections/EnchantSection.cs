using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

namespace Scripts.Hub.Sections
{
    /// <summary>
    /// ENCHANTSECTION - Time-gated elemental enchantments for weapons.
    /// <para>PURPOSE: The Enchanter imbues a weapon with a chosen affinity (Flame, Frost,
    /// Spark, Shadow) in exchange for one matching essence + arcane dust + gold. Unlike the
    /// Blacksmith (one linear +N chain), enchanting branches — the player picks the element,
    /// and the same base weapon yields up to four distinct enchanted variants.</para>
    /// <para>FLOW: Pick a weapon → four element tiles appear in the detail pane → confirm
    /// starts a timed job (consume-at-start / collect-at-finish, identical to Blacksmith).</para>
    /// <para>RELATED FILES: EnchantLibrary.cs, EnchantRecipe.cs, CraftJobHelper.cs, ItemData_Essences.cs</para>
    /// </summary>
    public class EnchantSection : HubSection
    {
        private ItemDefinition selectedBase;
        private Element selectedElement;
        private bool elementChosen;
        private string selectedJobId;
        private Coroutine tickLoop;

        protected override void OnActivated()
        {
            EnchantLibrary.Ensure();
            var confirm = FindButton("ConfirmButton");
            Wire(confirm, ConfirmPressed);

            if (tickLoop != null) StopCoroutine(tickLoop);
            tickLoop = StartCoroutine(TickLoop());
        }

        private void OnDisable()
        {
            if (tickLoop != null) { StopCoroutine(tickLoop); tickLoop = null; }
        }

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                yield return wait;
                if (!isActiveAndEnabled) yield break;
                Refresh();
            }
        }

        public override void Refresh()
        {
            var list = FindList("ItemList/Viewport/Content");
            if (list == null) return;
            ClearList(list);

            // Pending enchantments surface at the top (Ready first, then soonest-done).
            foreach (var job in CraftJobHelper.ForStation(CraftStation.Enchanter).OrderByDescending(j => j.IsReady).ThenBy(j => j.Remaining))
                AddJobRow(list, job);

            // Then eligible base weapons — only clean (non-upgrade, non-enchanted) owned weapons.
            foreach (var item in EligibleBaseWeapons())
                AddBaseWeaponRow(list, item);

            UpdateDetail();
            UpdateConfirmButton();
        }

        // -----------------------------------------------------------------
        // Row builders
        // -----------------------------------------------------------------

        private IEnumerable<ItemDefinition> EligibleBaseWeapons()
        {
            return Hub.Inventory.All()
                .Select(e => e.Definition)
                .Where(d => d != null
                            && d.Slot == EquipmentSlot.Weapon
                            && !d.Id.Contains("_plus")
                            && !EnchantLibrary.IsEnchanted(d.Id)
                            && !CraftJobHelper.IsHeldByAnyVendor(d.Id))
                .Distinct()
                .OrderByDescending(d => (int)d.Rarity)
                .ThenBy(d => d.DisplayName);
        }

        private void AddBaseWeaponRow(Transform list, ItemDefinition item)
        {
            var row = HubItemRowFactory.Create(list);
            HubItemRowFactory.SetIcon(row, item);
            HubItemRowFactory.SetLabel(row, item.DisplayName);
            HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(item.Rarity));

            // When this weapon is the active selection, show the chosen element inline so the
            // player can see what Confirm will produce without scanning down to the detail pane.
            string sub = (selectedBase != null && selectedBase.Id == item.Id && elementChosen)
                ? $"<color=#CCBB77>Affinity:</color> {ElementLabel(selectedElement)}  <color=#888888>(tap to cycle)</color>"
                : "<color=#CCBB77>Imbue with an elemental essence</color>";
            HubItemRowFactory.SetSubLabel(row, sub);

            var captured = item;
            row.GetComponent<Button>().onClick.AddListener(() => OnWeaponRowTapped(captured));
            HubItemRowFactory.SetSelected(row, selectedBase != null && selectedBase.Id == item.Id && string.IsNullOrEmpty(selectedJobId));
        }

        /// <summary>First tap on a weapon selects it + auto-picks the first affordable element.
        /// Subsequent taps on the same weapon cycle through the four elements.</summary>
        private void OnWeaponRowTapped(ItemDefinition item)
        {
            selectedJobId = null;
            if (selectedBase != null && selectedBase.Id == item.Id && elementChosen)
            {
                selectedElement = NextElement(selectedElement);
            }
            else
            {
                selectedBase = item;
                selectedElement = PickDefaultElement(item);
                elementChosen = true;
            }
            Refresh();
        }

        private Element PickDefaultElement(ItemDefinition item)
        {
            foreach (Element el in System.Enum.GetValues(typeof(Element)))
            {
                var r = EnchantLibrary.GetRecipe(item.Id, el);
                if (r != null && r.CanEnchant(Hub.Inventory)) return el;
            }
            return Element.Flame;
        }

        private static Element NextElement(Element e) => e switch
        {
            Element.Flame  => Element.Frost,
            Element.Frost  => Element.Spark,
            Element.Spark  => Element.Shadow,
            Element.Shadow => Element.Flame,
            _ => Element.Flame,
        };

        private void AddJobRow(Transform list, CraftJob job)
        {
            var row = HubItemRowFactory.Create(list);
            var resultDef = ItemLibrary.Get(job.ResultItemId);
            if (resultDef != null)
            {
                HubItemRowFactory.SetIcon(row, resultDef);
                HubItemRowFactory.SetLabel(row, resultDef.DisplayName);
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(resultDef.Rarity));
            }
            else
            {
                HubItemRowFactory.SetLabel(row, job.ResultItemId);
            }

            if (job.IsReady)
            {
                HubItemRowFactory.SetSubLabel(row, "<color=#55DD55><b>Ready — tap to collect</b></color>");
                HubItemRowFactory.SetProgress(row, 1f);
            }
            else
            {
                HubItemRowFactory.SetSubLabel(row,
                    $"<color=#AA88FF>Enchanting…  {CraftJobHelper.FormatRemaining(job.Remaining)}</color>");
                HubItemRowFactory.SetProgress(row, job.Progress01);
            }

            var capturedId = job.JobId;
            row.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedBase = null;
                elementChosen = false;
                selectedJobId = capturedId;
                Refresh();
            });
            HubItemRowFactory.SetSelected(row, selectedJobId == job.JobId);
        }

        // -----------------------------------------------------------------
        // Confirm button state machine
        // -----------------------------------------------------------------

        private void ConfirmPressed()
        {
            // Collect path: a pending job is selected.
            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Enchanter).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { selectedJobId = null; Refresh(); return; }
                if (!job.IsReady) return;
                if (!CraftJobHelper.Collect(job, Hub.Inventory)) return;
                HubToast.Show($"Collected {ItemLibrary.Get(job.ResultItemId)?.DisplayName ?? job.ResultItemId}");
                selectedJobId = null;
                Hub.PersistAndRefresh();
                return;
            }

            // Picker path: a weapon is selected.
            // First press (no element yet) is a no-op — player still needs to pick the element
            // from the detail pane tiles. Second press with a valid element starts the job.
            if (selectedBase == null || !elementChosen) return;

            var recipe = EnchantLibrary.GetRecipe(selectedBase.Id, selectedElement);
            if (recipe == null) return;
            var started = CraftJobHelper.StartEnchant(recipe, Hub.Inventory);
            if (started == null) return;

            HubToast.Show($"Enchanting: {recipe.To.DisplayName}  ({CraftJobHelper.FormatRemaining(started.Remaining)})");
            selectedBase = null;
            elementChosen = false;
            selectedJobId = started.JobId;
            Hub.PersistAndRefresh();
        }

        private void UpdateConfirmButton()
        {
            var btn = FindButton("ConfirmButton");
            if (btn == null) return;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Enchanter).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { btn.interactable = false; if (label != null) label.text = "—"; return; }
                if (job.IsReady) { btn.interactable = true; if (label != null) label.text = "Collect"; }
                else { btn.interactable = false; if (label != null) label.text = $"Enchanting  {CraftJobHelper.FormatRemaining(job.Remaining)}"; }
                return;
            }

            if (selectedBase == null)
            {
                btn.interactable = false;
                if (label != null) label.text = "Select a weapon";
                return;
            }
            if (!elementChosen)
            {
                btn.interactable = false;
                if (label != null) label.text = "Choose an element";
                return;
            }

            var recipe = EnchantLibrary.GetRecipe(selectedBase.Id, selectedElement);
            if (recipe == null)
            {
                btn.interactable = false;
                if (label != null) label.text = "Unavailable";
                return;
            }

            bool can = recipe.CanEnchant(Hub.Inventory);
            btn.interactable = can;
            float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(recipe.GoldCost, 1 + recipe.Materials.Sum(m => m.count));
            if (label != null) label.text = can ? $"Start Enchant  ({FormatDuration(seconds)})" : "Missing materials";
        }

        // -----------------------------------------------------------------
        // Detail panel — doubles as the element picker
        // -----------------------------------------------------------------

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;

            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Enchanter).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { detail.text = "<b>Enchanter</b>\nJob complete."; return; }
                var result = ItemLibrary.Get(job.ResultItemId);
                string resultName = result?.DisplayName ?? job.ResultItemId;
                if (job.IsReady)
                {
                    detail.text = $"<b>{resultName}</b>\n<color=#55DD55>The infusion is stable. Ready to collect.</color>";
                }
                else
                {
                    detail.text = $"<b>{resultName}</b>\nElemental infusion in progress…\nTime remaining: <b>{CraftJobHelper.FormatRemaining(job.Remaining)}</b>\n\n"
                                + "Return when the magic has settled.";
                }
                return;
            }

            if (selectedBase == null)
            {
                detail.text = "<b>Enchanter</b>\nBring me a weapon and an elemental essence. I will weave them into something greater.\n\nEach weapon accepts one affinity: Flame, Frost, Spark, or Shadow.";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{selectedBase.DisplayName}</b>");
            if (!string.IsNullOrEmpty(selectedBase.Description)) sb.AppendLine(selectedBase.Description);
            sb.AppendLine();
            sb.AppendLine("<b>Affinity preview</b> <color=#888888>(tap weapon row again to cycle)</color>");

            foreach (Element el in System.Enum.GetValues(typeof(Element)))
            {
                var recipe = EnchantLibrary.GetRecipe(selectedBase.Id, el);
                if (recipe == null) continue;

                bool picked = elementChosen && selectedElement == el;
                bool canAfford = recipe.CanEnchant(Hub.Inventory);
                string marker = picked ? "<b>► </b>" : "   ";
                string statDelta = DescribeDelta(recipe);
                string matList = FormatMaterials(recipe);

                string line = $"{marker}{ElementLabel(el)} — {statDelta}  •  {HubTheme.FormatGold(recipe.GoldCost)} + {matList}";
                sb.AppendLine(HubTheme.ColorByAffordable(line, canAfford));
            }

            if (elementChosen)
            {
                var activeRecipe = EnchantLibrary.GetRecipe(selectedBase.Id, selectedElement);
                if (activeRecipe != null)
                {
                    float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(activeRecipe.GoldCost, 1 + activeRecipe.Materials.Sum(m => m.count));
                    sb.AppendLine();
                    sb.AppendLine($"<color=#AA88FF>Infusion time: {FormatDuration(seconds)}</color>");
                }
            }

            detail.text = sb.ToString();
        }

        private string DescribeDelta(EnchantRecipe r)
        {
            var from = r.From; var to = r.To;
            var parts = new List<string>();
            AddDelta(parts, "STR", from.Strength, to.Strength);
            AddDelta(parts, "INT", from.Intelligence, to.Intelligence);
            AddDelta(parts, "AGI", from.Agility, to.Agility);
            AddDelta(parts, "WIS", from.Wisdom, to.Wisdom);
            AddDelta(parts, "LCK", from.Luck, to.Luck);
            return parts.Count == 0 ? "no stat change" : string.Join(", ", parts);
        }

        private static void AddDelta(List<string> parts, string label, float a, float b)
        {
            float d = b - a;
            if (Mathf.Abs(d) < 0.01f) return;
            parts.Add(d > 0 ? $"+{d:0} {label}" : $"{d:0} {label}");
        }

        private string FormatMaterials(EnchantRecipe recipe)
        {
            var parts = new List<string>();
            foreach (var (id, count) in recipe.Materials)
            {
                var def = ItemLibrary.Get(id);
                string name = def != null ? def.DisplayName : id;
                int owned = Hub.Inventory.CountOf(id);
                parts.Add($"{count}× {name} ({owned})");
            }
            return string.Join(", ", parts);
        }

        private static string ElementLabel(Element el) => el switch
        {
            Element.Flame  => "<color=#FF7744>Flame</color>",
            Element.Frost  => "<color=#66AAFF>Frost</color>",
            Element.Spark  => "<color=#FFE066>Spark</color>",
            Element.Shadow => "<color=#B088FF>Shadow</color>",
            _ => el.ToString(),
        };

        private static string FormatDuration(float seconds)
        {
            if (seconds < 60f) return $"{Mathf.CeilToInt(seconds)}s";
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds - mins * 60f);
            return secs == 0 ? $"{mins}m" : $"{mins}m{secs:00}s";
        }
    }
}
