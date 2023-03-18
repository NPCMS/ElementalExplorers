using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

public class ProceduralManager : MonoBehaviour
{
    [Header("Pipeline")]
    [SerializeField] private ProceduralPipeline pipeline;
    [SerializeField] private bool runMultiple = false;
    [SerializeField] private int iterations = 1;
    [SerializeField] private List<Vector2Int> tileQueue = new List<Vector2Int>();

    [Header("Output References")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private GrassRendererInstanced grassInstanced;
    [SerializeField] private GeneralIndirectInstancer[] instancers;
    [SerializeField] private string shaderTerrainSizeIdentifier = "_TerrainWidth";
    [Header("Temporary")]
    [SerializeField] private Texture2D grassClumping;

    [Header("Debug, click Run Pipeline to run in editor")]
    [SerializeField] private bool runPipelineOnStart = false;
    [SerializeField] private UnityEvent onFinishPipeline;
    [SerializeField] private bool runPipeline = false;
    [SerializeField] private bool clearPipeline = false;
    [SerializeField] private float terrainScaleFactor = 1;
    [SerializeField] private string debugInfo = "";

    private Stack<Stack<ExtendedNode>> runOrder;
    private Stack<ExtendedNode> running;
    private HashSet<ExtendedNode> hasRun;
    private ExtendedNode runningNode;

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
            StartCoroutine(DelayRun());
        }
    }

    //LOGIC FOR RUNNING THE PIPELINE
    private void OnValidate()
    {
        if (runPipeline)
        {
            tiles = new Dictionary<Vector2Int, TileComponent>();
            instances = new Dictionary<Vector2Int, List<InstanceData>>();
            runPipeline = false;
            tileSet = false;
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

        //is output node
        if (runOrder.Count == 0)
        {
            ((OutputNode)runningNode).ApplyOutput(this);
        }

        RunNextNode();
    }

    public void RunNextNode()
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
            if (runningNode is InputNode)
            {
                ((InputNode)runningNode).ApplyInputs(this);
            }

            StartCoroutine(DelayRunNode());
        }
    }

    private IEnumerator DelayRunNode()
    {
        // yield return new WaitForSeconds(1.5f);
        yield return null;
        runningNode.CalculateOutputs(OnNodeFinish);
    }
    private IEnumerator DelayRun()
    {
        // yield return new WaitForSeconds(5);
        yield return null;
        BuildPipeline();
        ClearPipeline();
        BuildPipeline();
        RunNextLayer();
    }

    public void RunNextLayer()
    {
        if (runOrder.Count == 0)
        {
            iterations -= 1;
            if (iterations > 0 && runMultiple)
            {
                StartCoroutine(DelayRun());
            }
            else
            {
                if (runMultiple)
                {
                    SetupTiles();
                }
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
        Stack<ExtendedNode> currentLayer;
        while (runOrder.Count > 0)
        {
            currentLayer = runOrder.Pop();
            while (currentLayer.Count > 0)
            {
                ExtendedNode node = currentLayer.Pop();
                node.Release();
            }
        }

        print("Pipeline Cleared");
    }

    //BFS from output nodes and run the nodes from highest depth to lowest
    //highest depth must be inputs, lowest depth must be output
    //each layer in BFS must only depend on the layer above, therefore once one layer is complete the next one can be computed
    //assumes graph is a DAG, otherwise this will result in infinite loop
    public void BuildPipeline()
    {
        hasRun = new HashSet<ExtendedNode>();
        runOrder = new Stack<Stack<ExtendedNode>>();
        //output layer
        List<ExtendedNode> currentLayer = new List<ExtendedNode>();
        List<Node> nodes = pipeline.nodes;
        //get all outputs
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] is OutputNode)
            {
                currentLayer.Add((OutputNode)nodes[i]);
            }
        }

        List<ExtendedNode> nextLayer = new List<ExtendedNode>();
        //while BFS can iterate further, will be infinite if cycle
        while (currentLayer.Count > 0)
        {
            //current layer has nodes, add it to the runorder
            runOrder.Push(new Stack<ExtendedNode>(currentLayer));
            for (int i = 0; i < currentLayer.Count; i++)
            {
                foreach (NodePort port in currentLayer[i].Inputs)
                {
                    if (port.IsConnected)
                    {
                        nextLayer.Add((ExtendedNode)port.Connection.node);
                    }
                }
            }

            currentLayer = nextLayer;
            nextLayer = new List<ExtendedNode>();
        }
    }



    //OUTPUT CALLBACK FUNCTIONS FOR MAKING CHANGES TO THE SCENE

    //Applies textures to material
    public void SetTerrainMaterialTexture(string identifier, Texture2D tex)
    {
        terrainMaterial.SetTexture(identifier, tex);
    }

    //Applies elevation to terrain
    public void SetTerrainElevation(ElevationData elevation)
    {
        Debug.Assert(elevation.height.GetLength(0) == elevation.height.GetLength(1), "Heightmap is not square, run through upsample node before output");
        terrain.transform.position = new Vector3(0, (float)elevation.minHeight, 0);
        terrain.terrainData.heightmapResolution = elevation.height.GetLength(0);
        double width = GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
        terrain.terrainData.size = new Vector3((float)width, (float)(elevation.maxHeight - elevation.minHeight), (float)width);
        terrain.terrainData.SetHeights(0, 0, elevation.height);
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

        grassInstanced.InitialiseMultiTile(terrainSize, grassClumping, heightmaps, masks, minHeights, heightScales);
    }

    private void SetMainTerrain(ElevationData elevation)
    {
        Shader.SetGlobalFloat(shaderTerrainSizeIdentifier,
            (float)GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south));
    }


    //Legacy grass
    public void ApplyGrass(GrassRenderer.GrassChunk[] grass, ChunkContainer chunking)
    {
        //this.grass.InitialiseGrass(chunking, grass);
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