using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Output/Output Texture")]
public class OutputTextureNode : SyncOutputNode {
    //output nodes should have no output connections, only inputs
    [Input] public Material material;
    [Input] public string identifier;
    [Input] public Texture2D texture;

    // Use this for initialization
    protected override void Init()
    {
        base.Init();
    }

    public override void Release()
    {
        texture = null;
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        //calls the set material texture function in the manager, and gets the inputs
        material.SetTexture(GetInputValue("identifier", identifier), GetInputValue("texture", texture));
        // manager.SetTerrainMaterialTexture(GetInputValue<string>("identifier", identifier), GetInputValue<Texture2D>("texture", texture));
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }
}