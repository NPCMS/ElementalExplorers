using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class UIInteractVR : MonoBehaviour
{
    [Tooltip("The SteamVR boolean action that starts grappling")]
    [SerializeField] private SteamVR_Action_Boolean triggerPull;

    [SerializeField] private SteamVR_Input_Sources[] handControllers;
    [SerializeField] private GameObject[] handObjects = new GameObject[2];

    [SerializeField] float grappleMaxDistance;

    private SteamVR_Behaviour_Pose[] handPoses;
    private LayerMask lm;

    private void Start()
    {
        if (triggerPull == null)
        {
            Debug.LogError("[SteamVR] Boolean action not set.", this);
            return;
        }
        if (handControllers.Length != 2)
        {
            Debug.LogError("[SteamVR] hands not added", this);
        }
        handPoses = new SteamVR_Behaviour_Pose[2] { handObjects[0].GetComponent<SteamVR_Behaviour_Pose>(), handObjects[1].GetComponent<SteamVR_Behaviour_Pose>() };
        for (int i = 0; i < 2; i++)
        {
            triggerPull[handControllers[i]].onStateDown += OnTriggerPull(i);
        }
        lm = ~gameObject.layer; // not player layer
    }

    public SteamVR_Action_Boolean.StateDownHandler OnTriggerPull(int i)
    {
        return delegate (SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            Ray ray = new(handPoses[i].transform.position, handPoses[i].transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, grappleMaxDistance, lm))
            {
                return;
            }

            if (hit.transform.gameObject.layer == 5) // if ui interact
            {
                hit.transform.gameObject.GetComponent<UIInteraction>().Interact();
            }
        };
    }
}
