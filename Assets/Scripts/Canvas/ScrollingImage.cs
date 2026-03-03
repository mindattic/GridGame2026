using UnityEngine;
using UnityEngine.UI;
using Scripts.Data.Actor;
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
/// SCROLLINGIMAGE - Animated scrolling texture effect.
/// 
/// PURPOSE:
/// Creates a continuously scrolling texture effect on UI Image
/// or RawImage components for parallax backgrounds and effects.
/// 
/// SUPPORTED COMPONENTS:
/// - RawImage: Scrolls UV rect directly
/// - Image: Scrolls texture offset via material property
/// 
/// CONFIGURATION:
/// - scrollSpeed: UV units per second (X, Y)
/// - useUnscaledTime: Ignore Time.timeScale
/// - textureProperty: Material property to offset ("_MainTex")
/// 
/// MATERIAL HANDLING:
/// For Image components, creates a runtime material instance
/// to avoid modifying shared materials. Cleans up on destroy.
/// 
/// RELATED FILES:
/// - ScrollingRawImage.cs: Alternative with direction changes
/// - CityScroll.cs: Simpler horizontal scroll
/// </summary>
public class ScrollingImage : MonoBehaviour
{
    [Header("Scroll")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.1f, 0.0f); // UV units per second
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Material (for Image)")]
    [SerializeField] private string textureProperty = "_MainTex"; // property to offset

    private RawImage rawImage;
    private Image uiImage;

    private Rect uvRect;
    private Material runtimeMaterial;
    private Vector2 offset;

    /// <summary>Caches the RawImage or Image component and creates a runtime material if needed.</summary>
    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        if (rawImage != null)
        {
            uvRect = rawImage.uvRect;
            offset = uvRect.position;
            return;
        }

        uiImage = GetComponent<Image>();
        if (uiImage != null)
        {
            // Ensure we have a per-instance material we can modify safely
            var sourceMat = uiImage.material != null ? uiImage.material : new Material(Shader.Find("UI/Default"));
            runtimeMaterial = new Material(sourceMat);
            uiImage.material = runtimeMaterial;

            if (runtimeMaterial.HasProperty(textureProperty))
                offset = runtimeMaterial.GetTextureOffset(textureProperty);
            else
                offset = Vector2.zero;
        }
    }

    /// <summary>Cleans up the instantiated runtime material to prevent memory leaks.</summary>
    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            // Clean up the instantiated material
            if (Application.isPlaying)
                Destroy(runtimeMaterial);
            else
                DestroyImmediate(runtimeMaterial);
        }
    }

    /// <summary>Advances the texture UV offset each frame based on scroll speed.</summary>
    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        var delta = scrollSpeed * dt;

        if (rawImage != null)
        {
            offset += delta;
            offset.x = Mathf.Repeat(offset.x, 1f);
            offset.y = Mathf.Repeat(offset.y, 1f);

            uvRect.position = offset;
            rawImage.uvRect = uvRect;
            return;
        }

        if (runtimeMaterial != null && runtimeMaterial.HasProperty(textureProperty))
        {
            offset += delta;
            offset.x = Mathf.Repeat(offset.x, 1f);
            offset.y = Mathf.Repeat(offset.y, 1f);
            runtimeMaterial.SetTextureOffset(textureProperty, offset);
        }
    }

    /// <summary>Sets both X and Y scroll speed.</summary>
    public void SetScrollSpeed(Vector2 speed) => scrollSpeed = speed;
    /// <summary>Sets the horizontal scroll speed.</summary>
    public void SetScrollX(float x) => scrollSpeed.x = x;
    /// <summary>Sets the vertical scroll speed.</summary>
    public void SetScrollY(float y) => scrollSpeed.y = y;
}
}
