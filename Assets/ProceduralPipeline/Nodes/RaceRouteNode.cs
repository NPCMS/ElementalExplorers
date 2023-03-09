using System;
using XNode;

[CreateNodeMenuAttribute("Race Generator")]
public class RaceRouteNode : ExtendedNode
{
    [InputAttribute] public GlobeBoundingBox boundingBox;
    [InputAttribute] public int timeout;
    [InputAttribute] public int maxSize;
    [InputAttribute] public bool debug;
    [OutputAttribute] public OSMRoadWay[] wayArray;
    
    public override void CalculateOutputs(Action<bool> callback)
    {
        throw new NotImplementedException();
    }
}