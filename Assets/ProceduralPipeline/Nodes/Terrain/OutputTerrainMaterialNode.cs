using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class OutputTerrainMaterialNode : OutputNode
{
    [Input] public string identifier;
    [Input] public Texture2D texture;

	// Use this for initialization
	protected override void Init() {
		base.Init();
	}

    public override void ApplyOutput(ProceduralManager manager)
    {
        manager.SetTerrainMaterialTexture(GetInputValue<string>("identifier", identifier), GetInputValue<Texture2D>("texture", texture));
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
    }
}