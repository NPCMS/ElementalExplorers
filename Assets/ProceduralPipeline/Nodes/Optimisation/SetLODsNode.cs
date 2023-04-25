using System;
using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("Optimisation/Set LODs")]
public class SetLODsNode : SyncExtendedNode {
	[Input] public GameObject[] gameObjects;
	[Input] public int lod = 2;
	[Output] public GameObject[] output;
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

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		GameObject[] gos = GetInputValue("gameObjects", gameObjects);
		int level = GetInputValue("lod", lod);
		string lodname = level == 0 ? "Lod_0" : level == 1 ? "Lod_1" : "Lod_2";
		SyncYieldingWait wait = new SyncYieldingWait();
		for (int i = 0; i < gos.Length; i++)
		{
			gos[i].name = lodname;
			if (wait.YieldIfTimePassed())
			{
				yield return new WaitForEndOfFrame();
			}
			
		}
		output = gos;
		callback.Invoke(true);
	}

	public override void Release()
	{
		gameObjects = null;
		output = null;
	}
}