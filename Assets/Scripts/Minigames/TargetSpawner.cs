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
        SpeakerController.speakerController.PlayMinigameMusic();
        if (!IsHost) throw new Exception("Should be called on host only startminigame");
        inMinigame = true;
        var position = transform.position;
        lastPosP1 = position + Vector3.forward * radius;
        lastPosP2 = position + Vector3.back * radius;
        SpawnTargetP1();
        SpawnTargetP2();
        StartCoroutine(EndMinigame());
    }

    public IEnumerator EndMinigame()
    {
        if (!IsHost) throw new Exception("Should be called on host only endminigame");
        yield return new WaitForSeconds(5f);
        inMinigame = false;
        if (spawnedP1Target != null) spawnedP1Target.GetComponentInChildren<TargetScript>().Explode();
        if (spawnedP2Target != null) spawnedP2Target.GetComponentInChildren<TargetScript>().Explode();
        RaceController.Instance.MinigameEnded();
    }

    public void HitTargetP1(Vector3 pos, bool wasP1)
    {
        if (!IsHost) throw new Exception("Should be called on host only hittarget");
        lastPosP1 = pos;
        
        SpawnTargetP1();
        
        if (wasP1)
        {
            RaceController.Instance.player1Score.Value += 100;
        }
        else
        {
            RaceController.Instance.player2Score.Value -= 100;
        }
    }
    
    public void HitTargetP2(Vector3 pos, bool wasP2)
    {
        if (!IsHost) throw new Exception("Should be called on host only hittarget");
        lastPosP2 = pos;
        SpawnTargetP2();
        
        if (wasP2)
        {
            RaceController.Instance.player2Score.Value += 100;
        }
        else
        {
            RaceController.Instance.player1Score.Value -= 100;
        }
    }
    
    private void SpawnTargetP1()
    {
        if (!IsHost) throw new Exception("Should be called on host only spawntarget");
        if (!inMinigame) return;
        Vector3 pos = CreateRandomPosFromCenter(lastPosP1, lastPosP2);
        // spawn new target
        spawnedP1Target = Instantiate(targetObjectP1, pos, Quaternion.LookRotation(pos - transform.position));
        spawnedP1Target.GetComponent<NetworkObject>().Spawn();
    }
    
    private void SpawnTargetP2()
    {
        if (!IsHost) throw new Exception("Should be called on host only spawntarget");
        if (!inMinigame) return;
        Vector3 pos = CreateRandomPosFromCenter(lastPosP2, lastPosP1);
        // spawn new target
        spawnedP2Target = Instantiate(targetObjectP2, pos, Quaternion.LookRotation(pos - transform.position));
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