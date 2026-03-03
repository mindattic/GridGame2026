using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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
/// CITYSCROLL - Scrolling city background effect.
/// 
/// PURPOSE:
/// Creates an infinitely scrolling horizontal background by
/// animating the UV offset of a RawImage texture.
/// 
/// VISUAL EFFECT:
/// ```
/// [City Background] ←← scrolling left ←←
/// ```
/// 
/// CONFIGURATION:
/// - rawImage: The RawImage component with tiled texture
/// - scrollFocus: Speed of horizontal scrolling
/// 
/// UV WRAPPING:
/// - Scrolls UVs leftward continuously
/// - Wraps at -1 to create seamless loop
/// - Requires tileable texture
/// 
/// RELATED FILES:
/// - ScrollingImage.cs: Similar scrolling effect
/// - ScrollingRawImage.cs: Alternative implementation
/// </summary>
public class CityScroll : MonoBehaviour
{
    public RawImage rawImage; // SelectProfile the RawImage in the inspector
    public float scrollFocus = 0.1f; // Intelligence of scrolling

    /// <summary>Begins the infinite UV scrolling coroutine.</summary>
    private void Start()
    {
        StartCoroutine(ScrollUVRoutine());
    }

    /// <summary>Continuously scrolls the RawImage UV rect leftward, wrapping at -1 for a seamless loop.</summary>
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


}
