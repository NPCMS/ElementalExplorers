using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

public class AsyncPipelineManager : MonoBehaviour
{
[Header("Pipeline")]
    [SerializeField] private ProceduralPipeline pipeline;
    [SerializeField] private int iterations = 1;
    [SerializeField] private List<Vector2Int> tileQueue = new();

    [Header("Debug, click Run Pipeline to run in editor")]
    [SerializeField] private bool runPipelineOnStart;
    [SerializeField] private UnityEvent onFinishPipeline;
    [SerializeField] private bool runPipeline;
    [SerializeField] private bool clearPipeline;
    [SerializeField] private string debugInfo = "";

    private Stack<Stack<SyncExtendedNode>> runOrder;
    private Stack<SyncExtendedNode> running;
    private HashSet<SyncExtendedNode> hasRun;
    private SyncExtendedNode runningNode;

    private void Start()
    {
        if (runPipelineOnStart)
        {
            StartCoroutine(DelayRun());
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
    private IEnumerator DelayRun()
    {
        yield return new WaitForSeconds(5);
        
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
            if (iterations > 0)
            {
                StartCoroutine(DelayRun());
            }
            else
            {
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
    public void BuildPipeline()
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
}
