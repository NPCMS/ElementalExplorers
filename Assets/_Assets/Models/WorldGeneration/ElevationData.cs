using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ElevationData
{
    public float[,] height;
    public GlobeBoundingBox box;
    public double maxHeight;
    public double minHeight;

    public ElevationData(float[,] height, GlobeBoundingBox box, double minHeight, double maxHeight)
    {
        this.height = height;
        this.box = box;
        this.maxHeight = maxHeight;
        this.minHeight = minHeight;
    }
}
