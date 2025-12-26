using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class CanvasLineRenderer : Graphic
{
    [SerializeField] public List<Vector2> points = new List<Vector2>();
    [SerializeField] public float thickness = 5f;
    [SerializeField] public float dotSpacing = 20f;
    [SerializeField] public RectTransform content; // Reference to the ScrollView's Content

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Count < 2) return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            DrawDottedLine(vh, points[i], points[i + 1]);
        }
    }

    public void UpdateLine(Button startButton, Button endButton)
    {
        if (startButton == null || endButton == null || content == null)
            return;

        points.Clear();
        points.Add(ConvertToLocalSpace(startButton.GetComponent<RectTransform>()));
        points.Add(ConvertToLocalSpace(endButton.GetComponent<RectTransform>()));

        SetVerticesDirty(); // Force UI to refresh
    }

    private Vector2 ConvertToLocalSpace(RectTransform buttonTransform)
    {
        Vector2 localPoint;

        // Convert world position to screen position
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, buttonTransform.position);

        // Convert screen position to local position in Content
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content, screenPos, null, out localPoint
        );

        return localPoint;
    }

    void DrawDottedLine(VertexHelper vh, Vector2 start, Vector2 end)
    {
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        float step = dotSpacing;

        for (float d = 0; d < distance; d += step)
        {
            Vector2 position = start + direction * d;
            DrawDot(vh, position);
        }
    }

    void DrawDot(VertexHelper vh, Vector2 position)
    {
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;

        float halfSize = thickness / 2f;
        Vector2[] corners = new Vector2[]
        {
            position + new Vector2(-halfSize, -halfSize),
            position + new Vector2(-halfSize, halfSize),
            position + new Vector2(halfSize, halfSize),
            position + new Vector2(halfSize, -halfSize)
        };

        int startIndex = vh.currentVertCount;
        for (int i = 0; i < 4; i++)
        {
            vert.position = corners[i];
            vh.AddVert(vert);
        }

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }
}
