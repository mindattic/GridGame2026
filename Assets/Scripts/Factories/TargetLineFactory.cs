using UnityEngine;
using UnityEngine.Rendering;
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
    /// TARGETLINEFACTORY - Creates FFXII-style targeting arc GameObjects.
    /// <para>PURPOSE: Builds the full arc hierarchy: a world-space root with two LineRenderer
    /// passes (core + wider additive glow halo) and a traveling circular bead that slides from
    /// source → destination to communicate direction.</para>
    /// <para>CREATED HIERARCHY:
    /// <code>
    /// TargetLine (root)
    /// ├── LineRenderer (core, thin, additive, tapered ends)
    /// ├── Glow/LineRenderer (wider, additive, lower alpha)
    /// ├── Bead/SpriteRenderer (traveling direction indicator)
    /// ├── SortingGroup
    /// └── TargetLineInstance (behavior)
    /// </code>
    /// </para>
    /// <para>RELATED FILES: TargetLineInstance.cs, TargetLineManager.cs</para>
    /// </summary>
    public static class TargetLineFactory
    {
        /// <summary>Creates a new targeting arc. Color is applied at spawn time via TargetLineInstance.SetColor.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("TargetLine");
            root.layer = LayerMask.NameToLayer("Actor");
            root.tag = "SupportLine";

            var t = root.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            // ---- Core stroke ----
            var coreLR = root.AddComponent<LineRenderer>();
            ConfigureLineRenderer(
                coreLR,
                width: TargetLineInstanceConfig.CoreWidth,
                alpha: 1f,
                orderOffset: 1);

            // ---- Glow halo (second pass) ----
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(t, false);
            var glowLR = glowGO.AddComponent<LineRenderer>();
            ConfigureLineRenderer(
                glowLR,
                width: TargetLineInstanceConfig.CoreWidth * TargetLineInstanceConfig.GlowWidthMultiplier,
                alpha: TargetLineInstanceConfig.GlowAlpha,
                orderOffset: 0);

            // ---- Direction bead ----
            var beadGO = new GameObject("Bead");
            beadGO.transform.SetParent(t, false);
            beadGO.transform.localScale = Vector3.one * TargetLineInstanceConfig.BeadSize;
            var beadSR = beadGO.AddComponent<SpriteRenderer>();
            beadSR.sortingLayerName = "VFX";
            beadSR.sortingOrder = 2;
            // Additive-ish white: use a circular sprite from SpriteLibrary if present,
            // else leave null — TargetLineInstance will attempt to resolve at Awake.
            var spark = SpriteLibrary.Sprites != null
                && SpriteLibrary.Sprites.TryGetValue("SynergySpark", out var s) ? s : null;
            if (spark == null && SpriteLibrary.Sprites != null
                && SpriteLibrary.Sprites.TryGetValue("Coin", out var c2)) spark = c2;
            beadSR.sprite = spark;
            beadSR.material = BuildAdditiveMaterial();
            beadSR.color = Color.white;

            // ---- Sorting ----
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "VFX";
            sortingGroup.sortingOrder = 0;

            root.AddComponent<TargetLineInstance>();

            if (parent != null) t.SetParent(parent, false);
            return root;
        }

        /// <summary>Shared LineRenderer configuration — additive material, tapered width, 2-key gradient.</summary>
        private static void ConfigureLineRenderer(LineRenderer lr, float width, float alpha, int orderOffset)
        {
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            lr.lightProbeUsage = LightProbeUsage.Off;
            lr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            lr.sortingLayerName = "VFX";
            lr.sortingOrder = orderOffset;
            var mat = BuildAdditiveMaterial();
            // Particles/Additive multiplies texture × color; without a texture some shaders
            // sample black and the line vanishes. Force white so the additive math degrades
            // to (color × 1) and the start/end colors drive output.
            mat.mainTexture = Texture2D.whiteTexture;
            lr.material = mat;
            lr.positionCount = TargetLineInstanceConfig.Segments + 1;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 0;
            lr.numCornerVertices = 0;
            lr.widthMultiplier = 1f;
            // Tapered width: sharp at both ends, peak in the middle. Three keyframes with
            // smoothed interpolation give a readable "pointed" look at source and target.
            lr.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f * width),
                new Keyframe(0.5f, width, 0f, 0f),
                new Keyframe(1f, 0f, -4f * width, 0f));

            // White gradient — TargetLineInstance.SetColor tints at runtime by multiplying
            // each LineRenderer's startColor/endColor (avoids re-authoring the gradient per color).
            lr.startColor = new Color(1f, 1f, 1f, alpha);
            lr.endColor = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Builds an additive-blended material. Sprites/Default has hardcoded alpha
        /// blending and ignores _SrcBlend/_DstBlend overrides, so we use Particles/Additive
        /// (legacy but stable) with a URP fallback — matching the SynergySpark approach.</summary>
        private static Material BuildAdditiveMaterial()
        {
            var shader = Shader.Find("Particles/Additive");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }
    }
}
