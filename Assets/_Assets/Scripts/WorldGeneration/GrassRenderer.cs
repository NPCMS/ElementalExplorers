using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class GrassRenderer : MonoBehaviour
{
    [System.Serializable]
    public class GrassChunk
    {
        public const int MaxInstancesPerBatch = 1023;

        public List<Matrix4x4> transforms;
        private Matrix4x4[][] batchedTransforms;

        public GrassChunk(List<Matrix4x4> transforms)
        {
            this.transforms = transforms;
        }

        public void Batch()
        {
            int batches = Mathf.CeilToInt(transforms.Count / MaxInstancesPerBatch);
            this.batchedTransforms = new Matrix4x4[batches][];
            for (int i = 0; i < batches; i++)
            {
                int amount = Mathf.Min(MaxInstancesPerBatch, transforms.Count - i * MaxInstancesPerBatch);
                this.batchedTransforms[i] = transforms.GetRange(i * MaxInstancesPerBatch, amount).ToArray();
            }
        }

        public int GetNumberOfBatches => batchedTransforms.Length;
        public Matrix4x4[] GetBatch(int i) => batchedTransforms[i];
    }

    private struct GrassProperties
    {
        public Matrix4x4 mat;
        public static int Size()
        {
            return sizeof(float) * 4 * 4;
        }
    }

    [SerializeField] private Mesh grassMesh;
    [SerializeField] private Material grassMaterial;
    [SerializeField] private int layer;
    
    [Header("Debug")]
    [SerializeField] private bool render = false;

    private ChunkingInfo chunkInfo;
    private GrassChunk[,] chunks;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    private void Update()
    {
        if (render && chunks != null)
        {
            RenderGrassInstanced();
            //RenderGrassInstancedProcedural();
        }
    }

    private void InitialiseChunksForInstancing(GrassChunk[] grassChunks)
    {
        chunks = new GrassChunk[chunkInfo.chunkWidthCount, chunkInfo.chunkWidthCount];
        for (int i = 0; i < chunkInfo.chunkWidthCount; i++)
        {
            for (int j = 0; j < chunkInfo.chunkWidthCount; j++)
            {
                GrassChunk grassChunk = grassChunks[i + chunkInfo.chunkWidthCount * j];
                grassChunk.Batch();
                chunks[i, j] = grassChunk;
            }
        }
    }

    private void InitialiseBuffers(GrassProperties[] properties)
    {
        uint[] args = new uint[5];
        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)properties.Length;
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        meshPropertiesBuffer = new ComputeBuffer(properties.Length, GrassProperties.Size());
        meshPropertiesBuffer.SetData(properties);
        grassMaterial.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    private void InitialiseForProceduralInstancing(GrassChunk[] grassChunks)
    {
        List<GrassProperties> meshProperties = new List<GrassProperties>();
        foreach (GrassChunk chunk in grassChunks)
        {
            foreach (Matrix4x4 mat in chunk.transforms)
            {
                meshProperties.Add(new GrassProperties() { mat = mat });
            }
        }

        InitialiseBuffers(meshProperties.ToArray());
    }

    public void InitialiseGrass(ChunkContainer chunking, GrassChunk[] grassChunks)
    {
        Release();
        chunkInfo = chunking.chunkInfo;
        //using DrawMeshInstanced
        InitialiseChunksForInstancing(grassChunks);

        //using DrawMeshProcedural
        //InitialiseForProceduralInstancing(grassChunks);
    }

    private void RenderGrassInstancedProcedural()
    {
        float width = chunkInfo.chunkWidth * chunkInfo.chunkWidthCount;
        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, new Bounds(new Vector3(width / 2, 0, width / 2), new Vector3(width, width, width)), argsBuffer);
    }

    private void RenderGrassInstanced()
    {
        foreach (GrassChunk chunk in chunks)
        {
            for (int i = 0; i < chunk.GetNumberOfBatches; i++)
            {
                Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, chunk.GetBatch(i), GrassChunk.MaxInstancesPerBatch, null, UnityEngine.Rendering.ShadowCastingMode.TwoSided, true, layer);
            }
        }
    }

    private void Release()
    {
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }

    private void OnDisable()
    {
        Release();
    }
}
