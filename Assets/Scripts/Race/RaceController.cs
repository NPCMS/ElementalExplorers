using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaceController : NetworkBehaviour
{
    private NetworkList<ulong> clientIds;
    private NetworkList<float> playerSplits;
    private NetworkVariable<int> checkpointNumber = new();

    private readonly List<GameObject> checkpoints = new();

    private int nextCheckpoint;
    public Dictionary<ulong, PlayerObjects> playerBodies;
    public HUDController hudController;

    public void Awake()
    {
        clientIds = new NetworkList<ulong>();
        playerSplits = new NetworkList<float>();
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
        Debug.Log("Tracking");
        hudController.TrackCheckpoint(checkpoints[nextCheckpoint].transform);
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
            body = 
        }
        
        public GameObject body;
        public GameObject[] hands;
    }
}
