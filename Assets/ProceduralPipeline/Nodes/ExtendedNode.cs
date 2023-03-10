using System;
using XNode;

public abstract class ExtendedNode : Node
{

    //Initialises outputs should initialise each variable with the attribute [Output]
    //GetValue then just returns the already initialised output
    //Assumes each input is grounded
    public abstract void CalculateOutputs(Action<bool> callback);

#if UNITY_EDITOR
    public virtual void ApplyGUI() { }
#endif

    public virtual void Release() { }
}
