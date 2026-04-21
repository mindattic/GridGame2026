using Scripts.Helpers;
using Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Factories
{
    /// <summary>
    /// STATROWFACTORY - Creates a PartyManager stat row (Label + Value + Bar).
    /// <para>PURPOSE: Single programmatic entry point for one row inside the
    /// PartyManager stats panel. Each row contains a label (e.g. "STR"), a
    /// numeric value TMP field, and a horizontal bar with Back / Fill / (hidden)
    /// Front images.</para>
    /// <para>CREATED HIERARCHY:
    /// <code>
    /// {statName}         (root, RectTransform anchored top-left)
    /// ├── Label          (TextMeshProUGUI — stat name)
    /// ├── Value          (TextMeshProUGUI — numeric value, default "0")
    /// └── Bar            (RectTransform + CanvasRenderer)
    ///     ├── Back       (Image)
    ///     ├── Fill       (Image)
    ///     └── Front      (Image, SetActive(false))
    /// </code>
    /// </para>
    /// <para>CALLED BY: PartyManagerScaffold.CreateStatRow (edit-time) and any
    /// future runtime PartyManager builder.</para>
    /// <para>RELATED FILES: PartyManager.cs, PartyManagerScaffold.cs</para>
    /// </summary>
    public static class StatRowFactory
    {
        /// <summary>Creates a stat row parented to <paramref name="parent"/>. Returns the row's RectTransform.</summary>
        public static RectTransform Create(RectTransform parent, string statName, float yPos)
        {
            var go = new GameObject(statName);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = new Vector2(0f, yPos);

            CreateTMPChild(rt, "Label", statName, new Vector2(-100f, -2f), new Vector2(100f, 32f));
            CreateTMPChild(rt, "Value", "0", new Vector2(165f, 0f), new Vector2(100f, 32f));

            var barGO = new GameObject("Bar");
            barGO.layer = LayerMask.NameToLayer("UI");
            var barRT = barGO.AddComponent<RectTransform>();
            barRT.SetParent(rt, false);
            barRT.anchorMin = barRT.anchorMax = new Vector2(0f, 1f);
            barRT.sizeDelta = new Vector2(100f, 32f);
            barRT.anchoredPosition = Vector2.zero;
            barGO.AddComponent<CanvasRenderer>();

            CreateBarImage(barRT, "Back", Vector2.zero);
            CreateBarImage(barRT, "Fill", Vector2.zero);
            var front = CreateBarImage(barRT, "Front", new Vector2(0f, 16f));
            if (front != null) front.gameObject.SetActive(false);

            return rt;
        }

        private static void CreateTMPChild(RectTransform parent, string name, string text, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.AddComponent<CanvasRenderer>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
        }

        private static RectTransform CreateBarImage(RectTransform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 32f);
            rt.anchoredPosition = pos;
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<Image>();
            return rt;
        }
    }
}
