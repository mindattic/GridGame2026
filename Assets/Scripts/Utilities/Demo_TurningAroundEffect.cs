using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Utilities
{
public class Demo_TurningAroundEffect : MonoBehaviour
{
    public float rotSpeed_X;
    public float rotSpeed_Y;
    public float rotSpeed_Z;

    public float globalSpeed = 1f;


    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(rotSpeed_X, rotSpeed_Y, rotSpeed_Z) * globalSpeed * Time.deltaTime);
    }
}

}
