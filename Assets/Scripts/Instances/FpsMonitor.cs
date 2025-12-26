//via: https://discussions.unity.com/t/accurate-frames-per-second-count/21088/6

using UnityEngine;

public class FpsMonitor
{
    const float measurePeriod = 0.5f;
    private int i = 0;
    private float nextPeriod = 0;
    public int currentFps;
    public bool isActive = false;

    //Method which is automatically called before the first frame update  
    public void Start(bool isActive)
    {
        this.isActive = isActive;
        nextPeriod = Time.realtimeSinceStartup + measurePeriod;
    }

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

