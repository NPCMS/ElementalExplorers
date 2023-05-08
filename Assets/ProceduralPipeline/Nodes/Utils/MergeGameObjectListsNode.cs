using System;
using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Merge GameObject arrays")]
public class MergeGameObjectListsNode : SyncExtendedNode
{

    [Input] public GameObject[] a;
    [Input] public GameObject[] b;
    [Output] public GameObject[] c;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "c") return c;
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        var al = GetInputValue("a", a);
        var bl = GetInputValue("b", b);
        Debug.Log("Starting!!!!");
        Debug.Log(al);
        Debug.Log(bl);
        c = new GameObject[al.Length + bl.Length];
        for (int i = 0; i < al.Length; i++)
        {
            c[i] = al[i];
        }
        for (int i = 0; i < bl.Length; i++)
        {
            c[i + al.Length] = bl[i];
        }
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        a = null;
        b = null;
        c = null;
    }
}
