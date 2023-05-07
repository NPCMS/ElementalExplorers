using Unity.Netcode;
using UnityEngine;

public class TargetStyler : NetworkBehaviour
{
    [SerializeField] private bool isP1;

    [SerializeField] private Color blue;
    [SerializeField] private Color red;
    
    [SerializeReference] private MeshRenderer flower;

    public void Start()
    {
        if (isP1)
        {
            flower.material.color = IsHost ? blue : red;
        }
        else
        {
            flower.material.color = !IsHost ? blue : red;
        }
    }
}
