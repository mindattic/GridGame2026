using System.Collections;
using UnityEngine;
using TMPro;

namespace Scripts.Hub
{
    /// <summary>
    /// HUBTOAST - Ephemeral announcement banner.
    /// <para>PURPOSE: Fades in a short confirmation message ("Started: Iron Sword +1") at the
    /// top of the Hub canvas, holds for a beat, then fades out. Used to give tactile feedback
    /// on successful actions without forcing the player to read the detail panel.</para>
    /// <para>USAGE: <c>HubToast.Show("message")</c> from anywhere inside the Hub scene. Scaffold
    /// places a single instance named "HubToast" directly under the Canvas.</para>
    /// <para>RELATED FILES: HubScaffold.cs, BlacksmithSection.cs, AlchemistSection.cs</para>
    /// </summary>
    public class HubToast : MonoBehaviour
    {
        public const string GameObjectName = "HubToast";

        [System.NonSerialized] public TMP_Text Label;
        [System.NonSerialized] public CanvasGroup Group;

        private Coroutine routine;

        public static void Show(string message)
        {
            var go = GameObject.Find(GameObjectName);
            if (go == null) { Debug.LogWarning("[HubToast] No HubToast gameobject in scene."); return; }
            var toast = go.GetComponent<HubToast>();
            if (toast == null) return;
            toast.Play(message);
        }

        private void Awake()
        {
            if (Group == null) Group = GetComponent<CanvasGroup>();
            if (Group == null) Group = gameObject.AddComponent<CanvasGroup>();
            Group.alpha = 0f;
            Group.interactable = false;
            Group.blocksRaycasts = false;

            if (Label == null)
            {
                var child = transform.Find("Label");
                if (child != null) Label = child.GetComponent<TMP_Text>();
            }
        }

        public void Play(string message)
        {
            if (Label != null) Label.text = message;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            if (Group == null) yield break;
            const float fadeIn = 0.15f;
            const float hold = 1.8f;
            const float fadeOut = 0.35f;

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                Group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            Group.alpha = 1f;

            yield return new WaitForSecondsRealtime(hold);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                Group.alpha = 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
            Group.alpha = 0f;
            routine = null;
        }
    }
}
