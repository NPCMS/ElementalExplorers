using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class AimingRange : MonoBehaviour
{
    [SerializeReference] private Transform boundOne;
    [SerializeReference] private Transform boundTwo;
    [SerializeField] private float spawnTimer;
    [SerializeReference] private GameObject targetObject;

    private void Start()
    {
        // begin spawner
        StartCoroutine(SpawnTargets());
    }

    IEnumerator SpawnTargets()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnTimer);
            var position = boundOne.position;
            var position1 = boundTwo.position;
            
            float randX = Random.Range(position.x, position1.x);
            float randY = Random.Range(position.y, position1.y);
            float randZ = Random.Range(position.z, position1.z);

            var temp = Instantiate(targetObject,new Vector3(randX, randY, randZ), quaternion.identity);
            temp.transform.localScale = new Vector3(2f, 2f, 0.2f);
        }
    }
}
