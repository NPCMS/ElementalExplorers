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
            int group = instances / 8 + 1;
            shader.Dispatch(0, group, group, 1);
        }

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
    

    [SerializeField, FormerlySerializedAs("cullShader")] private ComputeShader cull;
    [SerializeField, FormerlySerializedAs("instanceShader")] private ComputeShader instance;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private float occlusionCullingThreshold = 0.1f;
    [SerializeField] private float frustrumCullingThreshold = 0.05f;
    [SerializeField] private float distanceThreshold = 0.95f;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer vrArgsBuffer;
    private ComputeBuffer instancedBuffer;

    private Dictionary<Vector2Int, IndirectChunk> chunkedShaders;
    private ComputeBuffer culledBuffer;

    private float chunkWidth;
    private bool vr;

    private Camera cam;

    private ComputeShader cullShader;
    private ComputeShader instanceShader;

    private void OnValidate()
    {
        if (cullShader == null)
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
            Debug.Log(chunk.Key + " " + chunk.Value.Count);
            ComputeBuffer unculledBuffer = new ComputeBuffer(chunk.Value.Count, MeshProperties.Size(), ComputeBufferType.Default, ComputeBufferMode.Immutable);
            MeshProperties[] properties = new MeshProperties[chunk.Value.Count];
            for (int i = 0; i < properties.Length; i++)
            {
                properties[i] = new MeshProperties() { PositionMatrix = chunk.Value[i], InversePositionMatrix = chunk.Value[i].inverse };
            }
            unculledBuffer.SetData(properties);
            ComputeShader cull = Instantiate(cullShader);
            cull.SetBuffer(0, "Input", unculledBuffer);
            int instanceWidth = (int)(Mathf.Sqrt(chunk.Value.Count)) + 1;
            cull.SetInt("_BufferLength", chunk.Value.Count);
            cull.SetInt("_Size", instanceWidth);
            cull.SetBuffer(0, "Result", culledBuffer);
            chunkedShaders.Add(chunk.Key, new IndirectChunk(cull, unculledBuffer, chunk.Value.Count));
        }

    }

    public void Setup(Matrix4x4[] transforms)
    {
        chunkWidth = (distanceThreshold / 1.5f);
        cullShader = Instantiate(cull);
        instanceShader = Instantiate(instance);
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)transforms.Length;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsBuffer.SetData(args);
        //InitialiseBuffer(transforms);
        InitialiseOutputBuffer(transforms.Length);
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
            instanceShader.SetBuffer(0, "Input", culledBuffer);
            instanceShader.SetBuffer(0, "Result", instancedBuffer);
        }
        this.material.SetBuffer("VisibleShaderDataBuffer", culledBuffer);
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

    private void InitialiseOutputBuffer(int length)
    {

        culledBuffer = new ComputeBuffer(length, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
        //unculledBuffer.SetData(props);
        vr = XRSettings.enabled;
        if (vr)
        {
            vrArgsBuffer = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
            vrArgsBuffer.SetData(new uint[] { (uint)length, 1, 1 });
            instancedBuffer = new ComputeBuffer(length, MeshProperties.Size(),
                ComputeBufferType.Counter, ComputeBufferMode.Immutable);
        }
    }

    private void InitialiseBuffer(Matrix4x4[] transforms)
    {
        MeshProperties[] props = new MeshProperties[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Matrix4x4 mat = transforms[i];
            props[i] = new MeshProperties() { PositionMatrix = mat, InversePositionMatrix = mat.inverse };
        }
        
        //unculledBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Default, ComputeBufferMode.Immutable);
        culledBuffer = new ComputeBuffer(1, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
        //unculledBuffer.SetData(props);
        vr = XRSettings.enabled;
        if (vr)
        {
            vrArgsBuffer = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
            vrArgsBuffer.SetData(new uint[] { (uint)transforms.Length, 1, 1 });
            instancedBuffer = new ComputeBuffer(transforms.Length, MeshProperties.Size(),
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
        Profiler.BeginSample("GPU Instance Culling");
        culledBuffer.SetCounterValue(0);
        if (Camera.current != cam)
        {
            Vector2Int coord = new Vector2Int(Mathf.RoundToInt(cam.transform.position.x / chunkWidth), Mathf.RoundToInt(cam.transform.position.z / chunkWidth));
            //Debug.Log(coord);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2Int chunk = coord + new Vector2Int(i, j);
                    if (chunkedShaders.TryGetValue(chunk, out IndirectChunk value))
                    {
                        //Debug.Log("> " + chunk);
                        value.Dispatch();
                    }
                }
            }
        }
        Profiler.EndSample();
        
        if (vr)
        {
            Profiler.BeginSample("GPU Instance VR Instancing");
            ComputeBuffer.CopyCount(culledBuffer, vrArgsBuffer, 0);
            instancedBuffer.SetCounterValue(0);
            instanceShader.DispatchIndirect(0, vrArgsBuffer);
            ComputeBuffer.CopyCount(instancedBuffer, argsBuffer, sizeof(uint));
            Profiler.EndSample();
        }
        else
        {
            ComputeBuffer.CopyCount(culledBuffer, argsBuffer, sizeof(uint));
        }
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(cam.transform.position, new Vector3(distanceThreshold, distanceThreshold, distanceThreshold)), argsBuffer, 0, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
    }

    private void OnDestroy()
    {
        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
            foreach (KeyValuePair<Vector2Int, IndirectChunk> chunk in chunkedShaders)
            {
                chunk.Value.Dispose();
            }
            //unculledBuffer.Dispose();
            culledBuffer.Dispose();
            if (vr)
            {
                vrArgsBuffer.Dispose();
                instancedBuffer.Dispose();
            }
        }
    }
}
