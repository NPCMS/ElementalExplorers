using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SyncInputNode : SyncExtendedNode
{
    public abstract void ApplyInputs(AsyncPipelineManager manager);   
}
