using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testResourceLoad : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        List<String> matPaths = BuildingAssets.materialsPaths;

        foreach (string path in matPaths)
        {
            var resource = Resources.Load<Material>(path);
            Debug.Log("Correctly Loaded: " + resource.name);
        }
    }

}
