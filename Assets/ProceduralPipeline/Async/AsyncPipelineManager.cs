using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using XNode;
using Debug = UnityEngine.Debug;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class AsyncPipelineManager : MonoBehaviour, PipelineRunner
{
    [Header("Pipeline")]
    [SerializeField] private ProceduralPipeline pipeline;
    [SerializeField] private List<Vector2Int> queue = new();
    [SerializeField] private UnityEvent onFinishPipeline;
    [Header("Output References")]
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private FogShaderVariables fog;
    [SerializeField] private GrassRendererInstanced grassInstanced;
    [SerializeField] private GeneralIndirectInstancer[] instancers;
    [SerializeField] private GameObject colliderPrefab;
    [SerializeField] private string shaderTerrainSizeIdentifier = "_TerrainWidth";
    [Header("Load Data")] [SerializeField] private TileInfo selectedTiles;
    [Header("Debug")]
    [SerializeField] private bool clearPipeline;
    [SerializeField] private bool clearPipelineAfterRun = true;
    [SerializeField] private string tilesLeft = "";
    [SerializeField] private string debugInfo = "";

    private Stack<List<SyncExtendedNode>> layerStack;
    private HashSet<SyncExtendedNode> hasRun;
    private Stack<SyncExtendedNode> syncLayerNodes;
    private int totalAsyncJobs = 0;

#if UNITY_EDITOR
    [Header("Total time spend executing these nodes")]
    [SerializeField] private SerializableDictionary<string, float> syncTimes;
    private Stopwatch totalTimeTimer;
    [Header("Nodes which dropped the frame rate < ~20fps")]
    [SerializeField] private StringHashSet slowNodes;
#endif

    private List<Vector2Int> tileQueue = new();

    private Dictionary<Vector2Int, TileComponent> tiles;
    private Dictionary<Vector2Int, List<InstanceData>> instances;
    public Dictionary<Vector2Int,ElevationData> elevations;
    public List<GeoCoordinate> pois = new();
    public RoadNetworkGraph roadNetwork = new();

    private bool tileSet;
    private float terrainSize;
    
    private void Start()
    {
        tiles = new Dictionary<Vector2Int, TileComponent>();
        instances = new Dictionary<Vector2Int, List<InstanceData>>();
        tileSet = false;
        //tileQueue = selectedTiles.tiles.Count == 0 ? new List<Vector2Int>(queue) : selectedTiles.tiles;
        tileQueue = new List<Vector2Int>(selectedTiles.GetTiles());
        tilesLeft = tileQueue.Count.ToString();
        elevations = new Dictionary<Vector2Int, ElevationData>();
#if UNITY_EDITOR
        totalTimeTimer = Stopwatch.StartNew();
        // reset all node timings
        syncTimes = new SerializableDictionary<string, float>();
        slowNodes = new StringHashSet();
#endif
        
        Run();
    }
    
    // LOGIC FOR CLEARING THE PIPELINE
    private void OnValidate()
    {
        if (!clearPipeline) return;
        clearPipeline = false;
        ClearPipeline();
    }

    public Vector2Int PopTile()
    {
        Vector2Int tile = tileQueue[0];
        tileQueue.RemoveAt(0);
        return tile;
    }

    public Dictionary<Vector2Int, ElevationData> FetchElevationData()
    {
        throw new System.NotImplementedException();
    }

    public RoadNetworkGraph FetchRoadNetworkGraph()
    {
        throw new System.NotImplementedException();
    }

    public void SetElevation(ElevationData newElevationData)
    {
        throw new System.NotImplementedException();
    }

    private void Run()
    {
        if (clearPipelineAfterRun)
        {
            ClearPipeline();
        }

        tilesLeft = tileQueue.Count.ToString();
        if (tileQueue.Count > 0) // more tiles need processing re run the pipeline to process the next tile
        {
            Debug.Log("Starting tile: " + tileQueue[0]);
            if (!BuildPipeline()) return;
            RunNextLayer();
        }
        else // end of all tiles and end of pipeline
        {
            StartCoroutine(FinishPipelineRoutine());
#if UNITY_EDITOR
            totalTimeTimer.Stop();
            debugInfo = "Total time taken: " + totalTimeTimer.ElapsedMilliseconds / 1000f;
            // sort syncNodeTimes
            syncTimes = new SerializableDictionary<string, float>(syncTimes.OrderBy(x => -x.Value).ToDictionary(x => x.Key, x => x.Value));
#endif
        }
    }

    private IEnumerator FinishPipelineRoutine()
    {
        yield return SetupTiles();
        Debug.Log("Finished pipeline running on all tiles");
        if (clearPipelineAfterRun)
        {
            ClearPipeline(); // frees all nodes for garbage collection
        }
        onFinishPipeline?.Invoke();
    }

    private void NodeFinished(bool success, SyncExtendedNode node)
    {
        if (!success)
        {
            Debug.LogError(node.name + " in layer " + layerStack.Count + " has failed.");
            return;
        }

        hasRun.Add(node);
        // is output node
        if (layerStack.Count == 0)
        {
            ((SyncOutputNode)node).ApplyOutput(this);
        }
    }
    
    private void RunNextLayer()
    {
        if (layerStack.Count == 0) // finished running the current pipeline, run on next tile
        {
            Run();
        }
        else
        {
            List<SyncExtendedNode> runningLayer = layerStack.Pop();
            syncLayerNodes = new Stack<SyncExtendedNode>();
            foreach (var node in runningLayer.Where(node => !hasRun.Contains(node)))
            {
                if (node is AsyncExtendedNode) // start it now and keep count of how many async nodes have been started
                {
                    totalAsyncJobs += 1;
                    StartCoroutine(node.CalculateOutputs(success =>
                    {
                        NodeFinished(success, node);
                        totalAsyncJobs -= 1;
                    }));
                }
                else
                {
                    syncLayerNodes.Push(node);
                }
            }

            RunSyncNodes();
        }
    }

    private void RunSyncNodes()
    {
        if (syncLayerNodes.Count > 0)
        {
            var nextNode = syncLayerNodes.Pop();
#if UNITY_EDITOR
            var timer = Stopwatch.StartNew();
#endif 
            debugInfo = nextNode.name;
            if (nextNode is SyncInputNode inputNode)
            {
                inputNode.ApplyInputs(this);
            }
            StartCoroutine(nextNode.CalculateOutputs(success =>
            {
#if UNITY_EDITOR
                timer.Stop();
                if (20 > 1 / Time.smoothDeltaTime)
                {
                    if (!slowNodes.Contains(nextNode.name)) slowNodes.Add(nextNode.name);
                }
                if (syncTimes.ContainsKey(nextNode.name))
                {
                    syncTimes[nextNode.name] += timer.ElapsedMilliseconds / 1000f;
                }
                else
                {
                    syncTimes[nextNode.name] = timer.ElapsedMilliseconds / 1000f;
                }
#endif 
                
                NodeFinished(success, nextNode);
                RunSyncNodes();
            }));
        }
        else
        {
            StartCoroutine(WaitForAsyncToFinish());
        }
    }

    private IEnumerator WaitForAsyncToFinish()
    {
        while (totalAsyncJobs != 0) yield return null;
        // Layer finished
        RunNextLayer();
    }

    private void ClearPipeline()
    {
        // releases all nodes in graph
        foreach (Node node in pipeline.nodes)
        {
            if (node is not SyncExtendedNode)
            {
                Debug.LogError("Non sync node is async graph");
            }
            ((SyncExtendedNode)node).Release();
        }
        
        Debug.Log("Pipeline Cleared");
    }

    //BFS from output nodes and run the nodes from highest depth to lowest
    //highest depth must be inputs, lowest depth must be output
    //each layer in BFS must only depend on the layer above, therefore once one layer is complete the next one can be computed
    //assumes graph is a DAG, otherwise this will result in infinite loop
    private bool BuildPipeline()
    {
        // empty set of nodes already run. A node can be in multiple layers and prevents needless recalculations
        hasRun = new HashSet<SyncExtendedNode>();
        // stack of layers. the top of the stack in the next layer required to be run
        layerStack = new Stack<List<SyncExtendedNode>>();
        // output layer to start with
        List<SyncExtendedNode> currentLayer = new List<SyncExtendedNode>();
        List<Node> nodes = pipeline.nodes;
        // get all outputs
        foreach (var t in nodes)
        {
            if (t is SyncOutputNode node)
            {
                currentLayer.Add(node);
            }
        }

        List<SyncExtendedNode> nextLayer = new List<SyncExtendedNode>();
        // while BFS can iterate further, will be infinite if cycle
        while (currentLayer.Count > 0)
        {
            // current layer has nodes, add it to the runOrder
            layerStack.Push(currentLayer);
            // get all nodes in the layer before the current layer 
            foreach (var node in currentLayer)
            {
                foreach (NodePort port in node.Inputs)
                {
                    if (!port.IsConnected) continue;
                    if (port.Connection == null)
                    {
                        Debug.LogError("Error building pipeline: " + node.name + " is missing connection on input port " + port.fieldName);
                        return false;
                    }
                    if (!nextLayer.Contains((SyncExtendedNode)port.Connection.node)) nextLayer.Add((SyncExtendedNode)port.Connection.node);
                }
            }

            currentLayer = nextLayer;
            nextLayer = new List<SyncExtendedNode>();
        }
        return true;
    }

    private void OnDestroy()
    {
        ClearPipeline();
    }













    // BELOW IS CODE FOR OUTPUT TILES TO CALL

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
        if (children != null)
        {
            foreach (GameObject go in children)
            {
                go.transform.SetParent(terrain.transform, true);
            }
        }
        terrain.SetActive(false);
        tiles.Add(tileIndex, tileComponent);
        elevations[tileIndex] = elevation;
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

    private IEnumerator SetupTiles()
    {
        List<Vector2Int> tileIndexes = new List<Vector2Int>(tiles.Keys);
        if (tileIndexes.Count <= 0)
        {
            yield break;
        }
        Vector2Int[] ordered = Neighbours(tileIndexes);
        Vector2Int origin = ordered[0];
        tiles[origin].gameObject.SetActive(true);
        yield return null;
        foreach (Transform child in tiles[ordered[0]].transform)
        {
            child.gameObject.SetActive(true);
            yield return null;
        }
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
                yield return null;
            }
        }

        for (int i = 1; i < ordered.Length; i++)
        {
            yield return null;
            Vector2 difference = ordered[i] - origin;
            Vector2 offset = difference * terrainSize;
            if (instances.TryGetValue(ordered[i], out toInstance))
            {
                foreach (InstanceData data in toInstance)
                {
                    instanceLists[data.instancerIndex].AddRange(OffsetMatrixArray(data.instances, offset));
                }
            }

            TileComponent tileComponent = tiles[ordered[i]];
            tileComponent.SetTerrainOffset(offset);
            tileComponent.gameObject.SetActive(true);
            //might need to spread across multiple frames
            foreach (Transform child in tileComponent.transform)
            {
                yield return null;
                child.gameObject.SetActive(true);
            }
        }

        yield return null;
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

            tiles[pos].SetNeighbours(down, up, left, right, colliderPrefab);
            yield return null;
        }

        for (int i = 0; i < ordered.Length; i++)
        {
            Vector2Int pos = ordered[i];
            tiles.TryGetValue(pos + Vector2Int.right, out TileComponent right);
            tiles.TryGetValue(pos - Vector2Int.right, out TileComponent left);
            tiles.TryGetValue(pos - Vector2Int.up, out TileComponent up);
            tiles.TryGetValue(pos + Vector2Int.up, out TileComponent down);
            tiles.TryGetValue(pos + Vector2Int.up + Vector2Int.left, out TileComponent corner);

            tiles[pos].SetCornerNeighbours(down, up, left, right, corner);
            yield return null;
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
                    yield return null;
                }
            }
        }

        fog.InitialiseMultiTile(tiles, origin, terrainSize);
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
        if (a.x < b.x || a.y > b.y)
            return -1;
        return 1;
    }

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

    public void AddRoadNetworkSection(RoadNetworkGraph roads)
    {
        roadNetwork.AddVerticesAndEdgeRange(roads.Edges);
        // foreach (var roadsEdge in roads.Edges)
        // {
        //     roadNetwork.AddVerticesAndEdge(roadsEdge);
        // }
    }
    
    public void AddPois(List<GeoCoordinate> pointsOfInterest)
    {
        if (pointsOfInterest.Count == 0)
        {
            Debug.LogWarning("No pois found in tile :(");
        }
        pois.AddRange(pointsOfInterest);
    }

    public List<GeoCoordinate> FetchPois()
    {
        throw new System.NotImplementedException();
    }
}

public interface PipelineRunner
{
    public void AddRoadNetworkSection(RoadNetworkGraph roadNetwork);
    
    public void CreateTile(ElevationData elevation, GameObject[] children, Vector2Int tileIndex, Texture2D waterMask,
        Texture2D grassMask);

    public void SetInstances(InstanceData instanceData, Vector2Int tileIndex);

    public Vector2Int PopTile();

    public Dictionary<Vector2Int, ElevationData> FetchElevationData();

    public RoadNetworkGraph FetchRoadNetworkGraph();
    
    public void SetElevation(ElevationData newElevationData);

    public void AddPois(List<GeoCoordinate> pointsOfInterest);

    public List<GeoCoordinate> FetchPois();
}