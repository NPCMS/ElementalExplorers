using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class TargetScript : NetworkBehaviour
{
    [SerializeField] private bool isP1;
    [SerializeField] public Animator reverseTarget;
    [SerializeField] private VisualEffect targetHitEffect;

    private bool dieAnimationPlayed;
    private bool destroyed;

    private static readonly int Reverse = Animator.StringToHash("Reverse");

    public void TriggerTarget()
    {
        Debug.Log("Destroy call");
        if (destroyed) return;
        destroyed = true;
        // notify spawner to spawn a new target
        if (isP1)
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP1ServerRpc(transform.position);
        }
        else
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP2ServerRpc(transform.position);
        }

        ExplodeAnimation();
        ExplodeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExplodeServerRpc()
    {
        ExplodeAnimationClientRpc();
    }
    
    [ClientRpc]
    private void ExplodeAnimationClientRpc()
    {
        ExplodeAnimation();
    }

    private void ExplodeAnimation()
    {
        
        if (dieAnimationPlayed) return;
        targetHitEffect.Play();
        dieAnimationPlayed = true;
        reverseTarget.SetBool(Reverse, true);
    }
}