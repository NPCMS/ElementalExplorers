using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Merge Building Nodes")]
public class MergeBuildingsNode : SyncExtendedNode
{
    [Input] public OSMBuildingData[] buildingData;
    [Input] public float maxMergeDistance;
    [Output] public OSMBuildingData[] outputBuildingData;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "outputBuildingData")
        {
            return outputBuildingData;
        }
        return null;
    }

    private void MergeNodes(List<Vector2> footprint, float mergeDst)
    {
        for (int i = 0; i < footprint.Count - 1; i++)
        {
            if ((footprint[i] - footprint[i + 1]).sqrMagnitude <= mergeDst)
            {
                footprint[i] = (footprint[i] + footprint[i + 1]) / 2;

                footprint.RemoveAt(i);
                i--;
            }
        }
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        OSMBuildingData[] bd = GetInputValue("buildingData", buildingData);
        float mergeDst = GetInputValue("maxMergeDistance", maxMergeDistance);
        for (int i = 0; i < bd.Length; i++)
        {
            MergeNodes(bd[i].footprint, mergeDst);
        }
        outputBuildingData = bd;
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        buildingData = null;
        outputBuildingData = null;
    }
}