using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProceduralPipelineNodes.Nodes;
using Unity.Netcode;
using UnityEngine;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class RaceController : NetworkBehaviour
{
    public List<GameObject> checkpoints = new();

    private AsyncPostPipelineManager manager;
    
    // these are for drawing chevrons from the player to the next checkpoint
    [SerializeReference] private LineRenderer chevronRenderer;
    [SerializeReference] private Transform player;
    
    // index of the next free checkpoint
    private NetworkVariable<int> nextCheckpoint;
    // record of player id to reach each checkpoint first
    private NetworkList<ulong> checkpointCaptures;
    // I don't need to comment what this is for
    public bool raceStarted;
    // Time spend so far in the race
    private float time = 0;
    
    // reference to the race controller on the player
    [SerializeReference] private PlayerRaceController playerRaceController;

    public void Awake()
    {
        checkpointCaptures = new NetworkList<ulong>();
    }

    // this should be called in a client rpc I think once both players have loaded into the game
    public void StartRace()
    {
        // open door
        GameObject.FindWithTag("RaceStartDoor").SetActive(false);
        // start countdown
        StartCoroutine(StartRaceRoutine());
    }
    
    private IEnumerator StartRaceRoutine()
    {
        playerRaceController.hudController.StartCountdown();
        yield return new WaitForSeconds(3);
        raceStarted = true;
    }

    public void Update()
    {
        if (!raceStarted) return;
        time += Time.deltaTime;
        UpdateRoadChevrons(player.position);
    }

    public void PassCheckpoint(int n)
    {
        SetCheckPointServerRPC(n);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCheckPointServerRPC(int checkpoint, ServerRpcParams param = default)
    {
        var playerId = param.Receive.SenderClientId;
        if (checkpoint == nextCheckpoint.Value)
        {
            nextCheckpoint.Value += 1;
            checkpointCaptures.Add(playerId);
        }
    }

    private void UpdateRoadChevrons(Vector3 playerPos)
    {
        // get shortest path as a set of road nodes
        var path = RaceRouteNode.AStar(manager.roadNetwork, 
            manager.elevationData.box.MetersToGeoCoord(new Vector2(playerPos.x, playerPos.z)),
            manager.elevationData.box.MetersToGeoCoord(new Vector2(checkpoints[nextCheckpoint.Value].transform.position.x, checkpoints[nextCheckpoint.Value].transform.position.z)));
        // go from list of nodes to list of positions to draw with the line renderer
        List<Vector3> footprint = new List<Vector3>();
        for (int i = 0; i < path.Count - 1; i++)
        {
            // add footprint from node i to node i + 1
            var n1 = path[i];
            var n2 = path[i + 1];
            
            var success = manager.roadNetwork.TryGetEdge(n1, n2, out var edge);
            if (!success)
            {
                Debug.LogError("Couldn't find edge in path when there should be");
                return;
            }
            
            var worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(n1.location);
            var newPoint = new Vector3(worldPos.x, 0, worldPos.y);
            newPoint.y = (float)manager.elevationData.SampleHeightFromPositionAccurate(newPoint) + 0.3f;
            footprint.Add(newPoint);
            
            // if n1 -> n2. Add normally
            if (edge.Source.Equals(n1))
            {
                foreach (Vector2 tagEdgePoint in edge.Tag.edgePoints)
                {
                    worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(tagEdgePoint);
                    newPoint = new Vector3(worldPos.x, 0, worldPos.y);
                    newPoint.y = (float)manager.elevationData.SampleHeightFromPositionAccurate(newPoint) + 0.3f;
                    footprint.Add(newPoint);
                }
            }
            else // else n2 -> n1. Add in reverse direction
            {
                foreach (Vector2 tagEdgePoint in edge.Tag.edgePoints.Reverse())
                {
                    worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(tagEdgePoint);
                    newPoint = new Vector3(worldPos.x, 0, worldPos.y);
                    newPoint.y = (float)manager.elevationData.SampleHeightFromPositionAccurate(newPoint) + 0.3f;
                    footprint.Add(newPoint);
                }
            }

            if (i + 1 == path.Count - 1) { // if next node is the final node
                worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(n2.location);
                newPoint = new Vector3(worldPos.x, 0, worldPos.y);
                newPoint.y = (float)manager.elevationData.SampleHeightFromPosition(newPoint) + 0.3f;
                footprint.Add(newPoint);
            }
        }
        
        // update line renderer
        chevronRenderer.positionCount = footprint.Count;
        chevronRenderer.SetPositions(footprint.ToArray());
    }
    
}
