using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerVRController : MonoBehaviour
{

    [SerializeField] Transform cameraObject;
    [SerializeField] Transform handContainer;
    [SerializeField] Transform playerOffset;
    [SerializeField] Transform body;

    // Update is called once per frame
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
                    handContainer.localPosition = new Vector3 (-position.x, -transform.localScale.y, -position.z);
                    playerOffset.localPosition = new Vector3 (position.x, 0f, position.z);
                } else
                {
                    SetHeight(1.7f);
                    AdjustCamera(body.transform.localScale.y);
                    handContainer.localPosition = Vector3.zero;
                    playerOffset.localPosition = Vector3.zero;
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
        cameraObject.localPosition = (height - 0.05f) * Vector3.up;
    }
}
