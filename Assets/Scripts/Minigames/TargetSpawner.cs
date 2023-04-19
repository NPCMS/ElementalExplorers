using UnityEngine;
using Random = UnityEngine.Random;

public class TargetSpawner : MonoBehaviour
{
    [Header("references")] [SerializeReference]
    private Transform playerTransform;
    [SerializeReference] private GameObject targetObject;

    [Header("settings")] [SerializeField] private float spawnFrequency;
    [SerializeField] private float radius;
    public float completionPercent = 0f;
    public float percentPerTarget = 0.05f;

    private Vector3 lastPos = Vector3.zero;
    private void Start()
    {
        SpawnTarget();
    }

    public void HitTarget(Vector3 pos)
    {
        completionPercent += percentPerTarget;
        lastPos = pos;
        SpawnTarget();
        Debug.Log("Money");
    }

    private void SpawnTarget()
    {
            Vector3 pos = CreateRandomPosFromPlayer();
            // spawn new target
            Instantiate(targetObject, pos, Quaternion.LookRotation(pos - playerTransform.position), transform);
    }

    private Vector3 CreateRandomPosFromPlayer()
    {
        while (true)
        {
            Vector3 playerPos = playerTransform.position;
            Vector3 point = Random.onUnitSphere * radius;

            if (!((point.y + 0.5f) < playerPos.y || point.y > (playerPos.y + 1.5f)))
            {
                if (Vector3.Distance(lastPos, playerPos + point) < radius)
                    return playerPos + point;
            }
        }
    }
}