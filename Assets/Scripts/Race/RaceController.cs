using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class RaceController : NetworkBehaviour
{
    public List<GameObject> checkpoints = new();

    private AsyncPostPipelineManager manager;
    
    // these are for drawing chevrons from the player to the next checkpoint
    // [SerializeReference] private LineRenderer chevronRenderer;
    // [SerializeReference] private Transform player;
    
    // index of the next free checkpoint
    private NetworkVariable<int> nextCheckpoint = new ();
    // record of player id to reach each checkpoint first
    private NetworkList<ulong> checkpointCaptures;
    private NetworkList<float> checkpointTimes;
    
    // I don't need to comment what this is for
    public bool raceStarted;
    // Time spend so far in the race
    private float time;
    
    // reference to the race controller on the player
    [SerializeReference] private PlayerRaceController playerRaceController;

    public void Awake()
    {
        checkpointCaptures = new NetworkList<ulong>();
        checkpointTimes = new NetworkList<float>();

        nextCheckpoint.OnValueChanged += (oldValue, newValue) =>
        {
            // disable oldValue
            
            // enable newValue
        };
    }

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
        // UpdateRoadChevrons(player.position);
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
            checkpointTimes.Add(time);
        }
    }

    // returns a number based on how well the player is doing, higher number == better player, 1 for even
    // private float GetDifficultyMultiplier()
    // {
    //     // todo get id of this player
    //     const ulong id = 0;
    //
    //     int win = 0;
    //     int loss = 0;
    //
    //     if (checkpointCaptures.Count == 0)
    //     {
    //         return 1;
    //     }
    //     
    //     // todo do something with checkpoint timings / distance between the players to detect when one is super far ahead
    //     foreach (var capturedBy in checkpointCaptures)
    //     {
    //         if (id == capturedBy)
    //         {
    //             win++;
    //         }
    //         else
    //         {
    //             loss++;
    //         }
    //     }
    //     
    //     return 1 + (win - loss) / ((win + loss) * 2f);
    // }

    // private void UpdateRoadChevrons(Vector3 playerPos)
    // {
    //     // get shortest path as a set of road nodes
    //     var path = RaceRouteNode.AStar(manager.roadNetwork, 
    //         manager.elevationData.box.MetersToGeoCoord(new Vector2(playerPos.x, playerPos.z)),
    //         manager.elevationData.box.MetersToGeoCoord(new Vector2(checkpoints[nextCheckpoint.Value].transform.position.x, checkpoints[nextCheckpoint.Value].transform.position.z)));
    //     // go from list of nodes to list of positions to draw with the line renderer
    //     List<Vector3> footprint = new List<Vector3>();
    //     for (int i = 0; i < path.Count - 1; i++)
    //     {
    //         // add footprint from node i to node i + 1
    //         var n1 = path[i];
    //         var n2 = path[i + 1];
    //         
    //         var success = manager.roadNetwork.TryGetEdge(n1, n2, out var edge);
    //         if (!success)
    //         {
    //             Debug.LogError("Couldn't find edge in path when there should be");
    //             return;
    //         }
    //         
    //         var worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(n1.location);
    //         var newPoint = new Vector3(worldPos.x, 0, worldPos.y);
    //         newPoint.y = (float)manager.elevationData.SampleHeightFromPositionAccurate(newPoint) + 0.3f;
    //         footprint.Add(newPoint);
    //         
    //         // if n1 -> n2. Add normally
    //         if (edge.Source.Equals(n1))
    //         {
    //             foreach (Vector2 tagEdgePoint in edge.Tag.edgePoints)
    //             {
    //                 worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(tagEdgePoint);
    //                 newPoint = new Vector3(worldPos.x, 0, worldPos.y);
    //                 newPoint.y = (float)manager.elevationData.SampleHeightFromPositionAccurate(newPoint) + 0.3f;
    //                 footprint.Add(newPoint);
    //             }
    //         }
    //         else // else n2 -> n1. Add in reverse direction
    //         {
    //             foreach (Vector2 tagEdgePoint in edge.Tag.edgePoints.Reverse())
    //             {
    //                 worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(tagEdgePoint);
    //                 newPoint = new Vector3(worldPos.x, 0, worldPos.y);
    //                 newPoint.y = (float)manager.elevationData.SampleHeightFromPositionAccurate(newPoint) + 0.3f;
    //                 footprint.Add(newPoint);
    //             }
    //         }
    //
    //         if (i + 1 == path.Count - 1) { // if next node is the final node
    //             worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(n2.location);
    //             newPoint = new Vector3(worldPos.x, 0, worldPos.y);
    //             newPoint.y = (float)manager.elevationData.SampleHeightFromPosition(newPoint) + 0.3f;
    //             footprint.Add(newPoint);
    //         }
    //     }
    //     
    //     // update line renderer
    //     chevronRenderer.positionCount = footprint.Count;
    //     chevronRenderer.SetPositions(footprint.ToArray());
    // }
    
}
