using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GeneralIndirectInstancer : MonoBehaviour
{
    [SerializeField] private ComputeShader cullShader;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer unculledBuffer;
    private ComputeBuffer culledBuffer;

    public void Setup(Matrix4x4[] transforms)
    {
        this.material = new Material(material);
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)transforms.Length;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsBuffer.SetData(args);
        InitialiseBuffer(transforms);
    }

    private void InitialiseBuffer(Matrix4x4[] transforms)
    {
        MeshProperties[] props = new MeshProperties[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Matrix4x4 mat = transforms[i];
            props[i] = new MeshProperties() { PositionMatrix = mat, InversePositionMatrix = mat.inverse };
        }
        unculledBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Constant, ComputeBufferMode.Immutable);
        culledBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
        unculledBuffer.SetData(props);
        this.material.SetBuffer("VisibleShaderDataBuffer", unculledBuffer);
    }

    private void Update()
    {
        if (unculledBuffer == null)
        {
            return;
        }
        //cullShader.SetBuffer(0, "Input", unculledBuffer);
        //culledBuffer.SetCounterValue(0);
        //cullShader.SetBuffer(0, "Result", culledBuffer);
        //ComputeBuffer.CopyCount(culledBuffer, argsBuffer, sizeof(uint));
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(5000.0f, 1000.0f, 5000.0f)), argsBuffer);
    }

    private void OnDestroy()
    {
        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
            unculledBuffer.Dispose();
        }
    }

    internal void Initialise()
    {
        throw new NotImplementedException();
    }
}
