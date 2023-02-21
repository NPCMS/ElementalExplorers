using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

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
    [SerializeField] private float minBoxLength = 100;
    [SerializeField] private float extension = 50;
    [SerializeField] private Transform camera;
    [SerializeField] private bool compute = true;
    [SerializeField] private bool render = true;

    [Header("Precompute")]
    [SerializeField] private bool precomputed = false;
    [SerializeField] private Texture2D clumping;
    [SerializeField] private Texture2D heightmap;
    [SerializeField] private Texture2D mask;
    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
    [SerializeField] private float mapScale;

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

        if (precomputed)
        {
            Initialise(mapScale, clumping, mask, heightmap, minHeight, maxHeight);
        }
    }

    private void OnValidate()
    {
        InitialiseVariables();

        if (precomputed)
        {
            placementShader.SetFloat("_MinHeight", minHeight);
        placementShader.SetFloat("_HeightScale", maxHeight - minHeight);
        }
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
            //Vector3 right = Vector3.Cross(forward, Vector3.up);
            if (compute)
            {
                placementShader.SetVector("_CameraForward", forward);
                //placementShader.SetVector("_CameraRight", right);
                placementShader.SetVector("_BoundingBox", FrustrumBoundingBox());
                placementShader.SetVector("_CameraPosition", camera.position);

                placementShader.Dispatch(kernel, maxInstanceWidth / 8, maxInstanceWidth / 8, 1);
            }

                //ComputeBuffer.CopyCount(meshPropertyData, argsBuffer, sizeof(uint));
            if (render)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(5000, 5000, 5000)), argsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, 0, camera.GetComponent<Camera>());
            }
        }
    }

    //first point is origin
    private Vector4 BoundingBoxOfPoints(params Vector2[] points)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < points.Length; i++)
        {
            minX = Mathf.Min(points[i].x, minX);
            minY = Mathf.Min(points[i].y, minY);
            maxX = Mathf.Max(points[i].x, maxX);
            maxY = Mathf.Max(points[i].y, maxY);
        }

        return new Vector4(minX, minY, maxX, maxY);
    }

    private Vector4 FrustrumBoundingBox()
    {
        float fov = camera.GetComponent<Camera>().fieldOfView;
        Vector2 cameraPos = new Vector2(camera.position.x, camera.position.z);
        Vector2 forward = new Vector2(camera.forward.x, camera.forward.z).normalized;
        Vector2 frustrumLeft = (Vector2)(Quaternion.AngleAxis(-fov, Vector3.forward) * (Vector3)forward);
        Vector2 frustrumRight = (Vector2)(Quaternion.AngleAxis(fov, Vector3.forward) * (Vector3)forward);

        Vector4 bbox = BoundingBoxOfPoints(Vector2.zero, forward * extension, frustrumLeft * minBoxLength, frustrumRight * minBoxLength);
        Vector2 origin = cameraPos + new Vector2(bbox.x, bbox.y);
        Vector2 diagonal = new Vector2(bbox.z - bbox.x, bbox.w - bbox.y) / cellSize;

        return new Vector4(origin.x, origin.y, diagonal.x, diagonal.y);
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
        print(mapSize + " " + minHeight + " " + maxHeight);
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
        
        SetArgs(maxInstanceWidth * maxInstanceWidth);

        initialised = true;
    }

    private void OnDestroy()
    {
        argsBuffer.Dispose();
        meshPropertyData.Dispose();
    }
}
