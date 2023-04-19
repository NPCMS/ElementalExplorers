using System;
using System.Collections;
using System.Collections.Generic;

[CreateNodeMenu("Output/Output Points of Interest")]
public class OutputPointsOfInterestNode : SyncOutputNode
{

    [Input] public List<GeoCoordinate> pois; 
    
    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        pois = null;
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        manager.AddPois(GetInputValue("pois", pois));
    }
}
