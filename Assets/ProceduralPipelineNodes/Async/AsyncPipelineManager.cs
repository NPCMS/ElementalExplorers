using ProceduralPipelineNodes.Nodes.Chunking;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using XNode;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class AsyncPipelineManager : MonoBehaviour
{
    [Header("Pipeline")]
    [SerializeField] private ProceduralPipeline pipeline;
    [SerializeField] private List<Vector2Int> queue = new();
    [SerializeField] private bool runPipelineOnStart;
    [SerializeField] private UnityEvent onFinishPipeline;
    [Header("Output References")]
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private GrassRendererInstanced grassInstanced;
    [SerializeField] private GeneralIndirectInstancer[] instancers;
    [SerializeField] private string shaderTerrainSizeIdentifier = "_TerrainWidth";

    [Header("Debug, click Run Pipeline to run in editor")]
    [SerializeField] private bool runPipeline;
    [SerializeField] private bool clearPipeline;
    [SerializeField] private string tilesLeft = "";
    [SerializeField] private string debugInfo = "";

    private Stack<Stack<SyncExtendedNode>> runOrder;
    private Stack<SyncExtendedNode> running;
    private HashSet<SyncExtendedNode> hasRun;
    private SyncExtendedNode runningNode;

    private List<Vector2Int> tileQueue = new();

    private Dictionary<Vector2Int, TileComponent> tiles;
    private Dictionary<Vector2Int, List<InstanceData>> instances;

    private bool tileSet = false;
    private float terrainSize;

    private void Start()
    {
        if (runPipelineOnStart)
        {
            tiles = new Dictionary<Vector2Int, TileComponent>();
            instances = new Dictionary<Vector2Int, List<InstanceData>>();
            tileSet = false;
            tileQueue = new List<Vector2Int>(queue);
            tilesLeft = tileQueue.Count.ToString();
            StartCoroutine(Run());
        }
    }

    //LOGIC FOR RUNNING THE PIPELINE
    private void OnValidate()
    {
        if (runPipeline)
        {
            runPipeline = false;
            BuildPipeline();
            ClearPipeline();
            BuildPipeline();
            RunNextLayer();
        }
        if (clearPipeline)
        {
            clearPipeline = false;
            BuildPipeline();
            ClearPipeline();
        }
    }

    public Vector2Int PopTile()
    {
        Vector2Int tile = tileQueue[0];
        tileQueue.RemoveAt(0);
        return tile;
    }

    public void OnNodeFinish(bool success)
    {
        if (!success)
        {
            Debug.LogError(runningNode.name + " in layer " + runOrder.Count + " has failed.");
            return;
        }

        // is output node
        if (runOrder.Count == 0)
        {
            ((SyncOutputNode)runningNode).ApplyOutput(this);
        }

        RunNextNode();
    }

    private void RunNextNode()
    {
        if (running.Count == 0)
        {
            RunNextLayer();
        }
        else
        {
            runningNode = running.Pop();
            if (hasRun.Contains(runningNode))
            {
                RunNextNode();
                return;
            }
            hasRun.Add(runningNode);
            debugInfo = runningNode.name;
            if (runningNode is SyncInputNode inputNode)
            {
                inputNode.ApplyInputs(this);
            }

            RunNode();
        }
    }

    private void RunNode()
    {
        StartCoroutine(runningNode.CalculateOutputs(OnNodeFinish));
    }
    private IEnumerator Run()
    {
        BuildPipeline();
        ClearPipeline();
        BuildPipeline();
        RunNextLayer();
        yield break;
    }

    private void RunNextLayer()
    {
        Debug.Log("LAYER" + runOrder.Count);
        if (runOrder.Count == 0)
        {
            Debug.Log("DONE" + tilesLeft);
            tilesLeft = tileQueue.Count.ToString();
            if (tileQueue.Count > 0)
            {
                StartCoroutine(Run());
            }
            else
            {
                SetupTiles();
                if (Application.isPlaying)
                {
                    BuildPipeline();
                    ClearPipeline();

                    if (runPipelineOnStart)
                    {
                        onFinishPipeline?.Invoke();
                    }
                }
            }
            return;
        }

        running = runOrder.Pop();
        RunNextNode();
    }

    private void ClearPipeline()
    {
        while (runOrder.Count > 0)
        {
            var currentLayer = runOrder.Pop();
            while (currentLayer.Count > 0)
            {
                SyncExtendedNode node = currentLayer.Pop();
                node.Release();
            }
        }

        print("Pipeline Cleared");
    }

    //BFS from output nodes and run the nodes from highest depth to lowest
    //highest depth must be inputs, lowest depth must be output
    //each layer in BFS must only depend on the layer above, therefore once one layer is complete the next one can be computed
    //assumes graph is a DAG, otherwise this will result in infinite loop
    private void BuildPipeline()
    {
        // empty set of nodes already run. A node can be in multiple layers and prevents needless recalculations
        hasRun = new HashSet<SyncExtendedNode>();
        // stack of layers. the top of the stack in the next layer required to be run
        runOrder = new Stack<Stack<SyncExtendedNode>>();
        // output layer to start with
        List<SyncExtendedNode> currentLayer = new List<SyncExtendedNode>();
        List<Node> nodes = pipeline.nodes;
        //get all outputs
        foreach (var t in nodes)
        {
            if (t is SyncOutputNode node)
            {
                currentLayer.Add(node);
            }
        }

        List<SyncExtendedNode> nextLayer = new List<SyncExtendedNode>();
        //while BFS can iterate further, will be infinite if cycle
        while (currentLayer.Count > 0)
        {
            //current layer has nodes, add it to the runorder
            runOrder.Push(new Stack<SyncExtendedNode>(currentLayer));
            foreach (var node in currentLayer)
            {
                foreach (NodePort port in node.Inputs)
                {
                    if (port.IsConnected)
                    {
                        nextLayer.Add((SyncExtendedNode)port.Connection.node);
                    }
                }
            }

            currentLayer = nextLayer;
            nextLayer = new List<SyncExtendedNode>();
        }
    }
    public void CreateTile(ElevationData elevation, GameObject[] children, Vector2Int tileIndex, Texture2D waterMask, Texture2D grassMask)
    {
        GameObject terrain = new GameObject(tileIndex.ToString());
        TileComponent tileComponent = terrain.AddComponent<TileComponent>();

        if (!tileSet)
        {
            tileSet = true;
            terrainSize = (float)GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
            Shader.SetGlobalFloat(shaderTerrainSizeIdentifier, terrainSize);
            Shader.SetGlobalFloat("_TerrainResolution", elevation.height.GetLength(0));
        }

        tileComponent.SetTerrainElevation(elevation, terrainSize);
        tileComponent.SetMaterial(terrainMaterial, waterMask);
        tileComponent.SetGrassData(grassMask);
        foreach (GameObject go in children)
        {
            go.transform.SetParent(terrain.transform, true);
        }
        terrain.SetActive(false);
        tiles.Add(tileIndex, tileComponent);
    }

    private Matrix4x4[] OffsetMatrixArray(Matrix4x4[] mats, Vector2 offset)
    {
        for (int i = 0; i < mats.Length; i++)
        {
            Vector3 pos = mats[i].GetPosition();
            pos += new Vector3(offset.x, 0, -offset.y);
            mats[i].SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));
        }
        return mats;
    }

    private void SetupTiles()
    {
        List<Vector2Int> tileIndexes = new List<Vector2Int>(tiles.Keys);
        if (tileIndexes.Count <= 0)
        {
            return;
        }
        Vector2Int[] ordered = Neighbours(tileIndexes);
        Vector2Int origin = ordered[0];
        tiles[origin].gameObject.SetActive(true);
        Dictionary<int, List<Matrix4x4>> instanceLists = new Dictionary<int, List<Matrix4x4>>();
        for (int i = 0; i < instancers.Length; i++)
        {
            instanceLists.Add(i, new List<Matrix4x4>());
        }
        if (instances.TryGetValue(origin, out List<InstanceData> toInstance))
        {
            foreach (InstanceData data in toInstance)
            {
                instanceLists[data.instancerIndex].AddRange(data.instances);
            }
        }

        for (int i = 1; i < ordered.Length; i++)
        {
            Vector2 difference = ordered[i] - origin;
            Vector2 offset = difference * terrainSize;
            if (instances.TryGetValue(ordered[i], out toInstance))
            {
                foreach (InstanceData data in toInstance)
                {
                    instanceLists[data.instancerIndex].AddRange(OffsetMatrixArray(data.instances, offset));
                }
            }
            tiles[ordered[i]].SetTerrainOffset(offset);
            tiles[ordered[i]].gameObject.SetActive(true);
        }
        // Look for the only active camera from all cameras
        Camera cam = null;
        foreach (var c in Camera.allCameras)
        {
            if (c.isActiveAndEnabled)
            {
                cam = c;
                break;
            }
        }

        for (int i = 0; i < instancers.Length; i++)
        {
            instancers[i].Setup(instanceLists[i].ToArray());
        }

        for (int i = 0; i < ordered.Length; i++)
        {
            Vector2Int pos = ordered[i];
            tiles.TryGetValue(pos + Vector2Int.right, out TileComponent right);
            tiles.TryGetValue(pos - Vector2Int.right, out TileComponent left);
            tiles.TryGetValue(pos - Vector2Int.up, out TileComponent up);
            tiles.TryGetValue(pos + Vector2Int.up, out TileComponent down);
            tiles.TryGetValue(pos + Vector2Int.up + Vector2Int.left, out TileComponent corner);

            tiles[pos].SetNeighbours(down, up, left, right, corner);
        }

        Vector2Int last = ordered[ordered.Length - 1];
        int tileWidth = last.x - origin.x + 1;
        int tileHeight = origin.y - last.y + 1;

        Texture2D[,] heightmaps = new Texture2D[tileWidth, tileHeight];
        Texture2D[,] masks = new Texture2D[tileWidth, tileHeight];
        float[,] minHeights = new float[tileWidth, tileHeight];
        float[,] heightScales = new float[tileWidth, tileHeight];
        for (int i = 0; i < tileWidth; i++)
        {
            for (int j = 0; j < tileHeight; j++)
            {
                if (tiles.TryGetValue(origin + new Vector2Int(i, -j), out TileComponent tile))
                {
                    heightmaps[i, j] = tile.GenerateHeightmap(out double minHeight, out double scale);
                    masks[i, j] = tile.GrassMask;
                    minHeights[i, j] = (float)minHeight;
                    heightScales[i, j] = (float)scale;
                }
            }
        }

        grassInstanced.InitialiseMultiTile(terrainSize, heightmaps, masks, minHeights, heightScales);
    }

    public void ApplyInstancedGrass(float mapSize, Texture2D clumping, Texture2D mask, Texture2D heightmap, float minHeight, float maxHeight)
    {
        grassInstanced.InitialiseSingleTile(mapSize, clumping, heightmap, mask, minHeight, maxHeight);
    }

    public static int compareVec2(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x && a.y == b.y)
            return 0;

        else if (a.x < b.x || a.y > b.y)
            return -1;
        else
            return 1;
    }

    /*
     
        To my deareast Stephen,
            I think this is the function you want. However, I have no clue how this codebase works and therefore I have no clue how to test this.

            I hope this is of use to you. I must depart to take a shit and I hope you see you, my beloved, shortly.

        Much love
        Imran xoxoxo
     
     */
    public Vector2Int[] Neighbours(List<Vector2Int> orderedTiles)
    {
        orderedTiles.Sort(compareVec2);

        HashSet<Vector2Int> connectedTiles = new HashSet<Vector2Int>();
        connectedTiles.Add(orderedTiles[0]);
        for (int i = 1; i < orderedTiles.Count; i++)
        {
            Vector2Int tile = orderedTiles[i];
            if (connectedTiles.Contains(tile))
                continue;

            Vector2Int[] adjacentTiles = new Vector2Int[] { new Vector2Int(tile.x - 1, tile.y), new Vector2Int(tile.x, tile.y + 1) };
            if (connectedTiles.Contains(adjacentTiles[0]) || connectedTiles.Contains(adjacentTiles[1]))
            {
                connectedTiles.Add(tile);
            }
        }
        Vector2Int[] arr = new Vector2Int[connectedTiles.Count];
        connectedTiles.CopyTo(arr, 0);
        return arr;
    }

    public void SetInstances(InstanceData instanceData, Vector2Int tileIndex)
    {
        if (!instances.ContainsKey(tileIndex))
        {
            instances.Add(tileIndex, new List<InstanceData>());
        }
        instances[tileIndex].Add(instanceData);
    }
}
