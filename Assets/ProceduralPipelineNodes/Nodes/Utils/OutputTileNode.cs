using ProceduralPipelineNodes.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Output/Output Tile")]
public class OutputTileNode : SyncOutputNode 
{
    [Input] public ElevationData elevation;
    [Input] public Vector2Int tileIndex;
    [Input] public GameObject[] children;
    [Input] public Texture2D waterMask;
    [Input] public Texture2D grassMask;
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

    public override void ApplyOutput(AsyncPipelineManager manager)
    {
        Vector2Int tile = GetInputValue("tileIndex", tileIndex);
        GameObject[] c = GetInputValue("children", children);
        manager.CreateTile(GetInputValue("elevation", elevation), c, tile, GetInputValue("waterMask", waterMask), GetInputValue("grassMask", grassMask));
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        elevation = null;
        children = null;
        waterMask = null;
        grassMask = null;
    }
}