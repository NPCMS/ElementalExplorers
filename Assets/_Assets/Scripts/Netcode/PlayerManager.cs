using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject player;

    public void Start()
    {
        if (!IsOwner) return;

        SceneManager.activeSceneChanged += (Scene previous, Scene current) =>
        {
            if (current.name == "GameTestScene")
            {
                SpawnPlayerServerRPC();
            }
        };
    }

    [ServerRpc]
    private void SpawnPlayerServerRPC()
    {
        GameObject spawnedPlayer = Instantiate(player);
        spawnedPlayer.GetComponent<NetworkObject>().Spawn(true);
    }
}
