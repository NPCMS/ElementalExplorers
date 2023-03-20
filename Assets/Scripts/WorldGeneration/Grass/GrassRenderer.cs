using System.Collections.Generic;
using ProceduralPipelineNodes.Nodes.Chunking;
using UnityEngine;

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
    [SerializeField, Range(0, 1)] private float[] lodDistances;
    [SerializeField, Range(0, 1)] private float[] lodInstanceMultipliers;
    //[SerializeField] private float densityWithDistance = 0.5f;

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

    //private void Update()
    //{
    //    if (render && chunks != null && chunks[0,0].parent != null)
    //    {
    //        //ApplyLODToBatched();
    //        // RenderGrassInstanced();
    //        // RenderGrassInstancedProcedural();
    //    }
    //}

    //private void ApplyLODToBatched()
    //{
    //    Vector2 forward = new Vector2(camTransform.forward.x, camTransform.forward.z);
    //    Vector2Int cameraPos = chunkContainer.GetChunkCoordFromPosition(camTransform.position);
    //    for (int i = 0; i < chunks.GetLength(0); i++)
    //    {
    //        for (int j = 0; j < chunks.GetLength(1); j++)
    //        {
    //            GrassChunk chunk = chunks[i, j];
    //            float density = Mathf.Max(
    //                GetDensityMultiplier(new Vector2(j, i), cameraPos, forward),
    //                GetDensityMultiplier(new Vector2(j + 1, i), cameraPos, forward),
    //                GetDensityMultiplier(new Vector2(j, i + 1), cameraPos, forward),
    //                GetDensityMultiplier(new Vector2(j + 1, i + 1), cameraPos, forward));
    //            chunk.parent.gameObject.SetActive(density > 0);
    //        }
    //    }
    //}

    private void InitialiseChunksForBatching(GrassChunk[] grassChunks)
    {
        for (int i = 0; i < grassChunks.Length; i++)
        {
            GrassChunk grassChunk = grassChunks[i];
            GameObject groupParent = grassChunk.parent.gameObject;
            Vector3 center = grassChunk.parent.position;
            LOD[] lods = new LOD[lodDistances.Length];
            groupParent.isStatic = true;
            groupParent.layer = layer;
            List<CombineInstance> instances = new List<CombineInstance>();
            for (int l = 0; l < lodDistances.Length; l++)
            {
                instances.Clear();
                GameObject parent = new GameObject("LOD" + l);
                parent.transform.SetParent(groupParent.transform, false);
                for (int k = 0; k < grassChunk.transforms.Count * lodInstanceMultipliers[l]; k++)
                {
                    Matrix4x4 mat = grassChunk.transforms[(int)(k / lodInstanceMultipliers[l])];
                    Vector4 pos = mat.GetPosition();
                    pos -= (Vector4)center;
                    mat.SetColumn(3, pos);
                    instances.Add(new CombineInstance() { mesh = grassMesh, transform = mat, subMeshIndex = 0 });
                }

                MeshFilter filter = parent.AddComponent<MeshFilter>();
                var mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                parent.isStatic = true;
                parent.layer = layer;
                mesh.CombineMeshes(instances.ToArray());
                mesh.RecalculateTangents();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                filter.sharedMesh = mesh;
                MeshRenderer mRender = parent.AddComponent<MeshRenderer>();
                mRender.sharedMaterial = grassMaterial;
                mRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mRender.receiveShadows = true;

                lods[l] = new LOD(lodDistances[l], new Renderer[] { mRender });
            }

            LODGroup group = groupParent.AddComponent<LODGroup>();
            group.SetLODs(lods);
            group.RecalculateBounds();
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

    //private float GetDensityMultiplier(Vector2 chunk, Vector2Int cameraPos, Vector2 forward)
    //{
    //    var dir = chunk - cameraPos;
    //    return Vector2.Dot(forward, dir) < 0 ? 0 : 1 - Mathf.Clamp01(dir.sqrMagnitude * densityWithDistance);
    //}

    //private void RenderGrassInstanced()
    //{
    //    Vector2 forward = new Vector2(camTransform.forward.x, camTransform.forward.z);
    //    Vector2Int cameraPos = chunkContainer.GetChunkCoordFromPosition(camTransform.position);
    //    foreach (GrassChunk chunk in chunks)
    //    {
    //        Vector2Int pos = chunkContainer.GetChunkCoordFromPosition(chunk.center);
    //        float density = GetDensityMultiplier(pos, cameraPos, forward);
    //        for (int i = 0; i < chunk.GetNumberOfBatches * density; i++)
    //        {
    //            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, chunk.GetBatch(i), GrassChunk.MaxInstancesPerBatch, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, layer);
    //        }
    //    }
    //}

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
