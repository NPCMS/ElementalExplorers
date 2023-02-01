﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class BingElevationNode : ExtendedNode
{

	[Input] public GlobeBoundingBox boundingBox;
	[Output] public ElevationData elevationData;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "elevationData")
		{
			return elevationData;
		}
		return null;
	}

    public override void CalculateOutputs(Action<bool> callback)
    {
        throw new NotImplementedException();
    }
}