using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class TargetSpawner : NetworkBehaviour
{
    [Header("references")]
    [SerializeReference] private GameObject targetObject;

    [Header("settings")]
    [SerializeField] private float radius;
    public float completionPercent = 0f;
    public float percentPerTarget = 0.05f;

    private Vector3 lastPos;
    
    public void StartMinigame()
    {
        if (!IsHost) throw new Exception("Should be called on host only");
        lastPos = transform.position + Vector3.forward * radius;
        SpawnTarget();
    }

    // triggered by grapple script when target is hit
    public void HitTarget(Vector3 pos)
    {
        if (!IsHost) throw new Exception("Should be called on host only");
        completionPercent += percentPerTarget;
        lastPos = pos;
        SpawnTarget();
    }

    private void SpawnTarget()
    {
        if (!IsHost) throw new Exception("Should be called on host only");
        Vector3 pos = CreateRandomPosFromCenter();
        // spawn new target
        var spawnedTarget = Instantiate(targetObject, pos, Quaternion.LookRotation(pos - transform.position));
        spawnedTarget.GetComponent<NetworkObject>().Spawn();
        spawnedTarget.transform.parent = transform;
    }

    private Vector3 CreateRandomPosFromCenter()
    {
        while (true)
        {
            Vector3 spawnCenter = transform.position;

            var a = Random.value * (2 * Mathf.PI) - Mathf.PI;
            var nextPos = new Vector3(Mathf.Cos(a) * radius, Random.value + 0.5f, Mathf.Sin(a) * radius);

            
            if (Vector3.Distance(lastPos, spawnCenter + nextPos) <= radius * 0.7f)
                return spawnCenter + nextPos;
        }
    }
}