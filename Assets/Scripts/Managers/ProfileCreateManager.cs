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

        // Begin scene fade-in, then present the keyboard dialog.
        scene.FadeIn(showKeyboardRoutine());
    }
}

}
