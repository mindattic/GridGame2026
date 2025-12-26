// --- File: Assets/Scripts/Helpers/UnitConversionHelper.cs ---
using UnityEngine;
using UnityEngine.UI;
using c = Assets.Helpers.CanvasHelper;

namespace Assets.Helpers
{
    /// <summary>
    /// UnitConversionHelper
    /// Organized conversions between:
    ///   Screen   = pixel coordinates, origin bottom-left
    ///   Viewport = normalized [0..1], origin bottom-left
    ///   World    = Unity scene coordinates
    ///   Canvas   = RectTransform local coordinates in a target RectTransform
    ///
    /// Assumptions:
    ///   Camera.main exists and is orthographic.
    ///   Canvas references are provided by CanvasHelper.
    /// </summary>
    public static class UnitConversionHelper
    {
        // ---------------------------------------------------------------------
        // Camera access
        // ---------------------------------------------------------------------

        /// <summary>
        /// Returns Camera.main or throws if missing.
        /// </summary>
        private static Camera Cam
        {
            get
            {
                if (Camera.main == null)
                    throw new System.InvalidOperationException("UnitConversionHelper requires Camera.main.");
                return Camera.main;
            }
        }

        /// <summary>
        /// Public accessor so external callers can use the same canvas camera selection.
        /// Overlay: null, ScreenSpace-Camera: canvas.worldCamera if set, else Camera.main.
        /// </summary>
        public static Camera GetCanvasCamera()
        {
            var canvas = c.Canvas;
            if (canvas == null) return null;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            if (canvas.worldCamera != null) return canvas.worldCamera;
            return Cam;
        }

        // ---------------------------------------------------------------------
        // Screen conversions
        // ---------------------------------------------------------------------
        public static class Screen
        {
            /// <summary>
            /// Screen -> World at a given Z plane.
            /// </summary>
            public static Vector3 ToWorld(Vector2 screenPos, float worldZ = 0f)
            {
                return Cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, worldZ));
            }

            /// <summary>
            /// Screen -> Viewport [0..1].
            /// </summary>
            public static Vector2 ToViewport(Vector2 screenPos)
            {
                if (UnityEngine.Screen.width <= 0 || UnityEngine.Screen.height <= 0) return Vector2.zero;
                return new Vector2(screenPos.x / UnityEngine.Screen.width, screenPos.y / UnityEngine.Screen.height);
            }

            /// <summary>
            /// Screen -> Canvas local (inside targetRect).
            /// </summary>
            public static Vector2 ToCanvas(RectTransform targetRect, Vector2 screenPos)
            {
                if (targetRect == null) return Vector2.zero;
                Camera uiCam = GetCanvasCamera();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPos, uiCam, out var local);
                return local;
            }
        }

        // ---------------------------------------------------------------------
        // World conversions
        // ---------------------------------------------------------------------
        public static class World
        {
            /// <summary>
            /// World -> Screen pixels.
            /// </summary>
            public static Vector2 ToScreen(Vector3 worldPos)
            {
                Vector3 p = Cam.WorldToScreenPoint(worldPos);
                return new Vector2(p.x, p.y);
            }

            /// <summary>
            /// World -> Viewport [0..1].
            /// </summary>
            public static Vector2 ToViewport(Vector3 worldPos)
            {
                Vector3 p = Cam.WorldToViewportPoint(worldPos);
                return new Vector2(p.x, p.y);
            }

            /// <summary>
            /// World -> Canvas local (inside targetRect).
            /// </summary>
            public static Vector2 ToCanvas(RectTransform targetRect, Vector3 worldPos)
            {
                Vector2 screen = ToScreen(worldPos);
                return Screen.ToCanvas(targetRect, screen);
            }

            /// <summary>
            /// Visible world-space rectangle of Camera.main (orthographic).
            /// </summary>
            public static Rect VisibleRect()
            {
                Vector3 bl = Cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
                Vector3 tr = Cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

                float minX = Mathf.Min(bl.x, tr.x);
                float maxX = Mathf.Max(bl.x, tr.x);
                float minY = Mathf.Min(bl.y, tr.y);
                float maxY = Mathf.Max(bl.y, tr.y);

                return Rect.MinMaxRect(minX, minY, maxX, maxY);
            }

            /// <summary>
            /// Width and height of the visible world-space area.
            /// </summary>
            public static Vector2 VisibleSize()
            {
                Rect r = VisibleRect();
                return new Vector2(r.width, r.height);
            }

            /// <summary>
            /// Center of the visible world-space area.
            /// </summary>
            public static Vector2 VisibleCenter()
            {
                Rect r = VisibleRect();
                return r.center;
            }
        }

        // ---------------------------------------------------------------------
        // Viewport conversions
        // ---------------------------------------------------------------------
        public static class Viewport
        {
            /// <summary>
            /// Viewport [0..1] -> World at a given Z plane.
            /// </summary>
            public static Vector3 ToWorld(Vector2 viewportPos, float worldZ = 0f)
            {
                return Cam.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, worldZ));
            }

            /// <summary>
            /// Viewport [0..1] -> Screen pixels.
            /// </summary>
            public static Vector2 ToScreen(Vector2 viewportPos)
            {
                return new Vector2(viewportPos.x * UnityEngine.Screen.width, viewportPos.y * UnityEngine.Screen.height);
            }

            /// <summary>
            /// Viewport [0..1] -> Canvas local (inside targetRect).
            /// </summary>
            public static Vector2 ToCanvas(RectTransform targetRect, Vector2 viewportPos)
            {
                Vector2 screen = ToScreen(viewportPos);
                return Screen.ToCanvas(targetRect, screen);
            }
        }

        // ---------------------------------------------------------------------
        // Canvas conversions
        // ---------------------------------------------------------------------
        public static class Canvas
        {
            /// <summary>
            /// Canvas local -> Screen pixels.
            /// </summary>
            public static Vector2 ToScreen(RectTransform sourceRect, Vector2 localPoint)
            {
                if (sourceRect == null) return Vector2.zero;
                Camera uiCam = GetCanvasCamera();
                Vector3 world = sourceRect.TransformPoint(localPoint);
                if (uiCam == null)
                {
                    Vector3 sp = RectTransformUtility.WorldToScreenPoint(null, world);
                    return new Vector2(sp.x, sp.y);
                }
                Vector3 p = uiCam.WorldToScreenPoint(world);
                return new Vector2(p.x, p.y);
            }

            /// <summary>
            /// Canvas transform -> Screen pixels.
            /// </summary>
            public static Vector2 ToScreen(Transform uiTransform)
            {
                return TransformToScreen(uiTransform);
            }

            /// <summary>
            /// Canvas transform -> Screen pixels.
            /// </summary>
            public static Vector2 TransformToScreen(Transform uiTransform)
            {
                if (uiTransform == null) return Vector2.zero;
                Camera uiCam = GetCanvasCamera();
                if (uiCam == null)
                {
                    Vector3 sp = RectTransformUtility.WorldToScreenPoint(null, uiTransform.position);
                    return new Vector2(sp.x, sp.y);
                }
                Vector3 p = uiCam.WorldToScreenPoint(uiTransform.position);
                return new Vector2(p.x, p.y);
            }

            /// <summary>
            /// Canvas transform -> Viewport [0..1].
            /// </summary>
            public static Vector2 TransformToViewport(Transform uiTransform)
            {
                Vector2 screen = TransformToScreen(uiTransform);
                return UnitConversionHelper.Screen.ToViewport(screen);
            }

            /// <summary>
            /// Canvas local -> Viewport [0..1].
            /// </summary>
            public static Vector2 ToViewport(RectTransform sourceRect, Vector2 localPoint)
            {
                Vector2 screen = ToScreen(sourceRect, localPoint);
                return UnitConversionHelper.Screen.ToViewport(screen);
            }

            /// <summary>
            /// Canvas transform -> World at a given Z plane.
            /// </summary>
            public static Vector3 ToWorld(Transform uiTransform, float worldZ = 0f)
            {
                Vector2 screen = TransformToScreen(uiTransform);
                return UnitConversionHelper.Screen.ToWorld(screen, worldZ);
            }

            /// <summary>
            /// Canvas local -> World at a given Z plane.
            /// </summary>
            public static Vector3 ToWorld(RectTransform sourceRect, Vector2 localPoint, float worldZ = 0f)
            {
                if (sourceRect == null) return Vector3.zero;
                Vector2 screen = ToScreen(sourceRect, localPoint);
                return UnitConversionHelper.Screen.ToWorld(screen, worldZ);
            }

            /// <summary>
            /// Canvas local (source) -> Canvas local (target).
            /// </summary>
            public static Vector2 ToCanvas(RectTransform sourceRect, Vector2 localPoint, RectTransform targetRect)
            {
                if (sourceRect == null || targetRect == null) return Vector2.zero;
                Vector2 screen = ToScreen(sourceRect, localPoint);
                return UnitConversionHelper.Screen.ToCanvas(targetRect, screen);
            }
        }

        // ---------------------------------------------------------------------
        // UI scale helpers
        // ---------------------------------------------------------------------
        public static class UiScale
        {
            /// <summary>
            /// Effective UI scale for the current CanvasHelper.Canvas.
            /// </summary>
            public static float Get()
            {
                if (c.CanvasScaler != null && c.Canvas != null)
                {
                    switch (c.CanvasScaler.uiScaleMode)
                    {
                        case CanvasScaler.ScaleMode.ConstantPixelSize:
                            return c.CanvasScaler.scaleFactor > 0f ? c.CanvasScaler.scaleFactor : 1f;

                        case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                            {
                                Vector2 referenceResolution = c.CanvasScaler.referenceResolution;
                                if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
                                    return c.Canvas.scaleFactor > 0f ? c.Canvas.scaleFactor : 1f;

                                float w = UnityEngine.Screen.width / referenceResolution.x;
                                float h = UnityEngine.Screen.height / referenceResolution.y;

                                if (c.CanvasScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand)
                                    return Mathf.Min(w, h);

                                if (c.CanvasScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink)
                                    return Mathf.Max(w, h);

                                float lw = Mathf.Log(UnityEngine.Screen.width / referenceResolution.x, 2f);
                                float lh = Mathf.Log(UnityEngine.Screen.height / referenceResolution.y, 2f);
                                float m = Mathf.Clamp01(c.CanvasScaler.matchWidthOrHeight);
                                return Mathf.Pow(2f, Mathf.Lerp(lw, lh, m));
                            }

                        case CanvasScaler.ScaleMode.ConstantPhysicalSize:
                            {
                                float dpi = UnityEngine.Screen.dpi;
                                if (dpi <= 0f) dpi = 96f;
                                float refDpi = c.CanvasScaler.fallbackScreenDPI > 0f ? c.CanvasScaler.fallbackScreenDPI : 96f;
                                return dpi / refDpi;
                            }
                    }
                }

                return (c.Canvas != null && c.Canvas.scaleFactor > 0f) ? c.Canvas.scaleFactor : 1f;
            }

            /// <summary>
            /// Pixels -> Canvas units.
            /// </summary>
            public static float PixelsToCanvasUnits(float pixels)
            {
                float s = Get();
                return s <= 0f ? pixels : pixels / s;
            }

            /// <summary>
            /// Reference pixels -> Canvas units.
            /// </summary>
            public static Vector2 ReferencePixelsToCanvasUnits(Vector2 refPixels)
            {
                float s = Get();
                if (s <= 0f) s = 1f;
                return refPixels / s;
            }
        }

    }
}
