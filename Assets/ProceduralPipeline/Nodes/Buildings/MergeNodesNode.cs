using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Merge Building Nodes")]
public class MergeNodesNode : ExtendedNode
{
    [Input] public OSMBuildingData[] buildingData;
    [Input] public float maxMergeDistance;
    [Output] public OSMBuildingData[] outputBuildingData;
    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

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

    public override void CalculateOutputs(Action<bool> callback)
    {
        OSMBuildingData[] bd = GetInputValue("buildingData", buildingData);
        float mergeDst = GetInputValue("maxMergeDistance", maxMergeDistance);
        for (int i = 0; i < bd.Length; i++)
        {
            MergeNodes(bd[i].footprint, mergeDst);
        }
        outputBuildingData = bd;
        callback.Invoke(true);
    }

    public override void Release()
    {
        buildingData = null;
        outputBuildingData = null;
    }
}