using Assets.Helper;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CityScroll : MonoBehaviour
{
    public RawImage rawImage; // SelectProfile the RawImage in the inspector
    public float scrollFocus = 0.1f; // Intelligence of scrolling

    private void Start()
    {
        StartCoroutine(ScrollUVRoutine());
    }

    private IEnumerator ScrollUVRoutine()
    {
        Vector2 offset = rawImage.uvRect.position;

        while (true)
        {
            offset.x -= scrollFocus * Time.deltaTime; // Seek UVs to the left
            if (offset.x <= -1f) offset.x += 1f; // Wrap around at -1

            rawImage.uvRect = new Rect(offset, rawImage.uvRect.size);
            yield return Wait.None();
        }
    }
}

