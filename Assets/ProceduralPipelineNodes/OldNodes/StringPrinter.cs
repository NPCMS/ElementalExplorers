using System;
using UnityEngine;

namespace ProceduralPipelineNodes.Nodes
{
	[CreateNodeMenu("Legacy/Output/String Printer")]
	public class StringPrinter : OutputNode {
		[Input] public string str;
		// Use this for initialization
		protected override void Init() {
			base.Init();
		
		}

		public override void ApplyOutput(ProceduralManager manager)
		{
			Debug.Log(str);
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			callback.Invoke(true);
		}

		public override void Release()
		{
			str = null;
		}
	}
}