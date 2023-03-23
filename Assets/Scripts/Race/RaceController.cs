using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProceduralPipelineNodes.Nodes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class RaceController : NetworkBehaviour
{
    private NetworkList<ulong> clientIds;
    private NetworkList<float> playerSplits;
    private NetworkVariable<int> checkpointNumber = new();

    public List<GameObject> checkpoints = new();

    private RoadNetworkGraph roadGraph;
    private ElevationData elevationData;
    private GlobeBoundingBox bb;
    [SerializeReference] private LineRenderer chevronRenderer;
    [SerializeReference] private Transform player;
    [SerializeReference] private AudioSource raceMusic;
    
    private int nextCheckpoint;
    public PlayerRaceController playerRaceController;
    
    public NetworkList<GrappleData> grappleDataList; 
    public struct GrappleData : INetworkSerializable, IEquatable<GrappleData>
    {
        // This is not a nice way of storing data but necessary to get it serializable
        public float x;
        public float y;
        public float z;
        public bool connected;

        public GrappleData(float x, float y, float z, bool connected)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.connected = connected;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter 
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref z);

            serializer.SerializeValue(ref connected);
        }

        public bool Equals(GrappleData other)
        {
            return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && connected == other.connected;
        }
    }

    public void Awake()
    {
        clientIds = new NetworkList<ulong>();
        playerSplits = new NetworkList<float>();
        grappleDataList = new NetworkList<GrappleData>();
    }

    public void Update()
    {
        if (player == null)
        {
            if (Camera.main == null)
            {
                return;
            }

            player = Camera.main.transform.parent;
            playerRaceController = player.GetComponentInChildren<PlayerRaceController>();
            foreach (SuitUpPlayerOnPlayer suitUp in player.gameObject.GetComponentsInChildren<SuitUpPlayerOnPlayer>())
            {
                suitUp.SwitchToGauntlet();
            }
            // TODO add voice over and stuff to allow loading time
            Invoke(nameof(StartMusic), 6);
            Invoke(nameof(StartRace), 10);
        }

        if (roadGraph == null)
        {
            MapInfoContainer mapInfoContainer = FindObjectOfType<MapInfoContainer>();
            if (mapInfoContainer == null) return;
            roadGraph = mapInfoContainer.roadNetwork;
            elevationData = mapInfoContainer.elevation;
            bb = mapInfoContainer.bb;
        }
        UpdateRoadChevrons(player.position);
    }

    // Must happen 11s before the start of the race
    private void StartMusic()
    {
        raceMusic.Play();
    }

    public override void OnNetworkSpawn() // this needs changing in the future. See docs
    {
        if (GameObject.FindGameObjectsWithTag("Checkpoint").Length == 0)
        {
            SceneManager.activeSceneChanged += (_, _) =>
            {
                ConnectCheckpoints();
            };
        }
        else
        {
            ConnectCheckpoints();
        }
        playerSplits.OnListChanged += _ => PrintSplits();
    }

    private void ConnectCheckpoints() // checkpoints should be added as they are created. This is only required for pre-placed checkpoints
    {
        var checkpointsToAdd = GameObject.FindGameObjectsWithTag("Checkpoint");
        foreach (var checkpoint in checkpointsToAdd)
        {
            checkpoint.GetComponent<CheckpointController>().raceController = this;
            checkpoint.GetComponent<MeshRenderer>().enabled = false;
            checkpoints.Add(null);
        }
        
        foreach (var checkpoint in checkpointsToAdd)
        {
            int checkpointNum = checkpoint.GetComponent<CheckpointController>().checkpoint;
            if (checkpoints[checkpointNum] != null) Debug.LogWarning("Multiple checkpoints with the same number");
            checkpoints[checkpointNum] = checkpoint;
        }
        checkpoints[0].GetComponent<MeshRenderer>().enabled = true;
        if (!IsHost) return;
        checkpointNumber.Value = checkpointsToAdd.Length;
    }

    private void AddPlayer(ulong id)
    {
        if (!IsHost) return;
        clientIds.Add(id);
        for (int c = 0; c < checkpointNumber.Value; c++)
        {
            playerSplits.Add(-1f);
        }
        grappleDataList.Add(new GrappleData(0,0,0, false));
        grappleDataList.Add(new GrappleData(0,0,0, false));
    }

    public void PassCheckpoint(int n, float time, bool finish)
    {
        if (n != nextCheckpoint) return; // enforce player to complete the race in order
        checkpoints[n].GetComponent<MeshRenderer>().enabled = false;
        checkpoints[n].GetComponent<CheckpointController>().passed = true;
        if (!finish)
        {
            nextCheckpoint = n + 1;
            checkpoints[nextCheckpoint].GetComponent<MeshRenderer>().enabled = true;
        } else // finished!!!
        {
            Debug.Log("Finished!!!!! time: " + time);
            checkpoints[n].GetComponent<ParticleSystem>().Play();
        }
        SetCheckPointServerRPC(n, time); // do this last so that the above functionality doesn't break in single player
    }
    
    public void StartRace()
    {
        GameObject.FindWithTag("RaceStartDoor").SetActive(false);
        StartCoroutine(StartRaceRoutine());
    }

    private IEnumerator StartRaceRoutine()
    {
        playerRaceController.hudController.StartCountdown();
        yield return new WaitForSeconds(3);
        playerRaceController.raceStarted = true;
    }

    private void UpdateRoadChevrons(Vector3 playerPos)
    {
        // get shortest path as a set of road nodes
        var path = RaceRouteNode.AStar(roadGraph, 
            bb.MetersToGeoCoord(new Vector2(playerPos.x, playerPos.z)),
            bb.MetersToGeoCoord(new Vector2(checkpoints[nextCheckpoint].transform.position.x, checkpoints[nextCheckpoint].transform.position.z)));
        // go from list of nodes to list of positions to draw with the line renderer
        List<Vector3> footprint = new List<Vector3>();
        for (int i = 0; i < path.Count - 1; i++)
        {
            // add footprint from node i to node i + 1
            var n1 = path[i];
            var n2 = path[i + 1];
            
            var success = roadGraph.TryGetEdge(n1, n2, out var edge);
            if (!success)
            {
                Debug.LogError("Couldn't find edge in path when there should be");
                return;
            }
            
            var worldPos = bb.ConvertGeoCoordToMeters(n1.location);
            var newPoint = new Vector3(worldPos.x, 0, worldPos.y);
            newPoint.y = (float)elevationData.SampleHeightFromPosition(newPoint) + 0.3f;
            footprint.Add(newPoint);
            
            // if n1 -> n2. Add normally
            if (edge.Source.Equals(n1))
            {
                foreach (Vector2 tagEdgePoint in edge.Tag.edgePoints)
                {
                    worldPos = bb.ConvertGeoCoordToMeters(tagEdgePoint);
                    newPoint = new Vector3(worldPos.x, 0, worldPos.y);
                    newPoint.y = (float)elevationData.SampleHeightFromPosition(newPoint) + 0.3f;
                    footprint.Add(newPoint);
                }
            }
            else // else n2 -> n1. Add in reverse direction
            {
                foreach (Vector2 tagEdgePoint in edge.Tag.edgePoints.Reverse())
                {
                    worldPos = bb.ConvertGeoCoordToMeters(tagEdgePoint);
                    newPoint = new Vector3(worldPos.x, 0, worldPos.y);
                    newPoint.y = (float)elevationData.SampleHeightFromPosition(newPoint) + 0.3f;
                    footprint.Add(newPoint);
                }
            }

            if (i + 1 == path.Count - 1) { // if next node is the final node
                worldPos = bb.ConvertGeoCoordToMeters(n2.location);
                newPoint = new Vector3(worldPos.x, 0, worldPos.y);
                newPoint.y = (float)elevationData.SampleHeightFromPosition(newPoint) + 0.3f;
                footprint.Add(newPoint);
            }
        }
        
        // update line renderer
        chevronRenderer.positionCount = footprint.Count;
        chevronRenderer.SetPositions(footprint.ToArray());
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCheckPointServerRPC(int checkpoint, float time, ServerRpcParams param = default)
    {
        ulong id = param.Receive.SenderClientId;
        int index = clientIds.IndexOf(id);
        if (index == -1)
        {
            AddPlayer(id);
            index = clientIds.IndexOf(id);
        }

        if (playerSplits[index * checkpointNumber.Value + checkpoint] < 0)
        {
            playerSplits[index * checkpointNumber.Value + checkpoint] = time;
        }
    }

    private void PrintSplits()
    {
        string res = "";
        foreach (var time in playerSplits)
        {
            res += time + "s, ";
        }
        Debug.Log(res);
    }

    [ServerRpc (RequireOwnership = false)]
    public void BeginGrappleServerRpc(Vector3 grapplePoint, SteamInputCore.Hand hand, ServerRpcParams param = default)
    {
        ulong id = param.Receive.SenderClientId;
        int index = clientIds.IndexOf(id);
        if (index == -1)
        {
            AddPlayer(id);
            index = clientIds.IndexOf(id);
        }

        if (hand == SteamInputCore.Hand.Left)
        {
            grappleDataList[2 * index] = new GrappleData(grapplePoint.x, grapplePoint.y, grapplePoint.z, true);
        }
        else if (hand == SteamInputCore.Hand.Right)
        {
            grappleDataList[2 * index + 1] = new GrappleData(grapplePoint.x, grapplePoint.y, grapplePoint.z, true);
        }
        
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndGrappleServerRpc(SteamInputCore.Hand hand, ServerRpcParams param = default)
    {
        ulong id = param.Receive.SenderClientId;
        int index = clientIds.IndexOf(id);
        if (index == -1)
        {
            AddPlayer(id);
            index = clientIds.IndexOf(id);
        }
        
        if (hand == SteamInputCore.Hand.Left)
        {
            grappleDataList[2 * index] = new GrappleData(0, 0, 0, false);
        }
        else if (hand == SteamInputCore.Hand.Right)
        {
            grappleDataList[2 * index + 1] = new GrappleData(0, 0, 0, false);
        }
    }
}
