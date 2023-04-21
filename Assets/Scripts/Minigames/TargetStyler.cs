using Unity.Netcode;
using UnityEngine;

public class TargetStyler : NetworkBehaviour
{
    [SerializeField] private bool isP1;

    [SerializeReference] private Material blue;
    [SerializeReference] private Material red;

    public void Start()
    {
        if (isP1)
        {
            if (IsHost)
            {
                StyleBlue();
            }
            else
            {
                StyleRed();
            }
        }
        else
        {
            if (!IsHost)
            {
                StyleBlue();
            }
            else
            {
                StyleRed();
            }
        }
    }

    private void StyleBlue()
    {
        foreach (var part in GetComponentsInChildren<MeshRenderer>())
        {
            part.material = blue;
        }
    }

    private void StyleRed()
    {
        foreach (var part in GetComponentsInChildren<MeshRenderer>())
        {
            part.material = red;
        }
    }
}
