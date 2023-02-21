using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("Output/Save Texture (Editor only)")]
public class SaveTextureOutputNode : OutputNode
{
	[Input] public Texture2D texture;
	[Input] public string savePath;

	// Use this for initialization
	protected override void Init() 
	{
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) 
	{
		return null; // Replace this
	}

	public override void ApplyOutput(ProceduralManager manager)
	{
		#if UNITY_EDITOR
		byte[] file = GetInputValue("texture", texture).EncodeToPNG();
		string path = GetInputValue("savePath", savePath);
		Debug.Log(Application.dataPath);
		File.WriteAllBytes(Application.dataPath + "/" + path, file);
        AssetDatabase.ImportAsset("Assets/" + path);
		AssetDatabase.SaveAssets();
		#endif
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		texture = null;
	}
}