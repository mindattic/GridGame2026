using TMPro;
using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;

public class CoinCounter : MonoBehaviour
{
    //Fields
    [SerializeField] public AnimationCurve glowCurve;
    [HideInInspector] public Image icon;
    [HideInInspector] public Image glow;
    [HideInInspector] public TextMeshProUGUI value;
    private float maxGlowScale = 2f;
    private Camera mainCamera;

    // Initialization tasks before the game starts
    void Awake()
    {
        icon = transform.GetChild("Icon").GetComponent<Image>();
        glow = transform.GetChild("Glow").GetComponent<Image>();
        value = transform.GetChild("Value").GetComponent<TextMeshProUGUI>();
        mainCamera = Camera.main;
    }

    void FixedUpdate()
    {
        UpdateGlow();
    }

    public void Refresh()
    {
        value.text = g.TotalCoins.ToString("D7"); // Displays coin count with leading zeros
    }

    private void UpdateGlow()
    {
        // Make Glow pulse based on the Animation curve
        float glowScale = maxGlowScale * glowCurve.Evaluate(Time.time % glowCurve.length);
        glow.rectTransform.localScale = new Vector3(
            icon.rectTransform.localScale.x * glowScale,
            icon.rectTransform.localScale.y * glowScale,
            1.0f);
    }

    /// <summary>
    /// Converts the Icon Image's screen position to world coordinates.
    /// </summary>
    public Vector3 GetIconWorldPosition()
    {
        if (mainCamera == null)
            return Vector3.zero;

        // Convert UI position to world position
        Vector3 screenPosition = icon.rectTransform.position;
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));

        return worldPosition;
    }
}
