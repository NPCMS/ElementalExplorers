using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractDescentParser
{
    private readonly List<string> tokens;
    private int index;

    protected AbstractDescentParser(List<string> tokens, GameObject parent) {
        this.tokens = tokens;
        this.index = 0;
    }

    public abstract bool Parse(ElevationData elevation);
}
