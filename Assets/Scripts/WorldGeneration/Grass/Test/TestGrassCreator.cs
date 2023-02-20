using SerializableCallback;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

public class TestGrassCreator : MonoBehaviour
{
    private enum TestType
    {
        OneMesh,
        MultipleMeshes,
        OneMeshStatic,
        MultipleMeshesStatic,
        GraphicsInstanced,
        RenderPipeline,
        GraphicsIndirect,
        GraphicsProcedural
    }

    [Header("Test constants")]

    [SerializeField] private float halfWidth = 500;
    [SerializeField] private float density = 1;
    [SerializeField] private Material material;
    [SerializeField] private Material indirectMaterial;
    [SerializeField] private Mesh grassMesh;

    [Header("Test parameters")]
    [SerializeField] private TestType testType = TestType.OneMesh;
    [SerializeField] private UnityEngine.Rendering.IndexFormat chunkFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    [SerializeField] private int chunkResolution = 100;


    private void Start()
    {
        StartTest(CreateInstances());
    }

    private Matrix4x4[] CreateInstances()
    {
        Random random = new System.Random(1);
        Matrix4x4[] output = new Matrix4x4[(int)(halfWidth * 4 * halfWidth * density)];
        print(output.Length + " grass instances");
        for (int i = 0; i < output.Length; i++)
        {
            Vector3 pos = new Vector3((float)random.NextDouble() * 2 * halfWidth - halfWidth, 1, (float)random.NextDouble() * 2 * halfWidth - halfWidth);
            output[i] = Matrix4x4.TRS(pos, Quaternion.Euler(0, (float)random.NextDouble() * 360, 0), Vector3.one);
        }

        return output;
    }

    private void StartTest(Matrix4x4[] instances)
    {
        switch (testType)
        {
            case TestType.OneMesh:
                OneMeshTest(instances);
                break;
            case TestType.MultipleMeshes:
                MultipleMeshesTest(instances);
                break;
            case TestType.OneMeshStatic:
                OneMeshStaticTest(instances);
                break;
            case TestType.MultipleMeshesStatic:
                MultipleMeshesStaticTest(instances);
                break;
            case TestType.GraphicsInstanced:
                GraphicsInstancedTest(instances);
                break;
            case TestType.RenderPipeline:
                break;
            case TestType.GraphicsIndirect:
                GraphicsIndirectTest(instances);
                break;
            case TestType.GraphicsProcedural:
                GraphicsProceduralTest(instances);
                break;
        }
    }
    private void OneMeshTest(Matrix4x4[] instances)
    {
        GameObject go = new GameObject("One mesh");
        go.AddComponent<MeshRenderer>().sharedMaterial = material;

        CombineInstance[] combines = new CombineInstance[instances.Length];
        for (int i = 0; i < instances.Length; i++)
        {
            combines[i] = new CombineInstance() { mesh = grassMesh, transform = instances[i] };
        }
        Mesh combMesh = new Mesh();
        combMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combMesh.CombineMeshes(combines);
        combMesh.RecalculateNormals();
        combMesh.RecalculateBounds();
        combMesh.RecalculateTangents();
        go.AddComponent<MeshFilter>().sharedMesh = combMesh;
    }

    private void MultipleMeshesTest(Matrix4x4[] instances)
    {
        int instancesPerChunk = instances.Length / (chunkResolution * chunkResolution);
        for (int i = 0; i < chunkResolution; i++)
        {
            for (int j = 0; j < chunkResolution; j++)
            {
                int offset = (i + j * chunkResolution) * instancesPerChunk;
                GameObject go = new GameObject(i + " " + j);
                go.AddComponent<MeshRenderer>().sharedMaterial = material;

                CombineInstance[] combines = new CombineInstance[instancesPerChunk];
                for (int k = 0; k < instancesPerChunk; k++)
                {
                    int index = k + offset;
                    combines[k] = new CombineInstance() { mesh = grassMesh, transform = instances[index] };
                }
                Mesh combMesh = new Mesh();
                combMesh.indexFormat = chunkFormat;
                combMesh.CombineMeshes(combines);
                combMesh.RecalculateNormals();
                combMesh.RecalculateBounds();
                combMesh.RecalculateTangents();
                go.AddComponent<MeshFilter>().sharedMesh = combMesh;

            }
        }
    }

    private void OneMeshStaticTest(Matrix4x4[] instances)
    {
        GameObject go = new GameObject("One mesh");
        go.isStatic = true;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;

        CombineInstance[] combines = new CombineInstance[instances.Length];
        for (int i = 0; i < instances.Length; i++)
        {
            combines[i] = new CombineInstance() { mesh = grassMesh, transform = instances[i] };
        }
        Mesh combMesh = new Mesh();
        combMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combMesh.CombineMeshes(combines);
        combMesh.RecalculateNormals();
        combMesh.RecalculateBounds();
        combMesh.RecalculateTangents();
        go.AddComponent<MeshFilter>().sharedMesh = combMesh;
    }

    private void MultipleMeshesStaticTest(Matrix4x4[] instances)
    {
        int instancesPerChunk = instances.Length / (chunkResolution * chunkResolution);
        for (int i = 0; i < chunkResolution; i++)
        {
            for (int j = 0; j < chunkResolution; j++)
            {
                int offset = (i + j * chunkResolution) * instancesPerChunk;
                GameObject go = new GameObject(i + " " + j);
                go.isStatic = true;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;

                CombineInstance[] combines = new CombineInstance[instancesPerChunk];
                for (int k = 0; k < instancesPerChunk; k++)
                {
                    int index = k + offset;
                    combines[k] = new CombineInstance() { mesh = grassMesh, transform = instances[index] };
                }
                Mesh combMesh = new Mesh();
                combMesh.indexFormat = chunkFormat;
                combMesh.CombineMeshes(combines);
                combMesh.RecalculateNormals();
                combMesh.RecalculateBounds();
                combMesh.RecalculateTangents();
                go.AddComponent<MeshFilter>().sharedMesh = combMesh;

            }
        }
    }
    private void GraphicsInstancedTest(Matrix4x4[] instances)
    {
        List<Matrix4x4> matList = new List<Matrix4x4>(instances);
        int batchAmount = (int)(instances.Length / 1023) + 1;
        Matrix4x4[][] batches = new Matrix4x4[batchAmount][];
        for (int i = 0; i < batchAmount; i++)
        {
            batches[i] = matList.GetRange(i * 1023, Mathf.Min(instances.Length - i * 1023, 1023)).ToArray();
        }

        StartCoroutine(RunBatches(batches));
    }

    private IEnumerator RunBatches(Matrix4x4[][] batches)
    {
        while (isActiveAndEnabled)
        {
            for (int i = 0; i < batches.Length; i++)
            {
                Graphics.DrawMeshInstanced(grassMesh, 0, material, batches[i]);
            }
            yield return null;
        }
    }

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


    private void GraphicsIndirectTest(Matrix4x4[] instances)
    {
        StartCoroutine(RunBatches(instances));
    }

    private IEnumerator RunBatches(Matrix4x4[] instances)
    {
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)instances.Length;
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);
        ComputeBuffer argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[instances.Length];

        for (int i = 0; i < instances.Length; i++)
        {
            MeshProperties props = new MeshProperties();

            props.PositionMatrix = instances[i];
            props.InversePositionMatrix = instances[i].inverse;

            properties[i] = props;
        }

        ComputeBuffer meshPropertiesBuffer = new ComputeBuffer(instances.Length, MeshProperties.Size());

        meshPropertiesBuffer.SetData(properties);

        indirectMaterial.SetBuffer("VisibleShaderDataBuffer", meshPropertiesBuffer);

        Bounds bounds = new Bounds(Vector3.zero, new Vector3(halfWidth * 2, halfWidth * 2, halfWidth * 2));
        while (isActiveAndEnabled)
        {
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, indirectMaterial, bounds, argsBuffer);
            yield return null;
        }
        
        argsBuffer.Dispose();
        meshPropertiesBuffer.Dispose();
    }
    
    
    private void GraphicsProceduralTest(Matrix4x4[] instances)
    {
        StartCoroutine(RunBatches(instances));
    }

    private IEnumerator RunBatchesProcedural(Matrix4x4[] instances)
    {
        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[instances.Length];

        for (int i = 0; i < instances.Length; i++)
        {
            MeshProperties props = new MeshProperties();

            props.PositionMatrix = instances[i];
            props.InversePositionMatrix = instances[i].inverse;

            properties[i] = props;
        }

        ComputeBuffer meshPropertiesBuffer = new ComputeBuffer(instances.Length, MeshProperties.Size());

        meshPropertiesBuffer.SetData(properties);

        indirectMaterial.SetBuffer("VisibleShaderDataBuffer", meshPropertiesBuffer);

        Bounds bounds = new Bounds(Vector3.zero, new Vector3(halfWidth * 2, halfWidth * 2, halfWidth * 2));
        while (isActiveAndEnabled)
        {
            Graphics.DrawMeshInstancedProcedural(grassMesh, 0, indirectMaterial, bounds, instances.Length);
            yield return null;
        }
        
        meshPropertiesBuffer.Dispose();
    }

}