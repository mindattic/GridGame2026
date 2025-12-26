using UnityEngine;
using g = Assets.Helpers.GameHelper;
using System.Collections;
using Assets.Scripts.Libraries;

public class AudioManager : MonoBehaviour
{
    public void Play(string sfx)
    {
        var soundEffect = SoundEffectLibrary.SoundEffects[sfx];
        if (soundEffect == null)
        {
            Debug.LogError($@"Sound Effect `{sfx}` was not found.");
            return;
        }

        g.SoundSource.PlayOneShot(soundEffect);
    }

    /// <summary>
    /// Play a clip and then run the provided coroutine after it finishes (approximate: waits clip.length in realtime).
    /// </summary>
    public void PlayAndThen(string sfx, IEnumerator routine)
    {
        var soundEffect = SoundEffectLibrary.SoundEffects[sfx];
        if (soundEffect == null)
        {
            Debug.LogError($@"Sound Effect `{sfx}` was not found.");
            // Still proceed with follow-up routine so game flow is not blocked
            if (routine != null)
                StartCoroutine(routine);
            return;
        }
        g.SoundSource.PlayOneShot(soundEffect);
        if (routine != null)
            StartCoroutine(InvokeAfter(soundEffect.length, routine));
    }

    private IEnumerator InvokeAfter(float seconds, IEnumerator routine)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);
        if (routine != null)
            yield return StartCoroutine(routine);
    }
}
