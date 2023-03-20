using System.Collections.Generic;
using UnityEngine;

//SHOULD BE ABSTRACT
//DO NOT IMPLEMENT
[System.Serializable]
public class GameObjectData
{
    protected Matrix4x4 transform;
    public List<GameObjectData> children;

    public GameObjectData(Matrix4x4 transform)
    {
        this.transform = transform;
        children = new List<GameObjectData>();
    }

    protected void TransformGameObject(Transform thisTransform, Transform parent)
    {
        thisTransform.parent = parent;
        thisTransform.localPosition = transform.GetPosition();
        thisTransform.localRotation = transform.GetRotation();
        thisTransform.localScale = transform.GetScale();
    }

    public virtual GameObject Instantiate(Transform parent) { throw new System.Exception("Cannot instantiate abstract implementation"); }
}

[System.Serializable]
public class MeshGameObjectData : GameObjectData
{
    private SerializableMeshInfo mesh;
    private Material material;

    public MeshGameObjectData(Matrix4x4 transform, SerializableMeshInfo mesh, Material material) : base(transform)
    {
        this.mesh = mesh;
        this.material = material;
    }

    public override GameObject Instantiate(Transform parent)
    {
        GameObject go = new GameObject();
        TransformGameObject(go.transform, parent);
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        Mesh madeMesh = mesh.GetMesh();
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

    public PrefabGameObjectData(Matrix4x4 transform, GameObject prefab) : base(transform)
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
