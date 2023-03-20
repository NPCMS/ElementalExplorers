using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class AimingRange : MonoBehaviour
{
    [SerializeField] private Vector3 boundOne;
    [SerializeField] private Vector3 boundTwo;
    [SerializeField] private float spawnTimer;
    [SerializeReference] private GameObject targetObject;

    private void Start()
    {
        // begin spawner
        StartCoroutine(nameof(SpawnTargets));
    }

    IEnumerable SpawnTargets()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnTimer);
            
            float randX = Random.Range(boundOne.x, boundTwo.x);
            float randY = Random.Range(boundOne.y, boundTwo.y);
            float randZ = Random.Range(boundOne.z, boundTwo.z);

            Instantiate(targetObject,new Vector3(randX, randY, randZ), quaternion.identity);
        }
    }
}
