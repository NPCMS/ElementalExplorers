using System;
using UnityEngine;

[CreateNodeMenu("Output/Material Texture Output")]
public class OutputTextureNode : OutputNode
{
    //output nodes should have no output connections, only inputs
    [Input] public Material material;
    [Input] public string identifier;
    [Input] public Texture2D texture;

	// Use this for initialization
	protected override void Init() {
		base.Init();
	}

    //this function will be called by the ProceduralManager at the end of execution
    //a function in manager will then be called from this node applying whatever output this node has recieved
    public override void ApplyOutput(ProceduralManager manager)
    {
        //calls the set material texture function in the manager, and gets the inputs
        material.SetTexture(GetInputValue("identifier", identifier), GetInputValue("texture", texture));
        // manager.SetTerrainMaterialTexture(GetInputValue<string>("identifier", identifier), GetInputValue<Texture2D>("texture", texture));
    }

    //no computation is required so CalculateOutputs instantly calls success
    public override void CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
    }

    public override void Release()
    {
        texture = null;
    }
}