using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerHeightController : MonoBehaviour
{

    [SerializeField] GameObject c;
    [SerializeField] GameObject handContainer;

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
                    SetHeight(position.y + 0.1f);
                    c.transform.localPosition = (gameObject.transform.localScale.y - 0.05f) * Vector3.up;
                    handContainer.transform.localPosition = new Vector3 (-position.x, -transform.localScale.y, -position.z );
                } else
                {
                    SetHeight(1.7f);
                    c.transform.localPosition = 0.8f * Vector3.up;
                    handContainer.transform.localPosition = Vector3.zero;
                }
                return;
            }
        }
    }

    private void SetHeight(float height)
    {
        gameObject.transform.localScale = new Vector3(0.6f, height * 0.5f, 0.6f);
    }
}
