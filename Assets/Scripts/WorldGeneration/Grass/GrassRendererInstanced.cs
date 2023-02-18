using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GrassRendererInstanced : MonoBehaviour
{
    private struct MeshProperties
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

    [Header("Shaders")] [SerializeField] private ComputeShader placementShader;
    [Header("Parameters")]
    [SerializeField] private int maxInstanceWidth = 100;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    private uint[] args = new uint[5];
    private ComputeBuffer argsBuffer;
    private ComputeBuffer meshPropertyData;

    private int kernel;
    
    private void Start()
    {
        kernel = placementShader.FindKernel("CSMain");
        
        meshPropertyData = new ComputeBuffer(maxInstanceWidth * maxInstanceWidth, MeshProperties.Size(), ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        placementShader.SetBuffer(kernel, "Result", meshPropertyData);
        placementShader.SetFloat("_Size", maxInstanceWidth);
        placementShader.SetFloat("_Scale", 1000);
        placementShader.SetFloat("_Jitter", 500f / maxInstanceWidth);
        
        material.SetBuffer("VisibleShaderDataBuffer", meshPropertyData);
        
        SetArgs(maxInstanceWidth * maxInstanceWidth);

        ComputeBuffer.CopyCount(meshPropertyData, argsBuffer, sizeof(uint));
    }

    private void SetArgs(int instances, int submesh = 0)
    {
        args[0] = (uint)mesh.GetIndexCount(submesh);
        args[1] = (uint)instances;
        args[2] = (uint)mesh.GetIndexStart(submesh);
        args[3] = (uint)mesh.GetBaseVertex(submesh);
        argsBuffer.SetData(args);
    }

    private void Update()
    {
        meshPropertyData.SetCounterValue(0);
        placementShader.Dispatch(kernel, maxInstanceWidth / 8, maxInstanceWidth / 8, 1);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)), argsBuffer);
    }

    private void OnDestroy()
    {
        argsBuffer.Dispose();
        meshPropertyData.Dispose();
    }
}
