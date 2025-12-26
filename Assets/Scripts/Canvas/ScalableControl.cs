using Assets.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ScalableControl : MonoBehaviour
{
    private RectTransform top;
    private RectTransform bottom;
    private RectTransform left;
    private RectTransform right;

    private RectTransform topLeft;
    private RectTransform topRight;
    private RectTransform bottomLeft;
    private RectTransform bottomRight;

    private RectTransform background;
    private RectTransform label;

    private float cornerSize = 16f;
    private float edgeThickness = 16f;


    public void Awake()
    {
        top = this.transform.Find("Top")?.GetComponent<RectTransform>();
        bottom = this.transform.Find("Bottom")?.GetComponent<RectTransform>();
        left = this.transform.Find("Left")?.GetComponent<RectTransform>();
        right = this.transform.Find("Right")?.GetComponent<RectTransform>();

        topLeft = this.transform.Find("TopLeft")?.GetComponent<RectTransform>();
        topRight = this.transform.Find("TopRight")?.GetComponent<RectTransform>();
        bottomLeft = this.transform.Find("BottomLeft")?.GetComponent<RectTransform>();
        bottomRight = this.transform.Find("BottomRight")?.GetComponent<RectTransform>();

        background = this.transform.Find("Background")?.GetComponent<RectTransform>();
        label = this.transform.Find("Label")?.GetComponent<RectTransform>();
    }

    void Start() => ApplyLayout();
    void OnValidate() => ApplyLayout();

#if UNITY_EDITOR
    void Update()
    {
        // Continuously apply layout changes in SpawnPairRoutine & Edit mode (if in unity editor)
        ApplyLayout();
    }
#endif

    void ApplyLayout()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root.rect.width == 0 || root.rect.height == 0)
            return;

        float minAnchor = cornerSize / root.rect.width;
        float minAnchorY = cornerSize / root.rect.height;

        // Edges
        if (top)
        {
            top.anchorMin = new Vector2(minAnchor, 1);
            top.anchorMax = new Vector2(1 - minAnchor, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(0, edgeThickness);
        }

        if (bottom)
        {
            bottom.anchorMin = new Vector2(minAnchor, 0);
            bottom.anchorMax = new Vector2(1 - minAnchor, 0);
            bottom.pivot = new Vector2(0.5f, 0);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = new Vector2(0, edgeThickness);
        }

        if (left)
        {
            left.anchorMin = new Vector2(0, minAnchorY);
            left.anchorMax = new Vector2(0, 1 - minAnchorY);
            left.pivot = new Vector2(0, 0.5f);
            left.anchoredPosition = Vector2.zero;
            left.sizeDelta = new Vector2(edgeThickness, 0);
        }

        if (right)
        {
            right.anchorMin = new Vector2(1, minAnchorY);
            right.anchorMax = new Vector2(1, 1 - minAnchorY);
            right.pivot = new Vector2(1, 0.5f);
            right.anchoredPosition = Vector2.zero;
            right.sizeDelta = new Vector2(edgeThickness, 0);
        }

        // Corners
        if (topLeft)
        {
            topLeft.anchorMin = new Vector2(0, 1);
            topLeft.anchorMax = new Vector2(0, 1);
            topLeft.pivot = new Vector2(0, 1);
            topLeft.anchoredPosition = Vector2.zero;
            topLeft.sizeDelta = new Vector2(cornerSize, cornerSize);
        }

        if (topRight)
        {
            topRight.anchorMin = new Vector2(1, 1);
            topRight.anchorMax = new Vector2(1, 1);
            topRight.pivot = new Vector2(1, 1);
            topRight.anchoredPosition = Vector2.zero;
            topRight.sizeDelta = new Vector2(cornerSize, cornerSize);
        }

        if (bottomLeft)
        {
            bottomLeft.anchorMin = new Vector2(0, 0);
            bottomLeft.anchorMax = new Vector2(0, 0);
            bottomLeft.pivot = new Vector2(0, 0);
            bottomLeft.anchoredPosition = Vector2.zero;
            bottomLeft.sizeDelta = new Vector2(cornerSize, cornerSize);
        }

        if (bottomRight)
        {
            bottomRight.anchorMin = new Vector2(1, 0);
            bottomRight.anchorMax = new Vector2(1, 0);
            bottomRight.pivot = new Vector2(1, 0);
            bottomRight.anchoredPosition = Vector2.zero;
            bottomRight.sizeDelta = new Vector2(cornerSize, cornerSize);
        }

        // Background (fills inside border)
        if (background)
        {
            float bgMinAnchor = cornerSize * Increment.Percent33 / root.rect.width;
            float bgMinAnchorY = cornerSize * Increment.Percent33 / root.rect.height;

            background.anchorMin = new Vector2(bgMinAnchor, bgMinAnchorY);
            background.anchorMax = new Vector2(1 - bgMinAnchor, 1 - bgMinAnchorY);
            background.pivot = new Vector2(0.5f, 0.5f);
            background.anchoredPosition = Vector2.zero;
            background.sizeDelta = Vector2.zero;

            //if (background.TryGetComponent(out Image bgImage) && bgImage.type == Image.HitType.Tiled)
            //{
            //    float targetTileSize = root.rect.width; // Adjust this for your desired visual tile size

            //    float scaleFactor = 1f;
            //    if (bgImage.canvas != null)
            //        scaleFactor = bgImage.canvas.scaleFactor;

            //    float widthInPixels = root.rect.width * scaleFactor;
            //    float heightInPixels = root.rect.height * scaleFactor;

            //    float rawMultiplier = targetTileSize / ((widthInPixels + heightInPixels) * 0.5f);
            //    bgImage.pixelsPerUnitMultiplier = Mathf.Clamp(rawMultiplier, 0.1f, 10f);
            //}
        }


        // Label (inside same area as background)
        if (label)
        {
            label.anchorMin = new Vector2(minAnchor, minAnchorY);
            label.anchorMax = new Vector2(1 - minAnchor, 1 - minAnchorY);
            label.pivot = new Vector2(0.5f, 0.5f);
            label.anchoredPosition = Vector2.zero;
            label.sizeDelta = Vector2.zero;

            // Auto-adjust font size based on height (48 at 128 height)
            if (label.TryGetComponent(out TextMeshProUGUI text))
            {
                float fontSize = (root.rect.height / 128f) * 48f;
                text.fontSize = Mathf.Clamp(Mathf.RoundToInt(fontSize), 12, 96);
            }
        }

    }
}
