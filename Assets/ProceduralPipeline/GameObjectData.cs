using System.Collections.Generic;
using UnityEngine;

public abstract class GameObjectData
{
    protected Matrix4x4 transform;
    public List<GameObjectData> children;

    public GameObjectData(Matrix4x4 transform)
    {
        this.transform = transform;
    }

    protected void TransformGameObject(Transform thisTransform, Transform parent)
    {
        thisTransform.parent = parent;
        thisTransform.localPosition = transform.GetPosition();
        thisTransform.localRotation = transform.GetRotation();
        thisTransform.localScale = transform.GetScale();
    }

    public abstract GameObject Instantiate(Transform parent);
}

public class MeshGameObjectData : GameObjectData
{
    private Mesh mesh;
    private Material material;

    public MeshGameObjectData(Matrix4x4 transform, Mesh mesh, Material material) : base(transform)
    {
        this.mesh = mesh;
        this.material = material;
    }

    public override GameObject Instantiate(Transform parent)
    {
        GameObject go = new GameObject();
        TransformGameObject(go.transform, parent);
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        foreach (GameObjectData child in children)
        {
            child.Instantiate(go.transform);
        }
        return go;
    }
}


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
