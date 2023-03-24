using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Optimisation/Add IOC LOD")]
public class AddIOCLodNode : SyncExtendedNode {

    [Input] public GameObject[] input;
    [Output] public GameObject[] output;
    // Use this for initialization
    protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
    {
        if (port.fieldName == "output")
        {
            return output;
        }
        return null; // Replace this
    }

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
        SyncYieldingWait wait = new SyncYieldingWait();
        GameObject[] go = GetInputValue("input", input);
        for (int i = 0; i < go.Length; i++)
        {
            MeshFilter[] filters = go[i].GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                GameObject lodGO = filter.gameObject;
                if (lodGO.tag == "LOD")
                {
                    lodGO = lodGO.transform.parent.gameObject;
                    if (lodGO.tag == "LOD")
                    {
                        continue;
                    }
                }
                lodGO.layer = 8;
                lodGO.transform.parent = null;
                lodGO.AddComponent<IOClod>().Static = false;
                lodGO.tag = "LOD";
            }
            if (wait.YieldIfTimePassed())
            {
                yield return null;
            }
        }

        output = go;
        callback.Invoke(true);
	}

	public override void Release()
	{
        input = null;
        output = null;
	}
}