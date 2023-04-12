using SerializableCallback;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class GrassRendererInstanced : MonoBehaviour
{
    [System.Serializable]
    private class GrassLOD
    {
        public Mesh mesh;
        public Material material;
        public int maxInstanceWidth = 256;

        [HideInInspector] public uint[] args;
        [HideInInspector] public ComputeBuffer argsBuffer;
        [HideInInspector] public ComputeBuffer vrArgsBuffer;
        [HideInInspector] public ComputeBuffer meshPropertyData;
        [HideInInspector] public ComputeBuffer instancedData;

        public void SetArgs(int submesh = 0)
        {
            if (args == null || args.Length == 0)
            {
                args = new uint[5];
            }
            args[0] = (uint)mesh.GetIndexCount(submesh);
            args[1] = (uint)(maxInstanceWidth * maxInstanceWidth);
            args[2] = (uint)mesh.GetIndexStart(submesh);
            args[3] = (uint)mesh.GetBaseVertex(submesh);
            argsBuffer.SetData(args);
        }

        public void Clear(bool vr)
        {
            if (argsBuffer != null)
            {
                argsBuffer.Dispose();
                meshPropertyData.Dispose();

                if (vr)
                {
                    instancedData.Dispose();
                    vrArgsBuffer.Dispose();
                }
            }
        }
    }


    [Header("Shaders")] [SerializeField] private ComputeShader placementShader;
    [SerializeField] private ComputeShader toInstancedShader;
    [Header("Parameters")]
    [SerializeField] private GrassLOD[] lods;
    //[SerializeField] private int indexOffset = 0;
    //[SerializeField] private int maxInstanceWidth = 100;
    //[SerializeField] private Mesh mesh;
    //[SerializeField] private Material material;
    [SerializeField] private float cellSize = 1;
    [SerializeField] private Texture2D clump;
    [SerializeField] private float clumpAmount = 500;
    [SerializeField] private float jitterScale = 0.3f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    // [SerializeField] private float minBoxLength = 100;
    // [SerializeField] private float extension = 50;
    [Header("Optimisation Parameters")]
    [SerializeField] private int length = 24;
    [SerializeField, Range(0, 1)] private float diagonalThreshold = 0.95f;
    [SerializeField] private float width = 1;
    [SerializeField] private float widthScaling = 0.005f;
    [SerializeField] private float maxWidth = 2f;
    [SerializeField] private float occlusionCullingTreshold = 0.1f;
    [SerializeField] private float frustrumCullingThreshold = 0.05f;
    [SerializeField] private float skipDistance = 1.05f;
    [SerializeField] private float skipOffset = 5;
    [SerializeField] private float skipAmount = 5;
    [SerializeField] private float billboardDistance = 0.001f;
    [Space]
    [SerializeField] private bool compute = true;
    [SerializeField] private bool render = true;

    private Transform cameraTransform;
    private Camera cam;

    private int kernel;

    private bool initialised;
    private bool vr;

    private void Start()
    {
        kernel = placementShader.FindKernel("CSMain");

        vr = XRSettings.enabled;
        if (vr)
        {
            Shader.EnableKeyword("USING_VR");
        }
        else
        {
            Shader.DisableKeyword("USING_VR");
        }
        foreach (GrassLOD lod in lods)
        {
            lod.meshPropertyData = new ComputeBuffer(lod.maxInstanceWidth * lod.maxInstanceWidth, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
            lod.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
            if (vr)
            {
                lod.vrArgsBuffer = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
                lod.vrArgsBuffer.SetData(new uint[] { (uint)(lod.maxInstanceWidth * lod.maxInstanceWidth), 1, 1 });
                lod.instancedData = new ComputeBuffer(lod.maxInstanceWidth * lod.maxInstanceWidth, MeshProperties.Size(),
                    ComputeBufferType.Counter, ComputeBufferMode.Immutable);
            }
            lod.SetArgs();
        }
    }

    private void OnValidate()
    {
        InitialiseVariables();
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            // Look for the only active camera from all cameras
            foreach (var c in Camera.allCameras)
            {
                if (c.isActiveAndEnabled)
                {
                    cameraTransform = c.transform;
                    break;
                } 
            }
            cam = cameraTransform.GetComponent<Camera>();

            if (XRSettings.enabled)
            {
                Shader.EnableKeyword("USING_VR");
            }
            else
            {
                Shader.DisableKeyword("USING_VR");
            }
        }
        else
        {
            if (initialised) 
            {
                Vector3 forward = cameraTransform.forward;
                //Vector3 right = Vector3.Cross(forward, Vector3.up);
                if (compute)
                {
                    Profiler.BeginSample("Grass Instance Compute");

                    Shader.SetGlobalVector("_CameraForward", forward);
                    Shader.SetGlobalVector("_Frustrum", FrustrumSteps());
                    Shader.SetGlobalVector("_CameraPosition", cameraTransform.position);
                    Shader.SetGlobalMatrix("_Projection", (XRSettings.enabled ? cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) : cam.projectionMatrix) * cam.worldToCameraMatrix);

                    int indexCount = 0;
                    foreach (GrassLOD lod in lods)
                    {

                        placementShader.SetBuffer(kernel, "Result", lod.meshPropertyData);
                        placementShader.SetFloat("_Size", lod.maxInstanceWidth);
                        placementShader.SetInt("_IndexOffset", indexCount);

                        if (vr)
                        {
                            toInstancedShader.SetBuffer(kernel, "Input", lod.meshPropertyData);
                            toInstancedShader.SetBuffer(kernel, "Result", lod.instancedData);
                            lod.material.SetBuffer("VisibleShaderDataBuffer", lod.instancedData);
                        }
                        else
                        {
                            lod.material.SetBuffer("VisibleShaderDataBuffer", lod.meshPropertyData);
                        }

                        lod.meshPropertyData.SetCounterValue(0);
                        int groups = Mathf.CeilToInt(lod.maxInstanceWidth / 8.0f);
                        if (Camera.current != cam)
                        {
                            placementShader.Dispatch(kernel, groups, groups, 1);
                        }
                        if (render)
                        {
                            if (vr)
                            {
                                ComputeBuffer.CopyCount(lod.meshPropertyData, lod.vrArgsBuffer, 0);
                                lod.instancedData.SetCounterValue(0);
                                toInstancedShader.SetBuffer(kernel, "Input", lod.meshPropertyData);
                                toInstancedShader.SetBuffer(kernel, "Result", lod.instancedData);
                                toInstancedShader.DispatchIndirect(kernel, lod.vrArgsBuffer);
                                ComputeBuffer.CopyCount(lod.instancedData, lod.argsBuffer, sizeof(uint));
                            }
                            else
                            {
                                ComputeBuffer.CopyCount(lod.meshPropertyData, lod.argsBuffer, sizeof(uint));
                            }
                        }
                        indexCount += lod.maxInstanceWidth * lod.maxInstanceWidth;
                    }

                    Profiler.EndSample();

                }

                
                if (render)
                {
                    foreach (GrassLOD lod in lods)
                    {
                        Debug.Log(lod.mesh);
                        Graphics.DrawMeshInstancedIndirect(lod.mesh, 0, lod.material, new Bounds(Vector3.zero, new Vector3(5000, 5000, 5000)), lod.argsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
                    }
                }
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

    private Vector4 FrustrumSteps()
    {
        Vector2 forward = new Vector2(cameraTransform.forward.x, cameraTransform.forward.z).normalized;
        float vertical = (Vector2.Dot(forward, Vector2.up));
        float horizontal = (Vector2.Dot(forward, Vector2.right));
        bool diagnoal = Mathf.Abs(vertical) < diagonalThreshold && Mathf.Abs(horizontal) < diagonalThreshold;
        return new Vector4(Mathf.Abs(vertical) > 0.5f ? 1 : 0, diagnoal ? 1 : 0, vertical > 0.0f ? 1 : 0,
            horizontal > 0.0f ? 1 : 0);
    }

    private void InitialiseVariables()
    {
        placementShader.SetFloat("_CellSize", cellSize);
        placementShader.SetFloat("_ScaleJitter", jitterScale);
        placementShader.SetFloat("_MinScale", minScale);
        placementShader.SetFloat("_MaxScale", maxScale);
        placementShader.SetFloat("_OcclusionCullingThreshold", occlusionCullingTreshold);
        placementShader.SetFloat("_FrustrumCullingThreshold", frustrumCullingThreshold);
        placementShader.SetFloat("_ClumpAmount", clumpAmount);
        placementShader.SetInt("_Length", length);
        placementShader.SetFloat("_Width", width);
        placementShader.SetFloat("_MaxWidth", maxWidth);
        placementShader.SetFloat("_WidthScaling", widthScaling);
        placementShader.SetFloat("_SkipDistance", skipDistance);
        placementShader.SetFloat("_SkipAmount", skipAmount);
        placementShader.SetFloat("_SkipOffset", skipOffset);
        placementShader.SetFloat("_BillboardDistance", billboardDistance);
    }
    

    public void InitialiseSingleTile(float mapSize, Texture2D clump, Texture2D heightmap, Texture2D mask, float minHeight, float maxHeight)
    {
        throw new NotImplementedException();
        //InitialiseVariables();
        //placementShader.DisableKeyword("TILED");
        //placementShader.SetTexture(kernel, "_Heightmap", heightmap);
        //Shader.SetGlobalFloat("_TerrainWidth", mapSize);
        //placementShader.SetFloat("_MapSize", mapSize);
        //placementShader.SetFloat("_MinHeight", minHeight);
        //placementShader.SetFloat("_HeightScale", maxHeight - minHeight);
        //placementShader.SetTexture(kernel, "_Clumping", clump);
        //placementShader.SetTexture(kernel, "_Mask", mask);
        //cam = cameraTransform.GetComponent<Camera>();


        //placementShader.SetInt("_IndexOffset", indexOffset);
        //placementShader.SetBuffer(kernel, "Result", meshPropertyData);
        //placementShader.SetFloat("_Size", maxInstanceWidth);
        //SetArgs(maxInstanceWidth * maxInstanceWidth);

        //if (vr)
        //{
        //    toInstancedShader.SetBuffer(kernel, "Input", meshPropertyData);
        //    toInstancedShader.SetBuffer(kernel, "Result", instancedData);
        //    material.SetBuffer("VisibleShaderDataBuffer", instancedData);
        //}
        //else
        //{

        //    material.SetBuffer("VisibleShaderDataBuffer", meshPropertyData);
        //}

        //initialised = true;
    }
    
    public void InitialiseMultiTile(float mapSize, Texture2D[,] heightmap, Texture2D[,] mask, float[,] minHeight, float[,] heightScales)
    {
        InitialiseVariables();
        placementShader.EnableKeyword("TILED");
        placementShader.SetTexture(kernel, "_Clumping", clump);
        placementShader.SetFloat("_TerrainWidth", mapSize);
        
        SetTiledMaps(heightmap, mask, minHeight, heightScales);
        
        cam = cameraTransform.GetComponent<Camera>();

        initialised = true;
    }
    
    private void SetTiledMaps(Texture2D[,] heightmaps, Texture2D[,] masks, float[,] minHeight, float[,] heightScales)
    {
        placementShader.EnableKeyword("TILED");
        placementShader.SetInt("_TileWidth", heightmaps.GetLength(0));
        placementShader.SetInt("_TileHeight",heightmaps.GetLength(1));
        Texture2DArray heightFlat = new Texture2DArray(heightmaps[0, 0].width, heightmaps[0, 0].height, heightmaps.GetLength(0) * heightmaps.GetLength(1),
            heightmaps[0, 0].format, false, true);
        Texture2DArray maskFlat = new Texture2DArray(masks[0, 0].width, masks[0, 0].height, heightmaps.GetLength(0) * heightmaps.GetLength(1),
            masks[0, 0].format, false, true);
        Vector4[] terrainSizes = new Vector4[heightmaps.GetLength(0) * heightmaps.GetLength(1)];
        for (int i = 0; i < heightmaps.GetLength(0); i++)
        {
            for (int j = 0; j < heightmaps.GetLength(1); j++)
            {
                int index = i + j * heightmaps.GetLength(0);
                heightFlat.SetPixels(heightmaps[i,j].GetPixels(), index);
                maskFlat.SetPixels(masks[i,j].GetPixels(), index);
                terrainSizes[index] = new Vector2(minHeight[i, j], heightScales[i, j]);
            }
        }

        heightFlat.Apply();
        maskFlat.Apply();
        placementShader.SetTexture(kernel, "_Heightmap", heightFlat);
        placementShader.SetTexture(kernel, "_Mask", maskFlat);
        placementShader.SetVectorArray("_TileSizes", terrainSizes);
    }

    private void OnDestroy()
    {
        foreach (GrassLOD lod in lods)
        {
            lod.Clear(vr);
        }
    }
}
