using System;
using System.Collections.Generic;
using UnityEngine;

public class UIInteraction : MonoBehaviour
{

    private readonly List<Action<RaycastHit, SteamInputCore.Button>> callbacks = new();
    
    private readonly List<Action> onEnter = new(); // for hovering
    private readonly List<Action> onLeave = new();
    public void Interact(RaycastHit hit, SteamInputCore.Button button)
    {
        foreach (var callback in callbacks)
        {
            callback(hit, button);
        }
    }

    public void AddCallback(Action<RaycastHit, SteamInputCore.Button> a)
    {
        callbacks.Add(a);
    }

    public void AddOnEnterCallback(Action a)
    {
        onEnter.Add(a);
    }
    public void AddOnLeaveCallback(Action a)
    {
        onLeave.Add(a);
    }
}
