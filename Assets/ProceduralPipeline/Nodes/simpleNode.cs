using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class simpleNode : Node {

	// Use this for initialization
	protected override void Init() {
		base.Init();
	}

	// define inputs and outputs for the node using the tags
	[Input] public float value;
	[Output] public float result1;
	[Output] public float result2;
	
	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
	{
		// Check which output is being requested. 
		// In this node, there aren't any other outputs than "result".
		if (port.fieldName == "result1")
		{
			// Return input value + 1
			return GetInputValue<float>("value", this.value) + 1;
		}
		if (port.fieldName == "result2")
		{
			// Return input value - 1
			return GetInputValue<float>("value", this.value) - 1;
		}
		// Hopefully this won't ever happen, but we need to return something
		// in the odd case that the port isn't "result"
		else return null;
	}
}