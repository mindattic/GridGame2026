using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using g = Assets.Helpers.GameHelper;
using Assets.Helper;

/// <summary>
/// Draws a curved arc between a fixed button position and a dynamic cursor/hero position.
/// Handles overlay in/out and updates on each physics step.
/// </summary>
public class TargetLineInstance : MonoBehaviour
{

    public Transform parent
    {
        get => transform.parent;
        set => transform.SetParent(value, true);
    }

    public float alpha = 0f;
    private LineRenderer lineRenderer;
    private Color color = ColorHelper.RGBA(0, 100, 200, 0);

    public Vector3 buttonPosition;
    public Vector3 cursorPosition;

    [SerializeField] private float fadeDuration = 0.1f;
    [SerializeField][Range(2, 100)] private int segments = 32;
    [SerializeField] private float arcHeight = 1f;

    private float minAlpha = Opacity.Transparent;
    private float maxAlpha = Opacity.Percent50;

    public SortingGroup sortingGroup => GetComponent<SortingGroup>();

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        float width = g.TileSize * 0.25f;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.numCapVertices = 8;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.useWorldSpace = true;

        sortingGroup.sortingLayerID = SortingLayer.NameToID("Actor");
        sortingGroup.sortingOrder = 999;
    }

    private void FixedUpdate()
    {
        UpdateArcPoints(buttonPosition, cursorPosition);
    }

    public void UpdateArcPoints(Vector3 start, Vector3 end)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            //float arc = 4f * arcHeight * t * (1f - t);



            float distance = Vector3.Distance(start, end);
            float dynamicHeight = distance * 0.5f; // half the straight-line distance
            float arc = 4f * dynamicHeight * t * (1 - t);



            Vector3 point = Vector3.Lerp(start, end, t) + Vector3.up * arc;
            lineRenderer.SetPosition(i, point);
        }
    }

    //TODO: FIx this so that it despawns when overlay out routine completes....
    public void Despawn()
    {
        StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        yield return FadeRoutine(maxAlpha, minAlpha);
        Destroy(gameObject);
    }

    private IEnumerator FadeInRoutine()
    {
        yield return FadeRoutine(minAlpha, maxAlpha);
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return FadeRoutine(maxAlpha, minAlpha);

    }

    private IEnumerator FadeRoutine(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            UpdateLineAlpha(alpha);
            yield return Wait.None();
        }
        alpha = to;
        UpdateLineAlpha(alpha);
    }

    private void UpdateLineAlpha(float a)
    {
        color.a = a;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
