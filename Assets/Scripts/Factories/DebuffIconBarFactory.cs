using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;
using Scripts.Instances.Actor;

namespace Scripts.Factories
{
    /// <summary>
    /// DEBUFFICONBARFACTORY - Builds the 3-cell debuff icon strip in the upper-right of an actor.
    ///
    /// <para>Each cell now has a <b>radial yellow ring</b> around the icon disk (Image with Filled
    /// type, Radial360 fill, Clockwise) that ticks down as the buff drains. Cell layout: ring
    /// behind disk + letter on top — no separate countdown label.</para>
    ///
    /// <para><see cref="EnsureAttached"/> is idempotent — safe to call on actors spawned mid-battle
    /// (reinforcements) so they get a bar without doubling up.</para>
    /// </summary>
    public static class DebuffIconBarFactory
    {
        /// <summary>Cached bars per actor — prevents double-attach.</summary>
        private static readonly Dictionary<ActorInstance, DebuffIconBar> attached =
            new Dictionary<ActorInstance, DebuffIconBar>();

        /// <summary>Attach a bar to <paramref name="actor"/> if one isn't already present. Safe to call repeatedly.</summary>
        public static DebuffIconBar EnsureAttached(ActorInstance actor)
        {
            if (actor == null) return null;
            if (attached.TryGetValue(actor, out var existing) && existing != null) return existing;

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return null;
            var bar = Create(canvas.transform, actor);
            if (bar != null) attached[actor] = bar;
            return bar;
        }

        public static DebuffIconBar Create(Transform canvas, ActorInstance owner)
        {
            if (canvas == null || owner == null) return null;

            var rootGO = new GameObject($"DebuffBar_{owner.name}",
                typeof(RectTransform), typeof(DebuffIconBar), typeof(WorldFollow));
            rootGO.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)rootGO.transform;
            rt.SetParent(canvas, false);

            float width = DebuffIconBar.MaxVisible * DebuffIconBar.IconSize
                        + (DebuffIconBar.MaxVisible - 1) * DebuffIconBar.IconSpacing;
            rt.sizeDelta = new Vector2(width, DebuffIconBar.IconSize + 6f);

            rootGO.GetComponent<WorldFollow>().Bind(owner.transform, new Vector3(0.35f, 0.65f, 0f));

            var font = BorrowSceneFont();

            var images  = new Image[DebuffIconBar.MaxVisible];
            var letters = new TMP_Text[DebuffIconBar.MaxVisible];
            var rings   = new Image[DebuffIconBar.MaxVisible];

            for (int i = 0; i < DebuffIconBar.MaxVisible; i++)
            {
                var cellGO = new GameObject($"Cell{i}", typeof(RectTransform));
                cellGO.layer = rootGO.layer;
                var crt = (RectTransform)cellGO.transform;
                crt.SetParent(rootGO.transform, false);
                crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f);
                crt.pivot = new Vector2(0f, 0.5f);
                crt.sizeDelta = new Vector2(DebuffIconBar.IconSize, DebuffIconBar.IconSize);
                crt.anchoredPosition = new Vector2(i * (DebuffIconBar.IconSize + DebuffIconBar.IconSpacing), 0f);

                // Ring (yellow outline, radial 360°, clockwise countdown) — child #0 so it renders BEHIND the disk.
                var ringGO = new GameObject("Ring", typeof(RectTransform), typeof(Image));
                ringGO.layer = cellGO.layer;
                var rrt = (RectTransform)ringGO.transform;
                rrt.SetParent(cellGO.transform, false);
                rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
                rrt.offsetMin = new Vector2(-3f, -3f);   // ring slightly larger than disk
                rrt.offsetMax = new Vector2( 3f,  3f);
                var ringImg = ringGO.GetComponent<Image>();
                ringImg.color = new Color(1f, 0.95f, 0.35f, 0.95f); // yellow
                ringImg.type = Image.Type.Filled;
                ringImg.fillMethod = Image.FillMethod.Radial360;
                ringImg.fillOrigin = (int)Image.Origin360.Top;
                ringImg.fillClockwise = true;
                ringImg.fillAmount = 1f;
                ringImg.raycastTarget = false;
                rings[i] = ringImg;

                // Disk (the colored icon body) — on top of the ring.
                var diskGO = new GameObject("Disk", typeof(RectTransform), typeof(Image));
                diskGO.layer = cellGO.layer;
                var drt = (RectTransform)diskGO.transform;
                drt.SetParent(cellGO.transform, false);
                drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
                drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
                var diskImg = diskGO.GetComponent<Image>();
                diskImg.color = Color.gray;
                diskImg.raycastTarget = false;
                images[i] = diskImg;

                // Letter on top of the disk.
                var letterGO = new GameObject("Letter", typeof(RectTransform), typeof(TextMeshProUGUI));
                letterGO.layer = cellGO.layer;
                var lrt = (RectTransform)letterGO.transform;
                lrt.SetParent(cellGO.transform, false);
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                var tmp = letterGO.GetComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.fontSize = 16;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = "";
                tmp.raycastTarget = false;
                letters[i] = tmp;
            }

            var bar = rootGO.GetComponent<DebuffIconBar>();
            bar.Bind(owner, images, letters, rings);
            return bar;
        }

        private static TMP_FontAsset BorrowSceneFont()
        {
            var any = UnityEngine.Object.FindFirstObjectByType<TextMeshProUGUI>();
            return any != null ? any.font : null;
        }
    }
}
