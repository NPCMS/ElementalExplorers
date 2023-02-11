using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public abstract class OutputNode : ExtendedNode
{
    public abstract void ApplyOutput(ProceduralManager manager);
}
