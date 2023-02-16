using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaceController : NetworkBehaviour
{
    private NetworkList<ulong> clientIds;
    private NetworkList<float> playerSplits;
    private NetworkVariable<int> checkpointNumber = new();

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

    private void ConnectCheckpoints()
    {
        foreach (var checkpoint in GameObject.FindGameObjectsWithTag("Checkpoint"))
        {
            checkpoint.GetComponent<CheckpointController>().raceController = this;
        }
        
        if (!IsHost) return;
        checkpointNumber.Value = GameObject.FindGameObjectsWithTag("Checkpoint").Length;
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
            res += time.ToString() + "s, ";
        }
        Debug.Log(res);
    }
}
