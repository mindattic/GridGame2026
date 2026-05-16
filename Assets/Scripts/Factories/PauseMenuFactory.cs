using Scripts.Helpers;
using Scripts.Libraries;
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
    /// PAUSEMENUFACTORY - Creates the PauseMenu root overlay.
    /// <para>PURPOSE: Single programmatic entry point for the full-screen
    /// PauseMenu backdrop (RectTransform + Image + PauseMenu behaviour). The
    /// caller is responsible for populating children (ParallaxBackground, Inner
    /// container, buttons) — this factory only produces the bare root.</para>
    /// <para>CREATED HIERARCHY:
    /// <code>
    /// PauseMenu           (root, full-screen RectTransform)
    /// ├── RectTransform   (anchors 0,0 → 1,1; sizeDelta 0)
    /// ├── CanvasRenderer
    /// ├── Image           (darkBackdrop sprite, color rgba(0,0,0,0.768))
    /// └── PauseMenu       (Scripts.Managers.PauseMenu behaviour)
    /// </code>
    /// </para>
    /// <para>CALLED BY: GameBuilder (edit-time) and any future runtime Game
    /// builder.</para>
    /// <para>RELATED FILES: PauseMenu.cs, GameBuilder.cs</para>
    /// </summary>
    public static class PauseMenuFactory
    {
        /// <summary>Creates the PauseMenu root GameObject parented to <paramref name="parent"/> with the given backdrop sprite.</summary>
        public static GameObject Create(RectTransform parent, Sprite backdropSprite)
        {
            var go = new GameObject("PauseMenu");
            go.layer = 5;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.sprite = backdropSprite;
            img.color = new Color(0f, 0f, 0f, 0.7686275f);
            img.raycastTarget = false;
            go.AddComponent<Scripts.Managers.PauseMenu>();
            return go;
        }
    }
}
