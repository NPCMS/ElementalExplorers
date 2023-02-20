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
    [SerializeField] private float cellSize = 1;
    [SerializeField] private float clumpAmount = 500;
    [SerializeField] private float jitterScale = 0.3f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private Transform camera;

    private uint[] args = new uint[5];
    private ComputeBuffer argsBuffer;
    private ComputeBuffer meshPropertyData;

    private int kernel;

    private bool initialised;
    
    private void Start()
    {
        kernel = placementShader.FindKernel("CSMain");
        
        //meshPropertyData = new ComputeBuffer(maxInstanceWidth * maxInstanceWidth * 2, MeshProperties.Size(), ComputeBufferType.Append);
        meshPropertyData = new ComputeBuffer(maxInstanceWidth * maxInstanceWidth, MeshProperties.Size());
        
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    }

    private void OnValidate()
    {
        InitialiseVariables();
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
        if (initialised) 
        {
        Vector3 forward = new Vector3(camera.forward.x, 0, camera.forward.z).normalized;
        // float max = Mathf.Max(Mathf.Abs(forward.x), Mathf.Abs(forward.z));
        // float min = Mathf.Min(Mathf.Abs(forward.x) / max, Mathf.Abs(forward.z) / max);
        // forward.Normalize();
        // forward *= Mathf.Sqrt(1 + min * min);
        Vector3 right = Vector3.Cross(forward, Vector3.up);
        placementShader.SetVector("_CameraForward", forward);
        placementShader.SetVector("_CameraRight", right);
        placementShader.SetVector("_CameraPosition", camera.position);
        
        meshPropertyData.SetCounterValue(0);
        placementShader.Dispatch(kernel, maxInstanceWidth / 8, maxInstanceWidth / 8, 1);
        ComputeBuffer.CopyCount(meshPropertyData, argsBuffer, sizeof(uint));
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(5000, 5000, 5000)), argsBuffer);
        }
    }

    private void InitialiseVariables()
    {
        placementShader.SetFloat("_CellSize", cellSize);
        placementShader.SetFloat("_ScaleJitter", jitterScale);
        placementShader.SetFloat("_MinScale", minScale);
        placementShader.SetFloat("_MaxScale", maxScale);
        placementShader.SetFloat("_ClumpAmount", clumpAmount);
    }
    

    public void Initialise(float mapSize, Texture2D clump, Texture2D mask, Texture2D heightmap, float minHeight, float maxHeight)
    {
        InitialiseVariables();
        placementShader.SetBuffer(kernel, "Result", meshPropertyData);
        placementShader.SetFloat("_Size", maxInstanceWidth);
        placementShader.SetFloat("_MapSize", mapSize);
        placementShader.SetFloat("_MinHeight", minHeight);
        placementShader.SetFloat("_HeightScale", maxHeight - minHeight);
        placementShader.SetTexture(kernel, "_Clumping", clump);
        placementShader.SetTexture(kernel, "_Heightmap", heightmap);
        placementShader.SetTexture(kernel, "_Mask", mask);
        placementShader.SetFloat("_FOV", Mathf.Cos(camera.GetComponent<Camera>().fieldOfView * Mathf.Deg2Rad));
        
        material.SetBuffer("VisibleShaderDataBuffer", meshPropertyData);
        
        SetArgs(maxInstanceWidth * maxInstanceWidth * 2);

        ComputeBuffer.CopyCount(meshPropertyData, argsBuffer, sizeof(uint));

        initialised = true;
    }

    private void OnDestroy()
    {
        argsBuffer.Dispose();
        meshPropertyData.Dispose();
    }
}
