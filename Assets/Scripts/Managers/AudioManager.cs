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
    /// <summary>Play a sound effect by key. Resilient ([[feedback_chiptune_audio]]): uses the real
    /// authored clip when present, otherwise a generated CHIPTUNE clip — so every event makes a sound,
    /// nothing is silent, and unknown keys never error-spam. Routes through the battle SoundSource when
    /// available, else the cross-scene Jukebox (so vendor-scene SFX work too).</summary>
    public void Play(string sfx)
    {
        if (string.IsNullOrEmpty(sfx)) return;

        AudioClip clip = null;
        var lib = SoundEffectLibrary.SoundEffects;
        if (lib != null) lib.TryGetValue(sfx, out clip);
        if (clip == null) clip = ChiptuneBank.Sfx(sfx); // chiptune fallback — never silent

        if (clip == null) return;
        if (g.SoundSource != null) g.SoundSource.PlayOneShot(clip);
        else Jukebox.PlaySfx(clip); // no battle SoundSource (e.g. vendor scenes)
    }

    /// <summary>
    /// Play a clip and then run the provided coroutine after it finishes (approximate: waits clip.length in realtime).
    /// </summary>
    public void PlayAndThen(string sfx, IEnumerator routine)
    {
        AudioClip clip = null;
        var lib = SoundEffectLibrary.SoundEffects;
        if (lib != null && !string.IsNullOrEmpty(sfx)) lib.TryGetValue(sfx, out clip);
        if (clip == null && !string.IsNullOrEmpty(sfx)) clip = ChiptuneBank.Sfx(sfx); // chiptune fallback

        if (clip == null)
        {
            if (routine != null) StartCoroutine(routine); // don't block game flow
            return;
        }
        if (g.SoundSource != null) g.SoundSource.PlayOneShot(clip);
        else Jukebox.PlaySfx(clip);
        if (routine != null)
            StartCoroutine(InvokeAfter(clip.length, routine));
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
