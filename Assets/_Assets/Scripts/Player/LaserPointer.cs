using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointer : MonoBehaviour
{
    public GameObject pointer;
    private LineRenderer lr;
    private MeshRenderer pointerRenderer;
    [SerializeField] float maxPointerDistance;

    private void Start()
    {
        lr = pointer.GetComponent<LineRenderer>();
        pointerRenderer = pointer.GetComponent<MeshRenderer>();
        if (!lr)
        {
            Debug.LogError("No line renderer on pointer when one was expected");
        }
    }

    float getScale(float distance)
    {
        return (1 + (distance * distance) / 3000) * 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        lr.SetPositions(new Vector3[2] { gameObject.transform.position, gameObject.transform.position + gameObject.transform.forward * maxPointerDistance });

        Ray ray = new(gameObject.transform.position, gameObject.transform.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, maxPointerDistance))
        {
            if (!Physics.SphereCast(ray, 1f, out hit, maxPointerDistance))
            {
                pointerRenderer.enabled = false;
                pointer.transform.localPosition = maxPointerDistance * 0.5f * Vector3.forward;
                return;
            }
        }

        pointerRenderer.enabled = true;
        pointer.transform.position = hit.point;
        pointer.transform.localScale = getScale(hit.distance) * Vector3.one;
    }
}
