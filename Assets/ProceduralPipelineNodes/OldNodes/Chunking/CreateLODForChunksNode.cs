using System;
using UnityEngine;
using XNode;

namespace ProceduralPipelineNodes.Nodes.Chunking
{
	[CreateNodeMenu("Legacy/Chunking/Create LODs for Chunks")]
	public class CreateLODForChunksNode : ExtendedNode {
		[Input] public ChunkContainer chunkContainer;
		[Input, Range(0, 1)] public float relativeCullDistance = 0.25f;
		[Output] public ChunkContainer outputContainer;
		// Use this for initialization
		protected override void Init() {
			base.Init();
		
		}

		// Return the correct value of an output port when requested
		public override object GetValue(NodePort port) {
			if (port.fieldName == "outputContainer")
			{
				return outputContainer;
			}
			return null; // Replace this
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			ChunkContainer container = GetInputValue("chunkContainer", chunkContainer);
			float cullDst = GetInputValue("relativeCullDistance", relativeCullDistance);
			foreach (ChunkData chunk in container.chunks)
			{
				LODGroup group = chunk.chunkParent.gameObject.AddComponent<LODGroup>();
				Renderer[] renderers = chunk.chunkParent.GetComponentsInChildren<Renderer>();
				LOD[] lods = { new LOD(cullDst, renderers) };
				group.SetLODs(lods);
			}
			outputContainer = container;
			callback.Invoke(true);
		}

		public override void Release()
		{
			base.Release();
			chunkContainer = null;
			outputContainer = null;
		}
	}
}