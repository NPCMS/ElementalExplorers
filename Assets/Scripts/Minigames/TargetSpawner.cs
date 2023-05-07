using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class TargetSpawner : NetworkBehaviour
{
    [Header("references")]
    [SerializeReference] private GameObject targetObjectP1;
    [SerializeReference] private GameObject targetObjectP2;

    [Header("settings")]
    [SerializeField] private float radius;

    private Vector3 lastPosP1;
    private Vector3 lastPosP2;
    private GameObject spawnedP1Target;
    private GameObject spawnedP2Target;

    private bool inMinigame;

    public void Start()
    {
        if (radius <= 2f) throw new Exception("Radius is low and will probably cause a crash");
    }
    
    public void StartMinigame()
    {
        if (!IsHost) throw new Exception("Should be called on host only startminigame");
        inMinigame = true;
        StartCoroutine(StartMiniGameDelayed());
    }

    private IEnumerator StartMiniGameDelayed()
    {
        StartMinigameMusicClientRpc();
        yield return new WaitForSeconds(7f);
        var position = transform.position;
        lastPosP1 = position + Vector3.forward * radius - Vector3.up * 18;
        lastPosP2 = position + Vector3.back * radius - Vector3.up * 18;
        SpawnTargetP1();
        SpawnTargetP2();
        StartCoroutine(EndMinigame());
    }

    [ClientRpc]
    private void StartMinigameMusicClientRpc()
    {
        SpeakerController.speakerController.PlayMinigameMusic();
    } 

    public IEnumerator EndMinigame()
    {
        if (!IsHost) throw new Exception("Should be called on host only endminigame");
        yield return new WaitForSeconds(30f);
        inMinigame = false;
        if (spawnedP1Target != null) spawnedP1Target.GetComponent<NetworkObject>().Despawn();
        if (spawnedP2Target != null) spawnedP2Target.GetComponent<NetworkObject>().Despawn();
        RaceController.Instance.MinigameEnded();
    }

    [ServerRpc(RequireOwnership = false)]
    public void HitTargetP1ServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        if (!MultiPlayerWrapper.isGameHost) Debug.LogException(new Exception("Should be called on host only hittarget"));
        lastPosP1 = pos;
        var lastTarget = spawnedP1Target;
        SpawnTargetP1();

        bool wasP1 = MultiPlayerWrapper.localPlayer.OwnerClientId == rpcParams.Receive.SenderClientId;
        if (wasP1)
        {
            RaceController.Instance.player1Score.Value += 100;
        }
        else
        {
            RaceController.Instance.player2Score.Value -= 100;
        }

        StartCoroutine(DespawnPlant(lastTarget));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void HitTargetP2ServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        if (!MultiPlayerWrapper.isGameHost) Debug.LogException(new Exception("Should be called on host only hittarget"));
        lastPosP2 = pos;
        var lastTarget = spawnedP2Target;
        SpawnTargetP2();

        bool wasP2 = MultiPlayerWrapper.localPlayer.OwnerClientId != rpcParams.Receive.SenderClientId;
        if (wasP2)
        {
            RaceController.Instance.player2Score.Value += 100;
        }
        else
        {
            RaceController.Instance.player1Score.Value -= 100;
        }
        
        StartCoroutine(DespawnPlant(lastTarget));
    }

    private IEnumerator DespawnPlant(GameObject plant)
    {
        yield return new WaitForSeconds(0.5f);
        plant.GetComponent<NetworkObject>().Despawn();
    }
    
    private void SpawnTargetP1()
    {
        if (!IsHost) throw new Exception("Should be called on host only spawntarget");
        if (!inMinigame) return;
        Vector3 pos = CreateRandomPosFromCenter(lastPosP1 +  Vector3.up * 18, lastPosP2);
        // spawn new target
        spawnedP1Target = Instantiate(targetObjectP1, pos - Vector3.up * 18, Quaternion.LookRotation(pos - transform.position));
        spawnedP1Target.transform.Rotate(Vector3.up, 90);
        spawnedP1Target.GetComponent<NetworkObject>().Spawn();
    }
    
    private void SpawnTargetP2()
    {
        if (!IsHost) throw new Exception("Should be called on host only spawntarget");
        if (!inMinigame) return;
        Vector3 pos = CreateRandomPosFromCenter(lastPosP2 + Vector3.up * 18, lastPosP1);
        // spawn new target
        spawnedP2Target = Instantiate(targetObjectP2, pos - Vector3.up * 18, Quaternion.LookRotation(pos - transform.position));
        spawnedP2Target.transform.Rotate(Vector3.up, 90);
        spawnedP2Target.GetComponent<NetworkObject>().Spawn();
    }

    private Vector3 CreateRandomPosFromCenter(Vector3 lastPos, Vector3 avoidPos)
    {
        while (true)
        {
            Vector3 spawnCenter = transform.position;

            var a = Random.value * (2 * Mathf.PI) - Mathf.PI;
            var nextPos = new Vector3(Mathf.Cos(a) * radius, Random.Range(0.5f, 2.5f), Mathf.Sin(a) * radius);

            
            if (Vector3.Distance(lastPos, spawnCenter + nextPos) <= radius * 0.7f && Vector3.Distance(avoidPos, spawnCenter + nextPos) >= 2f)
                return spawnCenter + nextPos;
        }
    }

}