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

namespace Scripts.Hub
{
    /// <summary>
    /// HUBSECTION - Abstract base class for every Hub panel (Shop, Blacksmith, Equip, ...).
    /// <para>PURPOSE: Each section is a MonoBehaviour attached to its own panel GameObject in the
    /// Hub scene. HubManager discovers all sections at startup, hides every panel, then activates
    /// one panel at a time via <see cref="Activate"/>.</para>
    /// <para>LIFECYCLE:
    /// <list type="bullet">
    /// <item>Awake → cache transform references (ItemList, DetailLabel, GoldLabel, etc).</item>
    /// <item>Activate(show=true) → gameObject.SetActive(true), OnActivated(), Refresh().</item>
    /// <item>Activate(show=false) → OnDeactivated(), gameObject.SetActive(false).</item>
    /// </list>
    /// Derived classes override <see cref="OnActivated"/>, <see cref="OnDeactivated"/>, and
    /// <see cref="Refresh"/>. Refresh is called whenever shared state mutates (gold, inventory,
    /// loadouts) and the panel needs to redraw.</para>
    /// <para>RELATED FILES: HubManager.cs, HubTheme.cs, HubItemRowFactory.cs</para>
    /// </summary>
    public abstract class HubSection : MonoBehaviour
    {
        protected HubManager Hub { get; private set; }
        protected RectTransform Panel => (RectTransform)transform;

        /// <summary>Bind this section to the owning HubManager. Called once during initialization.</summary>
        public void Bind(HubManager hub) => Hub = hub;

        /// <summary>Activates (true) or deactivates (false) this section's panel.</summary>
        public void Activate(bool show)
        {
            if (show)
            {
                gameObject.SetActive(true);
                OnActivated();
                Refresh();
            }
            else
            {
                OnDeactivated();
                gameObject.SetActive(false);
            }
        }

        /// <summary>Called when this section becomes visible. Override to wire buttons / preselect rows.</summary>
        protected virtual void OnActivated() { }

        /// <summary>Called when this section is about to hide. Override to unwire ephemeral state.</summary>
        protected virtual void OnDeactivated() { }

        /// <summary>Repopulates the panel from current shared state. Called after Activate and every PersistAndRefresh.</summary>
        public virtual void Refresh() { }

        // ---- Helpers for the common panel children (guaranteed by HubScaffold) ----

        /// <summary>Finds a Transform child by path, or null. Logs a one-time warning if missing.</summary>
        protected Transform Find(string childPath)
        {
            var t = transform.Find(childPath);
            if (t == null) Debug.LogWarning($"[{GetType().Name}] missing child '{childPath}' under '{name}'.");
            return t;
        }

        protected TextMeshProUGUI FindLabel(string childPath)
        {
            var t = Find(childPath);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        protected Button FindButton(string childPath)
        {
            var t = Find(childPath);
            return t != null ? t.GetComponent<Button>() : null;
        }

        protected RectTransform FindList(string childPath)
        {
            var t = Find(childPath);
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        /// <summary>Destroys every child of a list container (typically the Content of a ScrollRect).</summary>
        protected static void ClearList(RectTransform list)
        {
            if (list == null) return;
            for (int i = list.childCount - 1; i >= 0; i--)
                Object.Destroy(list.GetChild(i).gameObject);
        }

        /// <summary>Wires a button's onClick to action, first clearing any existing listeners.</summary>
        protected static void Wire(Button btn, System.Action action)
        {
            if (btn == null || action == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action());
        }
    }
}
