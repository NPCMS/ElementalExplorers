using System;

namespace ProceduralPipelineNodes.Nodes.Terrain
{
	[CreateNodeMenu("Output/Terrain Elevation Output")]
	public class OutputTerrainElevationNode : OutputNode
	{
		[Input] public ElevationData elevation;
		// Use this for initialization
		protected override void Init() {
			base.Init();
		}

		public override void ApplyOutput(ProceduralManager manager)
		{
			manager.SetTerrainElevation(GetInputValue<ElevationData>("elevation"));
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			callback.Invoke(true);
		}

		public override void Release()
		{
			elevation = null;
		}
	}
}