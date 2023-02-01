using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class BingSatelliteNode : ExtendedNode
{

	[Input] public GlobeBoundingBox boundingBox;
	[Output] public Texture2D satelliteImage;

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
        throw new NotImplementedException();
    }
}