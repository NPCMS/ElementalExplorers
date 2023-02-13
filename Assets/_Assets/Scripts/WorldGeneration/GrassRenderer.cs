using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GrassRenderer : MonoBehaviour
{
    [System.Serializable]
    public class GrassChunk
    {
        public const int MaxInstancesPerBatch = 1023;

        public Transform parent;
        public List<Matrix4x4> transforms;
        private Matrix4x4[][] batchedTransforms;
        public Vector3 center;

        public GrassChunk(Transform parent, List<Matrix4x4> transforms, Vector3 center)
        {
            this.parent = parent;
            this.transforms = transforms;
            this.center = center;
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
    [Header("LOD")]
    [SerializeField] private Transform camTransform;
    [SerializeField] private float densityWithDistance = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool render = false;

    private ChunkContainer chunkContainer;
    private GrassChunk[,] chunks;

    private ComputeBuffer positionBuffer;

    private void OnValidate()
    {
        if (!render)
        {
            chunks = null;
            chunkContainer = null;
        }
    }

    private void Update()
    {
        if (render && chunks != null && chunks[0,0].parent != null)
        {
            ApplyLODToBatched();
            // RenderGrassInstanced();
            // RenderGrassInstancedProcedural();
        }
    }

    private void ApplyLODToBatched()
    {
        Vector2 forward = new Vector2(camTransform.forward.x, camTransform.forward.z);
        Vector2Int cameraPos = chunkContainer.GetChunkCoordFromPosition(camTransform.position);
        for (int i = 0; i < chunks.GetLength(0); i++)
        {
            for (int j = 0; j < chunks.GetLength(1); j++)
            {
                GrassChunk chunk = chunks[i, j];
                float density = GetDensityMultiplier(new Vector2Int(j, i), cameraPos, forward);
                chunk.parent.gameObject.SetActive(density > 0);
            }
        }
    }

    private void InitialiseChunksForBatching(GrassChunk[] grassChunks)
    {
        for (int i = 0; i < grassChunks.Length; i++)
        {
            GrassChunk grassChunk = grassChunks[i];
            Vector3 center = grassChunk.parent.position;
            List<CombineInstance> instances = new List<CombineInstance>();
            foreach (Matrix4x4 mat in grassChunk.transforms)
            {
                Vector4 pos = mat.GetPosition();
                pos -= (Vector4)center;
                mat.SetColumn(3, pos);
                instances.Add(new CombineInstance() { mesh = grassMesh, transform = mat});
            }

            MeshFilter filter = grassChunk.parent.gameObject.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            grassChunk.parent.gameObject.isStatic = true;
            grassChunk.parent.gameObject.layer = layer;
            mesh.CombineMeshes(instances.ToArray());
            mesh.RecalculateTangents();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            MeshRenderer mRender = grassChunk.parent.gameObject.AddComponent<MeshRenderer>();
            mRender.sharedMaterial = grassMaterial;
            mRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mRender.receiveShadows = true;
        }
    }

    private void InitialiseChunksForInstancing(GrassChunk[] grassChunks)
    {
        ChunkingInfo chunkInfo = chunkContainer.chunkInfo;
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
        positionBuffer = new ComputeBuffer(properties.Length, 3 * 4 * 4);
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
        chunkContainer = chunking;
        //using DrawMeshInstanced
        InitialiseChunksForInstancing(grassChunks);

        //using DrawMeshProcedural
        //InitialiseForProceduralInstancing(grassChunks);

        //using Batching
        InitialiseChunksForBatching(grassChunks);
    }

    private void RenderGrassInstancedProcedural()
    {
        float width = chunkContainer.chunkInfo.chunkWidth * chunkContainer.chunkInfo.chunkWidthCount;
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, new Bounds(new Vector3(width / 2, 0, width / 2), new Vector3(width, width, width)), positionBuffer.count);
    }

    private float GetDensityMultiplier(Vector2Int chunk, Vector2Int cameraPos, Vector2 forward)
    {
        var dir = chunk - cameraPos;
        return Vector2.Dot(forward, dir) < 0 ? 0 : 1 - Mathf.Clamp01(dir.sqrMagnitude * densityWithDistance);
    }

    private void RenderGrassInstanced()
    {
        Vector2 forward = new Vector2(camTransform.forward.x, camTransform.forward.z);
        Vector2Int cameraPos = chunkContainer.GetChunkCoordFromPosition(camTransform.position);
        foreach (GrassChunk chunk in chunks)
        {
            Vector2Int pos = chunkContainer.GetChunkCoordFromPosition(chunk.center);
            float density = GetDensityMultiplier(pos, cameraPos, forward);
            for (int i = 0; i < chunk.GetNumberOfBatches * density; i++)
            {
                Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, chunk.GetBatch(i), GrassChunk.MaxInstancesPerBatch, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, layer);
            }
        }
    }

    private void Release()
    {
        if (positionBuffer != null)
        {
            positionBuffer.Release();
            positionBuffer = null;
        }
    }

    private void OnDisable()
    {
        Release();
    }
}
