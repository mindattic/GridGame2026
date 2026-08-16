using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Scripts.Utilities
{
    /// <summary>
    /// ASPECTGUARD - Portrait aspect lock + black bars (US-001).
    ///
    /// <para>The game is authored for a tall portrait reference (1170×2532). Real phones/tablets vary,
    /// so this:</para>
    /// <list type="number">
    ///   <item><b>Letterboxes the camera</b> to the VALID portrait aspect CLOSEST to the device's —
    ///   "as close as possible without going over" (the content is fit, never cropped) — and fills the
    ///   remainder with <b>black bars</b> (a full-screen black background camera behind everything).
    ///   The world grid, which centers itself within the camera's visible rect, then sits centered with
    ///   padding on the short sides.</item>
    ///   <item><b>Normalizes every CanvasScaler</b> to the 1170×2532 reference with match 0.5, so HUD
    ///   anchored positions (e.g. the AnnouncementWindow's −360) are in a CONSISTENT reference space on
    ///   every device — universal across aspect ratios. (Also fixes the VendorBuilder (0,0)-reference
    ///   bug, US-111.)</item>
    ///   <item><b>Insets any "SafeArea" panel</b> — a RectTransform named exactly "SafeArea" as a direct
    ///   child of a root Canvas — to match <see cref="Screen.safeArea"/> (notch / home-indicator margin),
    ///   converting the pixel rect to normalized anchors. Builders opt in by adding that child; canvases
    ///   without "SafeArea" are unaffected.</item>
    /// </list>
    ///
    /// <para>Self-installs on every scene (no per-builder wiring) via a runtime hook; re-applies on
    /// resolution / orientation / safe-area change. EDITOR-GATED: the exact bars/centering must be
    /// eyeballed across aspect ratios in the editor.</para>
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class AspectGuard : MonoBehaviour
    {
        /// <summary>Reference design resolution (portrait). All CanvasScalers are pinned to this.</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1170f, 2532f);

        /// <summary>Valid portrait aspect ratios (width / height), tall → wide: 9:21, 9:20, 9:19.5,
        /// 1:2, 9:16, 10:16, 3:4. The device snaps to the nearest of these.</summary>
        private static readonly float[] ValidAspects =
            { 9f / 21f, 9f / 20f, 9f / 19.5f, 1f / 2f, 9f / 16f, 10f / 16f, 3f / 4f };

        private static Camera background;

        private Camera cam;
        private int lastW, lastH;
        private Rect lastSafeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Apply();
            SceneManager.sceneLoaded += (s, m) => Apply();
        }

        private static void Apply()
        {
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<AspectGuard>() == null)
                cam.gameObject.AddComponent<AspectGuard>();
            NormalizeCanvases();
        }

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
            ApplyLetterbox();   // also calls ApplySafeArea
        }

        private void Update()
        {
            bool sizeChanged = Screen.width != lastW || Screen.height != lastH;
            bool safeChanged = Screen.safeArea != lastSafeArea;
            if (sizeChanged) ApplyLetterbox();
            else if (safeChanged) ApplySafeArea();
        }

        private void ApplyLetterbox()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null || Screen.width <= 0 || Screen.height <= 0) return;

            lastW = Screen.width;
            lastH = Screen.height;

            cam.rect = LetterboxRect(Screen.width, Screen.height);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            EnsureBackgroundCamera();
            ApplySafeArea();

            // Re-center the board within the new visible rect, if a board exists.
            var board = Scripts.Helpers.GameHelper.Board;
            if (board != null) board.SendMessage("AssignPosition", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>Apply the OS-reported safe-area inset to any RectTransform named "SafeArea" that is
        /// a direct child of a Canvas. Builders opt in by adding that child panel; everything else is
        /// unaffected. Converts the pixel-space <see cref="Screen.safeArea"/> to normalized anchors and
        /// zeroes the offsets so the panel exactly tracks the notch/home-indicator margins.</summary>
        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var (anchorMin, anchorMax) = SafeAreaAnchors(Screen.safeArea, Screen.width, Screen.height);

            var canvases = Object.FindObjectsByType<UnityEngine.Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas == null || !canvas.isRootCanvas) continue;
                var safePanel = canvas.transform.Find("SafeArea") as RectTransform;
                if (safePanel == null) continue;
                safePanel.anchorMin = anchorMin;
                safePanel.anchorMax = anchorMax;
                safePanel.offsetMin = Vector2.zero;
                safePanel.offsetMax = Vector2.zero;
            }
        }

        /// <summary>A persistent full-screen camera that clears the whole screen to black BEHIND the
        /// main camera, so the letterbox/pillarbox margins read as solid black bars (the main camera
        /// only clears within its sub-rect).</summary>
        private void EnsureBackgroundCamera()
        {
            if (background != null) return;
            var go = new GameObject("LetterboxBackground");
            Object.DontDestroyOnLoad(go);
            background = go.AddComponent<Camera>();
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = Color.black;
            background.cullingMask = 0;          // renders nothing — just clears black
            background.rect = new Rect(0f, 0f, 1f, 1f);
            background.depth = cam != null ? cam.depth - 1 : -100;
        }

        /// <summary>US-136: the pure letterbox math, extracted so the multi-aspect suite can
        /// verify every supported device ratio headless. Returns the camera's normalized
        /// viewport rect: full-screen when the device matches a valid aspect, pillarboxed when
        /// wider, letterboxed when taller — always centered, content fit and never cropped.</summary>
        public static Rect LetterboxRect(int screenWidth, int screenHeight)
        {
            float device = (float)screenWidth / screenHeight;
            float target = ClosestValidAspect(device);

            if (device > target)
            {
                // Device wider than target → pillarbox (bars left/right).
                float w = target / device;
                return new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            // Device taller/narrower than target → letterbox (bars top/bottom).
            float h = device / target;
            return new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }

        /// <summary>US-136: pure safe-area→anchor conversion (see ApplySafeArea).</summary>
        public static (Vector2 anchorMin, Vector2 anchorMax) SafeAreaAnchors(Rect safeArea, float screenWidth, float screenHeight)
        {
            var anchorMin = new Vector2(safeArea.x / screenWidth, safeArea.y / screenHeight);
            var anchorMax = new Vector2((safeArea.x + safeArea.width) / screenWidth,
                                        (safeArea.y + safeArea.height) / screenHeight);
            return (anchorMin, anchorMax);
        }

        /// <summary>The valid portrait aspect nearest the device's (public for the US-136 suite).</summary>
        public static float ClosestValidAspect(float device)
        {
            float best = ValidAspects[0];
            float bestDelta = Mathf.Abs(device - best);
            for (int i = 1; i < ValidAspects.Length; i++)
            {
                float d = Mathf.Abs(device - ValidAspects[i]);
                if (d < bestDelta) { bestDelta = d; best = ValidAspects[i]; }
            }
            return best;
        }

        /// <summary>Pin every CanvasScaler to the portrait reference so HUD anchors are device-universal.</summary>
        private static void NormalizeCanvases()
        {
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (var s in scalers)
            {
                if (s == null) continue;
                s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = ReferenceResolution;
                s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                s.matchWidthOrHeight = 0.5f;
            }
        }
    }
}
