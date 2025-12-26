using Assets.Helper;
using System.Collections;
using UnityEngine;


public class CanvasParticleInstance : MonoBehaviour
{
    //Fields
    private float xRotationFocus;
    private float yRotationFocus;
    private float zRotationFocus;
    private float horizontalFocus;
    private float fallFocus;
    private RectTransform rectTransform;

    //Properties
    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }


    public void Initialize(float rotationFocus, float horizontalFocus, float fallFocus)
    {

        this.xRotationFocus = RNG.Boolean ? RNG.Float(0, rotationFocus) : 0;
        this.yRotationFocus = RNG.Boolean ? RNG.Float(0, rotationFocus) : 0;
        this.zRotationFocus = rotationFocus;
        this.horizontalFocus = horizontalFocus;
        this.fallFocus = fallFocus;
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(MoveAndDestroyRoutine());
    }

    private IEnumerator MoveAndDestroyRoutine()
    {
        while (rectTransform.anchoredPosition.x < Screen.width)
        {
            rectTransform.anchoredPosition += new Vector2(
                horizontalFocus * Time.deltaTime,
                -fallFocus * Time.deltaTime);

            rectTransform.Rotate(
                xRotationFocus * Time.deltaTime,
                yRotationFocus * Time.deltaTime,
                zRotationFocus * Time.deltaTime);

            yield return Wait.None();
        }
        Destroy(gameObject);
    }
}