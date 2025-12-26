using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderManager : MonoBehaviour
{
    //Fields
    [SerializeField] public float Ceiling = 1f;
    [SerializeField] public float Floor = -1f;
    [SerializeField] public float Focus = 0.05f;
    private bool isRising = true;


    void FixedUpdate()
    {
        if (isRising && transform.position.y < 1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, Ceiling, transform.position.z), Focus * Time.deltaTime);
        }
        else
        {
            Focus = RNG.Int(2, 5) * 0.01f;
            Floor = -1f + (-1f * RNG.Percent);
            isRising = false;
        }

        if (!isRising && transform.position.y > -1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, Floor, transform.position.z), Focus * Time.deltaTime);
        }
        else
        {
            Focus = RNG.Int(2, 5) * 0.01f;
            Ceiling = 1f + (1f * RNG.Percent);
            isRising = true;
        }

        transform.Rotate(Vector3.up * (3f * Time.deltaTime));
    }
}
