using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputNode : ExtendedNode
{
    public abstract void ApplyInputs(ProceduralManager manager);
}
