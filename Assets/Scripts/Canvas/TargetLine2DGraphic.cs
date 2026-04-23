using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Data.Actor;
using Scripts.Data.Config;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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

namespace Scripts.Canvas
{
    /// <summary>
    /// TARGETLINE2DGRAPHIC - Canvas Graphic that renders a tapered ribbon through a polyline.
    /// <para>PURPOSE: Builds a UI mesh from a caller-supplied list of canvas-local points and a
    /// peak width. The ribbon tapers symmetrically — 0 at both ends, peak in the middle — so
    /// the result reads as a "pointed" targeting line matching the 3D LineRenderer taper.</para>
    /// <para>USAGE: TargetLine2DInstance populates <see cref="Points"/> every frame with the
    /// Bezier arc samples (in this graphic's local space), sets <see cref="PeakWidth"/>, then
    /// calls <see cref="SetVerticesDirty"/> to trigger a re-mesh.</para>
    /// <para>RELATED FILES: TargetLine2DInstance.cs, TargetLine2DFactory.cs</para>
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TargetLine2DGraphic : Graphic
    {
        public readonly List<Vector2> Points = new List<Vector2>();
        public float PeakWidth = TargetLineInstanceConfig.CoreWidth2D;

        // A custom Graphic's default mainTexture can resolve to null on URP Canvas setups,
        // which causes CanvasRenderer to skip the mesh entirely. Forcing the built-in white
        // texture guarantees the UI/Default shader gets (white × vertex color) → vertex color.
        public override Texture mainTexture => Texture2D.whiteTexture;

        /// <summary>Rebuilds the tapered-ribbon mesh from <see cref="Points"/>.</summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = Points.Count;
            if (n < 2 || PeakWidth <= 0f) return;

            // Emit two verts per sample point, offset along the 2D perpendicular of the local
            // tangent. Tapered width w(t) = peak * sin(πt) gives sharp 0-width ends and a
            // smooth peak at the midpoint — matches the 3D LineRenderer AnimationCurve shape.
            Color32 c = color;
            int lastIndex = n - 1;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)lastIndex;
                float w = PeakWidth * Mathf.Sin(t * Mathf.PI) * 0.5f;

                Vector2 tangent;
                if (i == 0) tangent = Points[1] - Points[0];
                else if (i == lastIndex) tangent = Points[lastIndex] - Points[lastIndex - 1];
                else tangent = Points[i + 1] - Points[i - 1];
                if (tangent.sqrMagnitude < 1e-6f) tangent = Vector2.right;
                tangent.Normalize();
                var perp = new Vector2(-tangent.y, tangent.x);

                Vector3 left = Points[i] + perp * w;
                Vector3 right = Points[i] - perp * w;

                vh.AddVert(left, c, new Vector2(0f, t));
                vh.AddVert(right, c, new Vector2(1f, t));
            }

            // Stitch quads between successive sample pairs. UI/Default uses Cull Off so
            // winding is irrelevant, but match CanvasLineRenderer's CW order for consistency.
            for (int i = 0; i < lastIndex; i++)
            {
                int a = i * 2;
                vh.AddTriangle(a, a + 1, a + 2);
                vh.AddTriangle(a + 1, a + 3, a + 2);
            }
        }
    }
}
