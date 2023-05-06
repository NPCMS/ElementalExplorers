using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class RaceController : NetworkBehaviour
{
    public static RaceController Instance;
    
    public List<GameObject> minigameLocations = new();

    [SerializeReference] private AsyncPostPipelineManager manager;
    
    // these are for drawing chevrons from the player to the next checkpoint
    [SerializeReference] private LineRenderer chevronRenderer;
    private Transform player;
    
    // index of the next free checkpoint
    private readonly NetworkVariable<int> nextMinigameLocation = new ();

    public readonly NetworkVariable<int> player1Score = new ();
    public readonly NetworkVariable<int> player2Score = new ();

    // I don't need to comment what this is for
    public bool raceStarted;
    // Time spend so far in the race
    private float time;

    private bool playerReachedMinigame;
    private float firstArrivalTime;
    private HashSet<ulong> playersReadyForMinigame = new();
    private static readonly int Transparency = Shader.PropertyToID("_Transparency");

    public GameObject GetMinigameInstance()
    {
        return minigameLocations[nextMinigameLocation.Value];
    }
    
    public void Awake()
    {
        Instance = this;
        nextMinigameLocation.OnValueChanged += (oldValue, newValue) =>
        {
            // disable oldValue
            minigameLocations[oldValue].SetActive(false);
            // enable newValue
            if (newValue < 1)
            {
                StartCoroutine(SpeakerController.speakerController.PlayAudio("10 - Tree defeated"));
                minigameLocations[newValue].SetActive(true);
            }
            else
            {
                StartCoroutine(SpeakerController.speakerController.PlayAudio("11 - Mission Completed"));
                GameObject.FindWithTag("DropShipMarker").transform.Find("DropshipMarker").gameObject.SetActive(true);
            }
        };
        player1Score.OnValueChanged += (value, newValue) =>
        {
            Debug.Log("Player1 score: " + newValue);
        };
        player2Score.OnValueChanged += (value, newValue) =>
        {
            Debug.Log("Player2 score: " + newValue);
        };
    }

    public void StartRace()
    {
        if (!IsHost) throw new Exception("This should only be called from the host");
        StartRaceClientRpc();
    }

    [ClientRpc]
    private void StartRaceClientRpc()
    {
        minigameLocations = new List<GameObject>(GameObject.FindGameObjectsWithTag("Minigame"));
        player = MultiPlayerWrapper.localPlayer.GetComponentInChildren<Rigidbody>().transform;
        for (int i = 1; i < minigameLocations.Count; i++)
        {
            minigameLocations[i].SetActive(false);
        }

        StartCoroutine(StartRaceCountdown());
    }

    private IEnumerator StartRaceCountdown()
    {
        yield return new WaitForSeconds(3);
        // start countdown
        GameObject raceDoor = GameObject.FindWithTag("RaceStartDoor");

        if (raceDoor != null)
        {
            raceDoor.GetComponent<Animator>().SetBool("Open", true);
        }

        yield return new WaitForSeconds(2);

        GameObject wall = GameObject.FindWithTag("CountdownWall");
        if (wall != null)
        {
            wall.GetComponent<DoorCountdownScript>().StartCountdown();
        }

        yield return new WaitForSeconds(3.5f);
        
        // Chevrons
        InvokeRepeating(nameof(StartRepeatChevrons), 0.1f, 3);
        
        raceStarted = true;
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(StartRepeatChevrons));
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerReachedTeleporterServerRpc(ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("Teleport server rpc");

        if (MultiPlayerWrapper.localPlayer.OwnerClientId == serverRpcParams.Receive.SenderClientId) // host reached teleporter
        {
            if (!playerReachedMinigame)
            {
                player1Score.Value += 1000;
                firstArrivalTime = time;
            }
            else
            {
                var dt = time - firstArrivalTime;
                player1Score.Value += (int)(1000 - 0.2 * dt);
            }
        }
        else // client reached teleporter
        {
            if (!playerReachedMinigame)
            {
                player2Score.Value += 1000;
                firstArrivalTime = time;
            }
            else
            {
                var dt = time - firstArrivalTime;
                player2Score.Value += (int)(1000 - 0.2 * dt);
            }
        }
        
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
        var minigame = minigameLocations[nextMinigameLocation.Value];
        if (IsHost)
        {
            player.transform.position = minigame.transform.Find("Player1Pos").position;
        }
        else
        {
            player.transform.position = minigame.transform.Find("Player2Pos").position;
        }
        StartCoroutine(TeleportAgainToPreventBug());

        PlayerReadyToStartMinigameServerRpc();
    }

    private IEnumerator TeleportAgainToPreventBug()
    {
        yield return null;
        var player = MultiPlayerWrapper.localPlayer;
        player.ResetPlayerPos();
        player.GetComponentInChildren<Rigidbody>().velocity = Vector3.zero;
        var minigame = minigameLocations[nextMinigameLocation.Value];
        if (IsHost)
        {
            player.transform.position = minigame.transform.Find("Player1Pos").position;
        }
        else
        {
            player.transform.position = minigame.transform.Find("Player2Pos").position;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerReadyToStartMinigameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playersReadyForMinigame.Add(serverRpcParams.Receive.SenderClientId);
        if (playersReadyForMinigame.Count == 2)
        {
            minigameLocations[nextMinigameLocation.Value].GetComponentInChildren<TargetSpawner>().StartMinigame();
        }
    }

    public void MinigameEnded()
    {
        MinigameEndedClientRpc();
        playerReachedMinigame = false;
        playersReadyForMinigame = new HashSet<ulong>();
        nextMinigameLocation.Value += 1;
        }
    
    [ClientRpc]
    private void MinigameEndedClientRpc()
    {
        MultiPlayerWrapper.localPlayer.GetComponentInChildren<PlayerMinigameManager>().LeaveMinigame();
    }
    
    public void Update()
    {
        if (!raceStarted) return;
        time += Time.deltaTime;
    }

    private void StartRepeatChevrons()
    {
        StartCoroutine(RepeatChevrons());
    }

    private IEnumerator RepeatChevrons()
    {
        UpdateRoadChevrons(player.position);

        StartCoroutine(FadeInChevrons());

        yield return new WaitForSeconds(2.3f);

        StartCoroutine(FadeOutChevrons());
    }

    

    /*
     *
     *  BELOW IS ROAD CHEVRON MAGIC
     * 
     */

    private IEnumerator FadeInChevrons()
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float tp = Mathf.Min(1, t / 0.3f);
            chevronRenderer.material.SetFloat(Transparency, tp);
            yield return null;
        }
    }
    
    private IEnumerator FadeOutChevrons()
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float tp = 1 - Mathf.Min(1, t / 0.3f);
            chevronRenderer.material.SetFloat(Transparency, tp);
            yield return null;
        }
    }

    private void UpdateRoadChevrons(Vector3 playerPos)
    {
        Vector3 targetPos = minigameLocations[nextMinigameLocation.Value].transform.position;
        
        List<Vector3> footprint = chevronPath(playerPos, targetPos);

        // update line renderer
        chevronRenderer.positionCount = footprint.Count;
        chevronRenderer.SetPositions(footprint.ToArray());
    }

    private List<Vector3> chevronPath(Vector3 playerPos, Vector3 targetPos)
    {
        GeoCoordinate playerGeoPos = manager.elevationData.box.MetersToGeoCoord(new Vector2(playerPos.x, playerPos.z));
        GeoCoordinate targetGeoPos = manager.elevationData.box.MetersToGeoCoord(new Vector2(targetPos.x, targetPos.z));
        
        RoadNetworkNode startingNode = GetClosestRoadNode(manager.roadNetwork, playerGeoPos);

        var nodePath = Dijkstra(manager.roadNetwork, startingNode, targetGeoPos);
        
        List<Vector3> footprint = new List<Vector3>();
        for (int i = 0; i < nodePath.Count - 1; i++)
        {
            // add footprint from node i to node i + 1
            var n1 = nodePath[i];
            var n2 = nodePath[i + 1];
            
            var success = manager.roadNetwork.TryGetEdge(n1, n2, out var edge);
            if (!success)
            {
                Debug.LogError("Couldn't find edge in path when there should be");
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
    
            if (i + 1 == nodePath.Count - 1) { // if next node is the final node
                worldPos = manager.elevationData.box.ConvertGeoCoordToMeters(n2.location);
                newPoint = new Vector3(worldPos.x, 0, worldPos.y);
                newPoint.y = (float)manager.elevationData.SampleHeightFromPosition(newPoint) + 0.3f;
                footprint.Add(newPoint);
            }
        }
        
        return footprint;
    }

    private static RoadNetworkNode GetClosestRoadNode(RoadNetworkGraph roadNetwork, GeoCoordinate s)
    {
        RoadNetworkNode baseNode = default;
        float baseNodeDistance = float.MaxValue;
        foreach (RoadNetworkNode node in roadNetwork.Vertices)
        {
            float nodeDistance = (float)((node.location.x - s.Latitude) * (node.location.x - s.Latitude) +
                                         (node.location.y - s.Longitude) * (node.location.y - s.Longitude));
            if (nodeDistance < baseNodeDistance)
            {
                baseNodeDistance = nodeDistance;
                baseNode = node;
            }
        }

        return baseNode;
    }
    
    private readonly struct PriorityPair : IComparable<PriorityPair>
    {
        public readonly RoadNetworkNode node;
        private readonly float priority;
        public readonly int depth; 

        public PriorityPair(RoadNetworkNode node, float priority, int depth)
        {
            this.node = node;
            this.priority = priority;
            this.depth = depth;
        }

        public int CompareTo(PriorityPair other)
        {
            return priority.CompareTo(other.priority);
        }
    }

    private static List<RoadNetworkNode> Dijkstra(RoadNetworkGraph roadNetwork, RoadNetworkNode startNode, GeoCoordinate endLocation, int maxDepth = 20)
    {
        // dict of type: node -> prev node, distance
        var visitedNodes = new Dictionary<RoadNetworkNode, Tuple<RoadNetworkNode, float>>();
        var openNodes = new List<PriorityPair> { new (startNode, 0, maxDepth) };

        visitedNodes[startNode] = new Tuple<RoadNetworkNode, float>(null, 0);
        RoadNetworkNode closestNode = null;
        double closestDistance = double.MaxValue;
        
        while (openNodes.Count > 0)
        {
            // RoadNetworkNode nextNode = openNodes.Dequeue();
            var bestPair = openNodes.Min();
            openNodes.Remove(bestPair);
            RoadNetworkNode nextNode = bestPair.node;

            double currentDistance = GlobeBoundingBox.HaversineDistance(nextNode.location,
                new Vector2((float)endLocation.Latitude, (float)endLocation.Longitude));
            if (closestDistance > currentDistance)
            {
                closestNode = nextNode;
                closestDistance = currentDistance;
            }

            if (bestPair.depth == 0) continue;
            
            foreach (var edge in roadNetwork.AdjacentEdges(nextNode))
            {
                var neighbour = edge.Source.Equals(nextNode) ? edge.Target : edge.Source;
        
                float currentScore = visitedNodes[nextNode].Item2 + RoadEdgeWeight(edge);
                if (!(currentScore < GetDefaultDistance(visitedNodes, neighbour))) continue;
                visitedNodes[neighbour] = new Tuple<RoadNetworkNode, float>(nextNode, currentScore);
                // if (!openNodes.Contains(neighbour))
                if (!openNodes.Exists(p => p.node.Equals(neighbour)))
                {
                    // openNodes.Enqueue(neighbour, currentScore + NodeHeuristicWeight(neighbour));
                    openNodes.Add(new PriorityPair(neighbour, currentScore, bestPair.depth - 1));
                }
            }
        }
        
        List<RoadNetworkNode> path = new List<RoadNetworkNode>();
        RoadNetworkNode currentNode = closestNode;
        while (currentNode != null)
        {
            path.Insert(0, currentNode);
            var prevNode = visitedNodes[currentNode].Item1;
            currentNode = prevNode;
        }
        return path;
    }

    private static float RoadEdgeWeight(TaggedEdge<RoadNetworkNode, RoadNetworkEdge> edge)
    {
        float x = 0.5f * (edge.Source.location.x + edge.Target.location.x);
        float y = 0.5f * (edge.Source.location.y + edge.Target.location.y);
        return edge.Tag.length;
    }

    private static float GetDefaultDistance(IReadOnlyDictionary<RoadNetworkNode, Tuple<RoadNetworkNode, float>> dictionary, RoadNetworkNode key)
    {
        return dictionary.TryGetValue(key, out var val) ? val.Item2 : float.MaxValue;
    }
    
}

