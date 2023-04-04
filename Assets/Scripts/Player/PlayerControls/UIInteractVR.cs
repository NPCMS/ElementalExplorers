using System;
using UnityEngine;
using Valve.VR;

public class UIInteractVR : MonoBehaviour
{
    [Tooltip("The SteamVR boolean action that starts grappling")]
    [SerializeField] private SteamVR_Action_Boolean triggerPull;
    [SerializeField] private SteamVR_Action_Boolean aPress;
    
    [SerializeField] private SteamVR_Input_Sources[] handControllers;
    [SerializeField] private GameObject[] handObjects = new GameObject[2];

    [SerializeField] float interactMaxDistance;

    private SteamVR_Behaviour_Pose[] handPoses;
    private LayerMask lm;
    private SteamVR_Action_Boolean.StateDownHandler[] triggerCallbacks = new SteamVR_Action_Boolean.StateDownHandler[2];
    private SteamVR_Action_Boolean.StateDownHandler[] aButtonCallbacks = new SteamVR_Action_Boolean.StateDownHandler[2];
    
    private UIInteraction[] previousHover = new UIInteraction[2];
    
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
            triggerCallbacks[i] = OnTriggerPull(i, SteamInputCore.Button.Trigger);
            aButtonCallbacks[i] = OnTriggerPull(i, SteamInputCore.Button.A);
            
            triggerPull[handControllers[i]].onStateDown += triggerCallbacks[i];
            aPress[handControllers[i]].onStateDown += aButtonCallbacks[i];
            
            
            
        }
        lm = ~((1 << gameObject.layer) | (1 << 2)); // not player layer or ignore raycast layer
    }

    private void OnDestroy()
    {
        for (int i = 0; i < 2; i++)
        {
            triggerPull[handControllers[i]].onStateDown -= triggerCallbacks[i];
        }
    }

    public SteamVR_Action_Boolean.StateDownHandler OnTriggerPull(int i, SteamInputCore.Button button)
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
                    obj.Interact(hit, button);
                }
            }
        };
    }
    
    

    private void Update()
    {
        for (int i = 0; i < 2; i++)
        {
            Ray ray = new(handPoses[i].transform.position, handPoses[i].transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactMaxDistance, lm))
                continue;

            if (hit.transform.gameObject.layer == 5) // ui
            {
                if (hit.transform.gameObject.TryGetComponent(out UIInteraction obj))
                {
                    if (obj != previousHover[i])
                    {
                        if (previousHover[i] != null && previousHover[0] != previousHover[1])
                            previousHover[i].HoverEnd();

                        
                        obj.HoverStart();
                        previousHover[i] = obj;
                        
                    }
                }
                else if (previousHover[i] != null && previousHover[0] != previousHover[1]) // if you move to the backplate it should stop the glow
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (previousHover[j] != null)
                        {
                            previousHover[j].HoverEnd();
                            previousHover[j] = null;
                        }
                    }

                }

            }


        }

    }
}
