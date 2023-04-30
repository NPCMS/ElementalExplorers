using UnityEngine;

public class DrawMeshInstancedIndirect : MonoBehaviour
{
    private ComputeBuffer argsBuffer;
    private ComputeBuffer matrixBuffer;
    private Material material;
    private Mesh mesh;
    private float size;
    private Matrix4x4[] transforms;

    private void OnEnable()
    {
        if (transforms != null)
        {
            InitialiseBuffer();
        }
    }

    public void Setup(Matrix4x4[] transforms, Mesh mesh, Material material, float size)
    {
        for (int i = 0; i < transforms.Length; i++)
        {
            Matrix4x4 mat = transforms[i];
            Vector3 pos = mat.GetPosition() - transform.position;
            mat.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));
            transforms[i] = mat;
        }
        this.transforms = transforms;
        this.mesh = mesh;
        this.material = new Material(material);
        this.size = size;
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)transforms.Length;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsBuffer.SetData(args);
        InitialiseBuffer();
    }

    private void InitialiseBuffer()
    {
        if (matrixBuffer != null)
        {
            matrixBuffer.Release();
        }
        MeshProperties[] props = new MeshProperties[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Matrix4x4 mat = transforms[i];
            Vector3 pos = mat.GetPosition() + transform.position;
            mat.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));
            props[i] = new MeshProperties() { PositionMatrix = mat, InversePositionMatrix = mat.inverse };
        }
        matrixBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Default, ComputeBufferMode.Immutable);
        matrixBuffer.SetData(props);
        this.material.SetBuffer("VisibleShaderDataBuffer", matrixBuffer);
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(transform.position + new Vector3(size / 2, -500.0f, size / 2), new Vector3(size, 1000.0f, size)), argsBuffer);
    }

    private void OnDestroy()
    {
        if (argsBuffer != null)
        {
            argsBuffer.Release();
            matrixBuffer.Release();
        }
    }
}
