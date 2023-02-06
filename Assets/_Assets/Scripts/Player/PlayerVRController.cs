using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerVRController : MonoBehaviour
{

    [SerializeField] private Camera cameraObject;
    [SerializeField] private Transform handContainer;
    [SerializeField] private Transform body;
    private GameObject playerOffset;

    private void Start()
    {
        playerOffset = new GameObject("PlayerOffset");
        gameObject.transform.parent = playerOffset.transform;
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
                    AdjustCamera(body.transform.localScale.y);
                    handContainer.localPosition = new Vector3(-position.x, -body.transform.localScale.y, -position.z);
                    playerOffset.transform.localPosition = new Vector3(position.x, 0f, position.z);
                }
                else
                {
                    SetHeight(1.7f);
                    AdjustCamera(body.transform.localScale.y);
                    handContainer.localPosition = Vector3.zero;
                    playerOffset.transform.localPosition = Vector3.zero;
                }
                return;
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
    }
}
