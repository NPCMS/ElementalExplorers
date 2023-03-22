using System;
using System.Collections.Generic;
using UnityEngine;

public class UIInteraction : MonoBehaviour
{

    private readonly List<Action> callbacks = new();
    private readonly List<Action> onEnter = new();
    private readonly List<Action> onLeave = new();
    public void Interact()
    {
        foreach (var callback in callbacks)
        {
            callback();
        }
    }

    public void HoverStart()
    {
        foreach (var callback in onEnter)
        {
            callback();
        }
    }

    public void HoverEnd()
    {
        foreach (var callback in onLeave)
        {
            callback();
        }
    }

    public void AddCallback(Action a)
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

    public void RemoveCallback()
    {
        
    }
}
