using System;
using System.Collections;
using XNode;

public abstract class SyncExtendedNode : Node
{
    //Initialises outputs should initialise each variable with the attribute [Output]
    //GetValue then just returns the already initialised output
    //Assumes each input is grounded
    public abstract IEnumerator CalculateOutputs(Action<bool> callback);

#if UNITY_EDITOR
    public virtual void ApplyGUI() { }
#endif

    // set all large structs stuff to null so that garbage collector can remove it from memory
    public abstract void Release();
}