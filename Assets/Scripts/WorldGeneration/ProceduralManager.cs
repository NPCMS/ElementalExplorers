using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class ProceduralManager : MonoBehaviour
{
    [Header("Pipeline")]
    [SerializeField] private ProceduralPipeline pipeline;

    [Header("Output References")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private Material terrainMaterial;

    [Header("Debug, click Run Pipeline to run in editor")]
    [SerializeField] private bool runPipeline = false;
    [SerializeField] private float terrainScaleFactor = 1;
    [SerializeField] private string debugInfo = "";

    private Stack<Stack<ExtendedNode>> runOrder;
    private Stack<ExtendedNode> running;
    private ExtendedNode runningNode;


    //LOGIC FOR RUNNING THE PIPELINE
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
            debugInfo = runningNode.name;
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
        RunNextNode();
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
                    if (port.IsConnected)
                    {
                        print(currentLayer[i].name);
                        nextLayer.Add((ExtendedNode)port.Connection.node);
                    }
                }
            }

            currentLayer = nextLayer;
            nextLayer = new List<ExtendedNode>();
        }

        RunNextLayer();
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
        terrain.transform.position = new Vector3(0, (float)elevation.minHeight, 0) * terrainScaleFactor;
        terrain.terrainData.heightmapResolution = elevation.height.GetLength(0);
        double width = GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
        terrain.terrainData.size = new Vector3((float)width, (float)(elevation.maxHeight - elevation.minHeight), (float)width) * terrainScaleFactor;
        terrain.terrainData.SetHeights(0, 0, elevation.height);
    }
}
