using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using XNode;
using Debug = UnityEngine.Debug;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class AsyncPostPipelineManager : MonoBehaviour, PipelineRunner
{
    [Header("Pipeline")] [SerializeField] private ProceduralPipeline pipeline;
    [SerializeField] private UnityEvent onFinishPipeline;
    public AsyncPipelineManager pipelineManager;
    private Dictionary<Vector2Int, ElevationData> elevationData;
    private RoadNetworkGraph roadNetwork;

    [Header("Output References")] [Header("Debug")] [SerializeField]
    private bool clearPipeline;

    [SerializeField] private string debugInfo = "";

    private Stack<List<SyncExtendedNode>> layerStack;
    private HashSet<SyncExtendedNode> hasRun;
    private Stack<SyncExtendedNode> syncLayerNodes;
    private int totalAsyncJobs = 0;

#if UNITY_EDITOR
    [Header("Total time spend executing these nodes")] [SerializeField]
    private SerializableDictionary<string, float> syncTimes;

    private Stopwatch totalTimeTimer;
    [Header("Nodes which dropped the frame rate < ~20fps")] [SerializeField]
    private StringHashSet slowNodes;
#endif

    public void StartPipeline()
    {
        elevationData = pipelineManager.elevations;
        roadNetwork = pipelineManager.roadNetwork;
        totalTimeTimer = Stopwatch.StartNew();

#if UNITY_EDITOR
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

    private void Run()
    {
        ClearPipeline();
        if (!BuildPipeline()) return;
        RunNextLayer();
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
        if (layerStack.Count == 0)
        {
            Debug.Log("Finished pipeline running on all tiles");
            ClearPipeline(); // frees all nodes for garbage collection
            totalTimeTimer.Stop();
            debugInfo = "Total time taken: " + totalTimeTimer.ElapsedMilliseconds / 1000f;
            onFinishPipeline?.Invoke();
#if UNITY_EDITOR
            // sort syncNodeTimes
            syncTimes = new SerializableDictionary<string, float>(syncTimes.OrderBy(x => -x.Value)
                .ToDictionary(x => x.Key, x => x.Value));
#endif
            return;
        } // finished running the pipeline
        
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
                        Debug.LogError("Error building pipeline: " + node.name +
                                       " is missing connection on input port " + port.fieldName);
                        return false;
                    }

                    if (!nextLayer.Contains((SyncExtendedNode)port.Connection.node))
                        nextLayer.Add((SyncExtendedNode)port.Connection.node);
                }
            }

            currentLayer = nextLayer;
            nextLayer = new List<SyncExtendedNode>();
        }

        return true;
    }

    public void AddRoadNetworkSection(RoadNetworkGraph roadNetwork)
    {
        throw new System.NotImplementedException();
    }

    public void CreateTile(ElevationData elevation, GameObject[] children, Vector2Int tileIndex, Texture2D waterMask,
        Texture2D grassMask)
    {
        throw new System.NotImplementedException();
    }

    public void SetInstances(InstanceData instanceData, Vector2Int tileIndex)
    {
        throw new System.NotImplementedException();
    }

    public Vector2Int PopTile()
    {
        throw new System.NotImplementedException();
    }

    public Dictionary<Vector2Int, ElevationData> FetchElevationData()
    {
        return elevationData;
    }

    public RoadNetworkGraph FetchRoadNetworkGraph()
    {
        return roadNetwork;
    }
}