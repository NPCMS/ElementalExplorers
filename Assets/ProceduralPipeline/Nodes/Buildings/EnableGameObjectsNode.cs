using System;
using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Enable Gameobjects")]
public class SetActiveGameObjects : SyncExtendedNode
{
	[Input] public GameObject[] input;
	[Input] public bool isActive;
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
		GameObject[] gos = GetInputValue("input", input);
		bool active = GetInputValue("isActive", isActive);
		SyncYieldingWait wait = new SyncYieldingWait();
		for (int i = 0; i < gos.Length; i++)
		{
			gos[i].SetActive(isActive);
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
		input = null;
		output = null;
	}
}