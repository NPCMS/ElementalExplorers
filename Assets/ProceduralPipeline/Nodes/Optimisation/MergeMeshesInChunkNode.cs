﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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

    private void AddInstance(HashSet<Material> materials, Dictionary<Material, List<CombineInstance>> instances, Dictionary<Material, List<CombineInstance>> lod0, Dictionary<Material, List<CombineInstance>> lod1, Dictionary<Material, List<CombineInstance>> lod2, Dictionary<Material, bool> hasCollider, GameObject go, Transform parent)
    {
        if (go.TryGetComponent(out MeshRenderer renderer))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material sharedMaterial = renderer.sharedMaterials[i];
                Matrix4x4 transform = Matrix4x4.TRS(go.transform.position - parent.position, go.transform.rotation, go.transform.localScale);
                Dictionary<Material, List<CombineInstance>> dict = go.name == "Lod_0" ? lod0 : go.name == "Lod_1" ? lod1 : go.name == "Lod_2" ? lod2 : instances;
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
            AddInstance(materials, instances, lod0, lod1, lod2, hasCollider, child.gameObject, parent);
        }
    }

    public GameObject Merge(Material material, CombineInstance[] instances, Transform parent, string name, string tag, bool collider)
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

        return mergeGO;
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
                AddInstance(materials, instances, lod0, lod1, lod2, hasCollider, go, parent);
                DestroyImmediate(go);
            }
            GameObject instanceParent = new GameObject("Instances");
            instanceParent.transform.parent = parent;
            instanceParent.transform.localPosition = Vector3.zero;
            GameObject lod0Parent = new GameObject("Lod_0");
            lod0Parent.tag = "LODO";
            lod0Parent.transform.parent = parent;
            lod0Parent.transform.localPosition = Vector3.zero;
            GameObject lod1Parent = new GameObject("Lod_1");
            lod1Parent.tag = "LODO";
            lod1Parent.transform.parent = parent;
            lod1Parent.transform.localPosition = Vector3.zero;
            GameObject lod2Parent = new GameObject("Lod_2");
            lod2Parent.tag = "LODO";
            lod2Parent.transform.parent = parent;
            lod2Parent.transform.localPosition = Vector3.zero;

            foreach (Material merge in materials)
            {
                if (instances.ContainsKey(merge))
                {
                    Merge(merge, instances[merge].ToArray(), instanceParent.transform, merge.name, "Untagged", true);
                }
                if (lod0.ContainsKey(merge))
                {
                    Merge(merge, lod0[merge].ToArray(), lod0Parent.transform, merge.name, "LODO", true);
                }
                if (lod1.ContainsKey(merge))
                {
                    Merge(merge, lod1[merge].ToArray(), lod1Parent.transform, merge.name, "LODO", true);
                }
                if (lod2.ContainsKey(merge))
                {
                    Merge(merge, lod2[merge].ToArray(), lod2Parent.transform, merge.name, "LODO", true);
                }

            }

            if (instanceParent.transform.childCount == 0)
            {
                DestroyImmediate(instanceParent);
            }
            if (lod0Parent.transform.childCount == 0)
            {
                DestroyImmediate(lod0Parent);
            }
            if (lod1Parent.transform.childCount == 0)
            {
                DestroyImmediate(lod1Parent);
            }
            if (lod2Parent.transform.childCount == 0)
            {
                DestroyImmediate(lod2Parent);
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