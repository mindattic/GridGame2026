using Assets.Helpers;
using UnityEngine;

public class PointerManager : MonoBehaviour
{
    // Indicates if the current pointer (touch or mouse) is within the screen viewport [0..1].
    public bool IsTouchInsideScreenBounds
    {
        get
        {
            Vector2 screenPos = GetCurrentPointerScreenPosition();
            Vector2 vp = UnitConversionHelper.Screen.ToViewport(screenPos);
            return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        }
    }

    // Method which is used for initialization tasks that need to occur before the game starts
    private void Awake()
    {
        // No initialization required after cleanup.
        // Left intentionally to preserve lifecycle structure.
    }

    // Updates the cached pointer positions each frame using touch when available or mouse as a fallback.
    public void Update()
    {
        Vector2 screenPos = GetCurrentPointerScreenPosition();

        GameManager.instance.touchPosition2D = screenPos;

        if (IsTouchInsideScreenBounds && Camera.main != null)
        {
            // Convert to world coordinates on Z = 0 plane or your gameplay plane as needed.
            GameManager.instance.touchPosition3D = UnitConversionHelper.Screen.ToWorld(screenPos, 0f);
        }
    }

    // Returns the active pointer position in screen pixels.
    // Prefers the first touch when present, otherwise falls back to mouse position.
    private static Vector2 GetCurrentPointerScreenPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }
}
