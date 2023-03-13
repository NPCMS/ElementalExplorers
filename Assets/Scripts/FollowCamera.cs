using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private Transform follow;
    private void Start()
    {
        foreach (var c in Camera.allCameras)
        {
            if (c.isActiveAndEnabled)
            {
                follow = c.transform;
                break;
            } 
        }
    }

    void LateUpdate()
    {
        transform.position = follow.position;
    }
}
