using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;


[System.Serializable]
public class TextureWrapAsync
{
	public float[,] tex;

	public TextureWrapAsync(float[,] tex)
	{
		this.tex = tex;
	}
}

[CreateNodeMenu("Utils/Texture To Float Array")]
public class TextureToFloatArrayNode : SyncExtendedNode {
	[Input] public Texture2D texture;
	[Output] public TextureWrapAsync output;
	[Output] public int width;
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
		else if (port.fieldName == "width")
		{
			return width;
		}
		return null; // Replace this
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		Texture2D tex = GetInputValue("texture", texture);
		width = tex.width;
		float[,] texFloat = new float[width, width];
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < width; j++)
			{
                texFloat[i,j] = tex.GetPixel(i, j).r;
			}
		}
		output = new TextureWrapAsync(texFloat);
		callback.Invoke(true);
		yield break;
	}

	public override void Release()
	{
		output = null;
		texture = null;
	}
}