using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class RaceController : NetworkBehaviour
{
    public static RaceController Instance;
    
    public List<GameObject> minigameLocations = new();

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

    private bool playerReachedMinigame;
    private HashSet<ulong> playersReadyForMinigame = new();

    public void Awake()
    {
        Instance = this;
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
        StartRaceClientRpc();
    }

    [ClientRpc]
    public void StartRaceClientRpc()
    {
        minigameLocations = new List<GameObject>(GameObject.FindGameObjectsWithTag("Minigame"));
        // start countdown
        GameObject raceDoor = GameObject.FindWithTag("RaceStartDoor");
        raceDoor.SetActive(false);
        // open door
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerReachedTeleporterServerRpc()
    {
        Debug.Log("Teleport server rpc");
        if (!playerReachedMinigame)
        {
            playerReachedMinigame = true;
            PlayerReachedTeleporterClientRpc();
        }
    }

    [ClientRpc]
    private void PlayerReachedTeleporterClientRpc()
    {
        Debug.Log("Teleport client rpc");
        if (MultiPlayerWrapper.localPlayer.GetComponentInChildren<PlayerMinigameManager>().reachedMinigame) return;
        StartCoroutine(TeleportPlayerIfTooSlow());
    }
    
    private IEnumerator TeleportPlayerIfTooSlow()
    {
        // todo warn player of being slow

        yield return new WaitForSeconds(5f);
        Debug.Log("Teleport started");
        var playerMinigameManager = MultiPlayerWrapper.localPlayer.GetComponentInChildren<PlayerMinigameManager>();
        if (playerMinigameManager.reachedMinigame) yield break;
        // if player is not yet at the minigame
        playerMinigameManager.EnterMinigame();
        TeleportLocalPlayerToMinigame();
    }

    public void TeleportLocalPlayerToMinigame()
    {
        var player = MultiPlayerWrapper.localPlayer;
        player.ResetPlayerPos();
        player.GetComponentInChildren<Rigidbody>().velocity = Vector3.zero;
        var minigame = minigameLocations[nextCheckpoint.Value];
        if (IsHost)
        {
            player.transform.position = minigame.transform.Find("Player1Pos").position;
        }
        else
        {
            player.transform.position = minigame.transform.Find("Player2Pos").position;
        }

        PlayerReadyToStartMinigameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerReadyToStartMinigameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playersReadyForMinigame.Add(serverRpcParams.Receive.SenderClientId);
        if (playersReadyForMinigame.Count == 2)
        {
            minigameLocations[nextCheckpoint.Value].GetComponentInChildren<TargetSpawner>().StartMinigame();
        }
    }

    public void Update()
    {
        if (!raceStarted) return;
        time += Time.deltaTime;
        // UpdateRoadChevrons(player.position);
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
