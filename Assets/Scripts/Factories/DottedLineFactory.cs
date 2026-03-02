using UnityEngine;
using UnityEngine.Rendering;
using Scripts.Canvas;
using Scripts.Data.Actor;
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
    /// DOTTEDLINEFACTORY - Creates movement path visualization segments.
    /// 
    /// PURPOSE:
    /// Creates dotted line segments that show the path a hero will take
    /// when being dragged to a new position.
    /// 
    /// PATH VISUALIZATION:
    /// ```
    /// [Hero] � � � � � � � � � [Destination]
    ///        ? dotted segments ?
    /// ```
    /// 
    /// SEGMENT TYPES (DottedLineSegment):
    /// - Horizontal: Left-right segments (?)
    /// - Vertical: Up-down segments (?)
    /// - Corner: L-shaped turn segments (?, ?, ?, ?)
    /// - Start/End: Path terminus markers
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// DottedLine (root)
    /// ??? SpriteRenderer (segment sprite)
    /// ??? DottedLineInstance (behavior)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Tag: "DottedLine"
    /// - SortingLayer: Board
    /// - Alpha: 77% (semi-transparent)
    /// - DrawMode: Sliced (for resizing)
    /// 
    /// SPRITE ASSIGNMENT:
    /// Sprite is set dynamically by DottedLineInstance.Spawn()
    /// based on the segment type.
    /// 
    /// CALLED BY:
    /// - DottedLineManager.Spawn()
    /// 
    /// RELATED FILES:
    /// - DottedLineInstance.cs: Segment behavior
    /// - DottedLineManager.cs: Manages path segments
    /// - InputManager.cs: Triggers path creation
    /// </summary>
    public static class DottedLineFactory
    {
        /// <summary>Creates a new dotted line segment.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("DottedLine");
            root.layer = 0;
            root.tag = "DottedLine";

            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.76862746f);
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Board";
            spriteRenderer.sortingOrder = 0;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = Vector2.one;

            // DottedLineInstance
            var dottedLineInstance = root.AddComponent<DottedLineInstance>();
            dottedLineInstance.location = Vector2Int.zero;
            dottedLineInstance.segment = DottedLineSegment.Vertical;
            dottedLineInstance.connectedLocations.Clear();

            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }
    }
}
