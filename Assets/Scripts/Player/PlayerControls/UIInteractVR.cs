using System;
using UnityEngine;
using Valve.VR;

public class UIInteractVR : MonoBehaviour
{
    [Tooltip("The SteamVR boolean action that starts grappling")]
    [SerializeField] private SteamVR_Action_Boolean triggerPull;

    [SerializeField] private SteamVR_Input_Sources[] handControllers;
    [SerializeField] private GameObject[] handObjects = new GameObject[2];

    [SerializeField] float interactMaxDistance;

    private SteamVR_Behaviour_Pose[] handPoses;
    private LayerMask lm;
    private SteamVR_Action_Boolean.StateDownHandler[] callbacks = new SteamVR_Action_Boolean.StateDownHandler[2];

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
        handPoses = new [] { handObjects[0].GetComponent<SteamVR_Behaviour_Pose>(), handObjects[1].GetComponent<SteamVR_Behaviour_Pose>() };
        for (int i = 0; i < 2; i++)
        {
            callbacks[i] = OnTriggerPull(i);
            triggerPull[handControllers[i]].onStateDown += callbacks[i];
        }
        lm = ~((1 << gameObject.layer) | (1 << 2)); // not player layer or ignore raycast layer
    }

    private void OnDestroy()
    {
        for (int i = 0; i < 2; i++)
        {
            triggerPull[handControllers[i]].onStateDown -= callbacks[i];
        }
    }

    public SteamVR_Action_Boolean.StateDownHandler OnTriggerPull(int i)
    {
        return delegate
        {
            Ray ray = new(handPoses[i].transform.position, handPoses[i].transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactMaxDistance, lm))
            {
                return;
            }

            if (hit.transform.gameObject.layer == 5) // if ui interact
            {
                UIInteraction obj = hit.transform.gameObject.GetComponent<UIInteraction>();
                if (obj != null)
                {
                    obj.Interact();
                }
            }
        };
    }

    private void Update()
    {
        foreach (var handPose in handPoses)
        {
            Ray ray = new(handPose.transform.position, handPose.transform.forward);
            Physics.Raycast(ray, out RaycastHit hit, interactMaxDistance);
            if (hit.transform.gameObject.layer == 5) // ui
            {
                CityOnHover component = hit.rigidbody.gameObject.GetComponent<CityOnHover>();
                if(component != null)
                    component.OnHover();
            }
        }
    }
}
