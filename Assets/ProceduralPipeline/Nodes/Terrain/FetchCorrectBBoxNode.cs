using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class FetchCorrectBBoxNode : Node {

	[Input] public double longitude;
	[Input] public double latitude;
	[Input] public double width;

	[Output] public GlobeBoundingBox boundingBox;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}