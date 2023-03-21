using System;
using UnityEngine;
using XNode;

namespace ProceduralPipelineNodes.Nodes.Chunking
{
	[CreateNodeMenu("Legacy/Chunking/Add Buildings to Chunks")]
	public class ChunkBuildingsNode : ExtendedNode {

		[Input] public ChunkContainer chunks;
		[Input] public GameObject[] buildings;
		[Output] public ChunkContainer outputChunks;

		protected override void Init() {
			base.Init();
		
		}

		public override object GetValue(NodePort port) {
			if (port.fieldName == "outputChunks")
			{
				return outputChunks;
			}
			return null; 
		}
		public override void CalculateOutputs(Action<bool> callback)
		{
			ChunkContainer chunkData = GetInputValue("chunks", chunks);
			GameObject[] gameObjects = GetInputValue("buildings", buildings);

			foreach (GameObject go in gameObjects)
			{ 
				Vector2Int chunk = chunkData.GetChunkCoordFromPosition(go.transform.position);
				ChunkData data = chunkData.chunks[chunk.x,chunk.y];
				go.transform.parent = data.chunkParent;
			}
			outputChunks = chunkData;
			callback.Invoke(true);
		}

		public override void Release()
		{
			chunks = null;
			buildings = null;
			outputChunks = null;
		}
	}
}