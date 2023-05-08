using System;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Texture Input")]
public class TextureInputNode : AsyncExtendedNode
{

	[Input] public Texture2D tex;

	[Output] public Texture2D output;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "output")
		{
			return output;
		}
		return null; // Replace this
	}

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		output = GetInputValue("tex", tex);
		callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		output = null;
	}
}