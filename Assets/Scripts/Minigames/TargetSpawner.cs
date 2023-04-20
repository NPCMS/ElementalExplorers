using UnityEngine;
using Random = UnityEngine.Random;

public class TargetSpawner : MonoBehaviour
{
    [Header("references")]
    [SerializeReference] private GameObject targetObject;

    [Header("settings")]
    [SerializeField] private float radius;
    public float completionPercent = 0f;
    public float percentPerTarget = 0.05f;

    private Vector3 lastPos;
    private void Start()
    {
        lastPos = transform.position + Vector3.forward * radius;
        SpawnTarget();
    }

    public void HitTarget(Vector3 pos)
    {
        completionPercent += percentPerTarget;
        lastPos = pos;
        SpawnTarget();
    }

    private void SpawnTarget()
    {
            Vector3 pos = CreateRandomPosFromPlayer();
            // spawn new target
            Instantiate(targetObject, pos, Quaternion.LookRotation(pos - transform.position), transform);
    }

    private Vector3 CreateRandomPosFromPlayer()
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