using UnityEngine;

[NodeTint(0.78f, 0.08f, 0.52f)]
public abstract class SyncYeildingNode : SyncExtendedNode
{
    [Input] public float minFrameRate;

    private float lastUpdateTime;

    protected bool YieldIfTimePassed()
    {
        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - lastUpdateTime < 1 / minFrameRate) return false;
        lastUpdateTime = currentTime;
        return true;
    }
}
