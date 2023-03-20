using System;
using ProceduralPipelineNodes.Nodes.Chunking;
using XNode;

namespace ProceduralPipelineNodes.Nodes.Terrain
{
	[CreateNodeMenu("Output/Grass Output")]
	public class GrassOutputNode : OutputNode {

		[Input] public GrassRenderer.GrassChunk[] grass;
		[Input] public ChunkContainer chunking;

		// Use this for initialization
		protected override void Init() {
			base.Init();
		
		}

		// Return the correct value of an output port when requested
		public override object GetValue(NodePort port) {
			return null; // Replace this
		}

		public override void ApplyOutput(ProceduralManager manager)
		{
			manager.ApplyGrass(GetInputValue("grass", grass), GetInputValue("chunking", chunking));
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			callback.Invoke(true);
		}

		public override void Release()
		{
			grass = null;
			chunking = null;
		}
	}
}