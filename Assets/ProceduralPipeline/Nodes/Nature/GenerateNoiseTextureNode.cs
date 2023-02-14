using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class GenerateNoiseTextureNode : ExtendedNode {


	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
	}
}