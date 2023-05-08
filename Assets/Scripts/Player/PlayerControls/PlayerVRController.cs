using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR.InteractionSystem;

public class PlayerVRController : MonoBehaviour
{

    [SerializeField] private Camera cameraObject;
    [SerializeReference] private GameObject playerHead;
    [SerializeField] private Transform handContainer;
    [SerializeField] private Transform body;
    private GameObject playerOffset;

    [SerializeReference] private GameObject leftHand;
    [SerializeReference] private GameObject rightHand;

    private void Start()
    {
        playerOffset = new GameObject("PlayerOffset");
        var o = gameObject;
        playerOffset.transform.parent = o.transform.parent;
        o.transform.parent = playerOffset.transform;
        o.transform.localPosition = Vector3.zero;

        InputTracking.trackingAcquired += AcquiredTracking;
        InputTracking.trackingLost += LostTracking;
        
        List<XRNodeState> nodeStates = new();
        InputTracking.GetNodeStates(nodeStates);

        SetHandState(XRNode.LeftHand, nodeStates.Exists(s => s.nodeType == XRNode.LeftHand));
        SetHandState(XRNode.RightHand, nodeStates.Exists(s => s.nodeType == XRNode.RightHand));
    }

    private void OnDestroy()
    {
        InputTracking.trackingAcquired -= AcquiredTracking;
        InputTracking.trackingLost -= LostTracking;
    }

    void Update()   
    {
        List<XRNodeState> nodeStates = new();
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.Head || nodeState.nodeType == XRNode.CenterEye)
            {
                if (nodeState.TryGetPosition(out Vector3 position))
                {
                    SetHeight(position.y);
                    var localScale = body.transform.localScale;
                    AdjustCamera(localScale.y);
                    handContainer.localPosition = new Vector3(-position.x, -localScale.y, -position.z);
                    playerOffset.transform.localPosition = new Vector3(position.x, 0f, position.z);
                }
                else
                {
                    SetHeight(1.7f);
                    AdjustCamera(body.transform.localScale.y);
                    handContainer.localPosition = Vector3.zero;
                    playerOffset.transform.localPosition = Vector3.zero;
                }
            }
        } 
    }

    private void SetHeight(float height)
    {
        body.transform.localScale = new Vector3(0.6f, (height + 0.1f) * 0.5f, 0.6f);
    }

    private void AdjustCamera(float height)
    {
        cameraObject.transform.localPosition = (height - 0.05f) * Vector3.up;
        if (playerHead != null) playerHead.transform.localPosition = (height - 0.05f) * Vector3.up;
    }

    private void LostTracking(XRNodeState state)
    {
        SetHandState(state.nodeType, false);
    }
    
    private void AcquiredTracking(XRNodeState state)
    {
        SetHandState(state.nodeType, true);
    }

    private void SetHandState(XRNode hand, bool active)
    {
        Debug.Log("Changed input: " + hand + " to state: " + active);
        switch (hand)
        {
            case XRNode.LeftHand:
                leftHand.GetComponentsInChildren<MeshRenderer>().ForEach(r => r.enabled = active);
                leftHand.GetComponentsInChildren<Collider>().ForEach(c => c.enabled = active);
                leftHand.GetComponentsInChildren<LineRenderer>().ForEach(lr => lr.enabled = active);
                leftHand.GetComponentsInChildren<LaserPointer>().ForEach(lp => lp.enabled = active);
                break;
            case XRNode.RightHand:
                rightHand.GetComponentsInChildren<MeshRenderer>().ForEach(r => r.enabled = active);
                rightHand.GetComponentsInChildren<Collider>().ForEach(c => c.enabled = active);
                rightHand.GetComponentsInChildren<LineRenderer>().ForEach(lr => lr.enabled = active);
                rightHand.GetComponentsInChildren<LaserPointer>().ForEach(lp => lp.enabled = active);
                break;
        }
    }
}
