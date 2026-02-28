using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UI;

using scene = Assets.Helpers.SceneHelper;

/// <summary>
/// ZOOMEFFECT - Full-screen zoom/spin transition effect.
/// 
/// PURPOSE:
/// Creates a dramatic zoom/spin effect by capturing the current
/// screen and animating a zoom in with optional burn and invert.
/// 
/// EFFECT PARAMETERS:
/// - duration: Animation length
/// - zoomStrength: Final zoom amount
/// - spinRadians: Rotation over duration
/// - smudgeStrength: Motion blur effect
/// - centerUV: Zoom focus point
/// 
/// BURN EFFECT:
/// Circular burn/vignette from center:
/// - burnStrength: Intensity (0-1)
/// - burnRadius: Size of burn area
/// - burnFeather: Edge softness
/// 
/// USAGE:
/// ```csharp
/// yield return zoomEffect.Play(() => {
///     // Called when effect completes
/// });
/// ```
/// 
/// RELATED FILES:
/// - ScreenGrabber.cs: Frame capture
/// - ShardMeshBuilder.cs: Full-screen quad mesh
/// </summary>
[RequireComponent(typeof(ScreenGrabber))]
public class ZoomEffect : MonoBehaviour
{
    [Header("Material")]
    public Material zoomMaterial; // Optional. If null, a material is created from the URP shader.

    [Tooltip("Parent the quad to the main camera so its transform never culls it.")]
    public bool parentToCamera = true;

    [Header("Motion")]
    public float duration = 0.75f;
    public float zoomStrength = 2.0f; // final zoom amount contribution
    public float spinRadians = 6.0f;  // spin over the duration (radians)
    public float smudgeStrength = 1.0f;
    public Vector2 centerUV = new Vector2(0.5f, 0.5f);

    [Header("Burn")]
    [Range(0f, 1f)] public float burnStrength = 0f;
    [Range(0f, 1f)] public float burnRadius = 0.45f;
    [Range(0f, 1f)] public float burnFeather = 0.35f;
    [Range(0f, 4f)] public float burnMultiplier = 2f;
    [Range(0f, 8f)] public float burnGammaScale = 2f;

    [Header("Invert")]
    [Range(0f, 1f)] public float invertStrength = 1f;

    public IEnumerator Play(System.Action onFinished = null)
    {
        // 1) Grab frame
        Texture2D frame = null;
        yield return StartCoroutine(GetComponent<ScreenGrabber>().CaptureToTexture(t => frame = t));

        // 2) Build mesh object (simple full-screen grid)
        var go = new GameObject("ZoomFullScreen");
        go.layer = LayerMask.NameToLayer("Default");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        // Small grid to allow good interpolation if needed
        mf.sharedMesh = ShardMeshBuilder.BuildGrid(1, 1);

        go.transform.SetParent(Camera.main.transform, worldPositionStays: false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // 3) Setup material instance
        Material matInst = new Material(zoomMaterial);
        matInst.mainTexture = frame;
        matInst.SetFloat("_Progress", 0f);
        matInst.SetFloat("_Zoom", zoomStrength);
        matInst.SetFloat("_Spin", spinRadians);
        matInst.SetFloat("_Smudge", smudgeStrength);
        matInst.SetVector("_CenterUV", new Vector4(centerUV.x, centerUV.y, 0, 0));
        matInst.SetFloat("_BurnStrength", burnStrength);
        matInst.SetFloat("_BurnRadius", burnRadius);
        matInst.SetFloat("_BurnFeather", burnFeather);
        matInst.SetFloat("_BurnMultiplier", burnMultiplier);
        matInst.SetFloat("_BurnGammaScale", burnGammaScale);
        matInst.SetFloat("_InvertStrength", invertStrength);

        // Ensure it renders after most transparents
        matInst.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;
        mr.sharedMaterial = matInst;

        // Make sure it renders on top of world (optional: set sorting order)
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = short.MaxValue; // best effort within the same sorting layer

        // 4) Animate and switch as soon as screen is fully black
        bool switched = false;
        const float epsilon = 0.999f; // how close to black before switching
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            mr.sharedMaterial.SetFloat("_Progress", p);

            // Compute the same global burn factor used in the shader: saturate(_BurnStrength * _Progress * _BurnMultiplier)
            if (!switched && burnStrength > 0f && burnMultiplier > 0f)
            {
                float burnFactor = Mathf.Clamp01(burnStrength * burnMultiplier * p);
                if (burnFactor >= epsilon)
                {
                    switched = true;

                    scene.Switch.ToGame(); // switch immediately once fully black
                    // Do not break; scene load will likely destroy this object. If it doesn't, the loop will end naturally.
                }
            }

            yield return null;
        }

        // 5) Cleanup (if still alive) and callback
        if (go != null) Object.Destroy(go);
        if (frame != null) Object.Destroy(frame);
        onFinished?.Invoke();
    }
}
