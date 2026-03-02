using TMPro;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
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
/// COINCOUNTER - UI display for player coins.
/// 
/// PURPOSE:
/// Displays the current coin count with a pulsing glow
/// effect and provides world position for coin collection.
/// 
/// VISUAL ELEMENTS:
/// - Icon: Coin sprite
/// - Glow: Pulsing halo effect
/// - Value: Text showing coin count (7 digits)
/// 
/// FEATURES:
/// - Animated glow pulse via AnimationCurve
/// - World position for coin magnet effect
/// - Auto-refresh on coin changes
/// 
/// ACCESS: g.CoinCounter
/// 
/// RELATED FILES:
/// - CoinManager.cs: Coin spawning
/// - CoinInstance.cs: Coin collection
/// - GameHelper.cs: TotalCoins property
/// </summary>
public class CoinCounter : MonoBehaviour
{
    #region Fields

    [SerializeField] public AnimationCurve glowCurve;
    [HideInInspector] public Image icon;
    [HideInInspector] public Image glow;
    [HideInInspector] public TextMeshProUGUI value;
    private float maxGlowScale = 2f;
    private Camera mainCamera;

    #endregion

    #region Initialization

    void Awake()
    {
        icon = transform.GetChild("Icon").GetComponent<Image>();
        glow = transform.GetChild("Glow").GetComponent<Image>();
        value = transform.GetChild("Value").GetComponent<TextMeshProUGUI>();
        mainCamera = Camera.main;
    }

    #endregion

    #region Update

    void FixedUpdate()
    {
        UpdateGlow();
    }

    /// <summary>Refresh displayed coin count.</summary>
    public void Refresh()
    {
        value.text = g.TotalCoins.ToString("D7");
    }

    private void UpdateGlow()
    {
        float glowScale = maxGlowScale * glowCurve.Evaluate(Time.time % glowCurve.length);
        glow.rectTransform.localScale = new Vector3(
            icon.rectTransform.localScale.x * glowScale,
            icon.rectTransform.localScale.y * glowScale,
            1.0f);
    }

    #endregion

    #region Helpers

        /// <summary>Converts the Icon Image's screen position to world coordinates.</summary>
        public Vector3 GetIconWorldPosition()
        {
            if (mainCamera == null)
                return Vector3.zero;

            Vector3 screenPosition = icon.rectTransform.position;
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));

            return worldPosition;
        }

        #endregion
    }

}
