using Unity.Netcode;
using UnityEngine;

public class TargetStyler : NetworkBehaviour
{
    [SerializeField] private bool isP1;

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
        
    }

    private void StyleRed()
    {
        
    }
}
