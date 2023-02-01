using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class UpsampleElevationNode : Node {

	[Input] public ElevationData elevation;
	[Input] public int extraSubdivisions = 0;
	[Output] public ElevationData outputElevation;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}