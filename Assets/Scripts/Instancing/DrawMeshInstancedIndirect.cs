using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class DrawMeshInstancedIndirect : MonoBehaviour
{
    private ComputeBuffer argsBuffer;
    private ComputeBuffer matrixBuffer;
    private Material material;
    private Mesh mesh;
    private float size;

    public void Setup(Matrix4x4[] transforms, Mesh mesh, Material material, float size)
    {
        this.mesh = mesh;
        this.material = material;
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)transforms.Length;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsBuffer.SetData(args);
        matrixBuffer = new ComputeBuffer(transforms.Length, MeshProperties.Size(), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        matrixBuffer.SetData(transforms);
        material.SetBuffer("VisibleShaderDataBuffer", matrixBuffer);
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(transform.position - Vector3.down * 500.0f, new Vector3(size, 1000.0f, size)), argsBuffer);
    }
}
