//via: https://discussions.unity.com/t/accurate-frames-per-second-count/21088/6

using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
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
using Scripts.Utilities;

namespace Scripts.Instances
{
public class FpsMonitor
{
    const float measurePeriod = 0.5f;
    private int i = 0;
    private float nextPeriod = 0;
    public int currentFps;
    public bool isActive = false;

    //Method which is automatically called before the first frame update  
    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    public void Start(bool isActive)
    {
        this.isActive = isActive;
        nextPeriod = Time.realtimeSinceStartup + measurePeriod;
    }

    /// <summary>Runs per-frame update logic.</summary>
    public void Update()
    {
        if (!isActive) 
            return;

        i++;

        if (Time.realtimeSinceStartup < nextPeriod) return;
        currentFps = (int)(i / measurePeriod);
        nextPeriod += measurePeriod;
        i = 0;
    }
}


}
