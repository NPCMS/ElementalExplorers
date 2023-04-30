using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Utils/Load Precompute From Disk")]
public class LoadPrecomputeFromDisk : AsyncExtendedNode {

	[Input] public string filepath;
    [Output] public PrecomputeChunk chunk;
    
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
    { 
        if (port.fieldName == "chunk")
        {
            return chunk;
        }
        return null;
    }

    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        chunk = ChunkIO.LoadIn(GetInputValue("filepath", filepath));
        callback.Invoke(true);
    }

    protected override void ReleaseData()
    {
        chunk = null;
    }
}