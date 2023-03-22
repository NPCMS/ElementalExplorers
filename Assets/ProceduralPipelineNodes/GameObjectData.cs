using System.Collections.Generic;
using UnityEngine;

//SHOULD BE ABSTRACT
//DO NOT IMPLEMENT
public class GameObjectData
{
    public Vector3 Position { get; protected set; }
    protected Vector3 rotation;
    protected Vector3 scale;
    public List<GameObjectData> children;

    public GameObjectData(Vector3 position, Vector3 rotation, Vector3 scale)
    {
        this.Position = position;
        this.rotation = rotation;
        this.scale = scale;
        children = new List<GameObjectData>();
    }

    protected void TransformGameObject(Transform thisTransform, Transform parent)
    {
        thisTransform.parent = parent;
        thisTransform.localPosition = Position;
        thisTransform.localEulerAngles = rotation;
        thisTransform.localScale = scale;
    }

    public virtual GameObject Instantiate(Transform parent) { throw new System.Exception("Cannot instantiate abstract implementation"); }
}

[System.Serializable]
public class MeshGameObjectData : GameObjectData
{
    public SerializableMeshInfo Mesh { get; private set; }
    private Material material;

    public MeshGameObjectData(Vector3 position, Vector3 rotation, Vector3 scale, SerializableMeshInfo mesh, Material material) : base(position, rotation, scale)
    {
        this.Mesh = mesh;
        this.material = material;
    }

    public override GameObject Instantiate(Transform parent)
    {
        GameObject go = new GameObject();
        TransformGameObject(go.transform, parent);
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        Mesh madeMesh = Mesh.GetMesh();
        go.AddComponent<MeshFilter>().sharedMesh = madeMesh;
        go.AddComponent<MeshCollider>().sharedMesh = madeMesh;
        foreach (GameObjectData child in children)
        {
            child.Instantiate(go.transform);
        }
        return go;
    }
}

[System.Serializable]
public class PrefabGameObjectData : GameObjectData
{
    private GameObject prefab;

    public PrefabGameObjectData(Vector3 position, Vector3 rotation, Vector3 scale, GameObject prefab) : base(position, rotation, scale)
    {
        this.prefab = prefab;
    }

    public override GameObject Instantiate(Transform parent)
    {
        GameObject go = Object.Instantiate(prefab);
        TransformGameObject(go.transform, parent);
        foreach (GameObjectData child in children)
        {
            child.Instantiate(go.transform);
        }
        return go;
    }
}
