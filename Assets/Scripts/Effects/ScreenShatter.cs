using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
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

namespace Scripts.Effects
{
/// <summary>
/// SCREENSHATTER - Full-screen shatter transition effect.
/// 
/// PURPOSE:
/// Creates a dramatic screen shatter effect by capturing the
/// current frame, splitting it into shards, and animating
/// them exploding outward.
/// 
/// EFFECT FLOW:
/// 1. Capture current screen as texture
/// 2. Create shard grid mesh
/// 3. Apply shatter material
/// 4. Animate shards exploding from center
/// 5. Clean up after animation
/// 
/// CONFIGURATION:
/// - cols/rows: Shard grid density
/// - duration: Animation length
/// - explode: Explosion force
/// - spin: Rotation speed
/// - centerUV: Explosion origin
/// 
/// RELATED FILES:
/// - ScreenGrabber.cs: Frame capture
/// - ShardMeshBuilder.cs: Mesh generation
/// </summary>
[RequireComponent(typeof(ScreenGrabber))]
public class ScreenShatter : MonoBehaviour
{
    [Header("Grid")]
    public int cols = 40;
    public int rows = 22;

    [Header("Material")]
    public Material shatterMat; // Optional. If null, a material is created from the URP shader (or Built-in fallback).

    [Header("Rendering")] 
    [Tooltip("Layer to place the ScreenShards renderer on. Use a layer visible to the main camera.")]
    public string renderLayerName = "Default";

    [Tooltip("Temporarily disable all Screen Space - Overlay Canvases during the shatter so the effect is visible.")]
    public bool disableOverlayCanvases = true;

    [Tooltip("Temporarily hide FadeOverlayInstance Images during the shatter and capture to avoid a full-black capture.")]
    public bool hideFadeOverlayImages = true;

    [Tooltip("Parent the shards to the main camera so their transform never culls them.")]
    public bool parentToCamera = true;

    [Header("Motion")]
    public float duration = 0.6f;
    public float explode = 1.2f;
    public float spin = 9.0f;
    public float jitter = 0.8f;
    public Vector2 centerUV = new Vector2(0.5f, 0.5f);

    [Header("Debug / Inspector")]
    public bool pauseOnScreenshot = false;
    public bool pauseOnShatterStart = false;
    public bool pauseOnShatterEnd = false;
    [Tooltip("If pausing at end, keep shards GameObject and captured texture alive so you can inspect them in the Inspector.")]
    public bool keepShardsOnPause = true;
    [Tooltip("Expose the captured frame in the inspector for debugging while paused.")]
    public bool exposeCaptureInInspector = true;
    [SerializeField] private Texture2D debugCapturedFrame;
    [SerializeField] private GameObject debugShardsGO;

    private const string URPShaderPath = "Universal Render Pipeline/Unlit/ScreenShatter";
    private const string BuiltinShaderPath = "Unlit/ScreenShatter";

    public IEnumerator Play(System.Action onFinished = null)
    {
        // Optionally disable overlay canvases (they render above everything else)
        var disabledCanvases = new List<UnityEngine.Canvas>();
        if (disableOverlayCanvases)
        {
            var canvases = FindObjectsOfType<UnityEngine.Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay && canvases[i].enabled)
                {
                    canvases[i].enabled = false;
                    disabledCanvases.Add(canvases[i]);
                }
            }
        }

        // Optionally hide FadeOverlayInstance Images so capture isn't full black
        var fadedImages = new List<(Image img, bool wasEnabled)>();
        if (hideFadeOverlayImages)
        {
            var overlays = FindObjectsOfType<FadeOverlayInstance>(true);
            foreach (var o in overlays)
            {
                var img = o.GetComponent<Image>();
                if (img != null && img.enabled)
                {
                    fadedImages.Add((img, true));
                    img.enabled = false;
                }
            }
        }

        // 1) Grab frame
        Texture2D frame = null;
        yield return StartCoroutine(GetComponent<ScreenGrabber>().CaptureToTexture(t => frame = t));
        if (exposeCaptureInInspector) debugCapturedFrame = frame;
        if (pauseOnScreenshot) PauseHere("After Screenshot Capture");

        // 2) Build mesh object
        var go = new GameObject("ScreenShards");
        debugShardsGO = go;

        // Place on a camera-visible layer (default to "Default"). Avoid inheriting UI layer accidentally.
        int targetLayer = LayerMask.NameToLayer(string.IsNullOrEmpty(renderLayerName) ? "Default" : renderLayerName);
        if (targetLayer < 0) targetLayer = LayerMask.NameToLayer("Default");
        go.layer = targetLayer;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        mf.sharedMesh = ShardMeshBuilder.BuildGrid(cols, rows);

        // Parent to main camera so culling and transform are trivial
        if (parentToCamera && Camera.main != null)
        {
            go.transform.SetParent(Camera.main.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        // 3) Setup material instance (prefer URP shader, fallback to Built-in)
        Material matInst = null;
        if (shatterMat != null) matInst = new Material(shatterMat);
        else
        {
            var shader = Shader.Find(URPShaderPath);
            if (shader == null) shader = Shader.Find(BuiltinShaderPath);
            if (shader != null) matInst = new Material(shader);
        }

        if (matInst == null)
        {
            Debug.LogError("ScreenShatter: Could not create material. Assign shatterMat or add the ScreenShatter shader (URP or Built-in).");
            Destroy(go);
            if (frame != null) Destroy(frame);
            for (int i = 0; i < disabledCanvases.Count; i++) disabledCanvases[i].enabled = true;
            for (int i = 0; i < fadedImages.Count; i++) fadedImages[i].img.enabled = fadedImages[i].wasEnabled;
            yield break;
        }

        matInst.mainTexture = frame;
        matInst.SetFloat("_Progress", 0f);
        matInst.SetFloat("_Explode", explode);
        matInst.SetFloat("_Spin", spin);
        matInst.SetFloat("_Jitter", jitter);
        matInst.SetVector("_CenterUV", new Vector4(centerUV.x, centerUV.y, 0, 0));
        matInst.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;
        mr.sharedMaterial = matInst;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = short.MaxValue;

        if (pauseOnShatterStart) PauseHere("On Shatter Start (progress=0)");

        // 4) Animate
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            mr.sharedMaterial.SetFloat("_Progress", p);
            yield return null;
        }

        mr.sharedMaterial.SetFloat("_Progress", 1f);

        if (pauseOnShatterEnd)
        {
            PauseHere("On Shatter End (progress=1)");
            if (keepShardsOnPause) yield break;
        }

        // 5) Cleanup and restore UI
        Object.Destroy(go);
        debugShardsGO = null;
        if (frame != null) Object.Destroy(frame);
        debugCapturedFrame = null;
        for (int i = 0; i < disabledCanvases.Count; i++) disabledCanvases[i].enabled = true;
        for (int i = 0; i < fadedImages.Count; i++) fadedImages[i].img.enabled = fadedImages[i].wasEnabled;
        onFinished?.Invoke();
    }

    private void PauseHere(string reason)
    {
        Debug.Log($"ScreenShatter: Pause - {reason}");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = true;
        #else
        Debug.Break();
        #endif
    }
}

}
