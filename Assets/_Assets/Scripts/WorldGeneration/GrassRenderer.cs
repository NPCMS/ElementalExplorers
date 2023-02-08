using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GrassRenderer : MonoBehaviour
{
    [System.Serializable]
    public class GrassChunk
    {
        public const int MaxInstancesPerBatch = 1023;

        [SerializeField] private Matrix4x4[][] transforms;

        public GrassChunk(List<Matrix4x4> transforms)
        {
            int batches = Mathf.CeilToInt(transforms.Count / MaxInstancesPerBatch);
            this.transforms = new Matrix4x4[batches][];
            for (int i = 0; i < batches; i++)
            {
                int amount = Mathf.Min(MaxInstancesPerBatch, transforms.Count - i * MaxInstancesPerBatch);
                this.transforms[i] = transforms.GetRange(i * MaxInstancesPerBatch, amount).ToArray();
            }
        }

        public int GetNumberOfBatches => transforms.Length;
        public Matrix4x4[] GetBatch(int i) => transforms[i];
    }

    [SerializeField] private Mesh grassMesh;
    [SerializeField] private Material grassMaterial;
    
    [Header("Debug")]
    [SerializeField] private bool render = false;

    private ChunkingInfo chunkInfo;
    private GrassChunk[,] chunks;

    private void Update()
    {
        if (render && chunks != null)
        {
            RenderGrass();
        }
    }

    public void InitialiseGrass(ChunkContainer chunking, GrassChunk[] grassChunks)
    {
        chunkInfo = chunking.chunkInfo;
        chunks = new GrassChunk[chunkInfo.chunkWidthCount, chunkInfo.chunkWidthCount];
        for (int i = 0; i < chunkInfo.chunkWidthCount; i++)
        {
            for (int j = 0; j < chunkInfo.chunkWidthCount; j++)
            {
                chunks[i,j] = grassChunks[i + chunkInfo.chunkWidthCount * j];
            }
        }
    }

    private void RenderGrass()
    {
        foreach (GrassChunk chunk in chunks)
        {
            for (int i = 0; i < chunk.GetNumberOfBatches; i++)
            {
                Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, chunk.GetBatch(i));
            }
        }
    }
}
