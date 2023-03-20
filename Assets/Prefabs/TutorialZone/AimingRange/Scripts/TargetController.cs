using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetController : MonoBehaviour
{
    void Start()
    {
        Invoke(nameof(DestroyTarget), 2.0f);
    }


    void DestroyTarget()
    {
        Destroy(this.gameObject);
    }
}
