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
    [SerializeField] private Texture2D clump;
    [SerializeField] private Texture2D mask;
    [SerializeField] private Texture2D heightmap;
    [SerializeField] private float mapSize = 1000f;
    [SerializeField] private float cellSize = 1;
    [SerializeField] private float clumpAmount = 500;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private Transform camera;

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
        placementShader.SetFloat("_MapSize", mapSize);
        placementShader.SetFloat("_MinHeight", -10);
        placementShader.SetFloat("_HeightScale", 50);
        placementShader.SetFloat("_CellSize", cellSize);
        placementShader.SetFloat("_Jitter", 500f / maxInstanceWidth);
        placementShader.SetFloat("_MinScale", minScale);
        placementShader.SetFloat("_MaxScale", maxScale);
        placementShader.SetTexture(kernel, "_Clumping", clump);
        placementShader.SetTexture(kernel, "_Heightmap", heightmap);
        placementShader.SetTexture(kernel, "_Mask", mask);
        placementShader.SetFloat("_ClumpAmount", clumpAmount);
        placementShader.SetFloat("_FOV", Mathf.Cos(camera.GetComponent<Camera>().fieldOfView * Mathf.Deg2Rad));
        
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
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)), argsBuffer);
    }

    private void OnDestroy()
    {
        argsBuffer.Dispose();
        meshPropertyData.Dispose();
    }
}
