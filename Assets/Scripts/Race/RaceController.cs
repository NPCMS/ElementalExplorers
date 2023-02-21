using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaceController : NetworkBehaviour
{
    private NetworkList<ulong> clientIds;
    private NetworkList<float> playerSplits;
    private NetworkVariable<int> checkpointNumber = new();

    public List<GameObject> checkpoints = new();

    private int nextCheckpoint;
    public Dictionary<ulong, PlayerObjects> playerBodies = new();
    public HUDController hudController;
    
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
            checkpoints[n + 1].GetComponent<MeshRenderer>().enabled = true;
            nextCheckpoint = n + 1;
            TrackCheckpoint();
        } else // finished!!!
        {
            Debug.Log("Finished!!!!!");
            hudController.UnTrackCheckpoint();
            checkpoints[n].GetComponent<ParticleSystem>().Play();
        }
        SetCheckPointServerRPC(n, time); // do this last so that the above functionality doesn't break in single player
    }

    public void TrackCheckpoint()
    {
        // Debug.Log("Tracking");
        // hudController.TrackCheckpoint(checkpoints[nextCheckpoint].transform);
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
    
    public class PlayerObjects
    {

        public PlayerObjects(GameObject multiplayerWrapper)
        {
            var allChildObjects = multiplayerWrapper.GetComponentsInChildren<Transform>();
            body = allChildObjects.First(c => c.gameObject.name == "Body").gameObject;
            hands[0] = allChildObjects.First(c => c.gameObject.name == "LeftHand").gameObject;
            hands[1] = allChildObjects.First(c => c.gameObject.name == "RightHand").gameObject;
        }
        
        public readonly GameObject body;
        public readonly GameObject[] hands = new GameObject[2];
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
