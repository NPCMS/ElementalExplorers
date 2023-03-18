using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private Transform follow;
    void LateUpdate()
    {
        if (follow == null)
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
        transform.position = follow.position;
    }
}
