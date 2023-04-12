using System;
using System.Collections;
using System.Collections.Generic;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Buildings/Output Building Classes")]
public class OutputBuildingClassesNode : SyncOutputNode 
{
    [Input] public OSMBuildingData[] buildingDatas;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return null;
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        OSMBuildingData[] buildings = GetInputValue("buildingDatas", buildingDatas);
        List<OSMBuildingData> buildingList = new List<OSMBuildingData>();
        foreach (var building in buildings)
        {
            buildingList.Add(building);
        }
       
        manager.AddBuildingInformationSection(buildingList);
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        buildingDatas = null;
    }
}