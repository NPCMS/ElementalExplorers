using Unity.Netcode;
using UnityEngine;

public class TargetStyler : NetworkBehaviour
{
    [SerializeField] private bool isP1;

    [SerializeReference] private Material blue;
    [SerializeReference] private Material red;
    
    [SerializeReference] private Material blueInternal;
    [SerializeReference] private Material redInternal;

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
        transform.GetChild(0).GetComponent<MeshRenderer>().material = blue;
        foreach (var part in transform.GetChild(1).GetComponentsInChildren<MeshRenderer>())
        {
            part.materials = new[] { blueInternal, blue };
        }
    }

    private void StyleRed()
    {
        transform.GetChild(0).GetComponent<MeshRenderer>().material = red;
        foreach (var part in transform.GetChild(1).GetComponentsInChildren<MeshRenderer>())
        {
            part.materials = new[] { redInternal, red };
        }
    }
}
