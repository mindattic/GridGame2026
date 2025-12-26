using Assets.Helper;
using System.Collections;
using UnityEngine;
using scene = Assets.Helpers.SceneHelper;

public class SplashScreenManager : MonoBehaviour
{
    //Fields
    private float waitDuration = 30;

    private void Awake()
    {
    }

    void Start()
    {
        StartCoroutine(FadeInRoutine());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            scene.Fade.ToTitleScreen();
    }

    private IEnumerator FadeInRoutine()
    {
        scene.FadeIn();
        yield return new WaitForSeconds(waitDuration);
        scene.Fade.ToTitleScreen();
    }
}
