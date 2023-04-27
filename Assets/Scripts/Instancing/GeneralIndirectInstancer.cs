using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using UnityEngine.XR;

public class GeneralIndirectInstancer : MonoBehaviour
{
    private class IndirectChunk
    {
        public ComputeShader shader;
        public ComputeBuffer buffer;
        public int instances;
        
        public IndirectChunk(ComputeShader shader, ComputeBuffer buffer, int instances)
        {
            this.shader = shader;
            this.buffer = buffer;
            this.instances = instances;
        }

        public void Dispatch()
        {
            int group = Mathf.CeilToInt(instances / 64.0f);
            shader.Dispatch(0, group, 1, 1);
        }

        public void SetOutput(ComputeBuffer output, ComputeBuffer lowOutput)
        {
            shader.SetBuffer(0, "Result", output);
            shader.SetBuffer(0, "ResultLow", lowOutput);
        }

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
    

    [SerializeField, FormerlySerializedAs("cullShader")] private ComputeShader cull;
    // [SerializeField, FormerlySerializedAs("instanceShader")] private ComputeShader instance;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Mesh meshLow;
    [SerializeField] private Material material;
    [SerializeField] private Material materialLow;
    [SerializeField] private int maxInstances = 5000;
    [SerializeField] private float occlusionCullingThreshold = 0.1f;
    [SerializeField] private float frustrumCullingThreshold = 0.05f;
    [SerializeField] private float distanceThreshold = 0.95f;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer argsLowBuffer;
    // private ComputeBuffer vrArgsBuffer;
    private ComputeBuffer instancedBuffer;
    private ComputeBuffer instancedLowBuffer;

    private Dictionary<Vector2Int, IndirectChunk> chunkedShaders;
    private ComputeBuffer culledBuffer;
    private ComputeBuffer culledLowBuffer;

    private float chunkWidth;
    private bool vr;

    private Camera cam;
    private bool compute;
    private bool initial;
    // private ComputeShader instanceShader;

    private void OnValidate()
    {
        if (chunkedShaders == null)
        {
            return;
        }
        InitialiseVariables();
    }

    private void CreateBuffers(Matrix4x4[] transforms)
    {
        Dictionary<Vector2Int, List<Matrix4x4>> chunks = new Dictionary<Vector2Int, List<Matrix4x4>>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Vector3 pos = transforms[i].GetPosition();
            Vector2Int coord = new Vector2Int((int)(pos.x / chunkWidth), (int)(pos.z / chunkWidth));
            if (!chunks.ContainsKey(coord))
            {
                chunks.Add(coord, new List<Matrix4x4>());
            }

            chunks[coord].Add(transforms[i]);
        }
        chunkedShaders = new Dictionary<Vector2Int, IndirectChunk>();
        foreach (KeyValuePair<Vector2Int, List<Matrix4x4>> chunk in chunks)
        {
            ComputeBuffer unculledBuffer = new ComputeBuffer(chunk.Value.Count, MeshProperties.Size(), ComputeBufferType.Default, ComputeBufferMode.Immutable);
            MeshProperties[] properties = new MeshProperties[chunk.Value.Count];
            for (int i = 0; i < properties.Length; i++)
            {
                properties[i] = new MeshProperties() { PositionMatrix = chunk.Value[i], InversePositionMatrix = chunk.Value[i].inverse };
            }
            unculledBuffer.SetData(properties);
            ComputeShader newCull = Instantiate(cull);
            newCull.SetBuffer(0, "Input", unculledBuffer);
            newCull.SetInt("_BufferLength", chunk.Value.Count);
            chunkedShaders.Add(chunk.Key, new IndirectChunk(newCull, unculledBuffer, chunk.Value.Count));
            chunkedShaders[chunk.Key].SetOutput(culledBuffer, culledLowBuffer);
            if (vr)
            {
                chunkedShaders[chunk.Key].shader.SetBuffer(0, "Counter", instancedBuffer);
                chunkedShaders[chunk.Key].shader.SetBuffer(0, "CounterLow", instancedLowBuffer);
            }
        }

    }

    public void Setup(Matrix4x4[] transforms)
    {
        chunkWidth = (distanceThreshold / 1.5f);
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsBuffer.SetData(args);
        argsLowBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsLowBuffer.SetData(args);
        //InitialiseBuffer(transforms);
        InitialiseOutputBuffer();
        CreateBuffers(transforms);
        InitialiseVariables();
        //int length = transforms.Length;
        //cullShader.SetInt("_BufferLength", length);
        //instanceWidth = (int)(Mathf.Sqrt(length)) + 1;
        //cullShader.SetInt("_Size", instanceWidth);
        //cullShader.SetBuffer(0, "Input", unculledBuffer);
        //cullShader.SetBuffer(0, "Result", culledBuffer);
        if (vr)
        {
            // instanceShader.SetBuffer(0, "Input", culledBuffer);
            // instanceShader.SetBuffer(0, "Result", instancedBuffer);
        }
        this.material.SetBuffer("VisibleShaderDataBuffer", culledBuffer);
        this.materialLow.SetBuffer("VisibleShaderDataBuffer", culledLowBuffer);
    }

    private void InitialiseVariables()
    {
        foreach (KeyValuePair<Vector2Int, IndirectChunk> chunk in chunkedShaders)
        {
            chunk.Value.shader.SetFloat("_OcclusionCullingThreshold", occlusionCullingThreshold);
            chunk.Value.shader.SetFloat("_FrustrumCullingThreshold", frustrumCullingThreshold);
            chunk.Value.shader.SetFloat("_DistanceThreshold", distanceThreshold);
        }
    }

    private void InitialiseOutputBuffer()
    {

        culledBuffer = new ComputeBuffer(maxInstances, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
        culledLowBuffer = new ComputeBuffer(maxInstances, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
        //unculledBuffer.SetData(props);
        vr = XRSettings.enabled;
        if (vr)
        {
            // vrArgsBuffer = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
            // vrArgsBuffer.SetData(new uint[] { (uint)length, 1, 1 });
            instancedBuffer = new ComputeBuffer(1, MeshProperties.Size(),
                ComputeBufferType.Counter, ComputeBufferMode.Immutable);
            instancedLowBuffer = new ComputeBuffer(1, MeshProperties.Size(),
                ComputeBufferType.Counter, ComputeBufferMode.Immutable);
        }
    }

    private void Update()
    {
        if (cam == null)
        {
            // Look for the only active camera from all cameras
            foreach (var c in Camera.allCameras)
            {
                if (c.isActiveAndEnabled)
                {
                    cam = c;
                    break;
                }
            }
        }

        if (chunkedShaders == null)
        {
            return;
        }
        compute = !compute;
        if (Camera.current != cam && compute || !initial)
        {
            culledBuffer.SetCounterValue(0);
            culledLowBuffer.SetCounterValue(0);
            if (vr)
            {
                instancedBuffer.SetCounterValue(0);
                instancedLowBuffer.SetCounterValue(0);
            }
            Vector2Int coord = new Vector2Int(Mathf.RoundToInt(cam.transform.position.x / chunkWidth), Mathf.RoundToInt(cam.transform.position.z / chunkWidth));
            if (chunkedShaders.TryGetValue(coord, out IndirectChunk centerChunk))
            {
                centerChunk.Dispatch();
            }
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2Int chunk = coord + new Vector2Int(i, j);
                    if (!(i == 0 && j == 0) && chunkedShaders.TryGetValue(chunk, out IndirectChunk value))
                    {
                        value.Dispatch();
                    }
                }
            }
        }

        if (!compute || !initial)
        {
            if (vr)
            {
                // ComputeBuffer.CopyCount(culledBuffer, vrArgsBuffer, 0);
                ComputeBuffer.CopyCount(instancedBuffer, argsBuffer, sizeof(uint));
                ComputeBuffer.CopyCount(instancedLowBuffer, argsLowBuffer, sizeof(uint));
                // instanceShader.DispatchIndirect(0, vrArgsBuffer);
                // ComputeBuffer.CopyCount(instancedBuffer, argsBuffer, sizeof(uint));
            }
            else
            {
                ComputeBuffer.CopyCount(culledBuffer, argsBuffer, sizeof(uint));
                ComputeBuffer.CopyCount(culledLowBuffer, argsLowBuffer, sizeof(uint));
            }
            initial = true;
        }
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(cam.transform.position, new Vector3(distanceThreshold, distanceThreshold, distanceThreshold)), argsBuffer, 0, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
        Graphics.DrawMeshInstancedIndirect(meshLow, 0, materialLow, new Bounds(cam.transform.position, new Vector3(distanceThreshold, distanceThreshold, distanceThreshold)), argsLowBuffer, 0, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
    }

    private void OnDestroy()
    {
        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
            argsLowBuffer.Dispose();
            foreach (KeyValuePair<Vector2Int, IndirectChunk> chunk in chunkedShaders)
            {
                chunk.Value.Dispose();
            }
            //unculledBuffer.Dispose();
            culledBuffer.Dispose();
            culledLowBuffer.Dispose();
            if (vr)
            {
                // vrArgsBuffer.Dispose();
                instancedBuffer.Dispose();
                instancedLowBuffer.Dispose();
            }
        }
    }
}
