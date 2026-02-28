using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Libraries;
using Game.Instances.Actor;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Factories
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

            // === FRONT CONTAINER ===
            var front = CreateChild(root, "Front");

            // === FRONT CHILDREN ===
            CreateOpaque(front);
            CreateQuality(front);
            CreateGlow(front);
            CreateParallax(front);
            CreateThumbnail(front);
            CreateGradient(front);
            CreateFrame(front);
            CreateStatusIcon(front);
            CreateHealthBar(front);
            CreateActionBar(front);
            CreateMask(front);
            CreateRadialBack(front);
            CreateRadialFill(front);
            CreateRadialText(front);
            CreateTurnDelayText(front);
            CreateNameTagText(front);
            CreateWeaponIcon(front);
            CreateArmor(front);
            CreateActiveIndicator(front);
            CreateFocusIndicator(front);
            CreateTargetIndicator(front);

            // === BACK CONTAINER ===
            CreateBack(root);

            // === ADD ACTORINSTANCE LAST ===
            var actorInstance = root.AddComponent<ActorInstance>();
            actorInstance.glowCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(1f, 0.25f, 0f, 0f)
            );

            return root;
        }

        #endregion

        #region Helper Methods

        private static GameObject CreateChild(GameObject parent, string name, bool isActive = true)
        {
            var child = new GameObject(name);
            child.layer = ActorGameLayer;
            child.SetActive(isActive);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static SpriteRenderer AddSpriteRenderer(
            GameObject go,
            Sprite sprite,
            Color color,
            string material,
            int sortingOrder,
            SpriteDrawMode drawMode = SpriteDrawMode.Sliced,
            Vector2? size = null,
            SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None,
            SpriteSortPoint sortPoint = SpriteSortPoint.Center)
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
            return sr;
        }

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

        private static void CreateOpaque(GameObject parent)
        {
            var go = CreateChild(parent, "Opaque");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Mask4"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                1,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateQuality(GameObject parent)
        {
            var go = CreateChild(parent, "Quality");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Base4"],
                new Color(1f, 1f, 1f, 0f),
                "SpritesDefault",
                2,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateGlow(GameObject parent)
        {
            var go = CreateChild(parent, "Glow");
            go.transform.localScale = new Vector3(2.56f, 2.56f, 1f);
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["ThumbnailFade"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                11,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateParallax(GameObject parent)
        {
            var go = CreateChild(parent, "Parallax", isActive: false);
            AddSpriteRenderer(go,
                null,
                new Color(1f, 1f, 1f, 0.5019608f),
                "PlayerParallax",
                4,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f),
                SpriteMaskInteraction.VisibleInsideMask);
        }

        private static void CreateThumbnail(GameObject parent)
        {
            var go = CreateChild(parent, "Thumbnail");
            AddSpriteRenderer(go,
                null, // Set dynamically
                new Color(1f, 1f, 1f, 1f),
                "SpritePan",
                5,
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

        private static void CreateGradient(GameObject parent)
        {
            var go = CreateChild(parent, "Gradient");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Gradient"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                6,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateFrame(GameObject parent)
        {
            var go = CreateChild(parent, "Frame", isActive: false);
            go.transform.localScale = new Vector3(0.390625f, 0.390625f, 1f);
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Frame4"],
                new Color(1f, 1f, 1f, 0.2509804f),
                "SpritesDefault",
                6,
                SpriteDrawMode.Simple);
        }

        private static void CreateStatusIcon(GameObject parent)
        {
            var go = CreateChild(parent, "StatusIcon", isActive: false);
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["StatusNone"],
                new Color(1f, 1f, 1f, 0f),
                "SpritesDefault",
                7,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        #endregion

        #region Health Bar

        private static void CreateHealthBar(GameObject parent)
        {
            var healthBar = CreateChild(parent, "HealthBar");
            healthBar.transform.localPosition = new Vector3(-0.25f, -0.4f, 0f);
            healthBar.transform.localScale = new Vector3(1f, 0.1f, 1f);

            // Back
            var back = CreateChild(healthBar, "HealthBarBack");
            AddSpriteRenderer(back,
                SpriteLibrary.Actor["HealthBar5"],
                new Color(0f, 0f, 0f, 1f),
                "SpritesDefault",
                8,
                SpriteDrawMode.Sliced,
                new Vector2(0.5f, 0.5f),
                SpriteMaskInteraction.None,
                SpriteSortPoint.Pivot);

            // Drain
            var drain = CreateChild(healthBar, "HealthBarDrain");
            AddSpriteRenderer(drain,
                SpriteLibrary.Actor["HealthBar5"],
                new Color(0.9014806f, 0.9433962f, 0.07564971f, 1f),
                "SpritesDefault",
                9,
                SpriteDrawMode.Sliced,
                new Vector2(0.5f, 0.5f),
                SpriteMaskInteraction.None,
                SpriteSortPoint.Pivot);

            // Fill
            var fill = CreateChild(healthBar, "HealthBarFill");
            AddSpriteRenderer(fill,
                SpriteLibrary.Actor["HealthBar5"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                10,
                SpriteDrawMode.Sliced,
                new Vector2(0.5f, 0.5f),
                SpriteMaskInteraction.None,
                SpriteSortPoint.Pivot);

            // Text
            var text = CreateChild(healthBar, "HealthBarText");
            AddTextMeshPro(text, "Consolas", 1f,
                new Color(1f, 1f, 1f, 0f),
                TextAlignmentOptions.Left,
                11, "100", enabled: false);
        }

        #endregion

        #region Action Bar

        private static void CreateActionBar(GameObject parent)
        {
            var actionBar = CreateChild(parent, "ActionBar", isActive: false);
            actionBar.transform.localPosition = new Vector3(-0.45f, -0.38f, 0f);
            actionBar.transform.localScale = new Vector3(0.75f, 1f, 1f);

            // Back
            var back = CreateChild(actionBar, "ActionBarBack");
            AddSpriteRenderer(back,
                SpriteLibrary.Actor["HealthBarBack3"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                13,
                SpriteDrawMode.Sliced,
                new Vector2(0.3333f, 0.03333f),
                SpriteMaskInteraction.None,
                SpriteSortPoint.Pivot);

            // Drain
            var drain = CreateChild(actionBar, "ActionBarDrain");
            AddSpriteRenderer(drain,
                SpriteLibrary.Actor["HealthBar3"],
                new Color(1f, 0f, 0f, 1f),
                "SpritesDefault",
                14,
                SpriteDrawMode.Sliced,
                new Vector2(0.3333f, 0.03333f),
                SpriteMaskInteraction.None,
                SpriteSortPoint.Pivot);

            // Fill
            var fill = CreateChild(actionBar, "ActionBarFill");
            AddSpriteRenderer(fill,
                SpriteLibrary.Actor["ActionBar2"],
                new Color(0f, 0.7686275f, 1f, 1f),
                "SpritesDefault",
                15,
                SpriteDrawMode.Sliced,
                new Vector2(0.3333f, 0.03333f),
                SpriteMaskInteraction.None,
                SpriteSortPoint.Pivot);

            // Text
            var text = CreateChild(actionBar, "ActionBarText");
            AddTextMeshPro(text, "Consolas", 1f,
                new Color(1f, 1f, 1f, 0f),
                TextAlignmentOptions.Left,
                16, "0%", enabled: false);
        }

        #endregion

        #region Mask & Radial

        private static void CreateMask(GameObject parent)
        {
            var go = CreateChild(parent, "Mask");
            go.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
            var mask = go.AddComponent<SpriteMask>();
            mask.sprite = SpriteLibrary.Actor["Mask7"];
            mask.alphaCutoff = 0.1f;
        }

        private static void CreateRadialBack(GameObject parent)
        {
            var go = CreateChild(parent, "RadialBack", isActive: false);
            go.transform.localPosition = new Vector3(-0.3333f, 0.3333f, 0f);
            go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["RingBack1"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                17,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateRadialFill(GameObject parent)
        {
            var go = CreateChild(parent, "RadialFill", isActive: false);
            go.transform.localPosition = new Vector3(-0.3333f, 0.3333f, 0f);
            go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["Ring1"],
                new Color(1f, 1f, 1f, 0.5019608f),
                "RadialFill",
                18,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateRadialText(GameObject parent)
        {
            var go = CreateChild(parent, "RadialText", isActive: false);
            go.transform.localPosition = new Vector3(-0.3333f, 0.3333f, 0f);
            AddTextMeshPro(go, "Roboto", 1f,
                new Color(1f, 1f, 1f, 0f),
                TextAlignmentOptions.Center,
                29, "", enabled: true);
        }

        #endregion

        #region Text Elements

        private static void CreateTurnDelayText(GameObject parent)
        {
            var go = CreateChild(parent, "TurnDelayText");
            go.transform.localPosition = new Vector3(0.4f, 0.36f, 0f);
            AddTextMeshPro(go, "Attic", 3f,
                new Color(1f, 1f, 1f, 1f),
                TextAlignmentOptions.Center,
                10, "", enabled: true);
        }

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

        private static void CreateWeaponIcon(GameObject parent)
        {
            var go = CreateChild(parent, "WeaponIcon", isActive: false);
            go.transform.localPosition = new Vector3(0.35f, 0.35f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 315f);
            go.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            AddSpriteRenderer(go,
                null,
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                22,
                SpriteDrawMode.Simple);
        }

        #endregion

        #region Armor

        private static void CreateArmor(GameObject parent)
        {
            var armor = CreateChild(parent, "Armor", isActive: false);

            var north = CreateChild(armor, "ArmorNorth");
            AddSpriteRenderer(north,
                SpriteLibrary.Actor["ArmorNorth"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                23,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));

            var east = CreateChild(armor, "ArmorEast");
            AddSpriteRenderer(east,
                SpriteLibrary.Actor["ArmorEast"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                24,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));

            var south = CreateChild(armor, "ArmorSouth");
            AddSpriteRenderer(south,
                SpriteLibrary.Actor["ArmorSouth"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                25,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));

            var west = CreateChild(armor, "ArmorWest");
            AddSpriteRenderer(west,
                SpriteLibrary.Actor["ArmorWest"],
                new Color(1f, 1f, 1f, 1f),
                "SpritesDefault",
                26,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        #endregion

        #region Indicators

        private static void CreateActiveIndicator(GameObject parent)
        {
            var go = CreateChild(parent, "ActiveIndicator");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["ActiveIndicator"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                27,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateFocusIndicator(GameObject parent)
        {
            var go = CreateChild(parent, "FocusIndicator");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["FocusIndicator"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                28,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        private static void CreateTargetIndicator(GameObject parent)
        {
            var go = CreateChild(parent, "TargetIndicator");
            AddSpriteRenderer(go,
                SpriteLibrary.Actor["TargetIndicator"],
                new Color(1f, 1f, 1f, 1f),
                "SpriteUnlitDefault",
                29,
                SpriteDrawMode.Sliced,
                new Vector2(1f, 1f));
        }

        #endregion

        #region Back

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
