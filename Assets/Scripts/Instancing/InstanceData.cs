using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InstanceData
{
    public int instancerIndex;
    public Matrix4x4[] instances;

    public InstanceData(int instancerIndex, Matrix4x4[] instances)
    {
        this.instancerIndex = instancerIndex;
        this.instances = instances;
    }
}


public struct MeshProperties
{
    public Matrix4x4 PositionMatrix;
    public Matrix4x4 InversePositionMatrix;
    //public float ControlData;

    public static int Size()
    {
        return
            sizeof(float) * 4 * 4 + // matrix;
            sizeof(float) * 4 * 4; // inverse matrix;
    }
}