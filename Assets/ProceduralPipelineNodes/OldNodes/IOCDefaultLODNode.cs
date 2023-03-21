using System;
using UnityEngine;
using XNode;

namespace ProceduralPipelineNodes.Nodes
{
	[CreateNodeMenu("Legacy/IOCDefault")]
	public class IOCDefaultLODNode : ExtendedNode
	{
		[Input] public GameObject[] input;
		[Output] public GameObject[] output;
		// Use this for initialization
		protected override void Init() {
			base.Init();
		
		}

		// Return the correct value of an output port when requested
		public override object GetValue(NodePort port) {
			return null; // Replace this
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			GameObject[] gos = GetInputValue("input", input);
			output = new GameObject[gos.Length];
			for (int i = 0; i < gos.Length; i++)
			{
			}
			callback.Invoke(true);
		}
	}
}