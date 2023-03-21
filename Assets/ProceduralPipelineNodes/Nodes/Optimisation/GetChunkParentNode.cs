using ProceduralPipelineNodes.Nodes.Chunking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using static UnityEditor.Rendering.CameraUI;

[CreateNodeMenu("Optimisation/Get Chunk Parents")]
public class GetChunkParentNode : SyncExtendedNode {
	[Input] public ChunkContainer container;
	[Output] public GameObject[] parents;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "parents")
		{
			return parents;
        }
		return null; // Replace this
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
        ChunkContainer chunks = GetInputValue("container", container);
        List<GameObject> gos = new List<GameObject>();
        foreach (ChunkData chunk in chunks.chunks)
        {
            gos.Add(chunk.chunkParent.gameObject);
        }

        parents = gos.ToArray();
        callback.Invoke(true);
		yield break;
    }

	public override void Release()
	{
		container = null;
		parents = null;
	}
}