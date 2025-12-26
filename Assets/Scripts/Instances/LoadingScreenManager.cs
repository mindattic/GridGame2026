using UnityEngine;
using scene = Assets.Helpers.SceneHelper;

public class LoadingScreenManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scene.FadeIn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
