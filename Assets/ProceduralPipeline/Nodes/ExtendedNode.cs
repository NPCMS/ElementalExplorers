using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public abstract class ExtendedNode : Node
{

    //Initialises outputs should initialise each variable with the attribute [Output]
    //GetValue then just returns the already initialised output
    //Assumes each input is grounded
    public abstract void CalculateOutputs(Action<bool> callback);

    public virtual void ApplyGUI() { }
}
