using System;
using System.Collections;

[CreateNodeMenu("PostPipeline/Set Elevation")]
public class SaveElevationDataNode : SyncOutputNode
{
    [Input] public ElevationData elevationData;
    
    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        elevationData = null;
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        manager.SetElevation(GetInputValue("elevationData", elevationData));
    }
}
