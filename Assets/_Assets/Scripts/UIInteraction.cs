using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInteraction : MonoBehaviour
{

    private readonly List<Action> callbacks = new();

    public void Interact()
    {
        foreach (var callback in callbacks)
        {
            callback();
        }
    }

    public void AddCallback(Action a)
    {
        callbacks.Add(a);
    }
}
