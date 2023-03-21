using System;
using UnityEngine;
using XNode;

namespace ProceduralPipelineNodes.Nodes.Precompute
{
	[CreateNodeMenu("Legacy/Precompute/Tile to Bounding Box")]
	public class TileToBoundingBoxNode : ExtendedNode
	{
		[Input] public Vector2Int tileInput;
		[Input] public float zoomLevel = 15;
		[Output] public GlobeBoundingBox boundingBox;
	
		// Use this for initialization
		protected override void Init() {
			base.Init();
		
		}

		// Return the correct value of an output port when requested
		public override object GetValue(NodePort port) 
		{
			if (port.fieldName == "boundingBox")
			{
				return boundingBox;
			}
			return null; // Replace this
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			boundingBox = TileCreation.GetBoundingBoxFromTile(GetInputValue("tileInput", tileInput), GetInputValue("zoomLevel", zoomLevel));
			callback.Invoke(true);
		}
	}
}