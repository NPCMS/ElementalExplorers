using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

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
}