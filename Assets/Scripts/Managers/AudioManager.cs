using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using System.Collections;
using Scripts.Libraries;
using Scripts.Canvas;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// AUDIOMANAGER - Sound effect playback.
/// 
/// PURPOSE:
/// Provides centralized sound effect playback with lookup
/// from SoundEffectLibrary.
/// 
/// METHODS:
/// - Play(sfx): Play sound immediately
/// - PlayAndThen(sfx, routine): Play sound then run coroutine
/// 
/// USAGE:
/// ```csharp
/// g.AudioManager.Play("Click");
/// g.AudioManager.PlayAndThen("Victory", LoadNextScene());
/// ```
/// 
/// RELATED FILES:
/// - SoundEffectLibrary.cs: Sound effect registry
/// - GameHelper.cs: Provides SoundSource reference
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>Play.</summary>
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

    /// <summary>Invoke after.</summary>
    private IEnumerator InvokeAfter(float seconds, IEnumerator routine)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);
        if (routine != null)
            yield return StartCoroutine(routine);
    }
}

}
