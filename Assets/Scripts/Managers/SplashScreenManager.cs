using Scripts.Helpers;
using System.Collections;
using UnityEngine;
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
public class SplashScreenManager : MonoBehaviour
{
    //Fields
    private float waitDuration = 30;

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
    }

    void Start()
    {
        StartCoroutine(FadeInRoutine());
    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            scene.Fade.ToTitleScreen();
    }

    /// <summary>Coroutine that executes the fade in sequence.</summary>
    private IEnumerator FadeInRoutine()
    {
        scene.FadeIn();
        yield return new WaitForSeconds(waitDuration);
        scene.Fade.ToTitleScreen();
    }
}

}
