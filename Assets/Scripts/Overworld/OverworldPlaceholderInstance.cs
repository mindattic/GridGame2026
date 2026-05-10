using UnityEngine;
using UnityEngine.UI;
using scene = Scripts.Helpers.SceneHelper;

namespace Scripts.Overworld
{
    /// <summary>
    /// OVERWORLDPLACEHOLDERINSTANCE - Tiny runtime that wires the placeholder's
    /// "Go to Campaign" button to <see cref="scene.Fade.ToStageSelect"/>.
    /// <para>STATUS: Slice 9 — exists only because the Overworld scene is parked but still
    /// reachable through legacy code paths. Click handler routes the player to StageSelect
    /// (the new campaign gateway).</para>
    /// </summary>
    public class OverworldPlaceholderInstance : MonoBehaviour
    {
        private void Start()
        {
            scene.FadeIn();
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var btnT = canvas.transform.Find("ToCampaignButton");
            var btn = btnT != null ? btnT.GetComponent<Button>() : null;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => scene.Fade.ToStageSelect());
            }
        }
    }
}
