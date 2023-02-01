using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class ProceduralManager : MonoBehaviour
{
    [SerializeField] private ProceduralPipeline pipeline;

    [SerializeField] private bool runPipeline = false;
    [SerializeField] private string debugInfo = "";

    private Stack<Stack<ExtendedNode>> runOrder;
    private Stack<ExtendedNode> running;
    private ExtendedNode runningNode;

    private void OnValidate()
    {
        if (runPipeline)
        {
            runPipeline = false;
            RunPipeline();
        }
    }

    public void OnNodeFinish(bool success)
    {
        if (!success)
        {
            Debug.LogError(runningNode.name + " in layer " + runOrder.Count + " has failed.");
            return;
        }

        //is output node
        if (runOrder.Count == 1)
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
            runningNode.CalculateOutputs(OnNodeFinish);
        }
    }

    public void RunNextLayer()
    {
        if (runOrder.Count == 0)
        {
            return;
        }

        running = runOrder.Pop();
    }


    //BFS from output nodes and run the nodes from highest depth to lowest
    //highest depth must be inputs, lowest depth must be output
    //each layer in BFS must only depend on the layer above, therefore once one layer is complete the next one can be computed
    //assumes graph is a DAG, otherwise this will result in infinite loop
    public void RunPipeline()
    {
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
                    nextLayer.Add((ExtendedNode)port.Connection.node);
                }
            }

            currentLayer = nextLayer;
            nextLayer = new List<ExtendedNode>();
        }

        RunNextLayer();
    }
}
