using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Config;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
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

namespace Scripts.Factories
{
    /// <summary>
    /// TARGETLINE2DFACTORY - Creates a canvas-space FFXII-style targeting arc.
    /// <para>PURPOSE: Builds a full-screen RectTransform hosting two tapered-ribbon Graphics
    /// (core + glow) and a bead Image. Parented under the main ScreenSpaceOverlay Canvas so
    /// the arc renders on top of the mana bar / HUD — the 3D variant sits beneath.</para>
    /// <para>WHEN TO USE: Targeting UI that must visually dominate (enemy select indicator,
    /// cast arc while a spell is loading). For effects that should feel rooted in the
    /// game world, use TargetLine3DFactory.</para>
    /// <para>CREATED HIERARCHY:
    /// <code>
    /// TargetLine2D (RectTransform, stretched to Canvas)
    /// ├── Glow (RectTransform, TargetLine2DGraphic — wider, low alpha)
    /// ├── Core (RectTransform, TargetLine2DGraphic — thin, full alpha)
    /// ├── Bead (RectTransform, Image — traveling direction indicator)
    /// └── TargetLine2DInstance (behavior)
    /// </code>
    /// </para>
    /// <para>RELATED FILES: TargetLine2DInstance.cs, TargetLine2DGraphic.cs, TargetLine3DFactory.cs, TargetLineManager.cs</para>
    /// </summary>
    public static class TargetLine2DFactory
    {
        /// <summary>Creates a new canvas-space targeting arc. Parent defaults to the scene's main Canvas.</summary>
        public static GameObject Create(RectTransform canvasRect = null)
        {
            var canvas = canvasRect != null ? canvasRect : FindMainCanvasRect();

            var root = new GameObject("TargetLine2D");
            root.layer = LayerMask.NameToLayer("UI");
            var rootRT = root.AddComponent<RectTransform>();
            if (canvas != null) rootRT.SetParent(canvas, false);
            StretchToParent(rootRT);

            // Glow first (behind core in sibling order).
            var glowGO = new GameObject("Glow");
            glowGO.layer = LayerMask.NameToLayer("UI");
            var glowRT = glowGO.AddComponent<RectTransform>();
            glowRT.SetParent(rootRT, false);
            StretchToParent(glowRT);
            var glowGraphic = glowGO.AddComponent<TargetLine2DGraphic>();
            glowGraphic.raycastTarget = false;
            glowGraphic.PeakWidth = TargetLineInstanceConfig.GlowWidth2D;
            glowGraphic.color = new Color(1f, 1f, 1f, TargetLineInstanceConfig.GlowAlpha);

            // Core on top of glow.
            var coreGO = new GameObject("Core");
            coreGO.layer = LayerMask.NameToLayer("UI");
            var coreRT = coreGO.AddComponent<RectTransform>();
            coreRT.SetParent(rootRT, false);
            StretchToParent(coreRT);
            var coreGraphic = coreGO.AddComponent<TargetLine2DGraphic>();
            coreGraphic.raycastTarget = false;
            coreGraphic.PeakWidth = TargetLineInstanceConfig.CoreWidth2D;
            coreGraphic.color = Color.white;

            // Bead — small circle Image that slides along the arc for direction.
            var beadGO = new GameObject("Bead");
            beadGO.layer = LayerMask.NameToLayer("UI");
            var beadRT = beadGO.AddComponent<RectTransform>();
            beadRT.SetParent(rootRT, false);
            beadRT.anchorMin = new Vector2(0.5f, 0.5f);
            beadRT.anchorMax = new Vector2(0.5f, 0.5f);
            beadRT.pivot = new Vector2(0.5f, 0.5f);
            beadRT.sizeDelta = new Vector2(TargetLineInstanceConfig.BeadSize2D,
                                           TargetLineInstanceConfig.BeadSize2D);
            var beadImage = beadGO.AddComponent<Image>();
            beadImage.raycastTarget = false;
            beadImage.color = Color.white;
            // Reuse any circular sprite available — SpriteLibrary pulls from Addressables.
            if (SpriteLibrary.Sprites != null)
            {
                if (SpriteLibrary.Sprites.TryGetValue("SynergySpark", out var s)) beadImage.sprite = s;
                else if (SpriteLibrary.Sprites.TryGetValue("Coin", out var c)) beadImage.sprite = c;
            }

            root.AddComponent<TargetLine2DInstance>();
            return root;
        }

        /// <summary>Stretches a RectTransform to fill its parent — used for the full-screen arc host.</summary>
        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        /// <summary>Finds the main overlay Canvas by name (matches GameBuilder's "Canvas" root).</summary>
        private static RectTransform FindMainCanvasRect()
        {
            var go = GameObject.Find("Canvas");
            return go != null ? go.GetComponent<RectTransform>() : null;
        }
    }
}
