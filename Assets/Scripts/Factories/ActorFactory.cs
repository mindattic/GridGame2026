using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Instances.Actor;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
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
    /// ACTORFACTORY - Programmatically creates Actor GameObjects.
    /// 
    /// PURPOSE:
    /// Replaces ActorPrefab.prefab with code-driven creation.
    /// Creates fully configured actor GameObjects with all child
    /// components, sprites, and settings at runtime.
    /// 
    /// NOTE: All values extracted from ActorPrefab.prefab via PrefabAnalyzer tool.
    /// </summary>
    public static class ActorFactory
    {
        #region Constants

        private const int ActorGameLayer = 10;
        private static readonly string SortingLayerName = "ActorBelow";

        #endregion

        #region Main Create Method

        /// <summary>Creates the instance.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT ===
            var root = new GameObject("ActorPrefab");
            root.layer = ActorGameLayer;
            root.tag = "Actor";

            var rootTransform = root.transform;
            rootTransform.localPosition = Vector3.zero;
            rootTransform.localRotation = Quaternion.identity;
            rootTransform.localScale = new Vector3(1.5f, 1.5f, 1f); // From prefab

            if (parent != null)
                rootTransform.SetParent(parent, false);

            // SortingGroup
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = SortingLayerName;
            sortingGroup.sortingOrder = 0;

            // Rigidbody2D — matches original ActorPrefab (Dynamic, no gravity, freeze rotation, continuous CD).
            // Required so Physics2D.OverlapPointAll picks the moving collider efficiently without rebuilding the static-collider tree every transform change.
            var rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.gravityScale = 0f;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Collider2D for click/drag picking via Physics2D.OverlapPointAll (matches the original ActorPrefab BoxCollider2D)
            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.offset = Vector2.zero;
            collider.size = Vector2.one;

            // === FRONT CONTAINER ===
            var front = CreateChild(root, "Front");

            // === FRONT CHILDREN ===
            // Visual stack (back→front): Backdrop, Thumbnail, Frame, Mask, ...
            CreateBackdrop(front);
            CreateThumbnail(front);
            CreateFrame(front);
            CreateMask(front);
            CreateGradient(front);
            CreateNameTagText(front);
            CreateHealthText(front);
            CreateActiveIndicator(front);
            CreateFocusIndicator(front);
            CreateTargetIndicator(front);

            // === BACK CONTAINER ===
            CreateBack(root);

            // === ADD ACTORINSTANCE LAST ===
            root.AddComponent<ActorInstance>();

            return root;
        }

        #endregion

        #region Helper Methods

        /// <summary>Creates the child.</summary>
        private static GameObject CreateChild(GameObject parent, string name, bool isActive = true)
        {
            var child = new GameObject(name);
            child.layer = ActorGameLayer;
            child.SetActive(isActive);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        /// <summary>Add sprite renderer.</summary>
        private static SpriteRenderer AddSpriteRenderer(
            GameObject go,
            Sprite sprite,
            Color color,
            string material,
            int sortingOrder,
            SpriteDrawMode drawMode = SpriteDrawMode.Sliced,
            Vector2? size = null,
            SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None,
            SpriteSortPoint sortPoint = SpriteSortPoint.Center,
            bool enabled = true)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.material = MaterialLibrary.Materials[material];
            sr.sortingLayerName = SortingLayerName;
            sr.sortingOrder = sortingOrder;
            sr.drawMode = drawMode;
            if (size.HasValue && drawMode == SpriteDrawMode.Sliced)
                sr.size = size.Value;
            sr.maskInteraction = maskInteraction;
            sr.spriteSortPoint = sortPoint;
            sr.enabled = enabled;
            return sr;
        }

        /// <summary>Add text mesh pro.</summary>
        private static TextMeshPro AddTextMeshPro(
            GameObject go,
            string fontKey,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            int sortingOrder,
            string initialText = "",
            bool enabled = true)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = FontLibrary.Get(fontKey);
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.text = initialText;
            tmp.sortingLayerID = UnityEngine.SortingLayer.NameToID(SortingLayerName);
            tmp.sortingOrder = sortingOrder;
            tmp.enabled = enabled;
            return tmp;
        }

        #endregion

        #region Front Children

        /// <summary>Creates the backdrop.</summary>
        private static void CreateBackdrop(GameObject parent)
        {
            var go = CreateChild(parent, "Backdrop", isActive: true);
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Mask4"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                1,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        /// <summary>Creates the frame.</summary>
        private static void CreateFrame(GameObject parent)
        {
            var go = CreateChild(parent, "Frame");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Base4"],
                new Color(1f, 1f, 1f, 0f),
                "SpritesDefault",
                5,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        /// <summary>Creates the thumbnail.</summary>
        private static void CreateThumbnail(GameObject parent)
        {
            var go = CreateChild(parent, "Thumbnail");
            AddSpriteRenderer(go,
                null, // Set dynamically
                new Color(1f, 1f, 1f, 1f),
                "SpritePan",
                2,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f),
                SpriteMaskInteraction.VisibleInsideMask);
            
            var thumbnail = go.AddComponent<ActorThumbnail>();
            thumbnail.wobbleAmplitudeFactorX = 0.5f;
            thumbnail.wobbleAmplitudeFactorY = 0.5f;
            thumbnail.nextPauseInterval = 5f;
            thumbnail.pauseDuration = 2f;
            thumbnail.pauseRampDuration = 0.5f;
        }

        #endregion

        #region Health Text

        /// <summary>Creates the right-aligned HP readout in the actor's top-right corner.</summary>
        private static void CreateHealthText(GameObject parent)
        {
            var go = CreateChild(parent, "HealthText");
            go.transform.localPosition = new Vector3(0.425f, -0.25f, 0f);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = Vector2.zero;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = FontLibrary.Get("Attic");
            tmp.fontSize = 1.5f;
            tmp.color = new Color(1f, 1f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.text = "";
            tmp.sortingLayerID = UnityEngine.SortingLayer.NameToID(SortingLayerName);
            tmp.sortingOrder = 12;
        }

        #endregion

        #region Mask

        /// <summary>Creates the mask.</summary>
        private static void CreateMask(GameObject parent)
        {
            var go = CreateChild(parent, "Mask");
            go.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
            var mask = go.AddComponent<SpriteMask>();
            mask.sprite = SpriteLibrary.Actor["Mask7"];
            mask.alphaCutoff = 0.1f;
        }

        #endregion

        #region Gradient

        /// <summary>Creates the Gradient overlay sprite. Inherits layer switching via the root SortingGroup.</summary>
        private static void CreateGradient(GameObject parent)
        {
            var go = CreateChild(parent, ActorLayer.Name.Gradient);
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Gradient"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                ActorLayer.Value.Gradient,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        #endregion

        #region Text Elements

        /// <summary>Creates the name tag text.</summary>
        private static void CreateNameTagText(GameObject parent)
        {
            var go = CreateChild(parent, "NameTagText");
            go.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = FontLibrary.Get("Attic");
            tmp.fontSize = 1.5f;
            tmp.color = new Color(1f, 1f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.text = "";
            tmp.sortingOrder = 21;
            tmp.enabled = false;
        }

        #endregion

        #region Indicators

        /// <summary>Creates the active indicator.</summary>
        private static void CreateActiveIndicator(GameObject parent)
        {
            var go = CreateChild(parent, "ActiveIndicator");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["ActiveIndicator"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                27,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f),
                enabled: false);
        }

        /// <summary>Creates the focus indicator.</summary>
        private static void CreateFocusIndicator(GameObject parent)
        {
            var go = CreateChild(parent, "FocusIndicator");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["FocusIndicator"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                28,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f),
                enabled: false);
        }

        /// <summary>Creates the target indicator.</summary>
        private static void CreateTargetIndicator(GameObject parent)
        {
            var go = CreateChild(parent, "TargetIndicator");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["TargetIndicator"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                29,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f),
                enabled: false);
        }

        #endregion

        #region Back

        /// <summary>Creates the back.</summary>
        private static void CreateBack(GameObject root)
        {
            var back = CreateChild(root, "Back", isActive: false);
            AddSpriteRenderer(back,
                SpriteLibrary.Actor["Back2"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                0,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        #endregion
    }
}
