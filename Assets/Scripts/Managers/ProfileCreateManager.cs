using Scripts.Helpers;
using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using c = Scripts.Helpers.CanvasHelper;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Canvas;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
public class ProfileCreateManager : MonoBehaviour
{

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {


        // Validate canvas rect is available.
        if (c.CanvasRect == null)
        {
            Debug.LogError("CanvasHelper.CanvasRect is null. Cannot size background.");
            return;
        }

        // Read canvas dimensions and size the background to match.
        float screenWidth = c.CanvasRect.rect.width;
        float screenHeight = c.CanvasRect.rect.height;

        // Local coroutine to show the keyboard dialog after fade-in.
        IEnumerator showKeyboardRoutine()
        {
            // Show a prompt to create a profile.
            KeyboardDialog.Show(
                "Who are you?",
                onSubmit: (value) =>
                {
                    try
                    {
                        // Create the profile with the provided name.
                        ProfileHelper.CreateProfile(value);

                        // Navigate back to the title screen once created.
                        scene.Fade.ToTitleScreen();
                    }
                    catch (System.SystemException ex)
                    {
                        Debug.LogError($"Failed to create profile: {ex.Message}");
                    }
                }
            );

            // Yield once to allow UI flow to continue.
            yield return Wait.None();
        }

        // Stash the routine; the fade itself starts in Start() — during Awake the
        // FadeOverlayInstance's own Awake may not have run yet (undefined cross-object
        // Awake order), so fading here NRE'd on its uncached Image.
        keyboardRoutine = showKeyboardRoutine();
    }

    private IEnumerator keyboardRoutine;

    /// <summary>Begins the scene fade-in, then presents the keyboard dialog.</summary>
    private void Start()
    {
        scene.FadeIn(keyboardRoutine);
    }
}

}
