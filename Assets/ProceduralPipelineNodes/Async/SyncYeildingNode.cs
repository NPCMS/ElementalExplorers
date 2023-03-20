using UnityEngine;
public class SyncYieldingWait
{
    private const float MaxTime = 1f / 60f;

    private float lastUpdateTime;

    public SyncYieldingWait()
    {
        lastUpdateTime = Time.realtimeSinceStartup;
    }

    public bool YieldIfTimePassed()
    {
        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - lastUpdateTime < MaxTime) return false;
        lastUpdateTime = currentTime;
        return true;
    }
}
