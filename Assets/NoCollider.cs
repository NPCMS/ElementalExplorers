using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoCollider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach (Collider c in gameObject.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
