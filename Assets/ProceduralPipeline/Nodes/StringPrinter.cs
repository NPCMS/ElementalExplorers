using System;
using UnityEngine;

[CreateNodeMenu("Output/String Printer")]
public class StringPrinter : OutputNode {
	[Input] public string roadNodesArray;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	public override void ApplyOutput(ProceduralManager manager)
	{
		Debug.Log(roadNodesArray);
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