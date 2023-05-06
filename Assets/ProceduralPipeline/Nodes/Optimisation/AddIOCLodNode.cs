using System;
using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("Optimisation/Add IOC LOD")]
public class AddIOCLodNode : SyncExtendedNode {

    [Input] public GameObject[] input;
    [Input] public bool isChunked = false;
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

    private void AddLOD(GameObject go)
    {
        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in filters)
        {
            GameObject lodGO = filter.gameObject;
            bool occluded = false;
            if (lodGO.tag == "LOD" || lodGO.tag == "LODO")
            {
                occluded = lodGO.tag == "LODO";
                lodGO = lodGO.transform.parent.gameObject;
                if (lodGO.tag == "LOD" || lodGO.tag == "LODO")
                {
                    continue;
                }
            }
            lodGO.layer = occluded ? 9 : 8;
            lodGO.transform.parent = null;
            lodGO.AddComponent<IOClod>().Static = true;
            lodGO.tag = "LOD";
        }
    }

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
        SyncYieldingWait wait = new SyncYieldingWait();
        GameObject[] go = GetInputValue("input", input);
        bool chunked = GetInputValue("isChunked", isChunked);
        if (chunked)
        {
            for (int i = 0; i < go.Length; i++)
            {
                if (go[i].transform.childCount > 0)
                {
                    go[i].layer = 8;
                    go[i].transform.parent = null;
                    go[i].AddComponent<IOClod>().Static = true;
                    go[i].tag = "LOD";
                }
                if (wait.YieldIfTimePassed())
                {
                    yield return null;
                }
            }
        }
        else
        {
            for (int i = 0; i < go.Length; i++) 
            {
                AddLOD(go[i]);
                if (wait.YieldIfTimePassed())
                {
                    yield return null;
                }
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