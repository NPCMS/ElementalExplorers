using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public abstract class ExtendedNode : Node
{
    //Calculate outputs should initialise each variable with the attribute [Output]
    //Assumes each input is grounded
    public abstract void CalculateOutputs(Action<bool> callback);
}
