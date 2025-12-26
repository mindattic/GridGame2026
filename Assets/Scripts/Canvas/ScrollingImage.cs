using UnityEngine;
using UnityEngine.UI;

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

    // Optional helpers
    public void SetScrollSpeed(Vector2 speed) => scrollSpeed = speed;
    public void SetScrollX(float x) => scrollSpeed.x = x;
    public void SetScrollY(float y) => scrollSpeed.y = y;
}