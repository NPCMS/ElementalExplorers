using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using XNode;

[CreateNodeMenu("Optimisation/Merge Meshes in Chunk")]
public class MergeMeshesInChunkNode : SyncExtendedNode
{

    [Input] public ChunkContainer chunkContainer;

    [Input] public GameObject[] toChunk;

    [Output] public ChunkContainer outputContainer;
    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "outputContainer")
        {
            return outputContainer;
        }
        return null; // Replace this
    }

    private void AddInstance(HashSet<Material> materials, Dictionary<Material, List<CombineInstance>> instances, Dictionary<Material, List<CombineInstance>> lod0, Dictionary<Material, List<CombineInstance>> lod1, Dictionary<Material, List<CombineInstance>> lod2, Dictionary<Material, bool> hasCollider, GameObject go, Transform parent, Vector3 scale)
    {
        Vector3 childScale = new Vector3(go.transform.localScale.x * scale.x, go.transform.localScale.y * scale.y, go.transform.localScale.z * scale.z);
        if (go.TryGetComponent(out MeshRenderer renderer))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material sharedMaterial = renderer.sharedMaterials[i];
                Matrix4x4 transform = Matrix4x4.TRS(go.transform.position - parent.position, go.transform.rotation, childScale);
                Dictionary<Material, List<CombineInstance>> dict = go.name == "Lod_0" ? lod0 : go.name == "Lod_1" ? lod1 : go.name == "Lod_2" ? lod2 : instances;
                if (sharedMaterial == null)
                {
                    Debug.Log(renderer.transform.parent.gameObject);
                }
                if (!dict.ContainsKey(sharedMaterial))
                {
                    if (!materials.Contains(sharedMaterial))
                    {
                        materials.Add(sharedMaterial);
                    }
                    dict.Add(sharedMaterial, new List<CombineInstance>());
                }

                if (!hasCollider.ContainsKey(sharedMaterial))
                {
                    hasCollider.Add(sharedMaterial, go.GetComponentInChildren<Collider>() != null);
                }
                else
                {
                    hasCollider[sharedMaterial] |= go.GetComponentInChildren<Collider>() != null;
                }

                dict[sharedMaterial].Add(new CombineInstance() { mesh = go.GetComponent<MeshFilter>().sharedMesh, transform = transform, subMeshIndex = i });
            }
        }

        foreach (Transform child in go.transform)
        {
            AddInstance(materials, instances, lod0, lod1, lod2, hasCollider, child.gameObject, parent, childScale);
        }
    }

    public void Merge(Material material, CombineInstance[] instances, Transform parent, string name, string tag, bool collider)
    {
        GameObject mergeGO = new GameObject(name);
        mergeGO.tag = tag;
        mergeGO.layer = 8;
        mergeGO.transform.parent = parent;
        mergeGO.transform.localPosition = Vector3.zero;
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.CombineMeshes(instances, true, true);
        mesh.RecalculateBounds();
        mergeGO.AddComponent<MeshFilter>().sharedMesh = mesh;
        mergeGO.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collider)
        {
            mergeGO.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
    public void Merge(Material material, CombineInstance[] instances, Transform[] parents, string name, string tag, bool collider)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.CombineMeshes(instances, true, true);
        mesh.RecalculateBounds();
        foreach (Transform parent in parents)
        {
            GameObject mergeGO = new GameObject(name);
            mergeGO.tag = tag;
            mergeGO.layer = 8;
            mergeGO.transform.parent = parent;
            mergeGO.transform.localPosition = Vector3.zero;
            mergeGO.AddComponent<MeshFilter>().sharedMesh = mesh;
            mergeGO.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider)
            {
                mergeGO.AddComponent<MeshCollider>().sharedMesh = mesh;
                collider = false;
            }
        }
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        SyncYieldingWait wait = new SyncYieldingWait();
        ChunkContainer chunks = GetInputValue("chunkContainer", chunkContainer);
        GameObject[] gos = GetInputValue("toChunk", toChunk);
        Dictionary<Vector2Int, List<GameObject>> parented = new Dictionary<Vector2Int, List<GameObject>>();
        foreach (GameObject go in gos)
        {
            Vector2Int index = chunks.GetChunkCoordFromPosition(go.transform.position);
            if (!parented.ContainsKey(index))
            {
                parented.Add(index, new List<GameObject>());
            }
            parented[index].Add(go);

            if (wait.YieldIfTimePassed())
            {
                yield return null;
            }
        }

        foreach (KeyValuePair<Vector2Int, List<GameObject>> pair in parented)
        {
            Transform parent = chunks.chunks[pair.Key.x, pair.Key.y].chunkParent;
            HashSet<Material> materials = new HashSet<Material>();
            Dictionary<Material, List<CombineInstance>> instances = new Dictionary<Material, List<CombineInstance>>();
            Dictionary<Material, List<CombineInstance>> lod0 = new Dictionary<Material, List<CombineInstance>>();
            Dictionary<Material, List<CombineInstance>> lod1 = new Dictionary<Material, List<CombineInstance>>();
            Dictionary<Material, List<CombineInstance>> lod2 = new Dictionary<Material, List<CombineInstance>>();
            Dictionary<Material, bool> hasCollider = new Dictionary<Material, bool>();
            //Dictionary<Material, Material[]> exampleRenderer = new Dictionary<Material, Material[]>();
            foreach (GameObject go in pair.Value)
            {
                AddInstance(materials, instances, lod0, lod1, lod2, hasCollider, go, parent, Vector3.one);
                DestroyImmediate(go);
            }
            GameObject lod0Parent = new GameObject("Lod_0");
            lod0Parent.tag = "LODO";
            lod0Parent.transform.parent = parent;
            lod0Parent.transform.localPosition = Vector3.zero;
            lod0Parent.AddComponent<MeshRenderer>();
            GameObject lod1Parent = new GameObject("Lod_1");
            lod1Parent.tag = "LODO";
            lod1Parent.transform.parent = parent;
            lod1Parent.transform.localPosition = Vector3.zero;
            lod1Parent.AddComponent<MeshRenderer>();
            GameObject lod2Parent = new GameObject("Lod_2");
            lod2Parent.tag = "LODO";
            lod2Parent.transform.parent = parent;
            lod2Parent.transform.localPosition = Vector3.zero;
            lod2Parent.AddComponent<MeshRenderer>();

            foreach (Material merge in materials)
            {
                if (instances.ContainsKey(merge))
                {
                    CombineInstance[] allLODs = instances[merge].ToArray();
                    Merge(merge, allLODs, new Transform[] { lod0Parent.transform, lod1Parent.transform, lod2Parent.transform }, merge.name, "LOD", true);
                }
                if (lod0.ContainsKey(merge))
                {
                    Merge(merge, lod0[merge].ToArray(), lod0Parent.transform, merge.name, "LOD", true);
                }
                if (lod1.ContainsKey(merge))
                {
                    Merge(merge, lod1[merge].ToArray(), lod1Parent.transform, merge.name, "LOD", true);
                }
                if (lod2.ContainsKey(merge))
                {
                    Merge(merge, lod2[merge].ToArray(), lod2Parent.transform, merge.name, "LOD", true);
                }

            }

            if (wait.YieldIfTimePassed())
            {
                yield return null;
            }
        }

        outputContainer = chunks;

        callback.Invoke(true);
    }

    public override void Release()
    {
        chunkContainer = null;
        toChunk = null;
        outputContainer = null;
    }
}